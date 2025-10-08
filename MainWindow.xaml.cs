﻿using System;
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
          MessageBox.Show("Задание 3 в разработке");
          break;
        case "4":
          MessageBox.Show("Задание 4 в разработке");
          break;
        case "5":
          MessageBox.Show("Задание 5 в разработке");
          break;
        case "6":
          MessageBox.Show("Задание 6 в разработке");
          break;
        case "7":
          MessageBox.Show("Задание 7 в разработке");
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