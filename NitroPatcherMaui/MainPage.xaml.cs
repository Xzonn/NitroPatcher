using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
using NitroPatcher;
#if MACCATALYST
using UniformTypeIdentifiers;
#endif

namespace NitroPatcherMaui;

public partial class MainPage : ContentPage
{
#if MACCATALYST
  static readonly string NdsUti = UTType.CreateFromExtension("nds")?.Identifier ?? "top.xzonn.nitropatcher.nds-rom";
  static readonly string PatchUti = UTType.CreateFromExtension("xzp")?.Identifier ?? "top.xzonn.nitropatcher.patch-package";
#else
  const string NdsUti = "top.xzonn.nitropatcher.nds-rom";
  const string PatchUti = "top.xzonn.nitropatcher.patch-package";
#endif

  string? ndsPath;
  string? patchPath;

  public MainPage()
  {
    InitializeComponent();
    VersionLabel.Text = $"作者：Xzonn 版本：{AppInfo.Current.VersionString}";
  }

  private async void NdsPathSelect(object sender, EventArgs e)
  {
    var fileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
      {
        { DevicePlatform.Android, [ "application/*" ] },
        { DevicePlatform.WinUI, [ ".nds" ] },
        { DevicePlatform.MacCatalyst, [ NdsUti ] },
      });

    PickOptions options = new()
    {
      PickerTitle = "Nintendo DS ROM 文件",
      FileTypes = fileType,
    };
    try
    {
      var result = await FilePicker.Default.PickAsync(options);
      if (result == null)
      {
        LastExceptionLabel.Text = "已取消选择原始 ROM。";
        return;
      }

      ndsPath = result.FullPath;
      NdsPathLabel.Text = result.FileName;
      NdsPathLabel.IsVisible = !string.IsNullOrEmpty(result.FileName);
      LastExceptionLabel.Text = "";
    }
    catch (OperationCanceledException)
    {
      LastExceptionLabel.Text = "已取消选择原始 ROM。";
    }
    catch (Exception ex)
    {
      LastExceptionLabel.Text = ex.Message;
    }
  }

  private async void PatchPathSelect(object sender, EventArgs e)
  {
    var fileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
      {
        { DevicePlatform.Android, [ "application/*" ] },
        { DevicePlatform.WinUI, [ ".xzp", ".zip" ] },
        { DevicePlatform.MacCatalyst, [ PatchUti, "public.zip-archive" ] },
      });

    PickOptions options = new()
    {
      PickerTitle = "补丁包",
      FileTypes = fileType,
    };
    try
    {
      var result = await FilePicker.Default.PickAsync(options);
      if (result == null)
      {
        LastExceptionLabel.Text = "已取消选择补丁包。";
        return;
      }

      patchPath = result.FullPath;
      PatchPathLabel.Text = result.FileName;
      PatchPathLabel.IsVisible = !string.IsNullOrEmpty(result.FileName);
      LastExceptionLabel.Text = "";
    }
    catch (OperationCanceledException)
    {
      LastExceptionLabel.Text = "已取消选择补丁包。";
    }
    catch (Exception ex)
    {
      LastExceptionLabel.Text = ex.Message;
    }
  }

  private async void StartPatch(object sender, EventArgs e)
  {
    if (string.IsNullOrWhiteSpace(ndsPath) || string.IsNullOrWhiteSpace(patchPath))
    {
      LastExceptionLabel.Text = "请先选择原始 ROM 和补丁包。";
      return;
    }

    var ndsFilePath = ndsPath;
    var patchFilePath = patchPath;

    SetBusy(true);
    LastExceptionLabel.Text = "正在应用补丁……";

    try
    {
      var result = await Task.Run(() => PatchHelper.PatchIt(ndsFilePath, patchFilePath));
      using var outputStream = result.stream;
      result.stream.Position = 0;
      var fileSaverResult = await FileSaver.Default.SaveAsync(Path.GetFileName(ndsFilePath), result.stream);

      if (!fileSaverResult.IsSuccessful)
      {
        throw fileSaverResult.Exception ?? new IOException("无法保存生成的 ROM。");
      }

      LastExceptionLabel.Text = result.returnValue switch
      {
        PatchReturnValue.SUCCESS => "已完成。",
        PatchReturnValue.MD5_MISMATCH => "已完成，但是原始 ROM 的 MD5 校验失败，可能是因为使用了错误的原始 ROM。",
        _ => throw new NotImplementedException(),
      } + $"\n\n原始 ROM 的 MD5：{result.inputMd5}\n生成 ROM 的 MD5：{result.outputMd5}";
    }
    catch (OperationCanceledException)
    {
      LastExceptionLabel.Text = "已取消保存。";
    }
    catch (Exception ex)
    {
      LastExceptionLabel.Text = $"错误：{ex.Message}";
      await Toast.Make("错误").Show();
    }
    finally
    {
      SetBusy(false);
    }
  }

  private void SetBusy(bool isBusy)
  {
    NdsSelectButton.IsEnabled = !isBusy;
    PatchSelectButton.IsEnabled = !isBusy;
    ConfirmButton.IsEnabled = !isBusy;
    ConfirmButton.Text = isBusy ? "正在运行" : "开始运行";
    ProgressIndicator.IsRunning = isBusy;
    ProgressIndicator.IsVisible = isBusy;
  }
}
