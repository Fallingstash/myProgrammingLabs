using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace myLAB {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window {
    public MainWindow() {
      InitializeComponent();
    }


    private void СhooseTask_Click(object sender, RoutedEventArgs e) {
      string text = ((Button)sender).Content.ToString();
      char num = '-';

      foreach (var symbol in text) {
        if (Char.IsDigit(symbol)) {
          num = symbol;
        }
      }

      switch (num) {
        case '1':
          new DichotomyMethod().Show();
          break;
        case '2':
          break;
        case '3':
          break;
        case '4':
          break;
        case '5':
          break;
        case '6':
          break;
        case '7':
          break;
        case '8':
          break;
        case '9':
          break;
      }
    }

  }
}