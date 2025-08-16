using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
using NitroPatcher;

namespace NitroPatcherMaui;

public partial class MainPage : ContentPage
{
  string ndsPath = "";
  string patchPath = "";

  public MainPage()
  {
    InitializeComponent();
  }

  private async void NdsPathSelect(object sender, EventArgs e)
  {
    var fileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
      {
        { DevicePlatform.Android, [ "application/*" ] },
        { DevicePlatform.WinUI, [ ".nds" ] },
        { DevicePlatform.macOS, [ "nds" ] },
      });

    PickOptions options = new()
    {
      PickerTitle = "Nintendo DS ROM 文件",
      FileTypes = fileType,
    };
    try
    {
      var result = await FilePicker.Default.PickAsync(options);
      if (result != null)
      {
        ndsPath = result.FullPath;
        NdsPathLabel.Text = result.FileName;
        NdsPathLabel.IsVisible = !string.IsNullOrEmpty(result.FileName);
        LastExceptionLabel.Text = "";
      }
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
        { DevicePlatform.Android, [ "application/*"] },
        { DevicePlatform.WinUI, [ ".xzp", ".zip" ] },
        { DevicePlatform.macOS, [ "xzp", "zip" ] },
      });

    PickOptions options = new()
    {
      PickerTitle = "补丁包",
      FileTypes = fileType,
    };
    try
    {
      var result = await FilePicker.Default.PickAsync(options);
      if (result != null)
      {
        patchPath = result.FullPath;
        PatchPathLabel.Text = result.FileName;
        PatchPathLabel.IsVisible = !string.IsNullOrEmpty(result.FileName);
        LastExceptionLabel.Text = "";
      }
    }
    catch (Exception ex)
    {
      LastExceptionLabel.Text = ex.Message;
    }
  }

  private async void StartPatch(object sender, EventArgs e)
  {
    string buttonConfirmText = ConfirmButton.Text;
    ConfirmButton.Text = "……";
    ConfirmButton.IsEnabled = false;

    try
    {
      var result = PatchHelper.PatchIt(ndsPath, patchPath);
      result.stream.Position = 0;
      var fileSaverResult = await FileSaver.Default.SaveAsync(Path.GetFileName(ndsPath), result.stream);

      LastExceptionLabel.Text = result.returnValue switch
      {
        PatchReturnValue.SUCCESS => "已完成。",
        PatchReturnValue.MD5_MISMATCH => "已完成，但是原始 ROM 的 MD5 校验失败，可能是因为使用了错误的原始 ROM。",
        _ => throw new NotImplementedException(),
      } + $"\n\n原始 ROM 的 MD5：{result.inputMd5}\n生成 ROM 的 MD5：{result.outputMd5}";
    }
    catch (Exception ex)
    {
      LastExceptionLabel.Text = $"错误：{ex.Message}\n\n{ex.StackTrace}";
      await Toast.Make("错误").Show();
    }

    GC.Collect();

    ConfirmButton.Text = buttonConfirmText;
    ConfirmButton.IsEnabled = true;
  }
}
