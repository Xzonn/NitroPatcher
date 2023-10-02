using Microsoft.Win32;
using System.Threading;
using System.Windows;
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
      InitializeComponent();
    }

    private void Button1_Click(object sender, RoutedEventArgs e)
    {
      OpenFileDialog ofd = new OpenFileDialog
      {
        Title = "原始ROM",
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
        Title = "输出ROM",
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
  }
}
