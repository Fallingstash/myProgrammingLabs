using NCalc;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MultiWindowApp {
  /// <summary>
  /// Логика взаимодействия для NewtonMethod.xaml
  /// </summary>
  public partial class NewtonMethod : Window {
    private CancellationTokenSource _cts;
    private bool _isCalculating = false;

    public NewtonMethod() {
      InitializeComponent();
    }

    private async void CalculateButton_Click(object sender, RoutedEventArgs e) {
      if (_isCalculating) {
        MessageBox.Show("Выполняется расчет. Дождитесь окончания.", "Внимание",
            MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      try {
        _isCalculating = true;
        CalculateButton.Content = "Остановить расчет";
        CalculateButton.Click -= CalculateButton_Click;
        CalculateButton.Click += CancelCalculation_Click;

        ResultTextBlock.Text = "⏳ Выполняется расчет...";
        ResultTextBlock.Foreground = Brushes.Blue;

        _cts = new CancellationTokenSource();

        await Task.Run(() => CalculateGlobalMinimum(_cts.Token), _cts.Token);
      }
      catch (OperationCanceledException) {
        ResultTextBlock.Text = "❌ Расчет отменен пользователем";
        ResultTextBlock.Foreground = Brushes.Orange;
      }
      catch (Exception ex) {
        ResultTextBlock.Text = $"❌ Ошибка: {ex.Message}";
        ResultTextBlock.Foreground = Brushes.Red;
      }
      finally {
        _isCalculating = false;
        CalculateButton.Content = "Найти минимум методом Ньютона";
        CalculateButton.Click -= CancelCalculation_Click;
        CalculateButton.Click += CalculateButton_Click;
        _cts?.Dispose();
      }
    }

    private void CancelCalculation_Click(object sender, RoutedEventArgs e) {
      _cts?.Cancel();
    }

    private void CalculateGlobalMinimum(CancellationToken ct) {
      // Проверка ввода в UI потоке
      double a, b, epsilon;
      string functionText;

      Dispatcher.Invoke(() =>
      {
        if (string.IsNullOrWhiteSpace(FunctionTextBox.Text) ||
            string.IsNullOrWhiteSpace(TextBoxA.Text) ||
            string.IsNullOrWhiteSpace(TextBoxB.Text) ||
            string.IsNullOrWhiteSpace(TextBoxEpsilon.Text)) {
          MessageBox.Show("Заполните все поля!", "Внимание",
              MessageBoxButton.OK, MessageBoxImage.Warning);
          throw new ArgumentException("Не все поля заполнены");
        }

        a = ParseDouble(TextBoxA.Text);
        b = ParseDouble(TextBoxB.Text);
        epsilon = ParseDouble(TextBoxEpsilon.Text);
        functionText = FunctionTextBox.Text;

        if (a >= b) {
          MessageBox.Show("a должно быть меньше b!", "Ошибка ввода",
              MessageBoxButton.OK, MessageBoxImage.Error);
          throw new ArgumentException("Некорректный интервал");
        }

        if (epsilon <= 0) {
          MessageBox.Show("Точность должна быть положительным числом!", "Ошибка ввода",
              MessageBoxButton.OK, MessageBoxImage.Error);
          throw new ArgumentException("Некорректная точность");
        }
      });

      // Получаем параметры
      a = Dispatcher.Invoke(() => ParseDouble(TextBoxA.Text));
      b = Dispatcher.Invoke(() => ParseDouble(TextBoxB.Text));
      epsilon = Dispatcher.Invoke(() => ParseDouble(TextBoxEpsilon.Text));
      functionText = Dispatcher.Invoke(() => FunctionTextBox.Text);

      // 🔍 Проверяем функцию на разрывы в интервале
      bool hasDiscontinuity = CheckForDiscontinuities(a, b, functionText, ct);

      if (hasDiscontinuity) {
        Dispatcher.Invoke(() =>
        {
          ResultTextBlock.Text = "⚠ Функция имеет разрыв(ы) на заданном интервале.\n" +
                                "Метод Ньютона может дать некорректные результаты.\n\n" +
                                "Рекомендации:\n" +
                                "1. Измените интервал, чтобы избежать разрыва\n" +
                                "2. Проверьте корректность функции";
          ResultTextBlock.Foreground = Brushes.Orange;
          PlotFunction(a, b, functionText);
        });
        return;
      }

      // Проверяем функцию в нескольких точках
      if (!IsFunctionSuitableForInterval(a, b, functionText)) {
        Dispatcher.Invoke(() =>
        {
          MessageBox.Show("Функция содержит ошибки на заданном интервале!",
              "Ошибка в функции", MessageBoxButton.OK, MessageBoxImage.Error);
        });
        throw new ArgumentException("Функция содержит ошибки");
      }

      ct.ThrowIfCancellationRequested();

      try {
        // 🔍 ШАГ 1: Находим критические точки (где f'(x) ≈ 0)
        List<double> criticalPoints = FindCriticalPoints(a, b, functionText, epsilon, ct);

        // 🔍 ШАГ 2: Формируем список кандидатов (критические точки + границы)
        List<CandidatePoint> candidates = new List<CandidatePoint>();

        // Добавляем граничные точки (если функция там определена)
        if (IsFunctionSuitable(a, functionText)) {
          double yA = CalculateFunction(a, functionText);
          candidates.Add(new CandidatePoint(a, yA, PointType.Boundary));
        }

        if (IsFunctionSuitable(b, functionText)) {
          double yB = CalculateFunction(b, functionText);
          candidates.Add(new CandidatePoint(b, yB, PointType.Boundary));
        }

        // Добавляем критические точки
        foreach (double cp in criticalPoints) {
          try {
            double y = CalculateFunction(cp, functionText);
            candidates.Add(new CandidatePoint(cp, y, PointType.Critical));
          }
          catch {
            // Пропускаем точки, где функция не определена
          }
        }

        ct.ThrowIfCancellationRequested();

        // 🔍 ШАГ 3: Находим точку с минимальным значением функции
        if (candidates.Count == 0) {
          Dispatcher.Invoke(() =>
          {
            ResultTextBlock.Text = "❌ Не удалось найти ни одной подходящей точки на интервале";
            ResultTextBlock.Foreground = Brushes.Red;
            PlotFunction(a, b, functionText);
          });
          return;
        }

        CandidatePoint globalMinimum = candidates.OrderBy(c => c.Y).First();

        // 🔍 ШАГ 4: Формируем отчет
        Dispatcher.Invoke(() =>
        {
          string report = GenerateReport(a, b, epsilon, functionText, globalMinimum, candidates);

          ResultTextBlock.Text = report;
          ResultTextBlock.Foreground = Brushes.Green;

          // Отображаем на графике
          PlotFunctionWithMinimum(a, b, functionText, globalMinimum);
        });
      }
      catch (Exception ex) {
        Dispatcher.Invoke(() =>
        {
          ResultTextBlock.Text = $"❌ Ошибка при расчете: {ex.Message}";
          ResultTextBlock.Foreground = Brushes.Red;
          PlotFunction(a, b, functionText);
        });
      }
    }

    private bool CheckForDiscontinuities(double a, double b, string functionText, CancellationToken ct) {
      // Проверяем функцию на 50 точках равномерно распределенных по интервалу
      int samples = 50;
      int undefinedPoints = 0;

      for (int i = 0; i <= samples; i++) {
        ct.ThrowIfCancellationRequested();

        double x = a + i * (b - a) / samples;
        if (!IsFunctionSuitable(x, functionText)) {
          undefinedPoints++;
        }
      }

      // Если более 20% точек не определены, считаем что есть разрыв
      return undefinedPoints > samples * 0.2;
    }

    private List<double> FindCriticalPoints(double a, double b, string functionText, double epsilon, CancellationToken ct) {
      List<double> criticalPoints = new List<double>();

      // Используем метод Ньютона для решения уравнения f'(x) = 0
      // Пробуем разные начальные точки
      int numStartPoints = 10;
      double step = (b - a) / (numStartPoints + 1);

      for (int i = 1; i <= numStartPoints; i++) {
        ct.ThrowIfCancellationRequested();

        double x0 = a + i * step;

        // Пропускаем точки, где функция не определена
        if (!IsFunctionSuitable(x0, functionText))
          continue;

        try {
          double criticalPoint = SolveDerivativeZero(x0, functionText, epsilon, ct);

          // Проверяем, что точка в интервале и не дублируется
          if (criticalPoint >= a - epsilon && criticalPoint <= b + epsilon) {
            // Проверяем, что производная действительно близка к нулю
            double derivative = FirstDerivative(criticalPoint, functionText);
            if (Math.Abs(derivative) < epsilon * 10) {
              if (!criticalPoints.Any(p => Math.Abs(p - criticalPoint) < epsilon * 10)) {
                criticalPoints.Add(criticalPoint);
              }
            }
          }
        }
        catch {
          // Пропускаем неудачные начальные точки
        }
      }

      return criticalPoints.Where(x => x >= a && x <= b).OrderBy(x => x).ToList();
    }

    private double SolveDerivativeZero(double x0, string functionText, double epsilon, CancellationToken ct) {
      // Метод Ньютона для решения f'(x) = 0
      double x = x0;
      int maxIterations = 50;

      for (int iteration = 0; iteration < maxIterations; iteration++) {
        ct.ThrowIfCancellationRequested();

        try {
          double f1 = FirstDerivative(x, functionText);  // f'(x)

          // Критерий остановки
          if (Math.Abs(f1) < epsilon)
            return x;

          double f2 = SecondDerivative(x, functionText); // f''(x)

          if (Math.Abs(f2) < 1e-12) {
            // Если вторая производная слишком мала, делаем небольшой шаг по градиенту
            x = x - Math.Sign(f1) * epsilon * 100;
            continue;
          }

          double xNew = x - f1 / f2;

          // Проверка на расходимость
          if (double.IsNaN(xNew) || double.IsInfinity(xNew))
            throw new InvalidOperationException("Метод расходится");

          // Критерий остановки по изменению x
          if (Math.Abs(xNew - x) < epsilon)
            return xNew;

          x = xNew;
        }
        catch {
          throw new InvalidOperationException("Ошибка в вычислениях");
        }
      }

      throw new InvalidOperationException($"Не сошлось за {maxIterations} итераций");
    }

    private string GenerateReport(double a, double b, double epsilon, string functionText,
        CandidatePoint globalMinimum, List<CandidatePoint> candidates) {
      string report = $"МИНИМУМ НА ИНТЕРВАЛЕ [{a:0.##}, {b:0.##}]:\n\n";

      report += $"Точка минимума: x = {globalMinimum.X:0.######}\n";
      report += $"Значение функции: f(x) = {globalMinimum.Y:0.######}\n\n";


      return report;
    }

    private void PlotFunctionWithMinimum(double a, double b, string functionText, CandidatePoint minimum) {
      Dispatcher.Invoke(() =>
      {
        try {
          var plotModel = new PlotModel {
            Title = $"f(x) = {functionText}",
            TitleFontSize = 14,
            Subtitle = $"Глобальный минимум: x = {minimum.X:0.#####}, f(x) = {minimum.Y:0.#####}",
            SubtitleFontSize = 10
          };

          // Ось X
          var xAxis = new LinearAxis {
            Position = AxisPosition.Bottom,
            Title = "x",
            Minimum = a,
            Maximum = b
          };

          // Ось Y  
          var yAxis = new LinearAxis {
            Position = AxisPosition.Left,
            Title = "f(x)"
          };

          plotModel.Axes.Add(xAxis);
          plotModel.Axes.Add(yAxis);

          // График функции
          var functionSeries = new LineSeries {
            Title = "Функция",
            Color = OxyColors.Blue,
            StrokeThickness = 2
          };

          int points = 400; // Уменьшили для скорости
          for (int i = 0; i <= points; i++) {
            double x = a + i * (b - a) / points;
            try {
              double y = CalculateFunction(x, functionText);
              functionSeries.Points.Add(new DataPoint(x, y));
            }
            catch {
              // Разрыв функции - не добавляем точку
            }
          }

          // Добавляем серию только если есть точки
          if (functionSeries.Points.Count > 0) {
            plotModel.Series.Add(functionSeries);
          }

          // ТОЛЬКО точка глобального минимума (без лишних маркеров)
          if (IsFunctionSuitable(minimum.X, functionText)) {
            var minimumSeries = new ScatterSeries {
              Title = "Глобальный минимум",
              MarkerType = MarkerType.Circle,
              MarkerSize = 8,
              MarkerFill = OxyColors.Red
            };

            minimumSeries.Points.Add(new ScatterPoint(minimum.X, minimum.Y));
            plotModel.Series.Add(minimumSeries);

            // Подпись к точке минимума
            var annotation = new OxyPlot.Annotations.PointAnnotation {
              X = minimum.X,
              Y = minimum.Y,
              Text = $"Минимум\nx={minimum.X:0.####}\nf(x)={minimum.Y:0.####}",
              TextColor = OxyColors.DarkRed,
              FontSize = 10
            };
            plotModel.Annotations.Add(annotation);
          }

          PlotView.Model = plotModel;
        }
        catch (Exception ex) {
          // Минимальная обработка ошибок при построении графика
          Console.WriteLine($"Ошибка при построении графика: {ex.Message}");
        }
      });
    }

    private void PlotFunction(double a, double b, string functionText) {
      Dispatcher.Invoke(() =>
      {
        try {
          var plotModel = new PlotModel {
            Title = $"f(x) = {functionText}",
            TitleFontSize = 14
          };

          var xAxis = new LinearAxis {
            Position = AxisPosition.Bottom,
            Title = "x",
            Minimum = a,
            Maximum = b
          };

          var yAxis = new LinearAxis {
            Position = AxisPosition.Left,
            Title = "f(x)"
          };

          plotModel.Axes.Add(xAxis);
          plotModel.Axes.Add(yAxis);

          var functionSeries = new LineSeries {
            Title = "Функция",
            Color = OxyColors.Blue,
            StrokeThickness = 2
          };

          int points = 400;
          for (int i = 0; i <= points; i++) {
            double x = a + i * (b - a) / points;
            try {
              double y = CalculateFunction(x, functionText);
              functionSeries.Points.Add(new DataPoint(x, y));
            }
            catch {
              // Не добавляем точку при разрыве
            }
          }

          if (functionSeries.Points.Count > 0) {
            plotModel.Series.Add(functionSeries);
          }

          PlotView.Model = plotModel;
        }
        catch (Exception ex) {
          Console.WriteLine($"Ошибка при построении графика: {ex.Message}");
        }
      });
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
      // Проверяем 5 случайных точек в интервале
      Random rand = new Random();
      int validPoints = 0;

      for (int i = 0; i < 5; i++) {
        double point = a + rand.NextDouble() * (b - a);
        if (IsFunctionSuitable(point, functionText))
          validPoints++;
      }

      return validPoints > 0; // Хотя бы одна точка должна работать
    }

    private double ParseDouble(string text) {
      return double.Parse(text.Replace(',', '.'), CultureInfo.InvariantCulture);
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) {
      if (_isCalculating) {
        var result = MessageBox.Show("Выполняется расчет. Закрыть окно?", "Подтверждение",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes) {
          _cts?.Cancel();
          this.Close();
        }
      } else {
        this.Close();
      }
    }

    private double CalculateFunction(double x, string functionText) {
      try {
        NCalc.Expression expression = new NCalc.Expression(functionText);
        expression.Parameters["x"] = x;

        expression.EvaluateFunction += delegate (string name, FunctionArgs args)
        {
          if (name == "sqrt") {
            double arg = Convert.ToDouble(args.Parameters[0].Evaluate());
            if (arg < 0)
              throw new ArgumentException("Корень из отрицательного числа");
            args.Result = Math.Sqrt(arg);
          } else if (name == "sin")
            args.Result = Math.Sin(Convert.ToDouble(args.Parameters[0].Evaluate()));
          else if (name == "cos")
            args.Result = Math.Cos(Convert.ToDouble(args.Parameters[0].Evaluate()));
          else if (name == "tan")
            args.Result = Math.Tan(Convert.ToDouble(args.Parameters[0].Evaluate()));
          else if (name == "log") {
            double arg = Convert.ToDouble(args.Parameters[0].Evaluate());
            if (arg <= 0)
              throw new ArgumentException("Логарифм неположительного числа");
            args.Result = Math.Log(arg);
          } else if (name == "exp")
            args.Result = Math.Exp(Convert.ToDouble(args.Parameters[0].Evaluate()));
          else if (name == "abs")
            args.Result = Math.Abs(Convert.ToDouble(args.Parameters[0].Evaluate()));
          else if (name == "pow") {
            double baseValue = Convert.ToDouble(args.Parameters[0].Evaluate());
            double exponent = Convert.ToDouble(args.Parameters[1].Evaluate());
            args.Result = Math.Pow(baseValue, exponent);
          }
        };

        object result = expression.Evaluate();
        double doubleResult = Convert.ToDouble(result);

        if (double.IsInfinity(doubleResult) || double.IsNaN(doubleResult))
          throw new ArgumentException("Функция не определена");

        return doubleResult;
      }
      catch (DivideByZeroException) {
        throw new ArgumentException("Деление на ноль");
      }
      catch (Exception ex) {
        throw new ArgumentException($"Ошибка вычисления: {ex.Message}");
      }
    }

    private double FirstDerivative(double x, string functionText, double h = 1e-5) {
      try {
        double f_plus = CalculateFunction(x + h, functionText);
        double f_minus = CalculateFunction(x - h, functionText);
        return (f_plus - f_minus) / (2 * h);
      }
      catch {
        // Если не удается вычислить центральную разность, пробуем одностороннюю
        try {
          double f_plus = CalculateFunction(x + h, functionText);
          double f_current = CalculateFunction(x, functionText);
          return (f_plus - f_current) / h;
        }
        catch {
          throw new ArgumentException("Не удается вычислить производную");
        }
      }
    }

    private double SecondDerivative(double x, string functionText, double h = 1e-5) {
      try {
        double f_plus = CalculateFunction(x + h, functionText);
        double f_current = CalculateFunction(x, functionText);
        double f_minus = CalculateFunction(x - h, functionText);
        return (f_plus - 2 * f_current + f_minus) / (h * h);
      }
      catch {
        throw new ArgumentException("Не удается вычислить вторую производную");
      }
    }
  }

  // Вспомогательные классы
  public enum PointType { Critical, Boundary }

  public class CandidatePoint {
    public double X { get; set; }
    public double Y { get; set; }
    public PointType Type { get; set; }

    public CandidatePoint(double x, double y, PointType type) {
      X = x;
      Y = y;
      Type = type;
    }
  }
}