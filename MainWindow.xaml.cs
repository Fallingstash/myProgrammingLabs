using System;
using System.Windows;
using System.Windows.Controls;

namespace MultiWindowApp {
  public partial class MainWindow : Window {
    public MainWindow() {
      InitializeComponent();
    }

    private void ChooseTask_Click(object sender, RoutedEventArgs e) {
      string text = ((Button)sender).Content.ToString();
      string taskNumber = "";

      foreach (var symbol in text) {
        if (Char.IsDigit(symbol)) {
          taskNumber = symbol + "";
        }
      }

      switch (taskNumber) {
        case "1":
          new DichotomyMethod().Show();
          break;
        case "2":
          new LinearSystemSolver().Show();
          break;
        case "3":
          new GoldenSectionMethod().Show();
          break;
        case "4":
          new NewtonMethod().Show();
          break;
        case "5":
          new SortingAlgorithms().Show();
          break;
        case "6":
          new IntegralCalculator().Show();
          break;
        case "7":
          new CoordinateDescentMethod().Show();
          break;
        case "8":
          MessageBox.Show("Задание 8 в разработке");
          break;
        case "9":
          MessageBox.Show("Задание 9 в разработке");
          break;
        default:
          MessageBox.Show("Неизвестное задание");
          break;
      }
    }
  }
}