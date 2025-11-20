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
using System.Windows.Media;
using System.Windows.Shapes;

namespace MultiWindowApp {
  public partial class SortingAlgorithms : Window {
    private ObservableCollection<NumberItem> dataCollection;
    private Stopwatch stopwatch = new Stopwatch();

    public SortingAlgorithms() {
      InitializeComponent();
      InitializeDataGrid();
    }

    private void InitializeDataGrid() {
      int size = int.Parse(ArraySizeTextBox.Text);
      dataCollection = new ObservableCollection<NumberItem>();

      Random random = new Random();
      for (int i = 0; i < size; i++) {
        dataCollection.Add(new NumberItem { Value = random.Next(1, 100) });
      }

      InputDataGrid.ItemsSource = dataCollection;
    }

    // Получаем массив из DataGrid
    private int[] GetArrayFromDataGrid() {
      return dataCollection.Select(item => item.Value).ToArray();
    }

    // Обновляем DataGrid из массива
    private void UpdateDataGridFromArray(int[] array) {
      for (int i = 0; i < array.Length && i < dataCollection.Count; i++) {
        dataCollection[i].Value = array[i];
      }
    }

    public class NumberItem {
      public int Value { get; set; }
    }

    private async Task ExecuteSortAsync(Func<int[], bool, int[]> sortMethod, string methodName, bool isAscending) {
      // Каждая сортировка получает СВОЮ копию исходных данных и СВОЙ таймер
      int[] localArr = GetArrayFromDataGrid();
      var localStopwatch = new Stopwatch(); // ✅ ОТДЕЛЬНЫЙ таймер для каждого алгоритма

      localStopwatch.Restart();
      int[] result = await Task.Run(() => sortMethod(localArr, isAscending));
      localStopwatch.Stop();

      // Обновляем ТОЛЬКО визуализацию, НЕ DataGrid
      UpdateTimeDisplay(methodName, localStopwatch.Elapsed);
      UpdateVisualization(result, methodName, localStopwatch.Elapsed);
    }

    private void UpdateVisualization(int[] result, string methodName, TimeSpan time) {
      Dispatcher.Invoke(() =>
      {
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

          // Добавляем подпись с временем прямо на Canvas
          var timeText = new TextBlock {
            Text = $"{time.TotalMilliseconds:F2} мс",
            Foreground = Brushes.Red,
            FontWeight = FontWeights.Bold,
            FontSize = 10,
            Background = Brushes.White
          };
          Canvas.SetTop(timeText, 2);
          Canvas.SetLeft(timeText, 2);
          targetCanvas.Children.Add(timeText);
        }
      });
    }

    // Обработчик для кнопки обновления размера массива
    private void UpdateArraySizeButton_Click(object sender, RoutedEventArgs e) {
      try {
        InitializeDataGrid();
      }
      catch (Exception ex) {
        MessageBox.Show($"Ошибка при обновлении размера: {ex.Message}");
      }
    }

    // Остальные методы остаются без изменений...
    private void UpdateTimeDisplay(string methodName, TimeSpan time) {
      Dispatcher.Invoke(() =>
      {
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
            BogoSortTime.Text = $"Болотной: {time.TotalMilliseconds:F2} мс";
            break;
          case "ShakerSort":
            ShakerSortTime.Text = $"Шейком: {time.TotalMilliseconds:F2} мс";
            break;
        }
      });
    }

    private void UpdateVisualization(int[] result, string methodName) {
      Dispatcher.Invoke(() =>
      {
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

          // Добавляем подпись с временем прямо на Canvas
          var timeText = new TextBlock {
            Text = $"{stopwatch.Elapsed.TotalMilliseconds:F2} мс",
            Foreground = Brushes.Red,
            FontWeight = FontWeights.Bold,
            FontSize = 10
          };
          Canvas.SetTop(timeText, 5);
          Canvas.SetLeft(timeText, 5);
          targetCanvas.Children.Add(timeText);
        }
      });
    }

    public int[] BubbleSort(int[] arr, bool isAscending) {
      for (int countOfIteration = 0; countOfIteration < arr.Length; ++countOfIteration) {
        for (int numberOfElement = 0; numberOfElement < arr.Length - 1; ++numberOfElement) {
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
      return arr; // ✅ ДОБАВЛЕНО возвращение массива
    }

    public int[] OptimizedBubbleSort(int[] arr, bool isAscending) {
      for (int countOfIteration = 0; countOfIteration < arr.Length; ++countOfIteration) {
        bool swapped = false;
        for (int numberOfElement = 0; numberOfElement < arr.Length - 1 - countOfIteration; ++numberOfElement) {
          if (isAscending) {
            if (arr[numberOfElement] > arr[numberOfElement + 1]) {
              swapped = true;
              var bufer = arr[numberOfElement];
              arr[numberOfElement] = arr[numberOfElement + 1];
              arr[numberOfElement + 1] = bufer;
            }
          } else {
            if (arr[numberOfElement] < arr[numberOfElement + 1]) {
              swapped = true;
              var bufer = arr[numberOfElement];
              arr[numberOfElement] = arr[numberOfElement + 1];
              arr[numberOfElement + 1] = bufer;
            }
          }
        }

        if (!swapped) {
          break;
        }
      }
      return arr; // ✅ ДОБАВЛЕНО возвращение массива
    }

    public int[] InsertSort(int[] arr, bool isAscending) {
      for (int countOfIteration = 1; countOfIteration < arr.Length; ++countOfIteration) {
        var key = arr[countOfIteration];
        int countOfElements = countOfIteration - 1;

        while (countOfElements >= 0 && ((arr[countOfElements] > key && isAscending) || (!isAscending && arr[countOfElements] < key))) {
          arr[countOfElements + 1] = arr[countOfElements];
          --countOfElements;
        }

        arr[countOfElements + 1] = key;
      }
      return arr; // ✅ ДОБАВЛЕНО возвращение массива
    }

    public int[] ShakerSort(int[] arr, bool isAscending) {
      int left = 0, right = arr.Length - 1;
      bool swapped = false;

      while (left < right) {
        swapped = false;

        for (int goRight = left; goRight < right; ++goRight) {
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
      return arr; // ✅ ДОБАВЛЕНО возвращение массива
    }

    public int[] QuickSort(int[] arr, bool isAscending) {
      QuickSortRecursive(arr, 0, arr.Length - 1, isAscending);
      return arr; // ✅ ДОБАВЛЕНО возвращение массива
    }

    private void QuickSortRecursive(int[] arr, int left, int right, bool isAscending) {
      if (left < right) {
        int pivotIndex = Partition(arr, left, right, isAscending);
        QuickSortRecursive(arr, left, pivotIndex - 1, isAscending);
        QuickSortRecursive(arr, pivotIndex + 1, right, isAscending);
      }
    }

    private int Partition(int[] arr, int left, int right, bool isAscending) {
      int pivot = arr[right];
      int pivotIndex = left - 1;

      for (int numberOfElement = left; numberOfElement < right; numberOfElement++) {
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

    public int[] BogoSort(int[] arr, bool isAscending = false) {
      Random random = new Random();
      while (true) {
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

        for (int numberOfElement = 0; numberOfElement < arr.Length; ++numberOfElement) {
          int randomIndex = random.Next(0, arr.Length);
          var bufer = arr[randomIndex];
          arr[randomIndex] = arr[numberOfElement];
          arr[numberOfElement] = bufer;
        }
      }
      return arr; // ✅ ДОБАВЛЕНО возвращение массива
    }

    private void DrawArrayOnCanvas(int[] array, Canvas canvas) {
      if (canvas == null)
        return;

      // Ждем обновления layout
      canvas.Dispatcher.Invoke(() =>
      {
        canvas.Children.Clear();
        if (array.Length == 0)
          return;

        double columnWidth = canvas.ActualWidth / array.Length;
        double maxValue = array.Max();

        for (int i = 0; i < array.Length; i++) {
          Rectangle rect = new Rectangle {
            Width = Math.Max(1, columnWidth - 1), // Минимальная ширина 1
            Height = (array[i] / maxValue) * canvas.ActualHeight,
            Fill = Brushes.Blue,
            Stroke = Brushes.Black,
            StrokeThickness = 0.5
          };

          Canvas.SetLeft(rect, i * columnWidth);
          Canvas.SetBottom(rect, 0);
          canvas.Children.Add(rect);
        }
      }, System.Windows.Threading.DispatcherPriority.Render);
    }

    private async void StartSortingButton_Click(object sender, RoutedEventArgs e) {
      try {
        ResetAllDisplays();

        int[] originalData = GetArrayFromDataGrid();
        bool isAscending = AscendingRadio.IsChecked == true;
        var tasks = new List<Task>();

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

    private void ImportFromExcel(string filePath) {
      using (var package = new ExcelPackage(new FileInfo(filePath))) {
        var worksheet = package.Workbook.Worksheets[0]; // Первый лист
        var data = new ObservableCollection<NumberItem>();

        // Читаем данные из первого столбца
        int row = 1;
        while (worksheet.Cells[row, 1].Value != null) {
          if (int.TryParse(worksheet.Cells[row, 1].Value?.ToString(), out int value)) {
            data.Add(new NumberItem { Value = value });
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

    private void GenerateRandomDataMenuItem_Click(object sender, RoutedEventArgs e) {
      try {
        int size = int.Parse(ArraySizeTextBox.Text);
        var random = new Random();
        var data = new ObservableCollection<NumberItem>();

        for (int i = 0; i < size; i++) {
          data.Add(new NumberItem { Value = random.Next(1, 1000) });
        }

        dataCollection = data;
        InputDataGrid.ItemsSource = dataCollection;
      }
      catch (Exception ex) {
        MessageBox.Show($"Ошибка при генерации данных: {ex.Message}");
      }
    }
  }
}
