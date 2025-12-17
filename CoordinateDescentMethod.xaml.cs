using NCalc;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static MultiWindowApp.CoordinateDescentMethod;
using Expression = NCalc.Expression;

namespace MultiWindowApp {
  public partial class CoordinateDescentMethod : Window {
    private CancellationTokenSource _cancellationTokenSource;
    private bool _isCalculating = false;
    private List<IterationPoint> _iterationHistory = new List<IterationPoint>();

    public class IterationPoint {
      public double X { get; set; }
      public double Y { get; set; }
      public double Z { get; set; }
      public double Value { get; set; }
      public int Iteration { get; set; }
    }

    public CoordinateDescentMethod() {
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
        ResultTextBlock.Foreground = Brushes.Blue;
        ProgressTextBlock.Text = "Инициализация...";

        // Создаем токен отмены
        _cancellationTokenSource = new CancellationTokenSource();

        // Проверка заполнения полей
        if (string.IsNullOrWhiteSpace(FunctionTextBox.Text) ||
            string.IsNullOrWhiteSpace(TextBoxX0.Text) ||
            string.IsNullOrWhiteSpace(TextBoxY0.Text)) {
          MessageBox.Show("Заполните функцию и начальные точки x0, y0!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
          return;
        }

        // Получаем параметры
        string functionText = PreprocessFunction(FunctionTextBox.Text);
        double x0 = ParseDouble(TextBoxX0.Text);
        double y0 = ParseDouble(TextBoxY0.Text);
        double z0 = string.IsNullOrWhiteSpace(TextBoxZ0.Text) ? 0 : ParseDouble(TextBoxZ0.Text);

        double epsilon = ParseDouble(TextBoxEpsilon.Text);
        int maxIterations = int.Parse(TextBoxMaxIterations.Text);
        double stepSize = ParseDouble(TextBoxStepSize.Text);

        // Область поиска
        double xMin = ParseDouble(TextBoxXmin.Text);
        double xMax = ParseDouble(TextBoxXmax.Text);
        double yMin = ParseDouble(TextBoxYmin.Text);
        double yMax = ParseDouble(TextBoxYmax.Text);
        double zMin = ParseDouble(TextBoxZmin.Text);
        double zMax = ParseDouble(TextBoxZmax.Text);

        // Валидация
        if (epsilon <= 0) {
          MessageBox.Show("Точность должна быть положительным числом!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        if (maxIterations <= 0) {
          MessageBox.Show("Максимальное число итераций должно быть положительным!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        if (stepSize <= 0) {
          MessageBox.Show("Шаг спуска должен быть положительным!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        // Проверяем начальную точку
        if (!IsPointInBounds(x0, y0, z0, xMin, xMax, yMin, yMax, zMin, zMax)) {
          MessageBox.Show("Начальная точка находится вне области поиска!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        // Определяем количество переменных
        int variableCount = DetermineVariableCount(functionText);

        // Запускаем метод покоординатного спуска
        var result = await Task.Run(() =>
            CoordinateDescentOptimization(functionText, x0, y0, z0, epsilon, maxIterations, stepSize,
                                         xMin, xMax, yMin, yMax, zMin, zMax, variableCount,
                                         _cancellationTokenSource.Token));

        // Проверяем не была ли отмена
        if (_cancellationTokenSource.Token.IsCancellationRequested) {
          ResultTextBlock.Text = "❌ Вычисление отменено";
          ResultTextBlock.Foreground = Brushes.Orange;
          return;
        }

        // Выводим результаты
        DisplayResults(result, variableCount);

        // Строим график (для 2D функций)
        if (variableCount == 2) {
          Plot2DFunction(xMin, xMax, yMin, yMax, functionText, result);
        } else {
          PlotView.Model = null;
        }

      }
      catch (OperationCanceledException) {
        ResultTextBlock.Text = "❌ Вычисление отменено";
        ResultTextBlock.Foreground = Brushes.Orange;
      }
      catch (Exception ex) {
        ResultTextBlock.Text = $"❌ Ошибка: {ex.Message}";
        ResultTextBlock.Foreground = Brushes.Red;
      }
      finally {
        _isCalculating = false;
        CalculateButton.IsEnabled = true;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
      }
    }

    private CoordinateDescentResult CoordinateDescentOptimization(string functionText, double x0, double y0, double z0,
                                                                  double epsilon, int maxIterations, double stepSize,
                                                                  double xMin, double xMax, double yMin, double yMax,
                                                                  double zMin, double zMax, int variableCount,
                                                                  CancellationToken cancellationToken) {

      _iterationHistory.Clear();
      double[] currentPoint = variableCount >= 3 ? new double[] { x0, y0, z0 } : new double[] { x0, y0 };
      double currentValue = CalculateFunction(currentPoint, functionText);

      _iterationHistory.Add(new IterationPoint {
        X = currentPoint[0],
        Y = currentPoint.Length > 1 ? currentPoint[1] : 0,
        Z = currentPoint.Length > 2 ? currentPoint[2] : 0,
        Value = currentValue,
        Iteration = 0
      });

      double[] previousPoint = (double[])currentPoint.Clone();
      double previousValue = currentValue;
      int iteration = 0;
      bool converged = false;

      // Основной цикл метода покоординатного спуска
      while (iteration < maxIterations && !converged) {
        cancellationToken.ThrowIfCancellationRequested();

        iteration++;

        // Обновляем Progress в UI потоке (через Dispatcher)
        Application.Current.Dispatcher.Invoke(() => {
          ProgressTextBlock.Text = $"Итерация {iteration}: f = {currentValue:0.######}";
        });

        // Запоминаем предыдущую точку
        previousPoint = (double[])currentPoint.Clone();
        previousValue = currentValue;

        // Цикл по координатам
        for (int coord = 0; coord < variableCount; coord++) {
          // Пробуем шаг в отрицательном направлении
          double[] testPointNeg = (double[])currentPoint.Clone();
          testPointNeg[coord] -= stepSize;

          // Проверяем границы
          testPointNeg[coord] = Math.Max(GetMinBound(coord, xMin, yMin, zMin),
                                         Math.Min(GetMaxBound(coord, xMax, yMax, zMax), testPointNeg[coord]));

          double valueNeg = CalculateFunction(testPointNeg, functionText);

          // Пробуем шаг в положительном направлении
          double[] testPointPos = (double[])currentPoint.Clone();
          testPointPos[coord] += stepSize;

          // Проверяем границы
          testPointPos[coord] = Math.Max(GetMinBound(coord, xMin, yMin, zMin),
                                         Math.Min(GetMaxBound(coord, xMax, yMax, zMax), testPointPos[coord]));

          double valuePos = CalculateFunction(testPointPos, functionText);

          // Выбираем направление, дающее наименьшее значение функции
          if (valueNeg < currentValue && valueNeg < valuePos) {
            currentPoint[coord] = testPointNeg[coord];
            currentValue = valueNeg;
          } else if (valuePos < currentValue) {
            currentPoint[coord] = testPointPos[coord];
            currentValue = valuePos;
          }
          // Если оба хуже, остаемся на месте
        }

        // Добавляем в историю
        _iterationHistory.Add(new IterationPoint {
          X = currentPoint[0],
          Y = currentPoint.Length > 1 ? currentPoint[1] : 0,
          Z = currentPoint.Length > 2 ? currentPoint[2] : 0,
          Value = currentValue,
          Iteration = iteration
        });

        // Проверка сходимости
        double pointChange = 0;
        for (int i = 0; i < variableCount; i++) {
          pointChange += Math.Pow(currentPoint[i] - previousPoint[i], 2);
        }
        pointChange = Math.Sqrt(pointChange);

        double valueChange = Math.Abs(currentValue - previousValue);

        if (pointChange < epsilon && valueChange < epsilon) {
          converged = true;
        }
      }

      return new CoordinateDescentResult {
        Point = currentPoint,
        Value = currentValue,
        Iterations = iteration,
        Converged = converged,
        History = _iterationHistory
      };
    }

    private double GetMinBound(int coord, double xMin, double yMin, double zMin) {
      return coord == 0 ? xMin : (coord == 1 ? yMin : zMin);
    }

    private double GetMaxBound(int coord, double xMax, double yMax, double zMax) {
      return coord == 0 ? xMax : (coord == 1 ? yMax : zMax);
    }

    private bool IsPointInBounds(double x, double y, double z, double xMin, double xMax, double yMin, double yMax, double zMin, double zMax) {
      return x >= xMin && x <= xMax && y >= yMin && y <= yMax && z >= zMin && z <= zMax;
    }
    private int DetermineVariableCount(string functionText) {
      bool hasX = Regex.IsMatch(functionText, @"(^|[^a-zA-Z])x([^a-zA-Z]|$)");
      bool hasY = Regex.IsMatch(functionText, @"(^|[^a-zA-Z])y([^a-zA-Z]|$)");
      bool hasZ = Regex.IsMatch(functionText, @"(^|[^a-zA-Z])z([^a-zA-Z]|$)");

      int count = 0;
      if (hasX)
        count++;
      if (hasY)
        count++;
      if (hasZ)
        count++;

      return Math.Max(count, 2); // Минимум 2 для совместимости
    }

    private void DisplayResults(CoordinateDescentResult result, int variableCount) {
      if (result.Converged) {
        ResultTextBlock.Text = $"✓ Минимум найден за {result.Iterations} итераций\n\n";
        ResultTextBlock.Foreground = Brushes.Green;
      } else {
        ResultTextBlock.Text = $"⚠ Достигнуто максимальное число итераций ({result.Iterations})\n\n";
        ResultTextBlock.Foreground = Brushes.Orange;
      }

      ResultTextBlock.Text += $"Значение функции: f = {result.Value:0.##########}\n\n";
      ResultTextBlock.Text += $"Точка минимума:\n";
      ResultTextBlock.Text += $"  x = {result.Point[0]:0.##########}\n";

      if (variableCount >= 2) {
        ResultTextBlock.Text += $"  y = {result.Point[1]:0.##########}\n";
      }

      if (variableCount >= 3) {
        ResultTextBlock.Text += $"  z = {result.Point[2]:0.##########}\n";
      }

      // Добавляем историю последних итераций
      if (_iterationHistory.Count > 0) {
        ResultTextBlock.Text += $"\nПоследние итерации:\n";
        int start = Math.Max(0, _iterationHistory.Count - 5);
        for (int i = start; i < _iterationHistory.Count; i++) {
          var point = _iterationHistory[i];
          ResultTextBlock.Text += $"  {point.Iteration}: f({point.X:0.###}, {point.Y:0.###}) = {point.Value:0.######}\n";
        }
      }
    }

    private void Plot2DFunction(double xMin, double xMax, double yMin, double yMax,
                               string functionText, CoordinateDescentResult result) {
      try {
        var plotModel = new PlotModel {
          Title = $"f(x,y) = {FunctionTextBox.Text}",
          TitleFontSize = 14,
          TitleColor = OxyColors.DarkBlue,
          PlotMargins = new OxyThickness(50, 20, 20, 40)
        };

        // Вместо ContourSeries используем HeatMap или обычный график
        // Для простоты создадим сетку точек и построим поверхность через LineSeries

        int gridSize = 30;
        double xStep = (xMax - xMin) / gridSize;
        double yStep = (yMax - yMin) / gridSize;

        // Создаем несколько серий для разных уровней Y
        for (int j = 0; j <= gridSize; j++) {
          double y = yMin + j * yStep;
          var series = new LineSeries {
            Title = $"y = {y:0.#}",
            Color = OxyColors.LightBlue
          };

          for (int i = 0; i <= gridSize; i++) {
            double x = xMin + i * xStep;
            try {
              double value = CalculateFunction(new double[] { x, y }, functionText);
              if (!double.IsInfinity(value) && !double.IsNaN(value) && Math.Abs(value) < 100) {
                series.Points.Add(new DataPoint(x, value));
              }
            }
            catch {
              // Пропускаем точки с ошибками
            }
          }

          if (series.Points.Count > 1) {
            plotModel.Series.Add(series);
          }
        }

        // Добавляем траекторию спуска (значения функции вдоль пути)
        if (_iterationHistory.Count > 0) {
          var pathSeries = new LineSeries {
            Title = "Траектория спуска",
            Color = OxyColors.Red,
            StrokeThickness = 3,
            MarkerType = MarkerType.Circle,
            MarkerSize = 5,
            MarkerFill = OxyColors.Red
          };

          foreach (var point in _iterationHistory) {
            pathSeries.Points.Add(new DataPoint(point.X, point.Value));
          }

          plotModel.Series.Add(pathSeries);

          // Добавляем начальную и конечную точки
          var startPoint = _iterationHistory.First();
          var endPoint = _iterationHistory.Last();

          var pointsSeries = new ScatterSeries {
            MarkerType = MarkerType.Circle,
            MarkerSize = 8
          };

          pointsSeries.Points.Add(new ScatterPoint(startPoint.X, startPoint.Value, 10));
          pointsSeries.Points.Add(new ScatterPoint(endPoint.X, endPoint.Value, 10)авуе);

          plotModel.Series.Add(pointsSeries);

          // Аннотации
          var startAnnotation = new PointAnnotation {
            X = startPoint.X,
            Y = startPoint.Value,
            Text = "Начало",
            TextColor = OxyColors.Green,
            FontSize = 10
          };

          var endAnnotation = new PointAnnotation {
            X = endPoint.X,
            Y = endPoint.Value,
            Text = $"Конец\nf={endPoint.Value:0.###}",
            TextColor = OxyColors.Red,
            FontSize = 10
          };

          plotModel.Annotations.Add(startAnnotation);
          plotModel.Annotations.Add(endAnnotation);
        }

        // Настройка осей
        plotModel.Axes.Add(new LinearAxis {
          Position = AxisPosition.Bottom,
          Title = "x",
          Minimum = xMin,
          Maximum = xMax
        });

        plotModel.Axes.Add(new LinearAxis {
          Position = AxisPosition.Left,
          Title = "f(x,y)",
          Minimum = -10,
          Maximum = 10
        });

        PlotView.Model = plotModel;
      }
      catch (Exception ex) {
        MessageBox.Show($"Ошибка при построении графика: {ex.Message}");
      }
    }

    // Предобработка функции
    private string PreprocessFunction(string functionText) {
      string result = functionText.Trim();

      // Заменяем ^ на pow
      result = System.Text.RegularExpressions.Regex.Replace(result, @"(\w+)\^(\d+)", "pow($1,$2)");
      result = System.Text.RegularExpressions.Regex.Replace(result, @"(\d+)\^(\w+)", "pow($1,$2)");
      result = System.Text.RegularExpressions.Regex.Replace(result, @"(\w+)\^\(([^)]+)\)", "pow($1,$2)");

      return result;
    }

    // Вычисление функции для нескольких переменных
    private double CalculateFunction(double[] point, string functionText) {
      try {
        Expression expression = new Expression(functionText);

        // Добавляем параметры в зависимости от размерности точки
        expression.Parameters["x"] = point[0];
        if (point.Length > 1)
          expression.Parameters["y"] = point[1];
        if (point.Length > 2)
          expression.Parameters["z"] = point[2];

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
        return double.MaxValue; // Возвращаем большое число для точек разрыва
      }
      catch (Exception ex) {
        throw new ArgumentException($"Ошибка в функции: {ex.Message}");
      }
    }

    private double ParseDouble(string text) {
      return double.Parse(text.Replace(',', '.'), CultureInfo.InvariantCulture);
    }
  }

  public class CoordinateDescentResult {
    public double[] Point { get; set; }
    public double Value { get; set; }
    public int Iterations { get; set; }
    public bool Converged { get; set; }
    public List<IterationPoint> History { get; set; }
  }
}