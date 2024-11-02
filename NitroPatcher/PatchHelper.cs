using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using NitroHelper;
using PleOps.XdeltaSharp.Decoder;

namespace NitroPatcher
{
  enum PatchResult
  {
    SUCCESS,
    MD5_MISMATCH,
  }

  internal class PatchHelper
  {
    public static PatchResult PatchIt(string originalPath, string patchPath, string outputPath)
    {
      if (!File.Exists(originalPath)) throw new Exception($"文件不存在：{originalPath}");
      if (!File.Exists(patchPath)) throw new Exception($"文件不存在：{patchPath}");

      var md5Matched = true;

      // 创建临时文件夹
      string tempPath = "";
      while (string.IsNullOrEmpty(tempPath) || Directory.Exists(tempPath) || File.Exists(tempPath))
      {
        tempPath = Path.Combine(Path.GetTempPath(), $"xz_nitropatcher_{DateTime.Now.GetHashCode():X8}");
      }
      try
      {
        DirectoryInfo di = Directory.CreateDirectory(tempPath);
        di.Attributes |= FileAttributes.Hidden;

        // 解压补丁包
        FileStream archiveStream = File.OpenRead(patchPath);
        ZipArchive archive = new ZipArchive(archiveStream);
        foreach (ZipArchiveEntry file in archive.Entries)
        {
          string completeFileName = Path.GetFullPath(Path.Combine(tempPath, file.FullName));
          if (file.Name == "")
          {
            Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
            continue;
          }
          else
          {
            if (!Directory.Exists(Path.GetDirectoryName(completeFileName)))
            {
              Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
            }
            file.ExtractToFile(completeFileName, true);
          }
        }
        archiveStream.Close();

        var ndsFile = new NDSFile(originalPath);
        var ndsFileStream = File.OpenRead(originalPath);
        if (File.Exists(Path.Combine(tempPath, "md5.txt")))
        {
          var originalMd5List = File.ReadAllLines(Path.Combine(tempPath, "md5.txt")).Select(_ => _.Trim());
          var calculatedMd5 = string.Join("", MD5.Create().ComputeHash(ndsFileStream).Select(_ => $"{_:x2}"));
          md5Matched = false;
          foreach (var md5 in originalMd5List)
          {
            if (md5.Length != 32)
            {
              throw new Exception("md5.txt 格式错误，可能是因为补丁包已损坏。");
            }
            if (string.Compare(calculatedMd5, md5, true) == 0)
            {
              md5Matched = true;
              break;
            }
          }
        }
        ReplaceFile(ndsFile.root, tempPath, new BinaryReader(ndsFileStream));
        ndsFileStream.Close();
        ndsFile.SaveAs(Path.Combine(tempPath, "temp.nds"));
        File.Copy(Path.Combine(tempPath, "temp.nds"), outputPath, true);
      }
      catch (Exception e)
      {
        Directory.Delete(tempPath, true);
        throw e;
      }
      // 删除临时文件夹
      Directory.Delete(tempPath, true);

      return md5Matched ? PatchResult.SUCCESS : PatchResult.MD5_MISMATCH;
    }

    static void ReplaceFile(sFolder sFolder, string inputFolder, BinaryReader ndsFileReader, string path = "")
    {
      if (sFolder.files != null)
      {
        foreach (var file in sFolder.files)
        {
          string xdeltaPath = Path.Combine(new string[] { inputFolder, "xdelta", path, file.name });
          string replacedPath = Path.Combine(new string[] { inputFolder, path, file.name });
          if (File.Exists(xdeltaPath) && string.IsNullOrEmpty(file.path))
          {
            ndsFileReader.BaseStream.Position = file.offset;
            var inputStream = new MemoryStream(ndsFileReader.ReadBytes((int)file.size));
            var patchStream = File.OpenRead(xdeltaPath);
            Directory.CreateDirectory(Path.GetDirectoryName(replacedPath));
            var outputStream = File.Create(replacedPath);
            var decoder = new Decoder(inputStream, patchStream, outputStream);
            decoder.Run();
            patchStream.Close();
            outputStream.Close();
            decoder.Dispose();
          }
          if (!File.Exists(replacedPath)) { continue; }
          file.size = (uint)new FileInfo(replacedPath).Length;
          file.path = replacedPath;
          file.offset = 0;
        }
      }
      if (sFolder.folders != null)
      {
        foreach (var folder in sFolder.folders)
        {
          ReplaceFile(folder, inputFolder, ndsFileReader, Path.Combine(path, folder.name));
        }
      }
    }
  }
}
