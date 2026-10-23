using lr1_v6.Calculator;
using lr1_v6.Models;
using lr1_v6.Services;
using System.Numerics;

namespace lr1_v6.Tests
{
    public class Variant6SpreadsheetTests
    {
        [Theory]
        [InlineData("2 + 3 * 4", "14")]
        [InlineData("(2 + 3) * 4", "20")]
        [InlineData("100 - 45 + 5", "60")]
        [InlineData("20 / 4", "5")]
        [InlineData("-5 + 12", "7")]
        public void Calculator_BasicBinaryOperations_ReturnsExpectedResult(string expr, string expected)
        {
            BigInteger actual = SpreadsheetCalculator.Evaluate(expr);
            Assert.Equal(BigInteger.Parse(expected), actual);
        }

        [Theory]
        [InlineData("25 div 7", "3")]
        [InlineData("25 mod 7", "4")]
        [InlineData("100 div 10", "10")]
        [InlineData("100 mod 10", "0")]
        [InlineData("17 DIV 5 + 17 MOD 5", "5")]
        public void Calculator_DivAndModOperations_ReturnsExpectedInteger(string expr, string expected)
        {
            BigInteger result = SpreadsheetCalculator.Evaluate(expr);
            Assert.Equal(BigInteger.Parse(expected), result);
        }

        [Theory]
        [InlineData("2 ^ 10", "1024")]
        [InlineData("2 ^ 3 ^ 2", "512")]
        [InlineData("(2 ^ 3) ^ 2", "64")]
        [InlineData("5 ^ 0", "1")]
        public void Calculator_PowerOperation_IsRightAssociativeAndExact(string expr, string expected)
        {
            BigInteger result = SpreadsheetCalculator.Evaluate(expr);
            Assert.Equal(BigInteger.Parse(expected), result);
        }

        [Theory]
        [InlineData("max(10, 25)", "25")]
        [InlineData("min(10, 25)", "10")]
        [InlineData("MAX(2 ^ 5, 30)", "32")]
        [InlineData("MIN(25 div 4, 25 mod 4)", "1")]
        [InlineData("max(min(10, 20), max(5, 8))", "10")]
        public void Calculator_MaxAndMinFunctions_ReturnsExpectedValue(string expr, string expected)
        {
            BigInteger result = SpreadsheetCalculator.Evaluate(expr);
            Assert.Equal(BigInteger.Parse(expected), result);
        }

        [Fact]
        public void Calculator_ArbitraryLengthBigIntegers_ComputesExactValue()
        {
            string expr = "999999999999999999999999999999 + 1";
            BigInteger result = SpreadsheetCalculator.Evaluate(expr);
            Assert.Equal(BigInteger.Parse("1000000000000000000000000000000"), result);
        }

        [Theory]
        [InlineData("10 + * 5")]
        [InlineData("(10 + 5")]
        [InlineData("max(10)")]
        [InlineData("min(1, 2, 3)")]
        [InlineData("unknownFunc(1, 2)")]
        [InlineData("10 @ 2")]
        public void Stage1_InvalidSyntax_ThrowsSyntaxErrorWithPosition(string badExpr)
        {
            var ex = Assert.Throws<SyntaxErrorException>(() =>
                SpreadsheetCalculator.Parse(badExpr, out _));

            Assert.True(ex.Position >= 0);
            Assert.False(string.IsNullOrWhiteSpace(ex.Message));
        }

        [Fact]
        public void SpreadsheetTable_CellReferencesAndDependencies_RecalculatesCorrectly()
        {
            var table = new SpreadsheetTable(4, 4);
            table.UpdateCell("A1", "25");
            table.UpdateCell("B1", "7");
            table.UpdateCell("C1", "max(A1 div B1, A1 mod B1) ^ 3");

            Assert.Equal(new BigInteger(64), table.TryGetCell("C1")?.Value);

            table.UpdateCell("B1", "5");
            Assert.Equal(new BigInteger(125), table.TryGetCell("C1")?.Value);
        }

        [Fact]
        public void SpreadsheetTable_SequenceDiagramScenario_CalculatesAccurately()
        {
            var table = new SpreadsheetTable(4, 4);
            table.UpdateCell("B1", "5");
            table.UpdateCell("A1", "max(B1, 2^3) + 10 div 3");

            var cellA1 = table.TryGetCell("A1")!;
            Assert.False(cellA1.HasError);
            Assert.Equal(new BigInteger(11), cellA1.Value);
        }

        [Fact]
        public void SpreadsheetTable_CircularReference_DetectsCycleOnStage2()
        {
            var table = new SpreadsheetTable(4, 4);
            table.UpdateCell("A1", "B1 + 1");
            table.UpdateCell("B1", "C1 + 1");
            table.UpdateCell("C1", "A1 + 1");

            var cellA1 = table.TryGetCell("A1")!;
            Assert.True(cellA1.HasError);
            Assert.Equal("#ЦИКЛ!", cellA1.Error);
        }

        [Fact]
        public void SpreadsheetTable_DivisionByZero_MarksDivZeroError()
        {
            var table = new SpreadsheetTable(4, 4);
            table.UpdateCell("A1", "10");
            table.UpdateCell("B1", "0");
            table.UpdateCell("C1", "A1 div B1");

            var cellC1 = table.TryGetCell("C1")!;
            Assert.True(cellC1.HasError);
            Assert.Equal("#ДІЛ/0!", cellC1.Error);
        }

        [Fact]
        public void SpreadsheetTable_DisplayModes_SwitchesBetweenValueAndExpression()
        {
            var table = new SpreadsheetTable(3, 3);
            table.SetCellExpression(0, 0, "2 ^ 5 + 8");

            table.DisplayMode = TableDisplayMode.Value;
            Assert.Equal("40", table.GetDisplayText(0, 0));

            table.DisplayMode = TableDisplayMode.Expression;
            Assert.Equal("2 ^ 5 + 8", table.GetDisplayText(0, 0));
        }

        [Fact]
        public void SpreadsheetFileService_SerializeAndDeserialize_PreservesTableAndValues()
        {
            var original = new SpreadsheetTable(6, 5);
            SpreadsheetFileService.LoadVariant6Demo(original);

            string tempFile = Path.Combine(Path.GetTempPath(), $"test_table_{Guid.NewGuid():N}.json");
            try
            {
                var service = new SpreadsheetFileService();
                service.SaveFile(tempFile, original);

                var loaded = service.OpenFile(tempFile);
                Assert.Equal(original.Rows, loaded.Rows);
                Assert.Equal(original.Columns, loaded.Columns);
                Assert.Equal(new BigInteger(11), loaded.TryGetCell("A2")?.Value);
                Assert.Equal(new BigInteger(512), loaded.TryGetCell("C1")?.Value);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }
    }
}
