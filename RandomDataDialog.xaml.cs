using System;
using System.Windows;

namespace MultiWindowApp {
  public partial class RandomDataDialog : Window {
    public bool IsInteger { get; private set; } = true;
    public double MinValue { get; private set; } = 1;
    public double MaxValue { get; private set; } = 100;

    public RandomDataDialog() {
      InitializeComponent();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e) {
      try {
        IsInteger = IntegerRadio.IsChecked == true;
        MinValue = double.Parse(MinValueTextBox.Text);
        MaxValue = double.Parse(MaxValueTextBox.Text);

        if (MinValue >= MaxValue) {
          MessageBox.Show("Минимальное значение должно быть меньше максимального!");
          return;
        }

        if (IsInteger && (MinValue != Math.Floor(MinValue) || MaxValue != Math.Floor(MaxValue))) {
          MessageBox.Show("Для целых чисел значения должны быть целыми!");
          return;
        }

        DialogResult = true;
        Close();
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