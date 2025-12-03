// BogoSettingsDialog.xaml.cs
using System;
using System.Windows;

namespace MultiWindowApp {
  public partial class BogoSettingsDialog : Window {
    public int MaxIterations { get; private set; }

    public BogoSettingsDialog(int currentMaxIterations) {
      InitializeComponent();
      MaxIterationsTextBox.Text = currentMaxIterations.ToString();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e) {
      try {
        if (int.TryParse(MaxIterationsTextBox.Text, out int maxIterations)) {
          if (maxIterations <= 0) {
            MessageBox.Show("Количество итераций должно быть положительным числом!");
            return;
          }

          if (maxIterations > 10000000) // 10 миллионов
          {
            MessageBox.Show("Слишком большое значение! Рекомендуется не более 1 000 000.");
            return;
          }

          MaxIterations = maxIterations;
          DialogResult = true;
          Close();
        } else {
          MessageBox.Show("Введите корректное число!");
        }
      }
      catch (Exception ex) {
        MessageBox.Show($"Ошибка: {ex.Message}");
      }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) {
      DialogResult = false;
      Close();
    }
  }
}