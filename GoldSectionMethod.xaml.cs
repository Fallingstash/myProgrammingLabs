using NCalc;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Annotations;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using Expression = NCalc.Expression;

namespace MultiWindowApp {
  public partial class GoldenSectionMethod : Window {
    private CancellationTokenSource _cancellationTokenSource;
    private bool _isCalculating = false;

    private const double GoldenRatio = 0.618; // ~~ (sqrt(5) - 1) / 2

    public GoldenSectionMethod() {
      InitializeComponent();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) {
      this.Close();
    }

    private async void CalculateButton_Click(object sender, RoutedEventArgs e) {
      if (_isCalculating) {
        MessageBox.Show("Вычисление уже выполняется. Дождитесь завершения или нажмите 'Назад' для отмены.");
        return;
      }

      try {
        _isCalculating = true;
        CalculateButton.IsEnabled = false;
        ResultTextBlock.Text = "Вычисление...";
        ResultTextBlock.Foreground = System.Windows.Media.Brushes.Blue;

        // Создаем токен отмены
        _cancellationTokenSource = new CancellationTokenSource();

        // Проверка заполнения полей
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

        // Валидация входных данных
        if (a >= b) {
          MessageBox.Show("a должно быть меньше b!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        if (epsilon <= 0) {
          MessageBox.Show("Точность должна быть положительным числом!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        if (!IsFunctionDefined(a, functionText)) {
          MessageBox.Show("Функция содержит ошибки!", "Ошибка в функции", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        // Запускаем вычисления в отдельной задаче
        var minima = await Task.Run(() => FindAllMinima(a, b, epsilon, functionText, _cancellationTokenSource.Token));

        // Проверяем не была ли отмена
        if (_cancellationTokenSource.Token.IsCancellationRequested) {
          ResultTextBlock.Text = "❌ Вычисление отменено";
          ResultTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
          return;
        }

        // Построение графика и вывод результатов в UI потоке
        PlotFunction(a, b, functionText, minima);

        if (minima.Count == 0) {
          ResultTextBlock.Text = "❌ На заданном интервале минимумов не найдено";
          ResultTextBlock.Foreground = System.Windows.Media.Brushes.Red;
        } else {
          // Фильтруем и форматируем результаты
          var displayMinima = minima
              .Select(m => new {
                X = Math.Abs(m.X) < 1e-10 ? 0 : Math.Round(m.X, 6),
                Y = Math.Abs(m.Y) < 1e-10 ? 0 : Math.Round(m.Y, 6)
              })
              .Distinct()
              .OrderBy(m => m.X)
              .ToList();

          ResultTextBlock.Text = $"✓ Найдено минимумов: {displayMinima.Count}\n\n";

          for (int i = 0; i < displayMinima.Count; i++) {
            var minimum = displayMinima[i];
            string xStr = minimum.X == 0 ? "0" : $"{minimum.X:0.######}";
            string yStr = minimum.Y == 0 ? "0" : $"{minimum.Y:0.######}";
            ResultTextBlock.Text += $"Минимум {i + 1}:";
            ResultTextBlock.Text += $"  x = {xStr}";
            ResultTextBlock.Text += $"  f(x) = {yStr}\n";
          }

          ResultTextBlock.Foreground = System.Windows.Media.Brushes.Green;
        }
      }
      catch (OperationCanceledException) {
        ResultTextBlock.Text = "❌ Вычисление отменено";
        ResultTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
      }
      catch (Exception ex) {
        ResultTextBlock.Text = $"❌ Ошибка: {ex.Message}";
        ResultTextBlock.Foreground = System.Windows.Media.Brushes.Red;
      }
      finally {
        _isCalculating = false;
        CalculateButton.IsEnabled = true;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
      }
    }

    private List<Point> FindAllMinima(double a, double b, double epsilon, string functionText, CancellationToken cancellationToken = default) {
      var minima = new List<Point>();
      int divisions = 100;
      double step = (b - a) / divisions;

      for (int i = 0; i < divisions; i++) {
        cancellationToken.ThrowIfCancellationRequested();

        double x1 = a + i * step;
        double x2 = x1 + step;

        // Пропускаем интервалы, которые содержат разрывы
        if (HasDiscontinuity(x1, x2, functionText))
          continue;

        bool f1Valid = IsFunctionDefined(x1, functionText);
        bool f2Valid = IsFunctionDefined(x2, functionText);

        if (f1Valid && f2Valid) {
          try {
            Point minimum = GoldenSectionSearch(x1, x2, epsilon, functionText);

            // Фильтруем ложные минимумы
            if (IsRealMinimum(minimum, functionText, epsilon)) {
              if (!IsMinimumAlreadyFound(minima, minimum, 0.001)) {
                minima.Add(minimum);
              }
            }
          }
          catch {
            // Пропускаем подынтервалы, где метод не срабатывает
          }
        }
      }

      return minima.OrderBy(m => m.X).ToList();
    }

    // Проверка на разрыв функции в интервале
    private bool HasDiscontinuity(double a, double b, string functionText) {
      // Проверяем несколько точек в интервале на резкие изменения
      int testPoints = 5;
      double step = (b - a) / testPoints;

      double? prevValue = null;

      for (int i = 0; i <= testPoints; i++) {
        double x = a + i * step;
        try {
          double y = CalculateFunction(x, functionText);

          if (double.IsInfinity(y) || double.IsNaN(y))
            return true;

          if (prevValue.HasValue) {
            // Если резкий скачок значения - вероятно разрыв
            if (Math.Abs(y - prevValue.Value) > 1000)
              return true;
          }

          prevValue = y;
        }
        catch {
          return true;
        }
      }

      return false;
    }

    // Улучшенная проверка реального минимума
    private bool IsRealMinimum(Point candidate, string functionText, double epsilon) {
      try {
        double y = candidate.Y;

        // Отсекаем аномально большие значения
        if (Math.Abs(y) > 1000)
          return false;

        // Проверяем окрестности
        double h = epsilon * 10;
        double yLeft = CalculateFunction(candidate.X - h, functionText);
        double yRight = CalculateFunction(candidate.X + h, functionText);

        // Должно быть меньше соседних значений
        return y < yLeft && y < yRight;
      }
      catch {
        return false;
      }
    }

    private Point GoldenSectionSearch(double a, double b, double epsilon, string functionText) {
      double x1 = b - (b - a) * GoldenRatio;
      double x2 = a + (b - a) * GoldenRatio;

      double f1 = CalculateFunction(x1, functionText);
      double f2 = CalculateFunction(x2, functionText);

      int iterations = 0;
      while (Math.Abs(b - a) > epsilon && iterations < 1000) {
        iterations++;

        if (f1 < f2) {
          b = x2;
          x2 = x1;
          f2 = f1;
          x1 = b - (b - a) * GoldenRatio;
          f1 = CalculateFunction(x1, functionText);
        } else {
          a = x1;
          x1 = x2;
          f1 = f2;
          x2 = a + (b - a) * GoldenRatio;
          f2 = CalculateFunction(x2, functionText);
        }
      }

      double minX = (a + b) / 2;
      double minY = CalculateFunction(minX, functionText);

      return new Point(minX, minY);
    }

    private bool IsMinimumAlreadyFound(List<Point> minima, Point candidate, double tolerance) {
      foreach (var minimum in minima) {
        if (Math.Abs(minimum.X - candidate.X) < tolerance)
          return true;
      }
      return false;
    }

    private void PlotFunction(double a, double b, string functionText, List<Point> minima) {
      try {
        var plotModel = new PlotModel {
          Title = $"f(x) = {functionText}",
          TitleFontSize = 14,
          TitleColor = OxyColors.DarkBlue,
          PlotMargins = new OxyThickness(50, 20, 20, 40)
        };

        // Собираем точки для графика, разбивая на сегменты для избежания соединения через разрывы
        var functionSeries = new LineSeries {
          Color = OxyColors.Blue,
          StrokeThickness = 2,
          Title = "f(x)"
        };

        int pointsCount = 500;
        double step = (b - a) / pointsCount;

        // Определяем пороги для отсечения асимптот
        double yCutoff = 100; // Максимальное отображаемое значение по Y

        List<DataPoint> currentSegment = new List<DataPoint>();

        for (int i = 0; i <= pointsCount; i++) {
          double x = a + i * step;
          try {
            double y = CalculateFunction(x, functionText);

            // Пропускаем точки с асимптотическим поведением
            if (double.IsInfinity(y) || double.IsNaN(y) || Math.Abs(y) > yCutoff) {
              // Если в текущем сегменте есть точки, добавляем его и начинаем новый
              if (currentSegment.Count > 0) {
                foreach (var point in currentSegment) {
                  functionSeries.Points.Add(point);
                }
                currentSegment.Clear();
              }
              continue;
            }

            currentSegment.Add(new DataPoint(x, y));
          }
          catch {
            // При ошибке завершаем текущий сегмент
            if (currentSegment.Count > 0) {
              foreach (var point in currentSegment) {
                functionSeries.Points.Add(point);
              }
              currentSegment.Clear();
            }
          }
        }

        // Добавляем последний сегмент
        if (currentSegment.Count > 0) {
          foreach (var point in currentSegment) {
            functionSeries.Points.Add(point);
          }
        }

        // Автоматически определяем разумные пределы для осей
        double xMin = a;
        double xMax = b;

        // Собираем все Y значения для определения диапазона
        var allYValues = new List<double>();
        foreach (var point in functionSeries.Points) {
          allYValues.Add(point.Y);
        }

        double yMin = allYValues.Count > 0 ? allYValues.Min() : -10;
        double yMax = allYValues.Count > 0 ? allYValues.Max() : 10;

        // Добавляем Y значения из минимумов
        foreach (var minimum in minima) {
          if (Math.Abs(minimum.Y) <= yCutoff) {
            allYValues.Add(minimum.Y);
          }
        }

        // Пересчитываем с учетом минимумов
        if (allYValues.Count > 0) {
          yMin = allYValues.Min();
          yMax = allYValues.Max();
        }

        // Добавляем немного отступов
        double yRange = yMax - yMin;
        if (yRange == 0)
          yRange = 1;
        yMin -= yRange * 0.1;
        yMax += yRange * 0.1;

        // Настройка оси X
        var xAxis = new LinearAxis {
          Position = AxisPosition.Bottom,
          Title = "x",
          TitleColor = OxyColors.Black,
          AxislineColor = OxyColors.Black,
          MajorGridlineColor = OxyColors.LightGray,
          MajorGridlineStyle = LineStyle.Dot,
          Minimum = xMin,
          Maximum = xMax,
          MajorStep = CalculateReasonableStep(xMin, xMax)
        };

        // Настройка оси Y с ограничениями
        var yAxis = new LinearAxis {
          Position = AxisPosition.Left,
          Title = "f(x)",
          TitleColor = OxyColors.Black,
          AxislineColor = OxyColors.Black,
          MajorGridlineColor = OxyColors.LightGray,
          MajorGridlineStyle = LineStyle.Dot,
          Minimum = yMin,
          Maximum = yMax,
          MajorStep = CalculateReasonableStep(yMin, yMax)
        };

        plotModel.Axes.Add(xAxis);
        plotModel.Axes.Add(yAxis);

        plotModel.Series.Add(functionSeries);

        // Линия y = 0
        var zeroLine = new LineSeries {
          Color = OxyColors.Gray,
          StrokeThickness = 1,
          LineStyle = LineStyle.Dash
        };
        zeroLine.Points.Add(new DataPoint(xMin, 0));
        zeroLine.Points.Add(new DataPoint(xMax, 0));
        plotModel.Series.Add(zeroLine);

        // Точки минимумов (только те, которые в разумных пределах)
        if (minima.Count > 0) {
          var minimaSeries = new ScatterSeries {
            MarkerType = MarkerType.Circle,
            MarkerSize = 6,
            MarkerFill = OxyColors.Red,
            MarkerStroke = OxyColors.DarkRed,
            MarkerStrokeThickness = 2,
            Title = "Минимумы"
          };

          foreach (Point minimum in minima) {
            // Показываем только минимумы в разумных пределах
            if (Math.Abs(minimum.Y) <= yCutoff) {
              minimaSeries.Points.Add(new ScatterPoint(minimum.X, minimum.Y));

              var annotation = new OxyPlot.Annotations.PointAnnotation {
                X = minimum.X,
                Y = minimum.Y,
                Text = $"({minimum.X:0.###}, {minimum.Y:0.###})",
                TextColor = OxyColors.DarkRed,
                FontSize = 10,
                Stroke = OxyColors.Red,
                StrokeThickness = 1
              };
              plotModel.Annotations.Add(annotation);
            }
          }

          if (minimaSeries.Points.Count > 0) {
            plotModel.Series.Add(minimaSeries);
          }
        }

        PlotView.Model = plotModel;
      }
      catch (Exception ex) {
        MessageBox.Show($"Ошибка при построении графика: {ex.Message}");
      }
    }

    // Вспомогательный метод для расчета разумного шага сетки
    private double CalculateReasonableStep(double min, double max) {
      double range = max - min;
      if (range <= 0)
        return 1;

      double step = Math.Pow(10, Math.Floor(Math.Log10(range)));

      // Подбираем оптимальный шаг
      if (range / step > 10)
        step *= 2;
      if (range / step > 20)
        step *= 2;
      if (range / step < 3)
        step /= 2;

      return Math.Max(step, 0.1); // Минимальный шаг
    }

    // Вспомогательные методы (такие же как в DichotomyMethod)
    private double ParseDouble(string text) {
      return double.Parse(text.Replace(',', '.'), CultureInfo.InvariantCulture);
    }

    private double CalculateFunction(double x, string functionText) {
      try {
        Expression expression = new Expression(functionText);
        expression.Parameters["x"] = x;

        // Настраиваем функции
        expression.EvaluateFunction += delegate (string name, FunctionArgs args)
        {
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

        object result = expression.Evaluate();

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

    private bool IsFunctionDefined(double x, string functionText) {
      try {
        CalculateFunction(x, functionText);
        return true;
      }
      catch {
        return false;
      }
    }
  }
}