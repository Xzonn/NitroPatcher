using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using NitroHelper;
using NitroPatcher;
using Xunit;

namespace NitroPatchHelper.Tests;

public sealed class PatchHelperTests : IDisposable
{
  const int RomLength = 0x20000;
  const int FileOffset = 0x5200;

  readonly string tempDirectory = Path.Combine(Path.GetTempPath(), $"NitroPatcherTests-{Guid.NewGuid():N}");

  public PatchHelperTests()
  {
    Directory.CreateDirectory(tempDirectory);
  }

  [Fact]
  public void PatchItReplacesAFileAndProducesDeterministicOutput()
  {
    var romPath = WriteFile("input.nds", CreateRom("TEST ROM", "OLD!"u8.ToArray()));
    var patchPath = CreatePatch("replace.zip", ("data/file.bin", "NEW!"u8.ToArray()));

    using var first = PatchHelper.PatchIt(romPath, patchPath).stream;
    using var second = PatchHelper.PatchIt(romPath, patchPath).stream;

    Assert.Equal(ReadDataFile(first), ReadDataFile(second));
    Assert.Equal("NEW!"u8.ToArray(), ReadDataFile(first));
    Assert.Equal(ComputeMd5(first), ComputeMd5(second));
    Assert.Equal("545a5f6af605e892eedb48010df7ef7b", ComputeMd5(first));
  }

  [Fact]
  public void PatchItAcceptsOneOfMultipleMd5Values()
  {
    var rom = CreateRom("TEST ROM", "OLD!"u8.ToArray());
    var romPath = WriteFile("input.nds", rom);
    var md5 = Convert.ToHexString(MD5.HashData(rom)).ToLowerInvariant();
    var patchPath = CreatePatch(
      "multiple-md5.zip",
      ("md5.txt", Encoding.UTF8.GetBytes($"# supported inputs\n00000000000000000000000000000000\n{md5}\n")),
      ("data/file.bin", "NEW!"u8.ToArray()));

    var result = PatchHelper.PatchIt(romPath, patchPath);
    using (result.stream)
    {
      Assert.Equal(PatchReturnValue.SUCCESS, result.returnValue);
      Assert.Equal(md5, result.inputMd5);
    }
  }

  [Fact]
  public void PatchItReportsMd5Mismatch()
  {
    var romPath = WriteFile("input.nds", CreateRom("TEST ROM", "OLD!"u8.ToArray()));
    var patchPath = CreatePatch(
      "mismatch.zip",
      ("md5.txt", Encoding.UTF8.GetBytes("00000000000000000000000000000000\n")));

    var result = PatchHelper.PatchIt(romPath, patchPath);
    using (result.stream)
    {
      Assert.Equal(PatchReturnValue.MD5_MISMATCH, result.returnValue);
    }
  }

  [Fact]
  public void PatchItRejectsMalformedMd5File()
  {
    var romPath = WriteFile("input.nds", CreateRom("TEST ROM", "OLD!"u8.ToArray()));
    var patchPath = CreatePatch("invalid-md5.zip", ("md5.txt", "invalid"u8.ToArray()));

    var exception = Assert.Throws<Exception>(() => PatchHelper.PatchIt(romPath, patchPath));

    Assert.Contains("md5.txt 格式错误", exception.Message);
  }

  [Fact]
  public void PatchItAppliesXdeltaToAnEmbeddedFile()
  {
    var romPath = WriteFile("input.nds", CreateRom("TEST ROM", "OLD!"u8.ToArray()));
    var patchPath = CreatePatch("xdelta.zip", ("xdelta/data/file.bin", CreateAddPatch("NEW!"u8.ToArray())));

    using var output = PatchHelper.PatchIt(romPath, patchPath).stream;

    Assert.Equal("NEW!"u8.ToArray(), ReadDataFile(output));
  }

  [Fact]
  public void PatchItAppliesPreprocessingXdeltaBeforeReadingTheRom()
  {
    var originalRom = CreateRom("ORIGINAL ROM", "OLD!"u8.ToArray());
    var preprocessedRom = CreateRom("PREPROCESSED", "PRE!"u8.ToArray());
    var romPath = WriteFile("input.nds", originalRom);
    var inputMd5 = Convert.ToHexString(MD5.HashData(originalRom)).ToLowerInvariant();
    var patchPath = CreatePatch(
      "preprocess.zip",
      ($"preprocessing/{inputMd5}.xdelta", CreateAddPatch(preprocessedRom)));

    using var output = PatchHelper.PatchIt(romPath, patchPath).stream;

    Assert.Equal("PRE!"u8.ToArray(), ReadDataFile(output));
  }

  [Fact]
  public void PatchItRejectsMissingAndCorruptInputs()
  {
    var romPath = WriteFile("input.nds", CreateRom("TEST ROM", "OLD!"u8.ToArray()));
    var corruptPatchPath = WriteFile("corrupt.zip", "not a zip"u8.ToArray());

    Assert.Throws<Exception>(() => PatchHelper.PatchIt(Path.Combine(tempDirectory, "missing.nds"), corruptPatchPath));
    Assert.Throws<InvalidDataException>(() => PatchHelper.PatchIt(romPath, corruptPatchPath));
  }

  public void Dispose()
  {
    Directory.Delete(tempDirectory, true);
  }

  string WriteFile(string name, byte[] data)
  {
    var path = Path.Combine(tempDirectory, name);
    File.WriteAllBytes(path, data);
    return path;
  }

  string CreatePatch(string name, params (string Path, byte[] Data)[] entries)
  {
    var path = Path.Combine(tempDirectory, name);
    using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
    foreach (var entry in entries)
    {
      var archiveEntry = archive.CreateEntry(entry.Path);
      using var stream = archiveEntry.Open();
      stream.Write(entry.Data);
    }
    return path;
  }

  static byte[] CreateRom(string title, byte[] fileData)
  {
    if (fileData.Length != 4)
    {
      throw new ArgumentException("The generated fixture expects a four-byte data file.", nameof(fileData));
    }

    var rom = new byte[RomLength];
    Encoding.ASCII.GetBytes(title.PadRight(12)[..12]).CopyTo(rom, 0x00);
    Encoding.ASCII.GetBytes("TEST").CopyTo(rom, 0x0C);
    Encoding.ASCII.GetBytes("00").CopyTo(rom, 0x10);
    rom[0x14] = 0;

    WriteUInt32(rom, 0x20, 0x4000);
    WriteUInt32(rom, 0x2C, 0x0200);
    WriteUInt32(rom, 0x30, 0x4200);
    WriteUInt32(rom, 0x3C, 0x0200);
    WriteUInt32(rom, 0x40, 0x4400);
    WriteUInt32(rom, 0x44, 0x0012);
    WriteUInt32(rom, 0x48, 0x4600);
    WriteUInt32(rom, 0x4C, 0x0008);
    WriteUInt32(rom, 0x68, 0x4800);
    WriteUInt32(rom, 0x80, FileOffset + (uint)fileData.Length);
    WriteUInt32(rom, 0x84, 0x4000);

    WriteUInt32(rom, 0x4400, 0x0008);
    WriteUInt16(rom, 0x4404, 0x0000);
    WriteUInt16(rom, 0x4406, 0x0001);
    rom[0x4408] = 8;
    Encoding.ASCII.GetBytes("file.bin").CopyTo(rom, 0x4409);
    rom[0x4411] = 0;

    WriteUInt32(rom, 0x4600, FileOffset);
    WriteUInt32(rom, 0x4604, FileOffset + (uint)fileData.Length);
    WriteUInt16(rom, 0x4800, 1);
    fileData.CopyTo(rom, FileOffset);
    return rom;
  }

  static byte[] CreateAddPatch(byte[] target)
  {
    using var instructions = new MemoryStream();
    instructions.WriteByte(0x01);
    WriteVcdInteger(instructions, (uint)target.Length);

    using var delta = new MemoryStream();
    WriteVcdInteger(delta, (uint)target.Length);
    delta.WriteByte(0x00);
    WriteVcdInteger(delta, (uint)target.Length);
    WriteVcdInteger(delta, (uint)instructions.Length);
    WriteVcdInteger(delta, 0);
    delta.Write(target);
    instructions.Position = 0;
    instructions.CopyTo(delta);

    using var patch = new MemoryStream();
    patch.Write([0xD6, 0xC3, 0xC4, 0x00, 0x00]);
    patch.WriteByte(0x00);
    WriteVcdInteger(patch, (uint)delta.Length);
    delta.Position = 0;
    delta.CopyTo(patch);
    return patch.ToArray();
  }

  static void WriteVcdInteger(Stream stream, uint value)
  {
    Span<byte> encoded = stackalloc byte[5];
    var index = encoded.Length;
    encoded[--index] = (byte)(value & 0x7F);
    while ((value >>= 7) != 0)
    {
      encoded[--index] = (byte)((value & 0x7F) | 0x80);
    }
    stream.Write(encoded[index..]);
  }

  static byte[] ReadDataFile(Stream stream)
  {
    stream.Position = 0;
    var ndsFile = new NDSFile(stream);
    var file = Assert.Single(ndsFile.data.files);
    stream.Position = file.offset;
    var data = new byte[(int)file.size];
    stream.ReadExactly(data);
    return data;
  }

  static string ComputeMd5(Stream stream)
  {
    stream.Position = 0;
    return Convert.ToHexString(MD5.HashData(stream)).ToLowerInvariant();
  }

  static void WriteUInt16(byte[] data, int offset, ushort value)
  {
    BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset, sizeof(ushort)), value);
  }

  static void WriteUInt32(byte[] data, int offset, uint value)
  {
    BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset, sizeof(uint)), value);
  }
}
