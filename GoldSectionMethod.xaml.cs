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
using System.Threading.Tasks;
using Expression = NCalc.Expression;

namespace MultiWindowApp {
  public partial class GoldenSectionMethod : Window {
    private CancellationTokenSource _cancellationTokenSource;
    private bool _isCalculating = false;

    private const double GoldenRatio = 0.618033988749895; // (sqrt(5) - 1) / 2

    // Типы поиска
    private enum SearchType { Minimum, Maximum, Root }

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
        string functionText = PreprocessFunction(FunctionTextBox.Text);

        // Определяем тип поиска
        SearchType searchType = SearchType.Minimum;
        if (MaxRadioButton.IsChecked == true)
          searchType = SearchType.Maximum;
        else if (RootRadioButton.IsChecked == true)
          searchType = SearchType.Root;

        // Валидация входных данных
        if (a >= b) {
          MessageBox.Show("a должно быть меньше b!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        if (epsilon <= 0) {
          MessageBox.Show("Точность должна быть положительным числом!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        // Проверяем функцию в нескольких точках
        if (!IsFunctionValidInInterval(a, b, functionText)) {
          MessageBox.Show("Функция содержит ошибки или не определена на интервале!", "Ошибка в функции", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        // Запускаем вычисления в отдельной задаче
        List<Point> results;
        if (searchType == SearchType.Root) {
          results = await Task.Run(() => FindAllRoots(a, b, epsilon, functionText, _cancellationTokenSource.Token));
        } else {
          results = await Task.Run(() => FindAllExtrema(a, b, epsilon, functionText, searchType, _cancellationTokenSource.Token));
        }

        // Проверяем не была ли отмена
        if (_cancellationTokenSource.Token.IsCancellationRequested) {
          ResultTextBlock.Text = "❌ Вычисление отменено";
          ResultTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
          return;
        }

        // Построение графика и вывод результатов в UI потоке
        PlotFunction(a, b, functionText, results, searchType);

        if (results.Count == 0) {
          string message = searchType == SearchType.Root
            ? "❌ На заданном интервале корней не найдено"
            : "❌ На заданном интервале экстремумов не найдено";
          ResultTextBlock.Text = message;
          ResultTextBlock.Foreground = System.Windows.Media.Brushes.Red;
        } else {
          // Фильтруем и форматируем результаты
          var displayResults = results
              .Select(m => new {
                X = Math.Abs(m.X) < 1e-10 ? 0 : Math.Round(m.X, 6),
                Y = Math.Abs(m.Y) < 1e-10 ? 0 : Math.Round(m.Y, 6)
              })
              .Distinct()
              .OrderBy(m => m.X)
              .ToList();

          string typeName = searchType == SearchType.Root ? "корней" :
                           (searchType == SearchType.Minimum ? "минимумов" : "максимумов");
          ResultTextBlock.Text = $"✓ Найдено {typeName}: {displayResults.Count}\n\n";

          for (int i = 0; i < displayResults.Count; i++) {
            var result = displayResults[i];
            string xStr = result.X == 0 ? "0" : $"{result.X:0.######}";
            string yStr = result.Y == 0 ? "0" : $"{result.Y:0.######}";

            if (searchType == SearchType.Root) {
              ResultTextBlock.Text += $"Корень {i + 1}: x = {xStr}\n";
            } else {
              ResultTextBlock.Text += $"{(searchType == SearchType.Minimum ? "Минимум" : "Максимум")} {i + 1}: ";
              ResultTextBlock.Text += $"x = {xStr}, f(x) = {yStr}\n";
            }
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

    // Предобработка функции для поддержки разных форматов
    private string PreprocessFunction(string functionText) {
      string result = functionText.Trim();

      // Заменяем ^ на pow
      result = System.Text.RegularExpressions.Regex.Replace(result, @"(\w+)\^(\d+)", "pow($1,$2)");
      result = System.Text.RegularExpressions.Regex.Replace(result, @"(\d+)\^(\w+)", "pow($1,$2)");
      result = System.Text.RegularExpressions.Regex.Replace(result, @"(\w+)\^\(([^)]+)\)", "pow($1,$2)");

      // Заменяем 1/x на деление
      result = result.Replace("1/x", "(1)/(x)");

      return result;
    }

    private List<Point> FindAllExtrema(double a, double b, double epsilon, string functionText, SearchType searchType, CancellationToken cancellationToken = default) {
      var extrema = new List<Point>();
      int divisions = 200; // Увеличиваем для лучшего поиска
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
            Point extremum;
            if (searchType == SearchType.Maximum) {
              // Ищем максимум как минимум отрицательной функции
              extremum = GoldenSectionSearch(x1, x2, epsilon, functionText, SearchType.Maximum);
            } else {
              extremum = GoldenSectionSearch(x1, x2, epsilon, functionText, SearchType.Minimum);
            }

            // Фильтруем ложные экстремумы
            if (IsRealExtremum(extremum, functionText, epsilon, searchType)) {
              if (!IsExtremumAlreadyFound(extrema, extremum, 0.001)) {
                extrema.Add(extremum);
              }
            }
          }
          catch {
            // Пропускаем подынтервалы, где метод не срабатывает
          }
        }
      }

      return extrema.OrderBy(m => m.X).ToList();
    }

    private List<Point> FindAllRoots(double a, double b, double epsilon, string functionText, CancellationToken cancellationToken = default) {
      var roots = new List<Point>();
      int divisions = 500; // Больше делений для поиска корней
      double step = (b - a) / divisions;

      double? prevY = null;
      double prevX = a;

      for (int i = 0; i <= divisions; i++) {
        cancellationToken.ThrowIfCancellationRequested();

        double x = a + i * step;

        try {
          double y = CalculateFunction(x, functionText);

          if (prevY.HasValue) {
            // Проверяем смену знака
            if (prevY.Value * y <= 0) {
              // Нашли интервал, где функция меняет знак
              try {
                double root = FindRootByBisection(prevX, x, epsilon, functionText, cancellationToken);
                double rootY = CalculateFunction(root, functionText);

                if (Math.Abs(rootY) < 1000) { // Отсекаем асимптоты
                  Point rootPoint = new Point(root, rootY);
                  if (!IsRootAlreadyFound(roots, rootPoint, epsilon)) {
                    roots.Add(rootPoint);
                  }
                }
              }
              catch {
                // Пропускаем если не удалось найти корень
              }
            }
          }

          prevY = y;
          prevX = x;
        }
        catch {
          // При разрыве сбрасываем предыдущее значение
          prevY = null;
        }
      }

      return roots.OrderBy(r => r.X).ToList();
    }

    private double FindRootByBisection(double a, double b, double epsilon, string functionText, CancellationToken cancellationToken) {
      double fa = CalculateFunction(a, functionText);
      double fb = CalculateFunction(b, functionText);

      if (Math.Abs(fa) < epsilon)
        return a;
      if (Math.Abs(fb) < epsilon)
        return b;

      if (fa * fb > 0) {
        throw new ArgumentException("Функция не меняет знак на интервале");
      }

      double mid;
      double fmid;
      int maxIterations = 1000;

      for (int i = 0; i < maxIterations; i++) {
        cancellationToken.ThrowIfCancellationRequested();

        mid = (a + b) / 2;
        fmid = CalculateFunction(mid, functionText);

        if (Math.Abs(fmid) < epsilon || Math.Abs(b - a) < epsilon) {
          return mid;
        }

        if (fa * fmid < 0) {
          b = mid;
          fb = fmid;
        } else {
          a = mid;
          fa = fmid;
        }
      }

      return (a + b) / 2;
    }

    // Проверка на разрыв функции в интервале
    private bool HasDiscontinuity(double a, double b, string functionText) {
      int testPoints = 10;
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

    private bool IsFunctionValidInInterval(double a, double b, string functionText) {
      // Проверяем функцию в нескольких точках
      double[] testPoints = { a, (a + b) / 2, b };

      foreach (double x in testPoints) {
        try {
          CalculateFunction(x, functionText);
        }
        catch {
          return false;
        }
      }

      return true;
    }

    // Улучшенная проверка реального экстремума
    private bool IsRealExtremum(Point candidate, string functionText, double epsilon, SearchType searchType) {
      try {
        double y = candidate.Y;

        // Отсекаем аномально большие значения
        if (Math.Abs(y) > 1000)
          return false;

        // Проверяем окрестности
        double h = epsilon * 10;
        double yLeft = CalculateFunction(candidate.X - h, functionText);
        double yRight = CalculateFunction(candidate.X + h, functionText);

        if (searchType == SearchType.Minimum) {
          return y < yLeft && y < yRight;
        } else { // Maximum
          return y > yLeft && y > yRight;
        }
      }
      catch {
        return false;
      }
    }

    private Point GoldenSectionSearch(double a, double b, double epsilon, string functionText, SearchType searchType) {
      double x1 = b - (b - a) * GoldenRatio;
      double x2 = a + (b - a) * GoldenRatio;

      double f1 = CalculateFunction(x1, functionText);
      double f2 = CalculateFunction(x2, functionText);

      // Для максимума инвертируем сравнение
      int compareFactor = (searchType == SearchType.Maximum) ? -1 : 1;

      int iterations = 0;
      while (Math.Abs(b - a) > epsilon && iterations < 1000) {
        iterations++;

        if (f1 * compareFactor < f2 * compareFactor) {
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

      double extremumX = (a + b) / 2;
      double extremumY = CalculateFunction(extremumX, functionText);

      return new Point(extremumX, extremumY);
    }

    private bool IsExtremumAlreadyFound(List<Point> extrema, Point candidate, double tolerance) {
      foreach (var extremum in extrema) {
        if (Math.Abs(extremum.X - candidate.X) < tolerance)
          return true;
      }
      return false;
    }

    private bool IsRootAlreadyFound(List<Point> roots, Point candidate, double tolerance) {
      foreach (var root in roots) {
        if (Math.Abs(root.X - candidate.X) < tolerance)
          return true;
      }
      return false;
    }

    private void PlotFunction(double a, double b, string functionText, List<Point> points, SearchType searchType) {
      try {
        string title = $"f(x) = {FunctionTextBox.Text}";
        var plotModel = new PlotModel {
          Title = title,
          TitleFontSize = 14,
          TitleColor = OxyColors.DarkBlue,
          PlotMargins = new OxyThickness(50, 20, 20, 40)
        };

        // Собираем точки для графика
        var functionSeries = new LineSeries {
          Color = OxyColors.Blue,
          StrokeThickness = 2,
          Title = "f(x)"
        };

        int pointsCount = 1000; // Увеличиваем количество точек для точного отображения
        double step = (b - a) / pointsCount;

        // Определяем пороги для отсечения асимптот
        double yCutoff = 50;

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

        // Добавляем Y значения из точек
        foreach (var point in points) {
          if (Math.Abs(point.Y) <= yCutoff) {
            allYValues.Add(point.Y);
          }
        }

        // Пересчитываем с учетом точек
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

        // Точки (экстремумы или корни)
        if (points.Count > 0) {
          var pointSeries = new ScatterSeries {
            MarkerType = MarkerType.Circle,
            MarkerSize = 6,
            MarkerStrokeThickness = 2,
            Title = searchType == SearchType.Root ? "Корни" :
                   (searchType == SearchType.Minimum ? "Минимумы" : "Максимумы")
          };

          // Устанавливаем цвет в зависимости от типа поиска
          if (searchType == SearchType.Root) {
            pointSeries.MarkerFill = OxyColors.Green;
            pointSeries.MarkerStroke = OxyColors.DarkGreen;
          } else if (searchType == SearchType.Minimum) {
            pointSeries.MarkerFill = OxyColors.Red;
            pointSeries.MarkerStroke = OxyColors.DarkRed;
          } else {
            pointSeries.MarkerFill = OxyColors.Orange;
            pointSeries.MarkerStroke = OxyColors.DarkOrange;
          }

          foreach (Point point in points) {
            // Показываем только точки в разумных пределах
            if (Math.Abs(point.Y) <= yCutoff) {
              pointSeries.Points.Add(new ScatterPoint(point.X, point.Y));

              string annotationText = searchType == SearchType.Root
                ? $"({point.X:0.###}, 0)"
                : $"({point.X:0.###}, {point.Y:0.###})";

              var annotation = new OxyPlot.Annotations.PointAnnotation {
                X = point.X,
                Y = point.Y,
                Text = annotationText,
                TextColor = pointSeries.MarkerStroke,
                FontSize = 10,
                Stroke = pointSeries.MarkerStroke,
                StrokeThickness = 1
              };
              plotModel.Annotations.Add(annotation);
            }
          }

          if (pointSeries.Points.Count > 0) {
            plotModel.Series.Add(pointSeries);
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

      return Math.Max(step, 0.1);
    }

    // Вспомогательные методы
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
          else if (name == "log" || name == "ln")
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