using lr1_v6.Models;
using lr1_v6.Services;
using Cell = lr1_v6.Models.Cell;

namespace lr1_v6
{
    public partial class MainPage : ContentPage
    {
        public SpreadsheetTable Table { get; set; } = new(8, 6);
        public SpreadsheetFileService FileService { get; set; } = new();
        public bool IsExpressionMode { get; set; }

        private Entry[,] _cellEntries = new Entry[0, 0];
        private Border[,] _cellBorders = new Border[0, 0];

        private int _selectedRow;
        private int _selectedCol;
        private bool _isUpdatingUi;
        private int _editingRow = -1;
        private int _editingCol = -1;

        public MainPage()
        {
            InitializeComponent();
            EnsureSampleFilesCreated();
            SpreadsheetFileService.LoadVariant6Demo(Table);
            RebuildGridStructure();
            SelectCell(0, 0);
            RefreshGridDisplay();
        }

        private static string GetTestFilesDirectory()
        {
            string baseDir = AppContext.BaseDirectory;
            DirectoryInfo? current = new(baseDir);
            while (current != null)
            {
                string slnCandidate = Path.Combine(current.FullName, "lr1_v6.slnx");
                if (File.Exists(slnCandidate))
                {
                    string repoTestDir = Path.Combine(current.FullName, "TestFiles");
                    Directory.CreateDirectory(repoTestDir);
                    return repoTestDir;
                }
                current = current.Parent;
            }

            string fallback = Path.Combine(FileSystem.Current.AppDataDirectory, "TestFiles");
            Directory.CreateDirectory(fallback);
            return fallback;
        }

        private void EnsureSampleFilesCreated()
        {
            try
            {
                string dir = GetTestFilesDirectory();
                string demoPath = Path.Combine(dir, "variant6_demo.json");
                if (!File.Exists(demoPath))
                {
                    var sample = new SpreadsheetTable(8, 6);
                    SpreadsheetFileService.LoadVariant6Demo(sample);
                    FileService.SaveFile(demoPath, sample);
                }
            }
            catch
            {
            }
        }

        public void ChangeCell(string name, string text)
        {
            Table.UpdateCell(name, text);
            RefreshGridDisplay();
        }

        public void ToggleMode()
        {
            IsExpressionMode = !IsExpressionMode;
            Table.DisplayMode = IsExpressionMode ? TableDisplayMode.Expression : TableDisplayMode.Value;
            RefreshGridDisplay();
            string modeName = IsExpressionMode ? "ВИРАЗ" : "ЗНАЧЕННЯ";
            FooterStatusLabel.Text = $"Увімкнено режим відображення: {modeName}.";
        }

        public void AddRow()
        {
            Table.AddRow();
            RebuildGridStructure();
            RefreshGridDisplay();
            FooterStatusLabel.Text = $"Додано рядок №{Table.Rows}.";
        }

        public async void RemoveRow()
        {
            if (Table.Rows <= 1)
            {
                await DisplayAlertAsync("Увага", "У таблиці повинен залишатися щонайменше 1 рядок.", "ОК");
                return;
            }

            Table.RemoveRow();
            if (_selectedRow >= Table.Rows)
            {
                _selectedRow = Table.Rows - 1;
            }
            RebuildGridStructure();
            SelectCell(_selectedRow, _selectedCol);
            RefreshGridDisplay();
            FooterStatusLabel.Text = $"Видалено останній рядок. Поточний розмір: {Table.Rows} × {Table.Columns}.";
        }

        public void AddColumn()
        {
            Table.AddColumn();
            RebuildGridStructure();
            RefreshGridDisplay();
            FooterStatusLabel.Text = $"Додано стовпчик {Cell.ColumnIndexToName(Table.Columns - 1)}.";
        }

        public async void RemoveColumn()
        {
            if (Table.Columns <= 1)
            {
                await DisplayAlertAsync("Увага", "У таблиці повинен залишатися щонайменше 1 стовпчик.", "ОК");
                return;
            }

            Table.RemoveColumn();
            if (_selectedCol >= Table.Columns)
            {
                _selectedCol = Table.Columns - 1;
            }
            RebuildGridStructure();
            SelectCell(_selectedRow, _selectedCol);
            RefreshGridDisplay();
            FooterStatusLabel.Text = $"Видалено останній стовпчик. Поточний розмір: {Table.Rows} × {Table.Columns}.";
        }

        public async Task Save()
        {
            try
            {
                string testDir = GetTestFilesDirectory();
                string? fileName = await DisplayPromptAsync(
                    "Збереження таблиці у файл",
                    $"Введіть ім'я файлу (буде збережено у {testDir}):",
                    accept: "Зберегти",
                    cancel: "Скасувати",
                    initialValue: "table_variant6.json");

                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return;
                }

                fileName = fileName.Trim();
                if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ".json";
                }

                string fullPath = Path.IsPathRooted(fileName)
                    ? fileName
                    : Path.Combine(testDir, fileName);

                await FileService.SaveFileAsync(fullPath, Table);
                FooterStatusLabel.Text = $"Таблицю збережено: {fullPath}";
                await DisplayAlertAsync("Збережено успішно", $"Таблицю збережено у файл:\n{fullPath}", "ОК");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Помилка збереження", $"Не вдалося зберегти файл:\n{ex.Message}", "ОК");
            }
        }

        public async Task Open()
        {
            try
            {
                string testDir = GetTestFilesDirectory();
                var existingFiles = Directory.Exists(testDir)
                    ? Directory.GetFiles(testDir, "*.json").Select(Path.GetFileName).Where(f => f != null).Cast<string>().ToArray()
                    : Array.Empty<string>();

                var options = new List<string> { "Обрати файл з диска через Провідник..." };
                options.AddRange(existingFiles);

                string? choice = await DisplayActionSheetAsync(
                    "Відкрити таблицю з файлу",
                    "Скасувати",
                    null,
                    options.ToArray());

                if (string.IsNullOrWhiteSpace(choice) || choice == "Скасувати")
                {
                    return;
                }

                string? targetFilePath;
                if (choice == "Обрати файл з диска через Провідник...")
                {
                    var picked = await FilePicker.Default.PickAsync(new PickOptions
                    {
                        PickerTitle = "Оберіть JSON-файл електронної таблиці"
                    });
                    if (picked == null) return;
                    targetFilePath = picked.FullPath;
                }
                else
                {
                    targetFilePath = Path.Combine(testDir, choice);
                }

                Table = await FileService.OpenFileAsync(targetFilePath);
                RebuildGridStructure();
                SelectCell(0, 0);
                RefreshGridDisplay();
                FooterStatusLabel.Text = $"Завантажено таблицю: {Path.GetFileName(targetFilePath)}";
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Помилка відкриття файлу", $"Не вдалося прочитати файл:\n{ex.Message}", "ОК");
            }
        }

        public async Task Help()
        {
            await DisplayAlertAsync("Допомога користувачу (Варіант 6)", AppInfoProvider.GetHelpText(), "Зрозуміло");
        }

        public (Cell? B1, Cell? Target) FindCell(string name)
        {
            var target = Table.TryGetCell(name);
            var b1 = Table.TryGetCell("B1");
            return (b1, target);
        }

        public void SelectCell(string name)
        {
            if (Cell.TryParseCellName(name, out int r, out int c))
            {
                SelectCell(r, c);
            }
        }

        public string ShowContent(string name)
        {
            var cell = Table.TryGetCell(name);
            return cell?.Expression ?? string.Empty;
        }

        public bool CalculateAndWrite(string expression)
        {
            var cell = Table.GetCell(_selectedRow, _selectedCol);
            cell.SetExpression(expression);
            var result = cell.CheckSyntaxAndCycle(Table);
            Table.RecalculateAll();
            RefreshGridDisplay();
            return result.Success;
        }

        public async Task ShowMessage(string message)
        {
            await DisplayAlertAsync("Повідомлення", message, "ОК");
        }

        private void RebuildGridStructure()
        {
            _isUpdatingUi = true;
            try
            {
                SpreadsheetGrid.Children.Clear();
                SpreadsheetGrid.RowDefinitions.Clear();
                SpreadsheetGrid.ColumnDefinitions.Clear();

                int rows = Table.Rows;
                int cols = Table.Columns;

                SpreadsheetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
                for (int c = 0; c < cols; c++)
                {
                    SpreadsheetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(135) });
                }

                SpreadsheetGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(34) });
                for (int r = 0; r < rows; r++)
                {
                    SpreadsheetGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) });
                }

                var cornerBox = new Border
                {
                    BackgroundColor = Color.FromArgb("#E2E8F0"),
                    StrokeThickness = 0,
                    Content = new Label
                    {
                        Text = "№",
                        FontAttributes = FontAttributes.Bold,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center,
                        TextColor = Color.FromArgb("#334155")
                    }
                };
                SpreadsheetGrid.Add(cornerBox, 0, 0);

                for (int c = 0; c < cols; c++)
                {
                    var colHeader = new Border
                    {
                        BackgroundColor = Color.FromArgb("#E2E8F0"),
                        StrokeThickness = 0,
                        Content = new Label
                        {
                            Text = Cell.ColumnIndexToName(c),
                            FontAttributes = FontAttributes.Bold,
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center,
                            TextColor = Color.FromArgb("#1E293B")
                        }
                    };
                    SpreadsheetGrid.Add(colHeader, c + 1, 0);
                }

                _cellEntries = new Entry[rows, cols];
                _cellBorders = new Border[rows, cols];

                for (int r = 0; r < rows; r++)
                {
                    var rowHeader = new Border
                    {
                        BackgroundColor = Color.FromArgb("#E2E8F0"),
                        StrokeThickness = 0,
                        Content = new Label
                        {
                            Text = (r + 1).ToString(),
                            FontAttributes = FontAttributes.Bold,
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center,
                            TextColor = Color.FromArgb("#1E293B")
                        }
                    };
                    SpreadsheetGrid.Add(rowHeader, 0, r + 1);

                    for (int c = 0; c < cols; c++)
                    {
                        int capturedRow = r;
                        int capturedCol = c;

                        var entry = new Entry
                        {
                            FontSize = 13.5,
                            TextColor = Color.FromArgb("#0F172A"),
                            BackgroundColor = Colors.Transparent,
                            Margin = new Thickness(2, 0),
                            VerticalOptions = LayoutOptions.Center
                        };

                        entry.Focused += (_, _) => OnCellEntryFocused(capturedRow, capturedCol);
                        entry.Unfocused += (_, _) => OnCellEntryUnfocused(capturedRow, capturedCol);
                        entry.Completed += (_, _) => OnCellEntryCompleted(capturedRow, capturedCol);

                        var cellBorder = new Border
                        {
                            BackgroundColor = Colors.White,
                            Stroke = Color.FromArgb("#E2E8F0"),
                            StrokeThickness = 1,
                            Padding = new Thickness(2, 0),
                            Content = entry
                        };

                        _cellEntries[r, c] = entry;
                        _cellBorders[r, c] = cellBorder;

                        SpreadsheetGrid.Add(cellBorder, c + 1, r + 1);
                    }
                }

                DimensionsLabel.Text = $"Розмір: {rows} × {cols}";
            }
            finally
            {
                _isUpdatingUi = false;
            }
        }

        private void RefreshGridDisplay()
        {
            _isUpdatingUi = true;
            try
            {
                int rows = Table.Rows;
                int cols = Table.Columns;

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        var cell = Table.GetCell(r, c);
                        var entry = _cellEntries[r, c];
                        var border = _cellBorders[r, c];

                        if (r != _editingRow || c != _editingCol)
                        {
                            entry.Text = Table.GetDisplayText(r, c);
                        }

                        bool isSelected = r == _selectedRow && c == _selectedCol;
                        if (cell.HasError)
                        {
                            border.BackgroundColor = Color.FromArgb("#FEF2F2");
                            entry.TextColor = Color.FromArgb("#B91C1C");
                        }
                        else if (IsExpressionMode && !string.IsNullOrWhiteSpace(cell.Expression))
                        {
                            border.BackgroundColor = Color.FromArgb("#F5F3FF");
                            entry.TextColor = Color.FromArgb("#4C1D95");
                        }
                        else
                        {
                            border.BackgroundColor = isSelected ? Color.FromArgb("#EFF6FF") : Colors.White;
                            entry.TextColor = Color.FromArgb("#0F172A");
                        }

                        border.Stroke = isSelected ? Color.FromArgb("#2563EB") : Color.FromArgb("#E2E8F0");
                        border.StrokeThickness = isSelected ? 2 : 1;
                    }
                }

                ModeToggleButton.Text = IsExpressionMode ? "Режим: ВИРАЗ" : "Режим: ЗНАЧЕННЯ";
                ModeToggleButton.BackgroundColor = IsExpressionMode ? Color.FromArgb("#D97706") : Color.FromArgb("#7C3AED");

                UpdateSelectedCellDiagnostics();
            }
            finally
            {
                _isUpdatingUi = false;
            }
        }

        private void SelectCell(int row, int col)
        {
            if (row < 0 || row >= Table.Rows) row = 0;
            if (col < 0 || col >= Table.Columns) col = 0;

            _selectedRow = row;
            _selectedCol = col;

            var cell = Table.GetCell(row, col);
            SelectedCellLabel.Text = cell.Name;

            _isUpdatingUi = true;
            try
            {
                FormulaEntry.Text = cell.Expression;
            }
            finally
            {
                _isUpdatingUi = false;
            }

            UpdateSelectedCellDiagnostics();
        }

        private void UpdateSelectedCellDiagnostics()
        {
            if (_selectedRow < 0 || _selectedRow >= Table.Rows ||
                _selectedCol < 0 || _selectedCol >= Table.Columns)
            {
                return;
            }

            var cell = Table.GetCell(_selectedRow, _selectedCol);

            if (string.IsNullOrWhiteSpace(cell.Expression))
            {
                Stage1StatusLabel.Text = "Клітинка порожня";
                Stage1StatusLabel.TextColor = Color.FromArgb("#64748B");
                Stage2StatusLabel.Text = "—";
                Stage2StatusLabel.TextColor = Color.FromArgb("#64748B");
                return;
            }

            if (cell.HasError && cell.ParsedTree == null)
            {
                Stage1StatusLabel.Text = $"✗ {cell.Error}";
                Stage1StatusLabel.TextColor = Color.FromArgb("#DC2626");

                Stage2StatusLabel.Text = "Скасовано через синтаксичну помилку на Етапі 1";
                Stage2StatusLabel.TextColor = Color.FromArgb("#DC2626");
                return;
            }

            string refsInfo = cell.References.Count > 0
                ? $" (посилання: {string.Join(", ", cell.References)})"
                : "";
            Stage1StatusLabel.Text = $"✓ Синтаксис коректний{refsInfo}";
            Stage1StatusLabel.TextColor = Color.FromArgb("#15803D");

            if (cell.HasError)
            {
                Stage2StatusLabel.Text = $"✗ {cell.Error}";
                Stage2StatusLabel.TextColor = Color.FromArgb("#DC2626");
            }
            else
            {
                Stage2StatusLabel.Text = $"✓ Значення = {cell.Value}";
                Stage2StatusLabel.TextColor = Color.FromArgb("#0F172A");
            }
        }

        private void OnCellEntryFocused(int row, int col)
        {
            if (_isUpdatingUi) return;

            _editingRow = row;
            _editingCol = col;
            SelectCell(row, col);

            var cell = Table.GetCell(row, col);
            _isUpdatingUi = true;
            try
            {
                _cellEntries[row, col].Text = cell.Expression;
            }
            finally
            {
                _isUpdatingUi = false;
            }

            RefreshGridDisplay();
        }

        private void OnCellEntryUnfocused(int row, int col)
        {
            if (_isUpdatingUi) return;
            CommitCellEdit(row, col);
        }

        private void OnCellEntryCompleted(int row, int col)
        {
            if (_isUpdatingUi) return;
            CommitCellEdit(row, col);
        }

        private void CommitCellEdit(int row, int col)
        {
            if (row < 0 || row >= Table.Rows || col < 0 || col >= Table.Columns)
            {
                return;
            }

            string enteredText = _cellEntries[row, col].Text ?? string.Empty;
            _editingRow = -1;
            _editingCol = -1;

            Table.SetCellExpression(row, col, enteredText);
            if (_selectedRow == row && _selectedCol == col)
            {
                _isUpdatingUi = true;
                try
                {
                    FormulaEntry.Text = Table.GetCell(row, col).Expression;
                }
                finally
                {
                    _isUpdatingUi = false;
                }
            }

            RefreshGridDisplay();
            var updatedCell = Table.GetCell(row, col);
            FooterStatusLabel.Text = updatedCell.HasError
                ? $"Помилка у клітинці {updatedCell.Name}: {updatedCell.Error}"
                : $"Оновлено клітинку {updatedCell.Name}.";
        }

        private void OnApplyFormulaClicked(object? sender, EventArgs e)
        {
            ApplyFormulaFromBar();
        }

        private void OnFormulaEntryCompleted(object? sender, EventArgs e)
        {
            ApplyFormulaFromBar();
        }

        private void ApplyFormulaFromBar()
        {
            if (_isUpdatingUi) return;

            string expr = FormulaEntry.Text ?? string.Empty;
            _editingRow = -1;
            _editingCol = -1;

            Table.SetCellExpression(_selectedRow, _selectedCol, expr);
            RefreshGridDisplay();

            var cell = Table.GetCell(_selectedRow, _selectedCol);
            FooterStatusLabel.Text = cell.HasError
                ? $"Помилка у клітинці {cell.Name}: {cell.Error}"
                : $"Клітинку {cell.Name} обчислено: {cell.Value}";
        }

        private void OnAddRowClicked(object? sender, EventArgs e) => AddRow();
        private void OnRemoveRowClicked(object? sender, EventArgs e) => RemoveRow();
        private void OnAddColumnClicked(object? sender, EventArgs e) => AddColumn();
        private void OnRemoveColumnClicked(object? sender, EventArgs e) => RemoveColumn();
        private void OnToggleModeClicked(object? sender, EventArgs e) => ToggleMode();

        private void OnSetValueModeClicked(object? sender, EventArgs e)
        {
            IsExpressionMode = false;
            Table.DisplayMode = TableDisplayMode.Value;
            RefreshGridDisplay();
            FooterStatusLabel.Text = "Увімкнено режим: ЗНАЧЕННЯ.";
        }

        private void OnSetExpressionModeClicked(object? sender, EventArgs e)
        {
            IsExpressionMode = true;
            Table.DisplayMode = TableDisplayMode.Expression;
            RefreshGridDisplay();
            FooterStatusLabel.Text = "Увімкнено режим: ВИРАЗ.";
        }

        private void OnRecalculateClicked(object? sender, EventArgs e)
        {
            Table.RecalculateAll();
            RefreshGridDisplay();
            FooterStatusLabel.Text = "Таблицю перераховано.";
        }

        private async void OnClearTableClicked(object? sender, EventArgs e)
        {
            bool confirm = await DisplayAlertAsync(
                "Очищення таблиці",
                "Ви дійсно бажаєте очистити всі клітинки?",
                "Так, очистити",
                "Скасувати");

            if (!confirm) return;

            Table.ClearAll();
            SelectCell(0, 0);
            RefreshGridDisplay();
            FooterStatusLabel.Text = "Усі клітинки очищено.";
        }

        private async void OnNewTableClicked(object? sender, EventArgs e)
        {
            bool confirm = await DisplayAlertAsync(
                "Нова таблиця",
                "Створити нову порожню таблицю (8 × 6)?",
                "Так",
                "Скасувати");

            if (!confirm) return;

            Table.ResizeAndClear(8, 6);
            RebuildGridStructure();
            SelectCell(0, 0);
            RefreshGridDisplay();
            FooterStatusLabel.Text = "Створено нову таблицю (8 × 6).";
        }

        private void OnLoadDemoClicked(object? sender, EventArgs e)
        {
            SpreadsheetFileService.LoadVariant6Demo(Table);
            RebuildGridStructure();
            SelectCell(0, 0);
            RefreshGridDisplay();
            FooterStatusLabel.Text = "Завантажено демонстраційний приклад для Варіанта 6.";
        }

        private async void OnSaveFileClicked(object? sender, EventArgs e) => await Save();
        private async void OnOpenFileClicked(object? sender, EventArgs e) => await Open();
        private async void OnHelpClicked(object? sender, EventArgs e) => await Help();

        private async void OnAboutClicked(object? sender, EventArgs e)
        {
            await DisplayAlertAsync("Про програму", AppInfoProvider.GetAboutText(), "ОК");
        }

        private async void OnExitClicked(object? sender, EventArgs e)
        {
            bool confirm = await DisplayAlertAsync(
                "Вихід з програми",
                "Чи дійсно ви хочете завершити роботу з програмою?",
                "Так",
                "Ні");

            if (confirm)
            {
                Application.Current?.Quit();
            }
        }
    }
}
