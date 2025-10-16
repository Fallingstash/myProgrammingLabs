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
using NCalc;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot;
using Expression = NCalc.Expression;

namespace MultiWindowApp {
  /// <summary>
  /// Логика взаимодействия для MainWindow.xaml
  /// </summary>
  public partial class DichotomyMethod : Window {
    public DichotomyMethod() {
      InitializeComponent();

    }

    private void BackButton_Click(object sender, RoutedEventArgs e) {
      this.Close();
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

        if (epsilon >= 1) {
          MessageBox.Show("Точность слишком низкая! Рекомендуется ε < 1", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
          // Можно либо запретить, либо разрешить с предупреждением
        }

        if (epsilon < 1e-10) {
          MessageBox.Show("Точность слишком высокая! Это может привести к ошибкам вычислений.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        if (!IsFunctionValid(functionText, a)) {
          MessageBox.Show("Функция содержит ошибки!", "Ошибка в функции", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        var roots = FindAllRoots(a, b, epsilon, functionText);

        PlotFunction(a, b, functionText, roots);

        if (roots.Count == 0) {
          ResultTextBlock.Text = "❌ На заданном интервале корней не найдено";
          ResultTextBlock.Foreground = System.Windows.Media.Brushes.Red;
        }
        else {
          // Фильтруем и округляем корни
          var displayRoots = roots
      .Where(r => {
        try {
          double fr = CalculateFunction(r, functionText);
          return Math.Abs(fr) < Math.Max(epsilon, 1e-6); // Проверяем, что значение функции близко к 0
        }
        catch {
          return false; // Отбрасываем точки, где функция не определена
        }
      })
      .Select(r => Math.Abs(r) < 1e-10 ? 0 : Math.Round(r, 8)) // Используем маленькое значение для округления к 0
      .Where(r => !double.IsInfinity(r) && !double.IsNaN(r)) // Убираем бесконечности и NaN
      .Distinct()
      .OrderBy(r => r)
      .ToList();

          ResultTextBlock.Text = $"✓ Найдено корней: {displayRoots.Count}\n\n";

          for (int i = 0; i < displayRoots.Count; i++) {
            double root = displayRoots[i];
            string rootStr = root == 0 ? "0" : $"{root:0.########}";
            ResultTextBlock.Text += $"Корень {i + 1}: x = {rootStr}\n";
          }

          ResultTextBlock.Foreground = System.Windows.Media.Brushes.Green;
        }
      }
      catch (Exception ex) {
        ResultTextBlock.Text = $"❌ Ошибка: {ex.Message}";
        ResultTextBlock.Foreground = System.Windows.Media.Brushes.Red;
      }
    }

    private void PlotFunction(double a, double b, string functionText, List<double> roots) {
      try {
        var plotModel = new PlotModel {
          Title = $"f(x) = {functionText}",
          TitleFontSize = 14,
          TitleColor = OxyColors.DarkBlue
        };

        // Настраиваем оси для лучшего отображения
        var xAxis = new LinearAxis {
          Position = AxisPosition.Bottom,
          Title = "x",
          TitleColor = OxyColors.Black,
          AxislineColor = OxyColors.Black,
          AxislineStyle = LineStyle.Solid,
          AxislineThickness = 1,
          MajorGridlineColor = OxyColors.LightGray,
          MajorGridlineStyle = LineStyle.Dot,
          MajorStep = CalculateReasonableStep(a, b)
        };

        var yAxis = new LinearAxis {
          Position = AxisPosition.Left,
          Title = "f(x)",
          TitleColor = OxyColors.Black,
          AxislineColor = OxyColors.Black,
          AxislineStyle = LineStyle.Solid,
          AxislineThickness = 1,
          MajorGridlineColor = OxyColors.LightGray,
          MajorGridlineStyle = LineStyle.Dot
        };

        plotModel.Axes.Add(xAxis);
        plotModel.Axes.Add(yAxis);

        // График функции
        var functionSeries = new LineSeries {
          Color = OxyColors.Blue,
          StrokeThickness = 2
        };

        // Генерируем точки с адаптивным количеством
        int pointsCount = Math.Min(200, Math.Max(50, (int)((b - a) * 10)));
        for (int i = 0; i <= pointsCount; i++) {
          double x = a + i * (b - a) / pointsCount;
          try {
            double y = CalculateFunction(x, functionText);
            if (!double.IsNaN(y) && !double.IsInfinity(y))
              functionSeries.Points.Add(new DataPoint(x, y));
          }
          catch { }
        }
        plotModel.Series.Add(functionSeries);

        // Линия y = 0
        var zeroLine = new LineSeries {
          Color = OxyColors.Gray,
          StrokeThickness = 1,
          LineStyle = LineStyle.Dash
        };
        zeroLine.Points.Add(new DataPoint(a, 0));
        zeroLine.Points.Add(new DataPoint(b, 0));
        plotModel.Series.Add(zeroLine);

        // Корни - красные точки 
        if (roots.Count > 0) {
          var rootsSeries = new ScatterSeries {
            MarkerType = MarkerType.Circle,
            MarkerSize = 5,
            MarkerFill = OxyColors.Red,
            MarkerStroke = OxyColors.DarkRed,
            MarkerStrokeThickness = 1
          };

          foreach (double root in roots) {
            rootsSeries.Points.Add(new ScatterPoint(root, 0));
          }
          plotModel.Series.Add(rootsSeries);
        }

        PlotView.Model = plotModel;
      }
      catch (Exception ex) {
        MessageBox.Show($"Ошибка при построении графика: {ex.Message}");
      }
    }

    // Вспомогательный метод для расчета разумного шага сетки
    private double CalculateReasonableStep(double a, double b) {
      double range = b - a;
      if (range <= 0) return 1;

      double step = Math.Pow(10, Math.Floor(Math.Log10(range)));
      if (range / step > 10) step *= 2;
      if (range / step > 20) step *= 2.5;

      return step;
    }

    private List<double> FindAllRoots(double a, double b, double epsilon, string functionText) {
      var roots = new List<double>();
      int divisions = 50; // Еще меньше делений
      double step = (b - a) / divisions;

      // Сначала проверяем особо важные точки
      var specialPoints = new List<double> { a, b, (a + b) / 2 };

      if (0 >= a && 0 <= b) {
        specialPoints.Add(0);
      }

      foreach (double x in specialPoints) {
        if (x >= a && x <= b) {
          try {
            double fx = CalculateFunction(x, functionText);
            if (Math.Abs(fx) == 0) {
              if (!IsRootAlreadyFound(roots, x, epsilon * 100))
                roots.Add(x);
            }
          }
          catch {
            // Пропускаем точки, где функция не определена
            continue;
          }
        }
      }

      // Ищем смены знака
      for (int i = 0; i < divisions; i++) {
        double x1 = a + i * step;
        double x2 = x1 + step;

        double f1 = 0, f2 = 0;
        bool f1Valid = false, f2Valid = false;

        // Пробуем вычислить f(x1)
        try {
          f1 = CalculateFunction(x1, functionText);
          f1Valid = true;
        }
        catch {
          f1Valid = false;
        }

        // Пробуем вычислить f(x2)
        try {
          f2 = CalculateFunction(x2, functionText);
          f2Valid = true;
        }
        catch {
          f2Valid = false;
        }

        if (f1Valid && f2Valid && Math.Abs(f1) < 1e10 && Math.Abs(f2) < 1e10 && f1 * f2 < 0) {
          try {
            double root = DichotomyMethodFunc(x1, x2, epsilon, functionText);

            // Проверяем, что найденное значение действительно близко к нулю
            double fRoot = CalculateFunction(root, functionText);
            if (Math.Abs(fRoot) < Math.Max(epsilon, 1e-6)) {
              if (!IsRootAlreadyFound(roots, root, epsilon * 10))
                roots.Add(root);
            }
          }
          catch {
            // Пропускаем интервалы, где метод дихотомии не срабатывает
          }
        }
      }

      return roots.OrderBy(x => x).ToList();
    }

    private bool IsRootAlreadyFound(List<double> roots, double candidate, double tolerance) {
      foreach (var root in roots) {
        if (Math.Abs(root - candidate) < tolerance)
          return true;
      }
      return false;
    }

    private double DichotomyMethodFunc(double a, double b, double epsilon, string functionText) {
      double fa = 0;
      double fb = 0;

      try {
        fa = CalculateFunction(a, functionText);
        fb = CalculateFunction(b, functionText);
      }
      catch (Exception ex) {
        throw new ArgumentException($"Функция не определена на границах интервала: {ex.Message}");
      }

      // Если на границах уже ноль - возвращаем их
      if (Math.Abs(fa) == 0) return a;
      if (Math.Abs(fb) == 0) return b;


      if (fa * fb >= 0) {
        throw new ArgumentException($"Нет смены знака на интервале. f(a)={fa}, f(b)={fb}");
      }

      int iterations = 0;
      while (b - a > epsilon && iterations < 1000) {
        iterations++;
        double c = (a + b) / 2;
        double fc = CalculateFunction(c, functionText);

        try {
          fc = CalculateFunction(c, functionText);
        }
        catch (Exception ex) {
          throw new ArgumentException($"Функция не определена в точке x={c}: {ex.Message}");
        }

        if (Math.Abs(fc) == 0) return c;

        if (fa * fc < 0) {
          b = c;
          fb = fc;
        }
        else {
          a = c;
          fa = fc;
        }
      }

      double root = (a + b) / 2;
      try {
        double fRoot = CalculateFunction(root, functionText);
        if (double.IsInfinity(fRoot) || double.IsNaN(fRoot) || Math.Abs(fRoot) > 1e10) {
          throw new ArgumentException("Найденная точка не является корнем");
        }
        return root;
      }
      catch {
        throw new ArgumentException("Функция не определена в найденной точке");
      }
    }

    private double ParseDouble(string text) {
      return double.Parse(text.Replace(',', '.'), CultureInfo.InvariantCulture);
    }


    private double CalculateFunction(double x, string functionText) {
      try {
        Expression expression = new Expression(functionText);
        expression.Parameters["x"] = x;

        // Настраиваем функции
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
          else if (name == "pow")
          {
            double baseValue = Convert.ToDouble(args.Parameters[0].Evaluate());
            double exponent = Convert.ToDouble(args.Parameters[1].Evaluate());
            args.Result = Math.Pow(baseValue, exponent);
          }
        };

        object result = expression.Evaluate();

        // Проверяем на особые значения
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

    private bool IsFunctionValid(string functionText, double testValue) {
      try {
        CalculateFunction(testValue, functionText);
        return true;
      }
      catch {
        return false;
      }
    }
  }
}
