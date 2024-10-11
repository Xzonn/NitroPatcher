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
  internal class PatchHelper
  {
    public static bool PatchIt(string originalPath, string patchPath, string outputPath)
    {
      if (!CheckIfFileExists(originalPath)) return false;
      if (!CheckIfFileExists(patchPath)) return false;

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
          var originalMd5 = File.ReadAllText(Path.Combine(tempPath, "md5.txt")).Trim();
          if (originalMd5.Length != 32)
          {
            MessageBox.Show("错误：md5.txt 格式错误，可能是因为补丁包已损坏。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
          }
          var calculatedMd5 = string.Join("", MD5.Create().ComputeHash(ndsFileStream).Select(_ => $"{_:x2}"));
          if (string.Compare(calculatedMd5, originalMd5, true) != 0) { md5Matched = false; }
        }
        ReplaceFile(ndsFile.root, tempPath, new BinaryReader(ndsFileStream));
        ndsFileStream.Close();
        ndsFile.SaveAs(Path.Combine(tempPath, "temp.nds"));
        File.Copy(Path.Combine(tempPath, "temp.nds"), outputPath, true);
      }
      catch (Exception e)
      {
        MessageBox.Show($"错误：{e.Message}\n{e.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        Directory.Delete(tempPath, true);
        return false;
      }
      // 删除临时文件夹
      Directory.Delete(tempPath, true);
      if (md5Matched)
      {
        MessageBox.Show("已完成。", "完成");
      }
      else
      {
        MessageBox.Show("已完成，但是原始 ROM 的 MD5 校验失败，可能是因为使用了错误的原始 ROM。", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
      }
      return true;
    }

    public static bool CheckIfFileExists(string filePath)
    {
      bool exists = File.Exists(filePath);
      if (!exists)
      {
        MessageBox.Show($"文件不存在：{filePath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
      }
      return exists;
    }

    static void ReplaceFile(sFolder sFolder, string inputFolder, BinaryReader ndsFileReader, string path = "")
    {
      if (sFolder.files != null)
      {
        foreach (var file in sFolder.files)
        {
          string xdeltaPath = Path.Combine(new string[] { inputFolder, "xdelta", path, file.name });
          string replacedPath = Path.Combine(new string[] { inputFolder, path, file.name });
          if (File.Exists(xdeltaPath) && string.IsNullOrEmpty(file.path)) {
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
