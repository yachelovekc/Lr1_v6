using lr1_v6.Models;
using lr1_v6.Services;
using System.Numerics;
using System.Text.Json;

namespace lr1_v6.Tests
{
    public class SpreadsheetFileServiceTests
    {
        [Fact]
        public void OpenFile_MissingFile_ThrowsFileNotFoundException()
        {
            var service = new SpreadsheetFileService();
            string missingPath = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.json");

            Assert.Throws<FileNotFoundException>(() => service.OpenFile(missingPath));
        }

        [Fact]
        public void OpenFile_NullJsonPayload_ThrowsInvalidDataException()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"null_{Guid.NewGuid():N}.json");
            File.WriteAllText(tempFile, "null");

            try
            {
                var service = new SpreadsheetFileService();
                Assert.Throws<InvalidDataException>(() => service.OpenFile(tempFile));
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void SaveFile_CreatesNestedDirectoryAndOmitsEmptyCells()
        {
            string nestedDir = Path.Combine(Path.GetTempPath(), $"lr1_v6_dir_{Guid.NewGuid():N}", "sub");
            string filePath = Path.Combine(nestedDir, "sheet.json");

            try
            {
                var table = new SpreadsheetTable(4, 4);
                table.UpdateCell("A1", "7");
                table.UpdateCell("B2", "A1 * 3");

                var service = new SpreadsheetFileService();
                service.SaveFile(filePath, table);

                Assert.True(File.Exists(filePath));
                var dto = JsonSerializer.Deserialize<SpreadsheetDocumentDto>(File.ReadAllText(filePath));
                Assert.NotNull(dto);
                Assert.Equal(4, dto!.RowCount);
                Assert.Equal(4, dto.ColumnCount);
                Assert.Equal(2, dto.Cells.Count);
            }
            finally
            {
                string rootDir = Path.GetDirectoryName(nestedDir)!;
                if (Directory.Exists(rootDir)) Directory.Delete(rootDir, recursive: true);
            }
        }

        [Fact]
        public async Task SaveFileAsyncAndOpenFileAsync_RoundtripsTableCorrectly()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"async_sheet_{Guid.NewGuid():N}.json");
            try
            {
                var table = new SpreadsheetTable(3, 3);
                table.UpdateCell("A1", "100");
                table.UpdateCell("B1", "A1 div 3");

                var service = new SpreadsheetFileService();
                await service.SaveFileAsync(tempFile, table);
                var loaded = await service.OpenFileAsync(tempFile);

                Assert.Equal(new BigInteger(100), loaded.TryGetCell("A1")?.Value);
                Assert.Equal(new BigInteger(33), loaded.TryGetCell("B1")?.Value);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void LoadVariant6Demo_PopulatesExpectedTableAndValues()
        {
            var table = new SpreadsheetTable(2, 2);
            SpreadsheetFileService.LoadVariant6Demo(table);

            Assert.Equal(8, table.Rows);
            Assert.Equal(6, table.Columns);
            Assert.Equal(new BigInteger(5), table.TryGetCell("A1")?.Value);
            Assert.Equal(new BigInteger(5), table.TryGetCell("B1")?.Value);
            Assert.Equal(new BigInteger(11), table.TryGetCell("A2")?.Value);
            Assert.Equal(new BigInteger(4), table.TryGetCell("B2")?.Value);
            Assert.Equal(new BigInteger(512), table.TryGetCell("C1")?.Value);
            Assert.Equal(new BigInteger(18), table.TryGetCell("C2")?.Value);
            Assert.Equal(new BigInteger(5), table.TryGetCell("D1")?.Value);
            Assert.Equal(BigInteger.Parse("100000000000000000000000000025"), table.TryGetCell("A3")?.Value);
        }

        [Fact]
        public void OpenFile_BigIntScenarioJson_ComputesExactValues()
        {
            string json = """
            {
              "Title": "BigInt Test",
              "RowCount": 6,
              "ColumnCount": 5,
              "Cells": [
                { "Cell": "A1", "Row": 0, "Column": 0, "Expression": "999999999999999999999999999999" },
                { "Cell": "B1", "Row": 0, "Column": 1, "Expression": "1" },
                { "Cell": "C1", "Row": 0, "Column": 2, "Expression": "A1 + B1" },
                { "Cell": "A2", "Row": 1, "Column": 0, "Expression": "C1 div 1000000000000000" },
                { "Cell": "B2", "Row": 1, "Column": 1, "Expression": "(A1 + 8) mod 10" },
                { "Cell": "A3", "Row": 2, "Column": 0, "Expression": "max(2 ^ 50, 10 ^ 15)" },
                { "Cell": "B3", "Row": 2, "Column": 1, "Expression": "min(2 ^ 50, 10 ^ 15)" }
              ]
            }
            """;

            string tempFile = Path.Combine(Path.GetTempPath(), $"bigint_{Guid.NewGuid():N}.json");
            File.WriteAllText(tempFile, json);

            try
            {
                var table = new SpreadsheetFileService().OpenFile(tempFile);

                Assert.Equal(BigInteger.Parse("1000000000000000000000000000000"), table.TryGetCell("C1")?.Value);
                Assert.Equal(BigInteger.Parse("1000000000000000"), table.TryGetCell("A2")?.Value);
                Assert.Equal(new BigInteger(7), table.TryGetCell("B2")?.Value);
                Assert.Equal(BigInteger.Pow(2, 50), table.TryGetCell("A3")?.Value);
                Assert.Equal(BigInteger.Pow(10, 15), table.TryGetCell("B3")?.Value);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void OpenFile_ErrorsScenarioJson_MarksStage1AndStage2Errors()
        {
            string json = """
            {
              "Title": "Errors Test",
              "RowCount": 6,
              "ColumnCount": 5,
              "Cells": [
                { "Cell": "A1", "Row": 0, "Column": 0, "Expression": "10 + * 5" },
                { "Cell": "B1", "Row": 0, "Column": 1, "Expression": "max(10)" },
                { "Cell": "A2", "Row": 1, "Column": 0, "Expression": "100 / 0" },
                { "Cell": "B2", "Row": 1, "Column": 1, "Expression": "25 mod 0" },
                { "Cell": "A3", "Row": 2, "Column": 0, "Expression": "B3 + 1" },
                { "Cell": "B3", "Row": 2, "Column": 1, "Expression": "A3 + 1" },
                { "Cell": "A4", "Row": 3, "Column": 0, "Expression": "Z99 + 10" }
              ]
            }
            """;

            string tempFile = Path.Combine(Path.GetTempPath(), $"errors_{Guid.NewGuid():N}.json");
            File.WriteAllText(tempFile, json);

            try
            {
                var table = new SpreadsheetFileService().OpenFile(tempFile);

                Assert.True(table.TryGetCell("A1")?.HasError);
                Assert.True(table.TryGetCell("B1")?.HasError);
                Assert.Equal("#ДІЛ/0!", table.TryGetCell("A2")?.Error);
                Assert.Equal("#ДІЛ/0!", table.TryGetCell("B2")?.Error);
                Assert.Equal("#ЦИКЛ!", table.TryGetCell("A3")?.Error);
                Assert.Equal("#ЦИКЛ!", table.TryGetCell("B3")?.Error);
                Assert.Equal("#ПОСИЛ!", table.TryGetCell("A4")?.Error);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void AppInfoProvider_ReturnsNonEmptyAboutAndHelpText()
        {
            string about = AppInfoProvider.GetAboutText();
            string help = AppInfoProvider.GetHelpText();

            Assert.Contains("Варіант: 6", about);
            Assert.Contains("BigInteger", about);
            Assert.Contains("#ЦИКЛ!", help);
            Assert.Contains("#ДІЛ/0!", help);
            Assert.Contains("#ПОСИЛ!", help);
        }
    }
}
