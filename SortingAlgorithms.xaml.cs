using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MultiWindowApp {
  public partial class SortingAlgorithms : Window {
    private ObservableCollection<NumberItem> dataCollection;
    private Stopwatch stopwatch = new Stopwatch();

    // Ограничение BogoSort (по умолчанию 100000, но можно менять)
    private int maxBogoIterations = 100000;

    public SortingAlgorithms() {
      InitializeComponent();
      InitializeDataGrid();
    }

    // Класс для хранения чисел с индексом
    public class NumberItem {
      public int Index { get; set; }
      public double Value { get; set; }
    }

    private void InitializeDataGrid() {
      int size = int.Parse(ArraySizeTextBox.Text);
      dataCollection = new ObservableCollection<NumberItem>();

      Random random = new Random();
      for (int i = 0; i < size; i++) {
        dataCollection.Add(new NumberItem {
          Index = i + 1,
          Value = random.Next(1, 100)
        });
      }

      InputDataGrid.ItemsSource = dataCollection;

      // Настраиваем колонки вручную
      InputDataGrid.Columns.Clear();

      // Колонка индекса
      var indexColumn = new DataGridTextColumn {
        Header = "№",
        Binding = new Binding("Index"),
        Width = 50,
        IsReadOnly = true
      };
      InputDataGrid.Columns.Add(indexColumn);

      // Колонка значения
      var valueColumn = new DataGridTextColumn {
        Header = "Значение",
        Binding = new Binding("Value") { StringFormat = "F2" },
        Width = new DataGridLength(1, DataGridLengthUnitType.Star)
      };
      InputDataGrid.Columns.Add(valueColumn);
    }

    // Получаем массив из DataGrid
    private double[] GetArrayFromDataGrid() {
      return dataCollection.Select(item => item.Value).ToArray();
    }

    // Обновляем DataGrid из массива
    private void UpdateDataGridFromArray(double[] array) {
      for (int i = 0; i < array.Length && i < dataCollection.Count; i++) {
        dataCollection[i].Value = array[i];
      }
      InputDataGrid.Items.Refresh();
    }

    // Класс для хранения результатов сортировки
    public class SortResult {
      public double[] SortedArray { get; set; }
      public int Iterations { get; set; }
      public TimeSpan Time { get; set; }
    }

    // Диалог для настройки BogoSort
    private void ShowBogoSettingsDialog() {
      var dialog = new BogoSettingsDialog(maxBogoIterations);
      if (dialog.ShowDialog() == true) {
        maxBogoIterations = dialog.MaxIterations;
      }
    }

    private void ConfigureBogoMenuItem_Click(object sender, RoutedEventArgs e) {
      ShowBogoSettingsDialog();
    }

    private async Task<SortResult> ExecuteSortAsync(Func<double[], bool, SortResult> sortMethod, string methodName, bool isAscending) {
      double[] localArr = GetArrayFromDataGrid();
      var localStopwatch = new Stopwatch();

      localStopwatch.Restart();
      var result = await Task.Run(() => sortMethod(localArr, isAscending));
      localStopwatch.Stop();

      result.Time = localStopwatch.Elapsed;

      // Обновляем отображение
      Dispatcher.Invoke(() =>
      {
        UpdateTimeDisplay(methodName, result.Time);
        UpdateIterationsDisplay(methodName, result.Iterations);
        UpdateVisualization(result.SortedArray, methodName, result.Time, result.Iterations);
      });

      return result;
    }

    // Методы сортировки теперь возвращают SortResult
    public SortResult BubbleSort(double[] arr, bool isAscending) {
      int iterations = 0;

      for (int countOfIteration = 0; countOfIteration < arr.Length; ++countOfIteration) {
        for (int numberOfElement = 0; numberOfElement < arr.Length - 1; ++numberOfElement) {
          iterations++;
          if (isAscending) {
            if (arr[numberOfElement] > arr[numberOfElement + 1]) {
              var bufer = arr[numberOfElement];
              arr[numberOfElement] = arr[numberOfElement + 1];
              arr[numberOfElement + 1] = bufer;
            }
          } else {
            if (arr[numberOfElement] < arr[numberOfElement + 1]) {
              var bufer = arr[numberOfElement];
              arr[numberOfElement] = arr[numberOfElement + 1];
              arr[numberOfElement + 1] = bufer;
            }
          }
        }
      }
      return new SortResult { SortedArray = arr, Iterations = iterations };
    }

    public SortResult InsertSort(double[] arr, bool isAscending) {
      int iterations = 0;

      for (int countOfIteration = 1; countOfIteration < arr.Length; ++countOfIteration) {
        iterations++;
        var key = arr[countOfIteration];
        int countOfElements = countOfIteration - 1;

        while (countOfElements >= 0 && ((arr[countOfElements] > key && isAscending) || (!isAscending && arr[countOfElements] < key))) {
          iterations++;
          arr[countOfElements + 1] = arr[countOfElements];
          --countOfElements;
        }

        arr[countOfElements + 1] = key;
      }
      return new SortResult { SortedArray = arr, Iterations = iterations };
    }

    public SortResult ShakerSort(double[] arr, bool isAscending) {
      int iterations = 0;
      int left = 0, right = arr.Length - 1;
      bool swapped = false;

      while (left < right) {
        swapped = false;

        for (int goRight = left; goRight < right; ++goRight) {
          iterations++;
          if (isAscending) {
            if (arr[goRight] > arr[goRight + 1]) {
              swapped = true;
              var bufer = arr[goRight];
              arr[goRight] = arr[goRight + 1];
              arr[goRight + 1] = bufer;
            }
          } else {
            if (arr[goRight] < arr[goRight + 1]) {
              swapped = true;
              var bufer = arr[goRight];
              arr[goRight] = arr[goRight + 1];
              arr[goRight + 1] = bufer;
            }
          }
        }

        --right;

        for (int goLeft = right; goLeft > left; --goLeft) {
          iterations++;
          if (isAscending) {
            if (arr[goLeft] < arr[goLeft - 1]) {
              swapped = true;
              var bufer = arr[goLeft];
              arr[goLeft] = arr[goLeft - 1];
              arr[goLeft - 1] = bufer;
            }
          } else {
            if (arr[goLeft] > arr[goLeft - 1]) {
              swapped = true;
              var bufer = arr[goLeft];
              arr[goLeft] = arr[goLeft - 1];
              arr[goLeft - 1] = bufer;
            }
          }
        }

        ++left;

        if (!swapped) {
          break;
        }
      }
      return new SortResult { SortedArray = arr, Iterations = iterations };
    }

    private int quickSortIterations = 0;

    public SortResult QuickSort(double[] arr, bool isAscending) {
      quickSortIterations = 0;
      QuickSortRecursive(arr, 0, arr.Length - 1, isAscending);
      return new SortResult { SortedArray = arr, Iterations = quickSortIterations };
    }

    private void QuickSortRecursive(double[] arr, int left, int right, bool isAscending) {
      if (left < right) {
        quickSortIterations++;
        int pivotIndex = Partition(arr, left, right, isAscending);
        QuickSortRecursive(arr, left, pivotIndex - 1, isAscending);
        QuickSortRecursive(arr, pivotIndex + 1, right, isAscending);
      }
    }

    private int Partition(double[] arr, int left, int right, bool isAscending) {
      double pivot = arr[right];
      int pivotIndex = left - 1;

      for (int numberOfElement = left; numberOfElement < right; numberOfElement++) {
        quickSortIterations++;
        if (isAscending) {
          if (arr[numberOfElement] <= pivot) {
            ++pivotIndex;
            var bufer = arr[pivotIndex];
            arr[pivotIndex] = arr[numberOfElement];
            arr[numberOfElement] = bufer;
          }
        } else {
          if (arr[numberOfElement] >= pivot) {
            ++pivotIndex;
            var bufer = arr[pivotIndex];
            arr[pivotIndex] = arr[numberOfElement];
            arr[numberOfElement] = bufer;
          }
        }
      }

      var temp = arr[pivotIndex + 1];
      arr[pivotIndex + 1] = arr[right];
      arr[right] = temp;

      return pivotIndex + 1;
    }

    public SortResult BogoSort(double[] arr, bool isAscending) {
      int iterations = 0;
      Random random = new Random();

      // Показываем диалог настроек перед запуском BogoSort
      Dispatcher.Invoke(() => ShowBogoSettingsDialog());

      while (iterations < maxBogoIterations) {
        iterations++;
        bool isSorted = true;

        for (int numberOfElement = 0; numberOfElement < arr.Length - 1; numberOfElement++) {
          if (isAscending && arr[numberOfElement] > arr[numberOfElement + 1]) {
            isSorted = false;
            break;
          }
          if (!isAscending && arr[numberOfElement] < arr[numberOfElement + 1]) {
            isSorted = false;
            break;
          }
        }

        if (isSorted) {
          break;
        }

        // Перемешиваем массив
        for (int numberOfElement = 0; numberOfElement < arr.Length; ++numberOfElement) {
          int randomIndex = random.Next(0, arr.Length);
          var bufer = arr[randomIndex];
          arr[randomIndex] = arr[numberOfElement];
          arr[numberOfElement] = bufer;
        }
      }

      // Если достигнут лимит итераций
      if (iterations >= maxBogoIterations) {
        MessageBox.Show($"BogoSort достиг максимального количества итераций ({maxBogoIterations}). Массив может быть не отсортирован.");
      }

      return new SortResult { SortedArray = arr, Iterations = iterations };
    }

    private void UpdateTimeDisplay(string methodName, TimeSpan time) {
      switch (methodName) {
        case "BubbleSort":
          BubbleSortTime.Text = $"Пузырьковая: {time.TotalMilliseconds:F2} мс";
          break;
        case "QuickSort":
          QuickSortTime.Text = $"Быстрая: {time.TotalMilliseconds:F2} мс";
          break;
        case "InsertSort":
          InsertionSortTime.Text = $"Вставкой: {time.TotalMilliseconds:F2} мс";
          break;
        case "BogoSort":
          BogoSortTime.Text = $"BOGO: {time.TotalMilliseconds:F2} мс";
          break;
        case "ShakerSort":
          ShakerSortTime.Text = $"Шейкерная: {time.TotalMilliseconds:F2} мс";
          break;
      }
    }

    private void UpdateIterationsDisplay(string methodName, int iterations) {
      switch (methodName) {
        case "BubbleSort":
          BubbleSortIterations.Text = $"Пузырьковая: {iterations}";
          break;
        case "QuickSort":
          QuickSortIterations.Text = $"Быстрая: {iterations}";
          break;
        case "InsertSort":
          InsertionSortIterations.Text = $"Вставками: {iterations}";
          break;
        case "BogoSort":
          BogoSortIterations.Text = $"BOGO: {iterations}";
          break;
        case "ShakerSort":
          ShakerSortIterations.Text = $"Шейкерная: {iterations}";
          break;
      }
    }

    private void UpdateVisualization(double[] result, string methodName, TimeSpan time, int iterations) {
      Canvas targetCanvas = methodName switch {
        "BubbleSort" => BubbleSortCanvas,
        "QuickSort" => QuickSortCanvas,
        "InsertSort" => InsertSortCanvas,
        "ShakerSort" => ShakerSortCanvas,
        "BogoSort" => BogoSortCanvas,
        _ => null
      };

      if (targetCanvas != null) {
        DrawArrayOnCanvas(result, targetCanvas);

        // Добавляем подпись с временем и итерациями
        var infoText = new TextBlock {
          Text = $"{time.TotalMilliseconds:F2} мс\n{iterations} итераций",
          Foreground = Brushes.Red,
          FontWeight = FontWeights.Bold,
          FontSize = 10,
          Background = Brushes.White
        };
        Canvas.SetTop(infoText, 2);
        Canvas.SetLeft(infoText, 2);
        targetCanvas.Children.Add(infoText);
      }
    }

    private void DrawArrayOnCanvas(double[] array, Canvas canvas) {
      if (canvas == null)
        return;

      canvas.Children.Clear();
      if (array.Length == 0)
        return;

      double columnWidth = canvas.ActualWidth / array.Length;
      double maxValue = array.Max();

      for (int i = 0; i < array.Length; i++) {
        Rectangle rect = new Rectangle {
          Width = Math.Max(1, columnWidth - 1),
          Height = (array[i] / maxValue) * canvas.ActualHeight,
          Fill = Brushes.Blue,
          Stroke = Brushes.Black,
          StrokeThickness = 0.5
        };

        Canvas.SetLeft(rect, i * columnWidth);
        Canvas.SetBottom(rect, 0);
        canvas.Children.Add(rect);
      }
    }

    private async void StartSortingButton_Click(object sender, RoutedEventArgs e) {
      try {
        ResetAllDisplays();

        double[] originalData = GetArrayFromDataGrid();
        bool isAscending = AscendingRadio.IsChecked == true;
        var tasks = new List<Task<SortResult>>();


        if (BubbleSortCheckBox.IsChecked == true) {
          tasks.Add(ExecuteSortAsync(BubbleSort, "BubbleSort", isAscending));
        }

        if (QuickSortCheckBox.IsChecked == true) {
          tasks.Add(ExecuteSortAsync(QuickSort, "QuickSort", isAscending));
        }

        if (ShakerSortCheckBox.IsChecked == true) {
          tasks.Add(ExecuteSortAsync(ShakerSort, "ShakerSort", isAscending));
        }

        if (InsertionSortCheckBox.IsChecked == true) {
          tasks.Add(ExecuteSortAsync(InsertSort, "InsertSort", isAscending));
        }

        if (BogoSortCheckBox.IsChecked == true) {
          tasks.Add(ExecuteSortAsync(BogoSort, "BogoSort", isAscending));
        }

        if (tasks.Count == 0) {
          MessageBox.Show("Выберите хотя бы один алгоритм сортировки!");
          return;
        }

        await Task.WhenAll(tasks);

        // Восстанавливаем исходные данные в DataGrid
        UpdateDataGridFromArray(originalData);
        MessageBox.Show("Все сортировки завершены!");
      }
      catch (Exception ex) {
        MessageBox.Show($"Ошибка: {ex.Message}");
      }
    }

    private void ResetAllDisplays() {
      // Очищаем Canvas
      foreach (var border in VisualizationPanel.Children.OfType<Border>()) {
        if (border.Child is StackPanel stackPanel) {
          var canvas = stackPanel.Children.OfType<Canvas>().FirstOrDefault();
          canvas?.Children.Clear();
        }
      }

      // Сбрасываем время
      BubbleSortTime.Text = "Пузырьковая: - ";
      QuickSortTime.Text = "Быстрая: - ";
      InsertionSortTime.Text = "Вставками: - ";
      ShakerSortTime.Text = "Шейкерная: - ";
      BogoSortTime.Text = "BOGO: - ";

      // Сбрасываем итерации
      BubbleSortIterations.Text = "Пузырьковая: 0";
      QuickSortIterations.Text = "Быстрая: 0";
      InsertionSortIterations.Text = "Вставками: 0";
      ShakerSortIterations.Text = "Шейкерная: 0";
      BogoSortIterations.Text = "BOGO: 0";
    }

    // Обновим метод генерации случайных данных
    private void GenerateRandomDataMenuItem_Click(object sender, RoutedEventArgs e) {
      try {
        // Диалог для выбора типа данных и диапазона
        var dialog = new RandomDataDialog();
        if (dialog.ShowDialog() == true) {
          int size = int.Parse(ArraySizeTextBox.Text);
          var random = new Random();
          var data = new ObservableCollection<NumberItem>();

          if (dialog.IsInteger) {
            for (int i = 0; i < size; i++) {
              data.Add(new NumberItem {
                Index = i + 1,
                Value = random.Next((int)dialog.MinValue, (int)dialog.MaxValue + 1)
              });
            }
          } else {
            for (int i = 0; i < size; i++) {
              double value = dialog.MinValue + (random.NextDouble() * (dialog.MaxValue - dialog.MinValue));
              data.Add(new NumberItem {
                Index = i + 1,
                Value = Math.Round(value, 2)
              });
            }
          }

          dataCollection = data;
          InputDataGrid.ItemsSource = dataCollection;
        }
      }
      catch (Exception ex) {
        MessageBox.Show($"Ошибка при генерации данных: {ex.Message}");
      }
    }

    // Обновим импорт для поддержки double и индексов
    private void ImportFromExcel(string filePath) {
      using (var package = new ExcelPackage(new FileInfo(filePath))) {
        var worksheet = package.Workbook.Worksheets[0];
        var data = new ObservableCollection<NumberItem>();

        int row = 1;
        int index = 1;
        while (worksheet.Cells[row, 1].Value != null) {
          if (double.TryParse(worksheet.Cells[row, 1].Value?.ToString(),
              NumberStyles.Float, CultureInfo.InvariantCulture, out double value)) {
            data.Add(new NumberItem {
              Index = index++,
              Value = Math.Round(value, 2)
            });
          }
          row++;
        }

        if (data.Count > 0) {
          dataCollection = data;
          InputDataGrid.ItemsSource = dataCollection;
          ArraySizeTextBox.Text = data.Count.ToString();
          MessageBox.Show($"Успешно импортировано {data.Count} элементов");
        } else {
          MessageBox.Show("Не удалось найти числовые данные в файле");
        }
      }
    }

    // Остальные методы...
    private void UpdateArraySizeButton_Click(object sender, RoutedEventArgs e) {
      try {
        InitializeDataGrid();
      }
      catch (Exception ex) {
        MessageBox.Show($"Ошибка при обновлении размера: {ex.Message}");
      }
    }

    private async void ImportFromGoogleSheetsMenuItem_Click(object sender, RoutedEventArgs e) {
      try {
        // Запрос URL от пользователя
        var url = Microsoft.VisualBasic.Interaction.InputBox(
            "Введите URL Google Sheets (файл должен быть доступен для всех по ссылке):",
            "Импорт из Google Sheets",
            "https://docs.google.com/spreadsheets/d/.../export?format=csv");

        if (!string.IsNullOrEmpty(url)) {
          await ImportFromGoogleSheets(url);
        }
      }
      catch (Exception ex) {
        MessageBox.Show($"Ошибка при импорте из Google Sheets: {ex.Message}");
      }
    }

    private void ImportFromExcelMenuItem_Click(object sender, RoutedEventArgs e) {
      try {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog {
          Filter = "Excel files (*.xlsx;*.xls)|*.xlsx;*.xls",
          Title = "Выберите файл Excel"
        };

        if (openFileDialog.ShowDialog() == true) {
          ImportFromExcel(openFileDialog.FileName);
        }
      }
      catch (Exception ex) {
        MessageBox.Show($"Ошибка при импорте из Excel: {ex.Message}");
      }
    }

    private async Task ImportFromGoogleSheets(string url) {
      using (var client = new System.Net.Http.HttpClient()) {
        // Скачиваем CSV
        var csvData = await client.GetStringAsync(url);
        var data = new ObservableCollection<NumberItem>();

        // Парсим CSV
        using (var reader = new StringReader(csvData)) {
          string line;
          while ((line = reader.ReadLine()) != null) {
            var values = line.Split(',');

            foreach (var value in values) {
              if (int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int num)) {
                data.Add(new NumberItem { Value = num });
              }
            }
          }
        }

        if (data.Count > 0) {
          dataCollection = data;
          InputDataGrid.ItemsSource = dataCollection;
          ArraySizeTextBox.Text = data.Count.ToString();
          MessageBox.Show($"Успешно импортировано {data.Count} элементов из Google Sheets");
        } else {
          MessageBox.Show("Не удалось найти числовые данные в таблице");
        }
      }
    }

    private void ClearDataMenuItem_Click(object sender, RoutedEventArgs e) {
      dataCollection?.Clear();
      ResetAllDisplays();
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e) {
      this.Close();
    }
  }
}
