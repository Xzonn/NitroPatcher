if (args.Length != 3)
{
#if NET6_0_OR_GREATER
  var fileName = Path.GetFileName(Environment.ProcessPath);
#else
  var fileName = Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
#endif
  Console.WriteLine($"NitroPatcherCli\n作者：Xzonn 版本：1.5.1\n\n用法：{fileName} 原始ROM 补丁包 输出ROM");
  Environment.Exit(0);
}

var result = NitroPatcher.PatchHelper.PatchIt(args[0], args[1]);
var fileStream = File.Create(args[2]);
result.stream.CopyTo(fileStream);
fileStream.Close();

Console.WriteLine(result.returnValue.ToString() + $"\n\n原始 ROM 的 MD5：{result.inputMd5}\n生成 ROM 的 MD5：{result.outputMd5}");
