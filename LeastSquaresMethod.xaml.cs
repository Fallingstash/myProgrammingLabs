using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using OxyPlot.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MultiWindowApp {
  public partial class LeastSquaresMethod : Window {
    private ObservableCollection<DataPointLocal> _dataPoints;
    private PlotModel _plotModel;

    public class DataPointLocal {
      public int Index { get; set; }
      public double X { get; set; }
      public double Y { get; set; }
    }

    public LeastSquaresMethod() {
      InitializeComponent();
      InitializeDataGrid();
      InitializePlotModel();
      WireUpEvents();

      // Устанавливаем значения по умолчанию для генерации
      MinXTextBox.Text = "0";
      MaxXTextBox.Text = "10";
      MinYBaseTextBox.Text = "0";
      MaxYBaseTextBox.Text = "10";
      PointsCountTextBox.Text = "10";
      NoiseLevelTextBox.Text = "1.0";
    }

    private void InitializeDataGrid() {
      _dataPoints = new ObservableCollection<DataPointLocal>
      {
                new DataPointLocal { Index = 1, X = 1.0, Y = 2.1 },
                new DataPointLocal { Index = 2, X = 2.0, Y = 3.2 },
                new DataPointLocal { Index = 3, X = 3.0, Y = 4.8 },
                new DataPointLocal { Index = 4, X = 4.0, Y = 6.1 },
                new DataPointLocal { Index = 5, X = 5.0, Y = 7.3 }
            };

      DataGridPoints.ItemsSource = _dataPoints;
    }

    private void InitializePlotModel() {
      _plotModel = new PlotModel {
        Title = "Метод наименьших квадратов",
        TitleFontSize = 14,
        TitleColor = OxyColors.DarkBlue,
        PlotMargins = new OxyThickness(50, 20, 20, 40),
        Background = OxyColors.White
      };

      _plotModel.Axes.Add(new LinearAxis {
        Position = AxisPosition.Bottom,
        Title = "X",
        MajorGridlineStyle = LineStyle.Dash,
        MinorGridlineStyle = LineStyle.Dot,
        MajorGridlineColor = OxyColors.LightGray,
        MinorGridlineColor = OxyColors.LightGray,
        MinimumPadding = 0.1,
        MaximumPadding = 0.1
      });

      _plotModel.Axes.Add(new LinearAxis {
        Position = AxisPosition.Left,
        Title = "Y",
        MajorGridlineStyle = LineStyle.Dash,
        MinorGridlineStyle = LineStyle.Dot,
        MajorGridlineColor = OxyColors.LightGray,
        MinorGridlineColor = OxyColors.LightGray,
        MinimumPadding = 0.1,
        MaximumPadding = 0.1
      });

      var plotView = new PlotView();
      plotView.Model = _plotModel;

      ChartContainer.Children.Clear();
      ChartContainer.Children.Add(plotView);
    }

    private void WireUpEvents() {
      LoadExcelButton.Click += LoadExcelButton_Click;
      GenerateDataButton.Click += GenerateDataButton_Click;
      ClearButton.Click += ClearButton_Click;
      CalculateButton.Click += CalculateButton_Click;
      BackButton.Click += BackButton_Click;

      DataGridPoints.CellEditEnding += (s, e) => UpdateIndices();

      PointsCountTextBox.TextChanged += (s, e) => ValidateNumericTextBox(PointsCountTextBox, 2, 1000);
      MinXTextBox.TextChanged += (s, e) => ValidateNumericTextBox(MinXTextBox, -1000, 1000);
      MaxXTextBox.TextChanged += (s, e) => ValidateNumericTextBox(MaxXTextBox, -1000, 1000);
      MinYBaseTextBox.TextChanged += (s, e) => ValidateNumericTextBox(MinYBaseTextBox, -1000, 1000);
      MaxYBaseTextBox.TextChanged += (s, e) => ValidateNumericTextBox(MaxYBaseTextBox, -1000, 1000);
      NoiseLevelTextBox.TextChanged += (s, e) => ValidateNumericTextBox(NoiseLevelTextBox, 0, 10);
    }

    private void ValidateNumericTextBox(TextBox textBox, double minValue, double maxValue) {
      if (string.IsNullOrWhiteSpace(textBox.Text)) {
        textBox.Background = Brushes.White;
        return;
      }

      string text = textBox.Text.Replace(',', '.');

      if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double value)) {
        if (value < minValue || value > maxValue) {
          textBox.Background = Brushes.LightPink;
          textBox.ToolTip = $"Значение должно быть от {minValue} до {maxValue}";
        } else {
          textBox.Background = Brushes.White;
          textBox.ToolTip = null;
        }
      } else {
        textBox.Background = Brushes.LightPink;
        textBox.ToolTip = "Введите число";
      }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) {
      this.Close();
    }

    private void LoadExcelButton_Click(object sender, RoutedEventArgs e) {
      try {
        OpenFileDialog openFileDialog = new OpenFileDialog {
          Filter = "CSV файлы (*.csv)|*.csv|Текстовые файлы (*.txt)|*.txt|Excel файлы (*.xlsx;*.xls)|*.xlsx;*.xls|Все файлы (*.*)|*.*",
          Title = "Выберите файл с данными",
          DefaultExt = ".csv",
          Multiselect = false
        };

        if (openFileDialog.ShowDialog() == true) {
          LoadDataFromFile(openFileDialog.FileName);
        }
      }
      catch (Exception ex) {
        MessageBox.Show($"Ошибка при загрузке файла: {ex.Message}",
                      "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void LoadGoogleButton_Click(object sender, RoutedEventArgs e) {
      try {
        // Показываем диалог для ввода данных Google Sheets
        var dialog = new GoogleSheetsInputDialog();
        if (dialog.ShowDialog() == true) {
          // Создаем CSV файл на основе введенных данных
          string csvContent = dialog.Data;
          string tempFile = Path.GetTempFileName() + ".csv";

          File.WriteAllText(tempFile, csvContent, Encoding.UTF8);

          // Загружаем данные из временного файла
          LoadDataFromFile(tempFile);

          // Удаляем временный файл
          File.Delete(tempFile);

          MessageBox.Show("Данные успешно загружены из Google Sheets (имитация)",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
      }
      catch (Exception ex) {
        MessageBox.Show($"Ошибка при загрузке из Google Sheets: {ex.Message}",
                      "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void LoadDataFromFile(string filePath) {
      try {
        string extension = Path.GetExtension(filePath).ToLower();

        if (extension == ".csv" || extension == ".txt") {
          LoadFromCsvFile(filePath);
        } else if (extension == ".xlsx" || extension == ".xls") {
          // Для Excel файлов просто показываем сообщение
          MessageBox.Show("Для загрузки Excel файлов используйте форматы CSV или TXT. " +
                        "Пожалуйста, экспортируйте данные из Excel в CSV формат.",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        } else {
          MessageBox.Show("Неподдерживаемый формат файла. Используйте CSV или TXT.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }
      catch (Exception ex) {
        MessageBox.Show($"Ошибка чтения файла: {ex.Message}",
                      "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void LoadFromCsvFile(string filePath) {
      try {
        _dataPoints.Clear();

        string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
        int index = 1;
        int lineNumber = 0;

        foreach (string line in lines) {
          lineNumber++;

          // Пропускаем пустые строки
          if (string.IsNullOrWhiteSpace(line))
            continue;

          // Пропускаем строки с заголовками
          if (line.ToLower().Contains("x") && line.ToLower().Contains("y") && lineNumber == 1)
            continue;

          // Пробуем разные разделители
          string[] parts = line.Split(new char[] { ',', ';', '\t', '|' },
                                    StringSplitOptions.RemoveEmptyEntries);

          if (parts.Length >= 2) {
            if (double.TryParse(parts[0].Replace(',', '.'), NumberStyles.Any,
                              CultureInfo.InvariantCulture, out double x) &&
                double.TryParse(parts[1].Replace(',', '.'), NumberStyles.Any,
                              CultureInfo.InvariantCulture, out double y)) {
              _dataPoints.Add(new DataPointLocal {
                Index = index++,
                X = Math.Round(x, 3),
                Y = Math.Round(y, 3)
              });
            }
          }
        }

        DataGridPoints.Items.Refresh();

        if (_dataPoints.Count == 0) {
          MessageBox.Show("Не удалось загрузить данные из файла. Проверьте формат файла.\n" +
                        "Ожидаемый формат: X,Y в каждой строке",
                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
        } else {
          MessageBox.Show($"Успешно загружено {_dataPoints.Count} точек из файла",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
      }
      catch (Exception ex) {
        MessageBox.Show($"Ошибка чтения файла: {ex.Message}",
                      "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void ExportToCsvButton_Click(object sender, RoutedEventArgs e) {
      try {
        SaveFileDialog saveFileDialog = new SaveFileDialog {
          Filter = "CSV файлы (*.csv)|*.csv|Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
          Title = "Экспорт данных",
          DefaultExt = ".csv",
          FileName = $"least_squares_data_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };

        if (saveFileDialog.ShowDialog() == true) {
          using (var writer = new StreamWriter(saveFileDialog.FileName, false, Encoding.UTF8)) {
            // Заголовок
            writer.WriteLine("X,Y");

            // Данные
            foreach (var point in _dataPoints) {
              writer.WriteLine($"{point.X.ToString(CultureInfo.InvariantCulture)},{point.Y.ToString(CultureInfo.InvariantCulture)}");
            }

            // Если есть результаты, добавляем их
            if (!string.IsNullOrEmpty(LinearResultText.Text) && LinearResultText.Text != "Не рассчитано") {
              writer.WriteLine();
              writer.WriteLine("# Результаты метода наименьших квадратов:");
              writer.WriteLine($"# Линейная аппроксимация: {LinearResultText.Text}");
              writer.WriteLine($"# R² = {LinearRSquaredText.Text}");

              if (!string.IsNullOrEmpty(QuadraticResultText.Text) && QuadraticResultText.Text != "Не рассчитано") {
                writer.WriteLine($"# Квадратичная аппроксимация: {QuadraticResultText.Text}");
                writer.WriteLine($"# R² = {QuadraticRSquaredText.Text}");
              }
            }
          }

          MessageBox.Show($"Данные успешно экспортированы в файл:\n{saveFileDialog.FileName}",
                        "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
        }
      }
      catch (Exception ex) {
        MessageBox.Show($"Ошибка при экспорте: {ex.Message}",
                      "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void GenerateDataButton_Click(object sender, RoutedEventArgs e) {
      try {
        if (!ValidateGenerationParameters())
          return;

        int pointsCount = int.Parse(PointsCountTextBox.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
        double minX = double.Parse(MinXTextBox.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
        double maxX = double.Parse(MaxXTextBox.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
        double minYBase = double.Parse(MinYBaseTextBox.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
        double maxYBase = double.Parse(MaxYBaseTextBox.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
        double noiseLevel = double.Parse(NoiseLevelTextBox.Text.Replace(',', '.'), CultureInfo.InvariantCulture);

        _dataPoints.Clear();

        Random rand = new Random();
        double xRange = maxX - minX;
        double yBaseRange = maxYBase - minYBase;

        for (int i = 0; i < pointsCount; i++) {
          double x = minX + (xRange * i) / Math.Max(1, pointsCount - 1);
          double yBase = minYBase + (yBaseRange * (x - minX) / Math.Max(1, xRange));
          double noise = (rand.NextDouble() - 0.5) * 2 * noiseLevel;
          double y = yBase + noise;

          _dataPoints.Add(new DataPointLocal {
            Index = i + 1,
            X = Math.Round(x, 3),
            Y = Math.Round(y, 3)
          });
        }

        DataGridPoints.Items.Refresh();
      }
      catch (Exception ex) {
        MessageBox.Show($"Ошибка генерации: {ex.Message}",
                      "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private bool ValidateGenerationParameters() {
      var textBoxes = new[]
      {
                (TextBox: PointsCountTextBox, Name: "Количество точек", IsInt: true),
                (TextBox: MinXTextBox, Name: "Минимум X", IsInt: false),
                (TextBox: MaxXTextBox, Name: "Максимум X", IsInt: false),
                (TextBox: MinYBaseTextBox, Name: "Базовый минимум Y", IsInt: false),
                (TextBox: MaxYBaseTextBox, Name: "Базовый максимум Y", IsInt: false),
                (TextBox: NoiseLevelTextBox, Name: "Уровень шума", IsInt: false)
            };

      foreach (var item in textBoxes) {
        string text = item.TextBox.Text?.Replace(',', '.');

        if (string.IsNullOrWhiteSpace(text)) {
          MessageBox.Show($"Введите значение для '{item.Name}'",
                        "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
          item.TextBox.Focus();
          return false;
        }

        if (item.IsInt) {
          if (!int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out int intValue) || intValue < 2) {
            MessageBox.Show($"Некорректное значение для '{item.Name}'. Должно быть целым числом не меньше 2",
                          "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
            item.TextBox.Focus();
            return false;
          }
        } else {
          if (!double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleValue)) {
            MessageBox.Show($"Некорректное значение для '{item.Name}'. Должно быть числом",
                          "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
            item.TextBox.Focus();
            return false;
          }
        }
      }

      double minX = double.Parse(MinXTextBox.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
      double maxX = double.Parse(MaxXTextBox.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
      double minY = double.Parse(MinYBaseTextBox.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
      double maxY = double.Parse(MaxYBaseTextBox.Text.Replace(',', '.'), CultureInfo.InvariantCulture);

      if (minX >= maxX) {
        MessageBox.Show("Минимальное значение X должно быть меньше максимального",
                      "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
        MinXTextBox.Focus();
        return false;
      }

      if (minY >= maxY) {
        MessageBox.Show("Базовый минимум Y должен быть меньше базового максимума Y",
                      "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
        MinYBaseTextBox.Focus();
        return false;
      }

      return true;
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e) {
      _dataPoints.Clear();
      DataGridPoints.Items.Refresh();
      ClearResults();
    }

    private void CalculateButton_Click(object sender, RoutedEventArgs e) {
      try {
        var points = GetValidPointsFromGrid();

        if (points.Count < 2) {
          MessageBox.Show("Недостаточно точек для расчета (нужно минимум 2)",
                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
          return;
        }

        var linearResult = CalculateLeastSquares(points, 1);
        DisplayLinearResults(linearResult);

        var quadraticResult = CalculateLeastSquares(points, 2);
        DisplayQuadraticResults(quadraticResult);

        PlotGraph(points, linearResult, quadraticResult);
      }
      catch (Exception ex) {
        MessageBox.Show($"Ошибка расчета: {ex.Message}",
                      "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private List<PointData> GetValidPointsFromGrid() {
      var points = new List<PointData>();

      foreach (var item in DataGridPoints.Items) {
        if (item is DataPointLocal dataPoint) {
          if (!double.IsNaN(dataPoint.X) && !double.IsNaN(dataPoint.Y) &&
              !double.IsInfinity(dataPoint.X) && !double.IsInfinity(dataPoint.Y)) {
            points.Add(new PointData(dataPoint.X, dataPoint.Y));
          }
        }
      }

      return points;
    }

    private (double[] coefficients, double rSquared) CalculateLeastSquares(List<PointData> points, int degree) {
      int n = points.Count;
      int m = degree + 1;

      double[,] A = new double[m, m];
      double[] b = new double[m];

      for (int i = 0; i < m; i++) {
        for (int j = 0; j < m; j++) {
          double sum = 0;
          for (int k = 0; k < n; k++) {
            sum += Math.Pow(points[k].X, i + j);
          }
          A[i, j] = sum;
        }

        double sumB = 0;
        for (int k = 0; k < n; k++) {
          sumB += points[k].Y * Math.Pow(points[k].X, i);
        }
        b[i] = sumB;
      }

      double[] coefficients = SolveLinearSystem(A, b);
      double rSquared = CalculateRSquared(points, coefficients);

      return (coefficients, rSquared);
    }

    private double[] SolveLinearSystem(double[,] A, double[] b) {
      int n = b.Length;
      double[] x = new double[n];

      double[,] aCopy = (double[,])A.Clone();
      double[] bCopy = (double[])b.Clone();

      for (int i = 0; i < n; i++) {
        double maxEl = Math.Abs(aCopy[i, i]);
        int maxRow = i;
        for (int k = i + 1; k < n; k++) {
          if (Math.Abs(aCopy[k, i]) > maxEl) {
            maxEl = Math.Abs(aCopy[k, i]);
            maxRow = k;
          }
        }

        if (maxRow != i) {
          for (int k = i; k < n; k++) {
            double temp = aCopy[maxRow, k];
            aCopy[maxRow, k] = aCopy[i, k];
            aCopy[i, k] = temp;
          }
          double tempB = bCopy[maxRow];
          bCopy[maxRow] = bCopy[i];
          bCopy[i] = tempB;
        }

        for (int k = i + 1; k < n; k++) {
          double factor = aCopy[k, i] / aCopy[i, i];
          for (int j = i; j < n; j++) {
            aCopy[k, j] -= factor * aCopy[i, j];
          }
          bCopy[k] -= factor * bCopy[i];
        }
      }

      for (int i = n - 1; i >= 0; i--) {
        x[i] = bCopy[i];
        for (int k = i + 1; k < n; k++) {
          x[i] -= aCopy[i, k] * x[k];
        }
        x[i] /= aCopy[i, i];
      }

      return x;
    }

    private double CalculateRSquared(List<PointData> points, double[] coefficients) {
      double yMean = points.Average(p => p.Y);
      double ssTotal = 0;
      double ssResidual = 0;

      foreach (var point in points) {
        double yPredicted = 0;
        for (int i = 0; i < coefficients.Length; i++) {
          yPredicted += coefficients[i] * Math.Pow(point.X, i);
        }

        ssTotal += Math.Pow(point.Y - yMean, 2);
        ssResidual += Math.Pow(point.Y - yPredicted, 2);
      }

      if (Math.Abs(ssTotal) < 1e-10)
        return 1.0;

      return Math.Max(0, Math.Min(1, 1 - (ssResidual / ssTotal)));
    }

    private void DisplayLinearResults((double[] coefficients, double rSquared) result) {
      if (result.coefficients == null || result.coefficients.Length < 2)
        return;

      string equation = $"y = {result.coefficients[0]:0.######}";
      if (result.coefficients[1] >= 0)
        equation += $" + {result.coefficients[1]:0.######}x";
      else
        equation += $" - {Math.Abs(result.coefficients[1]):0.######}x";

      LinearResultText.Text = equation;
      LinearRSquaredText.Text = $"{result.rSquared:0.######}";
      LinearRSquaredText.Foreground = result.rSquared > 0.8 ? Brushes.Green :
                                     result.rSquared > 0.5 ? Brushes.Orange : Brushes.Red;
    }

    private void DisplayQuadraticResults((double[] coefficients, double rSquared) result) {
      if (result.coefficients == null || result.coefficients.Length < 3)
        return;

      string equation = $"y = {result.coefficients[0]:0.######}";

      if (result.coefficients[1] >= 0)
        equation += $" + {result.coefficients[1]:0.######}x";
      else
        equation += $" - {Math.Abs(result.coefficients[1]):0.######}x";

      if (result.coefficients[2] >= 0)
        equation += $" + {result.coefficients[2]:0.######}x²";
      else
        equation += $" - {Math.Abs(result.coefficients[2]):0.######}x²";

      QuadraticResultText.Text = equation;
      QuadraticRSquaredText.Text = $"{result.rSquared:0.######}";
      QuadraticRSquaredText.Foreground = result.rSquared > 0.8 ? Brushes.Green :
                                        result.rSquared > 0.5 ? Brushes.Orange : Brushes.Red;
    }

    private void PlotGraph(List<PointData> points,
                          (double[] coefficients, double rSquared) linearResult,
                          (double[] coefficients, double rSquared) quadraticResult) {
      _plotModel.Series.Clear();

      if (points.Count == 0)
        return;

      double minX = points.Min(p => p.X);
      double maxX = points.Max(p => p.X);
      double minY = points.Min(p => p.Y);
      double maxY = points.Max(p => p.Y);

      if (ShowPointsCheckBox.IsChecked == true) {
        var scatterSeries = new ScatterSeries {
          Title = "Исходные точки",
          MarkerType = MarkerType.Circle,
          MarkerSize = 6,
          MarkerFill = OxyColors.DarkBlue,
          MarkerStroke = OxyColors.White,
          MarkerStrokeThickness = 1
        };

        foreach (var point in points) {
          scatterSeries.Points.Add(new ScatterPoint(point.X, point.Y));
        }

        _plotModel.Series.Add(scatterSeries);
      }

      if (ShowLinearCheckBox.IsChecked == true && linearResult.coefficients != null && linearResult.coefficients.Length >= 2) {
        var lineSeries = new LineSeries {
          Title = $"Линейная (R²={linearResult.rSquared:0.###})",
          Color = OxyColors.Red,
          StrokeThickness = 2,
          LineStyle = LineStyle.Solid
        };

        int steps = 100;
        for (int i = 0; i <= steps; i++) {
          double x = minX + (maxX - minX) * i / steps;
          double y = linearResult.coefficients[0] + linearResult.coefficients[1] * x;
          lineSeries.Points.Add(new DataPoint(x, y));
        }

        _plotModel.Series.Add(lineSeries);
      }

      if (ShowQuadraticCheckBox.IsChecked == true && quadraticResult.coefficients != null && quadraticResult.coefficients.Length >= 3) {
        var quadSeries = new LineSeries {
          Title = $"Квадратичная (R²={quadraticResult.rSquared:0.###})",
          Color = OxyColors.Green,
          StrokeThickness = 2,
          LineStyle = LineStyle.Solid
        };

        int steps = 100;
        for (int i = 0; i <= steps; i++) {
          double x = minX + (maxX - minX) * i / steps;
          double y = quadraticResult.coefficients[0] +
                    quadraticResult.coefficients[1] * x +
                    quadraticResult.coefficients[2] * x * x;
          quadSeries.Points.Add(new DataPoint(x, y));
        }

        _plotModel.Series.Add(quadSeries);
      }

      double xPadding = Math.Max(0.1 * (maxX - minX), 0.5);
      double yPadding = Math.Max(0.1 * (maxY - minY), 0.5);

      ((LinearAxis)_plotModel.Axes[0]).Minimum = minX - xPadding;
      ((LinearAxis)_plotModel.Axes[0]).Maximum = maxX + xPadding;
      ((LinearAxis)_plotModel.Axes[1]).Minimum = minY - yPadding;
      ((LinearAxis)_plotModel.Axes[1]).Maximum = maxY + yPadding;

      _plotModel.InvalidatePlot(true);
    }

    private void ClearResults() {
      LinearResultText.Text = "Не рассчитано";
      LinearRSquaredText.Text = "-";
      QuadraticResultText.Text = "Не рассчитано";
      QuadraticRSquaredText.Text = "-";

      _plotModel.Series.Clear();
      _plotModel.InvalidatePlot(true);
    }

    private void UpdateIndices() {
      int index = 1;
      foreach (DataPointLocal point in _dataPoints) {
        point.Index = index++;
      }
      DataGridPoints.Items.Refresh();
    }

    public class PointData {
      public double X { get; set; }
      public double Y { get; set; }

      public PointData(double x, double y) {
        X = x;
        Y = y;
      }
    }
  }

  // Диалоговое окно для вставки данных из Google Sheets
  public class GoogleSheetsInputDialog : Window {
    private TextBox dataTextBox;

    public string Data { get; private set; }

    public GoogleSheetsInputDialog() {
      Title = "Вставка данных из Google Sheets";
      Width = 600;
      Height = 400;
      WindowStartupLocation = WindowStartupLocation.CenterOwner;

      InitializeComponent();
    }

    private void InitializeComponent() {
      var mainGrid = new Grid {
        Margin = new Thickness(20)
      };

      mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

      // Инструкция
      var instructionText = new TextBlock {
        Text = "Вставьте данные из Google Sheets (формат: X,Y в каждой строке):",
        Margin = new Thickness(0, 0, 0, 10),
        FontWeight = System.Windows.FontWeights.SemiBold
      };
      Grid.SetRow(instructionText, 0);
      mainGrid.Children.Add(instructionText);

      // Текстовое поле для вставки данных
      dataTextBox = new TextBox {
        AcceptsReturn = true,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        FontFamily = new System.Windows.Media.FontFamily("Courier New"),
        FontSize = 12
      };
      Grid.SetRow(dataTextBox, 1);
      mainGrid.Children.Add(dataTextBox);

      // Пример данных
      var exampleText = new TextBlock {
        Text = "Пример формата:\n1.0,2.1\n2.0,3.2\n3.0,4.8\n4.0,6.1\n5.0,7.3",
        Margin = new Thickness(0, 10, 0, 10),
        FontStyle = FontStyles.Italic,
        Foreground = Brushes.Gray
      };
      Grid.SetRow(exampleText, 2);
      mainGrid.Children.Add(exampleText);

      // Кнопки
      var buttonPanel = new StackPanel {
        Orientation = Orientation.Horizontal,
        Margin = new Thickness(0, 10, 0, 0)
      };
      Grid.SetRow(buttonPanel, 3);

      var okButton = new Button {
        Content = "Загрузить",
        Width = 100,
        Height = 30,
        Margin = new Thickness(0, 0, 10, 0),
        Background = Brushes.DodgerBlue,
        Foreground = Brushes.White
      };
      okButton.Click += OkButton_Click;

      var cancelButton = new Button {
        Content = "Отмена",
        Width = 100,
        Height = 30,
        Background = Brushes.LightGray
      };
      cancelButton.Click += (s, e) => DialogResult = false;

      buttonPanel.Children.Add(okButton);
      buttonPanel.Children.Add(cancelButton);
      mainGrid.Children.Add(buttonPanel);

      Content = mainGrid;

      // Фокус на текстовом поле
      Loaded += (s, e) => dataTextBox.Focus();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e) {
      if (string.IsNullOrWhiteSpace(dataTextBox.Text)) {
        MessageBox.Show("Введите данные для загрузки", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      Data = dataTextBox.Text;
      DialogResult = true;
    }
  }
}