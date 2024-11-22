using NitroHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Decoder = PleOps.XdeltaSharp.Decoder.Decoder;

namespace NitroPatcher;

public enum PatchReturnValue
{
  SUCCESS,
  MD5_MISMATCH,
}

public struct PatchResult
{
  public PatchReturnValue returnValue;
  public Stream stream;
  public string inputMd5;
  public string outputMd5;
}

public class PatchHelper
{
  public static PatchResult PatchIt(string originalPath, string patchPath)
  {
    if (!File.Exists(originalPath)) throw new Exception($"文件不存在：{originalPath}");
    if (!File.Exists(patchPath)) throw new Exception($"文件不存在：{patchPath}");

    var md5Matched = true;

    // 解压补丁包
    var newStreams = new Dictionary<string, MemoryStream>();
    using (var archiveStream = File.OpenRead(patchPath))
    {
      using var archive = new ZipArchive(archiveStream);
      foreach (ZipArchiveEntry file in archive.Entries)
      {
        if (file.Name == "")
        {
          continue;
        }
        var stream = new MemoryStream();
        using var zipStream = file.Open();
        zipStream.CopyTo(stream);
        newStreams[file.FullName.Replace('\\', '/')] = stream;
        stream.Position = 0;
        zipStream.Close();
      }
    }

    var ndsFile = new NDSFile(originalPath);
    using var ndsFileStream = File.OpenRead(originalPath);
    var inputMd5 = string.Join("", MD5.Create().ComputeHash(ndsFileStream).Select(_ => $"{_:x2}"));
    if (newStreams.TryGetValue("md5.txt", out var md5Stream))
    {
      var originalMd5List = Encoding.UTF8.GetString(md5Stream.ToArray()).Split('\n').Select(_ => _.Trim());
      md5Matched = false;
      foreach (var md5 in originalMd5List)
      {
        if (md5.StartsWith("#") || string.IsNullOrEmpty(md5)) continue;
        if (md5.Length != 32)
        {
          throw new Exception("md5.txt 格式错误，可能是因为补丁包已损坏。");
        }
        if (string.Compare(inputMd5, md5, true) == 0)
        {
          md5Matched = true;
          break;
        }
      }
    }
    ReplaceFile(ndsFile.root, newStreams, new BinaryReader(ndsFileStream));
    var outputStream = new MemoryStream();
    ndsFile.SaveAs(outputStream);
    outputStream.Position = 0;
    var outputMd5 = string.Join("", MD5.Create().ComputeHash(outputStream).Select(_ => $"{_:x2}"));
    outputStream.Position = 0;

    return new PatchResult
    {
      returnValue = md5Matched ? PatchReturnValue.SUCCESS : PatchReturnValue.MD5_MISMATCH,
      stream = outputStream,
      inputMd5 = inputMd5,
      outputMd5 = outputMd5,
    };
  }

  static void ReplaceFile(sFolder sFolder, Dictionary<string, MemoryStream> newStreams, BinaryReader ndsFileReader, string path = "")
  {
    if (sFolder.files != null)
    {
      foreach (var file in sFolder.files)
      {
        string xdeltaPath = Path.Combine(["xdelta", path, file.name]).Replace('\\', '/');
        string replacedPath = Path.Combine([path, file.name]).Replace('\\', '/');
        if (newStreams.TryGetValue(xdeltaPath, out var patchStream) && string.IsNullOrEmpty(file.path))
        {
          ndsFileReader.BaseStream.Position = file.offset;
          var inputStream = new MemoryStream(ndsFileReader.ReadBytes((int)file.size));
          var outputStream = new MemoryStream();
          using var decoder = new Decoder(inputStream, patchStream, outputStream);
          decoder.Run();
          newStreams[replacedPath] = outputStream;

          newStreams.Remove(xdeltaPath);
          patchStream.Close();
          patchStream.Dispose();
        }
        if (!newStreams.TryGetValue(replacedPath, out var replaceStream)) { continue; }
        file.size = (uint)replaceStream.Length;
        file.replaceStream = replaceStream;
        file.offset = 0;
      }
    }
    if (sFolder.folders != null)
    {
      foreach (var folder in sFolder.folders)
      {
        ReplaceFile(folder, newStreams, ndsFileReader, Path.Combine([path, folder.name]).Replace('\\', '/'));
      }
    }
  }
}
