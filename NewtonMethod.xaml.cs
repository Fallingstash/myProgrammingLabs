using NCalc;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MultiWindowApp {
  /// <summary>
  /// Логика взаимодействия для NewtonMethod.xaml
  /// </summary>
  public partial class NewtonMethod : Window {
    public NewtonMethod() {
      InitializeComponent();
    }

    private void CalculateButton_Click(object sender, RoutedEventArgs e) {
      try {
        ResultTextBlock.Text = "";

        if (string.IsNullOrWhiteSpace(FunctionTextBox.Text) ||
            string.IsNullOrWhiteSpace(TextBoxA.Text) ||
            string.IsNullOrWhiteSpace(TextBoxB.Text) ||
            string.IsNullOrWhiteSpace(TextBoxEpsilon.Text)) {
          MessageBox.Show("Заполните все поля!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
          return;
        }

        double a = ParseDouble(TextBoxA.Text);
        double b = ParseDouble(TextBoxB.Text);
        double epsilon = ParseDouble(TextBoxEpsilon.Text);
        string functionText = FunctionTextBox.Text;

        if (a >= b) {
          MessageBox.Show("a должно быть меньше b!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        if (epsilon <= 0) {
          MessageBox.Show("Точность должна быть положительным числом!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        if (!IsFunctionSuitableForInterval(a, b, functionText)) {
          MessageBox.Show("Функция содержит ошибки!", "Ошибка в функции", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        // 🎯 ПРЕДУПРЕЖДЕНИЯ ДЛЯ EPSILON
        if (epsilon >= 1) {
          var result = MessageBox.Show(
              "Точность ε >= 1 слишком грубая! Результаты могут быть неточными.\nПродолжить?",
              "Предупреждение о точности",
              MessageBoxButton.YesNo,
              MessageBoxImage.Warning);

          if (result == MessageBoxResult.No)
            return;
        }

        if (epsilon < 1e-10) {
          MessageBox.Show(
              "Точность ε < 1e-10 слишком высокая! Это может вызвать численные ошибки.",
              "Предупреждение о точности",
              MessageBoxButton.OK,
              MessageBoxImage.Warning);
        }


        List<Point> minima = FindMinimaNewton(a, b, epsilon, functionText);

        // Выводим результаты
        if (minima.Count == 0) {
          ResultTextBlock.Text = "❌ На заданном интервале минимумов не найдено";
          ResultTextBlock.Foreground = System.Windows.Media.Brushes.Red;
        } else {
          ResultTextBlock.Text = $"✓ Найдено минимумов: {minima.Count}\n\n";
          for (int i = 0; i < minima.Count; i++) {
            ResultTextBlock.Text += $"Минимум {i + 1}: x = {minima[i].X:0.#####}, f(x) = {minima[i].Y:0.#####}\n";
          }
          ResultTextBlock.Foreground = System.Windows.Media.Brushes.Green;
        }

        PlotFunctionWithMinima(a, b, functionText, minima);
      }
      catch (Exception ex) {
        ResultTextBlock.Text = $"❌ Ошибка: {ex.Message}";
        ResultTextBlock.Foreground = System.Windows.Media.Brushes.Red;
      }
    }

    private List<Point> FindMinimaNewton(double a, double b, double epsilon, string functionText) {
      var minima = new List<Point>();
      int testPoints = 100;

      for (int i = 0; i < testPoints; i++) {
        double x0 = a + i * (b - a) / (testPoints - 1);

        // 🎯 Пропускаем точки, где функция не определена
        if (!IsFunctionSuitable(x0, functionText))
          continue;

        try {
          Point minimum = NewtonMethodSinglePoint(x0, epsilon, functionText);

          // Дополнительная проверка: функция должна быть определена в минимуме
          if (minimum.X >= a && minimum.X <= b &&
              !IsMinimumAlreadyFound(minima, minimum, epsilon) &&
              IsFunctionSuitableForInterval(a, b, functionText)) {
            minima.Add(minimum);
          }
        }
        catch (Exception ex) {
          // Выводим отладочную информацию (можно убрать потом)
          Console.WriteLine($"Точка {x0}: {ex.Message}");
          continue;
        }
      }

      return minima.OrderBy(m => m.X).ToList();
    }



    // Модифицируем метод NewtonMethodSinglePoint:
    private Point NewtonMethodSinglePoint(double x0, double epsilon, string functionText, int maxIterations = 100) {
      double x = x0;

      for (int i = 0; i < maxIterations; i++) {
        double f1 = FirstDerivative(x, functionText);  // f'(x)
        double f2 = SecondDerivative(x, functionText); // f''(x)

        if (Math.Abs(f2) < 1e-10) {
          throw new InvalidOperationException("Вторая производная слишком мала - метод не применим");
        }

        double xNew = x - f1 / f2;

        // Критерий остановки
        if (Math.Abs(xNew - x) < epsilon) {
          // 🎯 ВАЖНО: Проверяем, что это действительно минимум (f''(x) > 0)
          double finalF2 = SecondDerivative(xNew, functionText);
          if (finalF2 > 0) // Это минимум
          {
            x = xNew;
            break;
          } else // Это максимум или точка перегиба
            {
            throw new InvalidOperationException("Найдена точка максимума или перегиба");
          }
        }

        x = xNew;

        if (i == maxIterations - 1) {
          throw new InvalidOperationException("Метод не сошелся за максимальное число итераций");
        }
      }

      double y = CalculateFunction(x, functionText);
      return new Point(x, y);
    }

    // Проверка, что минимум еще не найден
    private bool IsMinimumAlreadyFound(List<Point> minima, Point candidate, double tolerance) {
      return minima.Any(m => Math.Abs(m.X - candidate.X) < tolerance);
    }

    // Построение графика функции с минимумами
    private void PlotFunctionWithMinima(double a, double b, string functionText, List<Point> minima) {
      try {
        var plotModel = new PlotModel {
          Title = $"f(x) = {functionText}",
          TitleFontSize = 14
        };

        // Ось X
        var xAxis = new LinearAxis { Position = AxisPosition.Bottom, Title = "x" };
        // Ось Y  
        var yAxis = new LinearAxis { Position = AxisPosition.Left, Title = "f(x)" };

        plotModel.Axes.Add(xAxis);
        plotModel.Axes.Add(yAxis);

        // График функции
        var functionSeries = new LineSeries { Title = "Функция", Color = OxyColors.Blue };

        int points = 200;
        for (int i = 0; i <= points; i++) {
          double x = a + i * (b - a) / points;
          try {
            double y = CalculateFunction(x, functionText);
            functionSeries.Points.Add(new DataPoint(x, y));
          }
          catch {
            // Пропускаем точки, где функция не определена
          }
        }
        plotModel.Series.Add(functionSeries);

        // Точки минимумов
        if (minima.Count > 0) {
          var minimaSeries = new ScatterSeries {
            Title = "Минимумы",
            MarkerType = MarkerType.Circle,
            MarkerSize = 6,
            MarkerFill = OxyColors.Red
          };

          foreach (var minimum in minima) {
            minimaSeries.Points.Add(new ScatterPoint(minimum.X, minimum.Y));
          }
          plotModel.Series.Add(minimaSeries);
        }

        PlotView.Model = plotModel;
      }
      catch (Exception ex) {
        MessageBox.Show($"Ошибка при построении графика: {ex.Message}");
      }
    }

    private bool IsFunctionSuitable(double x, string functionText) {
      try {
        CalculateFunction(x, functionText);
        return true;
      }
      catch {
        return false;
      }
    }

    private bool IsFunctionSuitableForInterval(double a, double b, string functionText) {

      double[] testPoints = {
        a,                          // начало интервала
        b,                          // конец интервала  
        (a + b) / 2,               // середина
        a + (b - a) * 0.25,        // 25% интервала
        a + (b - a) * 0.75         // 75% интервала
    };

      int definedPoints = 0;

      foreach (double point in testPoints) {
        try {
          CalculateFunction(point, functionText);
          definedPoints++;
        }
        catch {
        }
      }

      return definedPoints > 0;
    }

    private double ParseDouble(string text) {
      return double.Parse(text.Replace(',', '.'), CultureInfo.InvariantCulture);
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) {
      this.Close();
    }

    // Метод для вычисления функции в точке x
    private double CalculateFunction(double x, string functionText) {
      try {
        // Создаем выражение из текста функции
        NCalc.Expression expression = new NCalc.Expression(functionText);

        // Подставляем значение x вместо переменной "x" в формуле
        expression.Parameters["x"] = x;

        // Настраиваем математические функции
        expression.EvaluateFunction += delegate (string name, FunctionArgs args) {
          if (name == "sqrt")
            args.Result = Math.Sqrt(Convert.ToDouble(args.Parameters[0].Evaluate()));
          else if (name == "sin")
            args.Result = Math.Sin(Convert.ToDouble(args.Parameters[0].Evaluate()));
          else if (name == "cos")
            args.Result = Math.Cos(Convert.ToDouble(args.Parameters[0].Evaluate()));
          else if (name == "tan")
            args.Result = Math.Tan(Convert.ToDouble(args.Parameters[0].Evaluate()));
          else if (name == "log")
            args.Result = Math.Log(Convert.ToDouble(args.Parameters[0].Evaluate()));
          else if (name == "exp")
            args.Result = Math.Exp(Convert.ToDouble(args.Parameters[0].Evaluate()));
          else if (name == "abs")
            args.Result = Math.Abs(Convert.ToDouble(args.Parameters[0].Evaluate()));
          else if (name == "pow") {
            double baseValue = Convert.ToDouble(args.Parameters[0].Evaluate());
            double exponent = Convert.ToDouble(args.Parameters[1].Evaluate());
            args.Result = Math.Pow(baseValue, exponent);
          }
        };

        // Вычисляем результат
        object result = expression.Evaluate();

        // Проверяем на особые значения (бесконечность, не число)
        if (double.IsInfinity(Convert.ToDouble(result)) || double.IsNaN(Convert.ToDouble(result))) {
          throw new ArgumentException("Функция не определена в этой точке");
        }

        return Convert.ToDouble(result);
      }
      catch (DivideByZeroException) {
        throw new ArgumentException("Деление на ноль");
      }
      catch (Exception ex) {
        throw new ArgumentException($"Ошибка в функции: {ex.Message}");
      }
    }

    private double FirstDerivative(double x, string functionText, double h = 1e-5) {

      double f_plus = CalculateFunction(x + h, functionText);
      double f_minus = CalculateFunction(x - h, functionText);

      return (f_plus - f_minus) / (2 * h);
    }

    private double SecondDerivative(double x, string functionText, double h = 1e-5) {
      double f_plus = CalculateFunction(x + h, functionText);
      double f_current = CalculateFunction(x, functionText);
      double f_minus = CalculateFunction(x - h, functionText);

      return (f_plus - 2 * f_current + f_minus) / (h * h);
    }
  }
}
