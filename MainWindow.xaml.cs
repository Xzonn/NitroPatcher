using Microsoft.Win32;
using System;
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
      if (args.Length == 4) { PatchHelper.PatchIt(args[1], args[2], args[3]); Environment.Exit(0); }
      InitializeComponent();
    }

    private void Button1_Click(object sender, RoutedEventArgs e)
    {
      OpenFileDialog ofd = new OpenFileDialog
      {
        Title = "原始 ROM",
        Filter = "Nintendo DS ROM文件|*.nds|所有文件|*.*"
      };
      ofd.ShowDialog();
      textBox1.Text = ofd.FileName;
    }

    private void Button2_Click(object sender, RoutedEventArgs e)
    {
      OpenFileDialog ofd = new OpenFileDialog
      {
        Title = "补丁包",
        Filter = "补丁包|*.zip|所有文件|*.*"
      };
      ofd.ShowDialog();
      textBox2.Text = ofd.FileName;
    }

    private void Button3_Click(object sender, RoutedEventArgs e)
    {
      SaveFileDialog sfd = new SaveFileDialog
      {
        Title = "输出 ROM",
        Filter = "Nintendo DS ROM文件|*.nds"
      };
      sfd.ShowDialog();
      textBox3.Text = sfd.FileName;
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
        PatchHelper.PatchIt(originalPath, patchPath, outputPath);
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
      if (PatchHelper.CheckIfFileExists(filePath))
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
