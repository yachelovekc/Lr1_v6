using lr1_v6.Models;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace lr1_v6.Services
{
    public class SavedCellDto
    {
        public string Cell { get; set; } = string.Empty;
        public int Row { get; set; }
        public int Column { get; set; }
        public string Expression { get; set; } = string.Empty;
    }

    public class SpreadsheetDocumentDto
    {
        public string Title { get; set; } = "Таблиця Варіант 6";
        public int RowCount { get; set; }
        public int ColumnCount { get; set; }
        public List<SavedCellDto> Cells { get; set; } = new();
    }

    public class SpreadsheetFileService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
        };

        public void SaveFile(string path, SpreadsheetTable table)
        {
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var dto = new SpreadsheetDocumentDto
            {
                Title = "Таблиця Варіант 6",
                RowCount = table.Rows,
                ColumnCount = table.Columns
            };

            foreach (var kvp in table.Cells)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Value.Expression))
                {
                    dto.Cells.Add(new SavedCellDto
                    {
                        Cell = kvp.Key,
                        Row = kvp.Value.Row,
                        Column = kvp.Value.Column,
                        Expression = kvp.Value.Expression
                    });
                }
            }

            string json = JsonSerializer.Serialize(dto, JsonOptions);
            File.WriteAllText(path, json);
        }

        public SpreadsheetTable OpenFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Файл не знайдено: {path}");
            }

            string json = File.ReadAllText(path);
            var dto = JsonSerializer.Deserialize<SpreadsheetDocumentDto>(json, JsonOptions)
                ?? throw new InvalidDataException("Файл таблиці порожній або некоректний.");

            var table = new SpreadsheetTable(dto.RowCount, dto.ColumnCount);
            if (dto.Cells != null)
            {
                foreach (var item in dto.Cells)
                {
                    if (!string.IsNullOrWhiteSpace(item.Cell))
                    {
                        table.UpdateCell(item.Cell, item.Expression ?? string.Empty);
                    }
                }
            }

            table.RecalculateAll();
            return table;
        }

        public async Task SaveFileAsync(string path, SpreadsheetTable table)
        {
            await Task.Run(() => SaveFile(path, table));
        }

        public async Task<SpreadsheetTable> OpenFileAsync(string path)
        {
            return await Task.Run(() => OpenFile(path));
        }

        public static void LoadVariant6Demo(SpreadsheetTable table)
        {
            table.ResizeAndClear(8, 6);

            table.UpdateCell("A1", "5");
            table.UpdateCell("B1", "5");
            table.UpdateCell("A2", "max(B1, 2^3) + 10 div 3");
            table.UpdateCell("B2", "25 mod 7");
            table.UpdateCell("C1", "2 ^ 3 ^ 2");
            table.UpdateCell("C2", "min(A2, 20) * 2 - 4");
            table.UpdateCell("D1", "20 / 4");
            table.UpdateCell("A3", "100000000000000000000000000000 + 25");

            table.RecalculateAll();
        }
    }
}
