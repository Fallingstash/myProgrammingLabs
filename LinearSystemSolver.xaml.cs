using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
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
  /// Логика взаимодействия для LinearSystemSolver.xaml
  /// </summary>
  public partial class LinearSystemSolver : Window {
    public LinearSystemSolver() {
      InitializeComponent();
    }

    private int MatrixSize { get; set; }

    private void ImportFromCsvButton_Click(object sender, RoutedEventArgs e) {
      try {
        var openDialog = new Microsoft.Win32.OpenFileDialog();
        openDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";

        if (openDialog.ShowDialog() == true) {
          var lines = System.IO.File.ReadAllLines(openDialog.FileName);

          // Ищем разделители
          int matrixStartIndex = -1;
          int vectorStartIndex = -1;
          int resultStartIndex = -1;

          for (int i = 0; i < lines.Length; i++) {
            if (lines[i].Contains("Матрица A") || (matrixStartIndex == -1 && !string.IsNullOrEmpty(lines[i]) && lines[i].Contains(","))) {
              matrixStartIndex = i + 1;
            } else if (lines[i].Contains("Вектор B") || (vectorStartIndex == -1 && matrixStartIndex != -1 && string.IsNullOrEmpty(lines[i - 1]) && !string.IsNullOrEmpty(lines[i]))) {
              vectorStartIndex = i + 1;
            } else if (lines[i].Contains("Результат") || lines[i].Contains("вектор X")) {
              resultStartIndex = i + 1;
            }
          }

          // Если не нашли заголовки, пытаемся определить структуру по содержимому
          if (matrixStartIndex == -1) {
            matrixStartIndex = 0;
          }

          // Определяем размер матрицы (по первой строке матрицы)
          var firstMatrixRow = lines[matrixStartIndex].Split(',');
          int matrixSize = firstMatrixRow.Length;

          // Создаем матрицу нужного размера
          SizeTextBox.Text = matrixSize.ToString();
          CreateMatrix();

          // Загружаем матрицу A
          DataTable matrixTable = ((DataView)MatrixDataGrid.ItemsSource).Table;
          for (int i = 0; i < matrixSize; i++) {
            var rowValues = lines[matrixStartIndex + i].Split(',');
            for (int j = 0; j < matrixSize; j++) {
              if (double.TryParse(rowValues[j], out double value)) {
                matrixTable.Rows[i][j] = value;
              }
            }
          }

          // Загружаем вектор B
          if (vectorStartIndex != -1) {
            DataTable vectorTable = ((DataView)VectorDataGrid.ItemsSource).Table;
            for (int i = 0; i < matrixSize; i++) {
              if (double.TryParse(lines[vectorStartIndex + i], out double value)) {
                vectorTable.Rows[i][0] = value;
              }
            }
          }

          MessageBox.Show($"Данные успешно импортированы из файла:\n{openDialog.FileName}", "Импорт завершен");
        }
      }
      catch (Exception ex) {
        MessageBox.Show($"Ошибка при импорте: {ex.Message}", "Ошибка импорта");
      }
    }

    private void ExportToCsvButton_Click(object sender, RoutedEventArgs e) {
      try {
        if (MatrixDataGrid.ItemsSource == null) {
          MessageBox.Show("Нет данных для экспорта!");
          return;
        }

        var saveDialog = new Microsoft.Win32.SaveFileDialog();
        saveDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
        saveDialog.DefaultExt = ".csv";
        saveDialog.FileName = $"matrix_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

        if (saveDialog.ShowDialog() == true) {
          using (var writer = new System.IO.StreamWriter(saveDialog.FileName, false, Encoding.UTF8)) {
            // Экспортируем матрицу A
            var matrixA = GetMatrixFromDataGrid();
            int n = matrixA.GetLength(0);

            writer.WriteLine("Матрица A:");
            for (int i = 0; i < n; i++) {
              var rowValues = new List<string>();
              for (int j = 0; j < n; j++) {
                rowValues.Add(matrixA[i, j].ToString("F6"));
              }
              writer.WriteLine(string.Join(",", rowValues));
            }

            // Пустая строка как разделитель
            writer.WriteLine();

            // Экспортируем вектор B
            var vectorB = GetVectorFromDataGrid();
            writer.WriteLine("Вектор B:");
            foreach (var value in vectorB) {
              writer.WriteLine(value.ToString("F6"));
            }

            // Экспортируем результат, если есть
            if (ResultXDataGrid.ItemsSource != null) {
              writer.WriteLine();
              writer.WriteLine("Результат (вектор X):");

              if (ResultXDataGrid.ItemsSource is DataView resultView) {
                foreach (DataRowView row in resultView) {
                  writer.WriteLine(row["Значение"].ToString());
                }
              }
            }
          }

          MessageBox.Show($"Данные успешно экспортированы в файл:\n{saveDialog.FileName}", "Экспорт завершен");
        }
      }
      catch (Exception ex) {
        MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка экспорта");
      }
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e) {

    }


    private void CreateMatrixButton_Click(object sender, RoutedEventArgs e) {
      if (!int.TryParse(SizeTextBox.Text, out _)) {
        MessageBox.Show("Введите число соответствующее размеру матрицы");
        return;
      }
      MatrixSize = int.Parse(SizeTextBox.Text);
      if (MatrixSize >= 2 && MatrixSize <= 50) {
        CreateMatrix();
      } else {
        MessageBox.Show("Введите число от 2 до 50!!!");
      }
    }

    private void CreateMatrix() {
      DataTable matrixTable = new DataTable();
      DataTable vectorTable = new DataTable();
      vectorTable.Columns.Add("Value", typeof(double));


      for (int i = 0; i < MatrixSize; ++i) {
        matrixTable.Columns.Add($"x{i + 1}", typeof(double));
      }

      for (int i = 0; i < MatrixSize; i++) {
        matrixTable.Rows.Add();
        vectorTable.Rows.Add();

      }

      VectorDataGrid.ItemsSource = vectorTable.DefaultView;
      MatrixDataGrid.ItemsSource = matrixTable.DefaultView;
      
    }

    private double[,] GetMatrixFromDataGrid() {
      if (MatrixDataGrid.ItemsSource is DataView dataView) {
        DataTable table = dataView.Table;
        var matrix = new double[MatrixSize, MatrixSize];

        for (int i = 0; i < MatrixSize; i++) {
          for (int j = 0; j < MatrixSize; j++) {

            var value = table.Rows[i][j];
            if (value != DBNull.Value && value != null) {
              matrix[i, j] = Convert.ToDouble(value);
            }
          }
        }
        return matrix;
      }
      throw new InvalidOperationException("Матрица A не создана");
    }

    private double[] GetVectorFromDataGrid() {
      if (VectorDataGrid.ItemsSource is DataView dataView) {
        DataTable table = dataView.Table;
        double[] vector = new double[MatrixSize];

        for (int i = 0; i < MatrixSize; i++) {
          var value = table.Rows[i][0];
          if (value != DBNull.Value && value != null) {
            vector[i] = Convert.ToDouble(value);
          }
        }
        return vector;
      }
      throw new InvalidOperationException("Вектор B не создан");  
    }

    private async Task SolveSystemAsync(string methodName, Func<double[,], double[], double[]> solveMethod) {
      try {
        if (MatrixDataGrid.ItemsSource == null) {
          MessageBox.Show("Сначала создайте матрицу!");
          return;
        }

        // Показываем прогресс-бар
        ProgressBar.Visibility = Visibility.Visible;
        TimeInfoTextBlock.Text = $"{methodName}: вычисления...";

        var watch = System.Diagnostics.Stopwatch.StartNew();

        double[] result = await Task.Run(() => {
          double[,] A = GetMatrixFromDataGrid();
          double[] B = GetVectorFromDataGrid();
          return solveMethod(A, B);
        });

        watch.Stop();

        // Скрываем прогресс-бар
        ProgressBar.Visibility = Visibility.Collapsed;
        TimeInfoTextBlock.Text = $"{methodName}: выполнено за {watch.ElapsedMilliseconds} мс";
        DisplayResult(result);

        MessageBox.Show("Решение найдено успешно!", methodName);
      }
      catch (Exception ex) {
        ProgressBar.Visibility = Visibility.Collapsed;
        TimeInfoTextBlock.Text = $"{methodName}: ошибка";
        MessageBox.Show($"Ошибка в {methodName}: {ex.Message}", "Ошибка");
      }
    }

    private async void JordanGaussMethodButton_Click(object sender, RoutedEventArgs e) {
      await SolveSystemAsync("Метод Жордана-Гаусса", SolveJordanGauss);
    }

    private double[] SolveJordanGauss(double[,] A, double[] B) {
      int length = B.Length;

      // Создаем копии
      double[,] ACopy = (double[,])A.Clone();
      double[] BCopy = (double[])B.Clone();

      for (int k = 0; k < length; ++k) {
        if (Math.Abs(ACopy[k, k]) < 1e-10) {
          throw new InvalidOperationException("Матрица вырождена");
        }

        double diagonalElement = ACopy[k, k];

        for (int col = 0; col < length; ++col) {
          ACopy[k, col] /= diagonalElement;
        }
        BCopy[k] /= diagonalElement;

        for (int row = 0; row < length; ++row) {
          if (row == k) {
            continue;
          }

          double factor = ACopy[row, k];
          for(int col = 0; col < length; ++col) {
            ACopy[row, col] -= factor * ACopy[k, col];
          }
          BCopy[row] -= factor * BCopy[k];
        }
      }


      return BCopy;
    }

    private async void CramerMethodButton_Click(object sender, RoutedEventArgs e) {
      await SolveSystemAsync("Метод Крамера", SolveCramer);
    }


    private double[] SolveCramer(double[,] A, double[] B) {
      int length = A.GetLength(0);
      double[] X = new double[length];


      double detMatrix = Determinant(A);
      if (Math.Abs(detMatrix) < 1e-10) {
        throw new InvalidOperationException("Матрица вырождена, метод Крамера не применим");
      }


      for (int col = 0; col < length; ++col) {
        double[,] newA = ReplaceColumn(A, B, col);
        X[col] = Determinant(newA) / detMatrix;
      }

      return X;
    }

    private double Determinant(double[,] matrix) {
      int n = matrix.GetLength(0);

      // Для матрицы 1x1
      if (n == 1)
        return matrix[0, 0];

      // Для матрицы 2x2
      if (n == 2)
        return matrix[0, 0] * matrix[1, 1] - matrix[0, 1] * matrix[1, 0];

      // Для матрицы 3x3 (правило Саррюса)
      if (n == 3) {
        return matrix[0, 0] * matrix[1, 1] * matrix[2, 2] +
               matrix[0, 1] * matrix[1, 2] * matrix[2, 0] +
               matrix[0, 2] * matrix[1, 0] * matrix[2, 1] -
               matrix[0, 2] * matrix[1, 1] * matrix[2, 0] -
               matrix[0, 1] * matrix[1, 0] * matrix[2, 2] -
               matrix[0, 0] * matrix[1, 2] * matrix[2, 1];
      }

      // Для матриц большего размера - разложение по первой строке
      double det = 0;
      for (int j = 0; j < n; j++) {
        // Создаем минор M[0,j]
        double[,] minor = CreateMinor(matrix, 0, j);
        // Рекурсивно вычисляем определитель минора
        det += (j % 2 == 0 ? 1 : -1) * matrix[0, j] * Determinant(minor);
      }

      return det;
    }

    private double[,] CreateMinor(double[,] matrix, int rowToRemove, int colToRemove) {
      int n = matrix.GetLength(0);
      double[,] result = new double[n - 1, n - 1];
      
      int minorRow = 0;
      
      for (int row = 0; row < n; ++row) {
        if (row == rowToRemove) {
          continue;
        }
        
        int minorCol = 0;

        for (int col = 0; col < n; ++col) {
          if (col == colToRemove) {
            continue;
          }
          result[minorRow, minorCol] = matrix[row, col];
          ++minorCol;
        }
        ++minorRow;
      }

      return result;
    }

    private double[,] ReplaceColumn(double[,] matrix, double[] newColumn, int columnIndex) {
      int n = matrix.GetLength(0);
      double[,] result = (double[,])matrix.Clone();

      for (int row = 0; row < n; ++row) {
        result[row, columnIndex] = newColumn[row];
      }

      return result;
    }

    private async void GaussMethodButton_Click(object sender, RoutedEventArgs e) {
      await SolveSystemAsync("Метод Гаусса", SolveGauss);
    }

    private double[] SolveGauss(double[,] A, double[] B) {
      int n = B.Length;
      double[] X = new double[n];

      // Создаем копии чтобы не портить исходные данные
      double[,] ACopy = (double[,])A.Clone();
      double[] BCopy = (double[])B.Clone();

      // Прямой ход
      for (int k = 0; k < n - 1; k++) {
        // Поиск главного элемента
        int maxRow = k;
        double maxVal = Math.Abs(ACopy[k, k]);

        for (int i = k + 1; i < n; i++) {
          if (Math.Abs(ACopy[i, k]) > maxVal) {
            maxVal = Math.Abs(ACopy[i, k]);
            maxRow = i;
          }
        }

        // Перестановка строк если нужно
        if (maxRow != k) {
          for (int j = k; j < n; j++) {
            double temp = ACopy[k, j];
            ACopy[k, j] = ACopy[maxRow, j];
            ACopy[maxRow, j] = temp;
          }
          double tempB = BCopy[k];
          BCopy[k] = BCopy[maxRow];
          BCopy[maxRow] = tempB;
        }

        // Исключение
        for (int i = k + 1; i < n; i++) {
          if (Math.Abs(ACopy[k, k]) < 1e-10)
            throw new InvalidOperationException("Матрица вырождена");

          double factor = ACopy[i, k] / ACopy[k, k];
          for (int j = k; j < n; j++) {
            ACopy[i, j] -= factor * ACopy[k, j];
          }
          BCopy[i] -= factor * BCopy[k];
        }
      }

      // Обратный ход
      for (int i = n - 1; i >= 0; i--) {
        X[i] = BCopy[i];
        for (int j = i + 1; j < n; j++) {
          X[i] -= ACopy[i, j] * X[j];
        }

        // Проверка деления на ноль
        if (Math.Abs(ACopy[i, i]) < 1e-10)
          throw new InvalidOperationException("Матрица вырождена");

        X[i] /= ACopy[i, i];
      }

      return X;
    }

    private void DisplayResult(double[] result) {
      DataTable resultTable = new DataTable();
      resultTable.Columns.Add("Индекс", typeof(int));
      resultTable.Columns.Add("Значение", typeof(double));

      // Заполняем данными
      for (int i = 0; i < result.Length; i++) {
        DataRow row = resultTable.NewRow();
        row["Индекс"] = i + 1;
        row["Значение"] = Math.Round(result[i], 6); // Округляем для красоты
        resultTable.Rows.Add(row);
      }

      // Привязываем к DataGrid
      ResultXDataGrid.ItemsSource = resultTable.DefaultView;
    }

    private void RandomGenerationButton_Click(object sender, RoutedEventArgs e) {
      if (MatrixDataGrid.ItemsSource == null || VectorDataGrid.ItemsSource == null) {
        MessageBox.Show("Сначала создайте матрицу!!!");
        return;
      }

      Random rand = new Random();

      DataTable matrixTable = ((DataView)MatrixDataGrid.ItemsSource).Table;
      DataTable vectorTable = ((DataView)VectorDataGrid.ItemsSource).Table;

      for (int row = 0; row < MatrixSize; ++row) {
        vectorTable.Rows[row][0] = Math.Round(rand.NextDouble() * 20 - 10, 2);

        for (int col = 0; col < MatrixSize; ++col) {
          matrixTable.Rows[row][col] = Math.Round(rand.NextDouble() * 20 - 10, 2);
        }
      }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) {
      this.Close();
    }

    private void AboutButton_Click(object sender, RoutedEventArgs e) {
      MessageBox.Show("Это Крутая Программа Александра Солдатова из ПИ-241");
    }
  }
}
