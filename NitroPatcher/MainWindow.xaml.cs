using Microsoft.Win32;
using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace NitroPatcher
{
  /// <summary>
  /// MainWindow.xaml 的交互逻辑
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      string[] args = Environment.GetCommandLineArgs();
      if (args.Length == 4)
      {
        var result = PatchHelper.PatchIt(args[1], args[2]);
        var fileStream = File.Create(args[3]);
        result.stream.Position = 0;
        result.stream.CopyTo(fileStream);
        fileStream.Close();

        Console.WriteLine(result.returnValue.ToString() + $"\n\n原始 ROM 的 MD5：{result.inputMd5}\n生成 ROM 的 MD5：{result.outputMd5}");
        Environment.Exit(0);
      }
      InitializeComponent();
    }

    private void Button1_Click(object sender, RoutedEventArgs e)
    {
      var originalPath = "";
      try { originalPath = Path.GetDirectoryName(textBox1.Text); } catch { }
      OpenFileDialog ofd = new OpenFileDialog
      {
        Title = "原始 ROM",
        Filter = "Nintendo DS ROM文件|*.nds|所有文件|*.*",
        InitialDirectory = originalPath,
      };
      ofd.ShowDialog();
      if (!string.IsNullOrEmpty(ofd.FileName)) { textBox1.Text = ofd.FileName; }
    }

    private void Button2_Click(object sender, RoutedEventArgs e)
    {
      var originalPath = "";
      try { originalPath = Path.GetDirectoryName(textBox2.Text); } catch { }
      OpenFileDialog ofd = new OpenFileDialog
      {
        Title = "补丁包",
        Filter = "补丁包|*.zip;*.xzp|所有文件|*.*",
        InitialDirectory = originalPath,
      };
      ofd.ShowDialog();
      if (!string.IsNullOrEmpty(ofd.FileName)) { textBox2.Text = ofd.FileName; }
    }

    private void Button3_Click(object sender, RoutedEventArgs e)
    {
      var originalPath = "";
      try { originalPath = Path.GetDirectoryName(textBox3.Text); } catch { }
      SaveFileDialog sfd = new SaveFileDialog
      {
        Title = "输出 ROM",
        Filter = "Nintendo DS ROM文件|*.nds|所有文件|*.*",
        InitialDirectory = originalPath,
      };
      sfd.ShowDialog();
      if (!string.IsNullOrEmpty(sfd.FileName)) { textBox3.Text = sfd.FileName; }
    }

    private void ButtonConfirm_Click(object sender, RoutedEventArgs e)
    {
      string buttonConfirmText = (string)buttonConfirm.Content;
      buttonConfirm.Content = "……";
      buttonConfirm.IsEnabled = false;
      string originalPath = textBox1.Text;
      string patchPath = textBox2.Text;
      string outputPath = textBox3.Text;
      Thread thread = new Thread(() =>
      {
        try
        {
          var result = PatchHelper.PatchIt(originalPath, patchPath);
          var fileStream = File.Create(outputPath);
          result.stream.Position = 0;
          result.stream.CopyTo(fileStream);
          fileStream.Close();
          var md5String = $"\n\n原始 ROM 的 MD5：{result.inputMd5}\n生成 ROM 的 MD5：{result.outputMd5}";

          switch (result.returnValue)
          {
            case PatchReturnValue.SUCCESS:
              MessageBox.Show("已完成。" + md5String, "完成");
              break;
            case PatchReturnValue.MD5_MISMATCH:
              MessageBox.Show("已完成，但是原始 ROM 的 MD5 校验失败，可能是因为使用了错误的原始 ROM。" + md5String, "完成", MessageBoxButton.OK, MessageBoxImage.Information);
              break;
          }
        }
        catch (Exception ex)
        {
          MessageBox.Show($"错误：{ex.Message}\n\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        buttonConfirm.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
        {
          buttonConfirm.Content = buttonConfirmText;
          buttonConfirm.IsEnabled = true;
        });
      });
      thread.Start();
    }

    private void TextBox_DragDrop(object sender, DragEventArgs e)
    {
      string filePath = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
      if (File.Exists(filePath))
      {
        ((TextBox)sender).Text = filePath;
      }
    }

    private void TextBox_DragEnter(object sender, DragEventArgs e)
    {
      if (e.Data.GetDataPresent(DataFormats.FileDrop) && ((Array)e.Data.GetData(DataFormats.FileDrop)).Length == 1)
      {
        e.Effects = DragDropEffects.Link;
      }
      else
      {
        e.Effects = DragDropEffects.None;
      }
    }

    private void TextBox_PreviewDragOver(object sender, DragEventArgs e)
    {
      e.Handled = true;
    }
  }
}
