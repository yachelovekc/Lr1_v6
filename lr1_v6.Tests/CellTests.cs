using lr1_v6.Models;
using System.Numerics;

namespace lr1_v6.Tests
{
    public class CellTests
    {
        [Fact]
        public void Constructor_FromCellName_SetsUppercaseNameAndCoordinates()
        {
            var cell = new Cell("b3");

            Assert.Equal("B3", cell.Name);
            Assert.Equal(2, cell.Row);
            Assert.Equal(1, cell.Column);
            Assert.False(cell.HasError);
            Assert.Equal(string.Empty, cell.DisplayValue);
        }

        [Fact]
        public void Constructor_FromRowAndColumn_SetsNameAndCoordinates()
        {
            var cell = new Cell(4, 2);

            Assert.Equal("C5", cell.Name);
            Assert.Equal(4, cell.Row);
            Assert.Equal(2, cell.Column);
        }

        [Theory]
        [InlineData(0, "A")]
        [InlineData(25, "Z")]
        [InlineData(26, "AA")]
        [InlineData(27, "AB")]
        [InlineData(51, "AZ")]
        [InlineData(52, "BA")]
        [InlineData(701, "ZZ")]
        [InlineData(702, "AAA")]
        public void ColumnIndexToName_ValidIndices_ReturnsExpectedLetters(int index, string expected)
        {
            Assert.Equal(expected, Cell.ColumnIndexToName(index));
        }

        [Fact]
        public void ColumnIndexToName_NegativeIndex_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Cell.ColumnIndexToName(-1));
        }

        [Theory]
        [InlineData(0, 0, "A1")]
        [InlineData(9, 26, "AA10")]
        public void ToCellName_ReturnsExpectedAddress(int row, int col, string expected)
        {
            Assert.Equal(expected, Cell.ToCellName(row, col));
        }

        [Theory]
        [InlineData("A1", true, 0, 0)]
        [InlineData("  b12 ", true, 11, 1)]
        [InlineData("AA10", true, 9, 26)]
        [InlineData("zz100", true, 99, 701)]
        [InlineData("", false, -1, -1)]
        [InlineData("   ", false, -1, -1)]
        [InlineData("1A", false, -1, -1)]
        [InlineData("A", false, -1, -1)]
        [InlineData("123", false, -1, -1)]
        [InlineData("A0", false, -1, -1)]
        [InlineData("A-1", false, -1, -1)]
        [InlineData("A1B", false, -1, -1)]
        public void TryParseCellName_VariousInputs_ParsesAsExpected(
            string input,
            bool expectedResult,
            int expectedRow,
            int expectedCol)
        {
            bool ok = Cell.TryParseCellName(input, out int row, out int col);

            Assert.Equal(expectedResult, ok);
            Assert.Equal(expectedRow, row);
            Assert.Equal(expectedCol, col);
        }

        [Fact]
        public void SetExpression_ValidExpression_TrimsAndExtractsReferencesAndTree()
        {
            var cell = new Cell("A1");
            cell.SetExpression("  b1 + c2 * 2  ");

            Assert.Equal("b1 + c2 * 2", cell.Expression);
            Assert.False(cell.HasError);
            Assert.NotNull(cell.ParsedTree);
            Assert.Equal(new[] { "B1", "C2" }, cell.References);
        }

        [Fact]
        public void SetExpression_InvalidSyntax_SetsErrorAndClearsTree()
        {
            var cell = new Cell("A1");
            cell.SetExpression("10 + * 5");

            Assert.True(cell.HasError);
            Assert.Null(cell.ParsedTree);
            Assert.Equal(cell.Error, cell.DisplayValue);
        }

        [Fact]
        public void SetExpression_EmptyString_ResetsValueAndError()
        {
            var cell = new Cell("A1");
            cell.Value = new BigInteger(42);
            cell.SetExpression("   ");

            Assert.Equal(string.Empty, cell.Expression);
            Assert.Equal(BigInteger.Zero, cell.Value);
            Assert.False(cell.HasError);
            Assert.Equal(string.Empty, cell.DisplayValue);
        }

        [Fact]
        public void Clear_ResetsAllCellFields()
        {
            var cell = new Cell("A1");
            cell.SetExpression("B1 + 5");
            cell.Value = new BigInteger(15);

            cell.Clear();

            Assert.Equal(string.Empty, cell.Expression);
            Assert.Equal(BigInteger.Zero, cell.Value);
            Assert.Equal(string.Empty, cell.Error);
            Assert.Empty(cell.References);
            Assert.Null(cell.ParsedTree);
            Assert.Equal(string.Empty, cell.DisplayValue);
        }

        [Fact]
        public void CheckSyntaxAndCycle_EmptyExpression_ReturnsZeroAndSuccess()
        {
            var cell = new Cell("A1");
            var (value, success) = cell.CheckSyntaxAndCycle();

            Assert.True(success);
            Assert.Equal(BigInteger.Zero, value);
            Assert.False(cell.HasError);
        }

        [Fact]
        public void CheckSyntaxAndCycle_ValidExpressionWithTable_EvaluatesAndUpdatesDisplayValue()
        {
            var table = new SpreadsheetTable(2, 2);
            table.UpdateCell("B1", "7");
            var cell = table.GetCell(0, 0);
            cell.Expression = "B1 * 6";

            var (value, success) = cell.CheckSyntaxAndCycle(table);

            Assert.True(success);
            Assert.Equal(new BigInteger(42), value);
            Assert.Equal("42", cell.DisplayValue);
        }

        [Fact]
        public void CheckSyntaxAndCycle_DivideByZero_SetsDivZeroError()
        {
            var cell = new Cell("A1");
            cell.Expression = "10 / 0";

            var (_, success) = cell.CheckSyntaxAndCycle();

            Assert.False(success);
            Assert.Equal("#ДІЛ/0!", cell.Error);
        }

        [Fact]
        public void CheckSyntaxAndCycle_MissingReference_SetsRefError()
        {
            var table = new SpreadsheetTable(2, 2);
            var cell = table.GetCell(0, 0);
            cell.Expression = "Z99 + 1";

            var (_, success) = cell.CheckSyntaxAndCycle(table);

            Assert.False(success);
            Assert.Equal("#ПОСИЛ!", cell.Error);
        }

        [Fact]
        public void CheckSyntaxAndCycle_CycleInTable_SetsCycleError()
        {
            var table = new SpreadsheetTable(2, 2);
            table.UpdateCell("B1", "A1 + 1");
            var cellA1 = table.GetCell(0, 0);
            cellA1.SetExpression("B1 + 1");

            var (_, success) = cellA1.CheckSyntaxAndCycle(table);

            Assert.False(success);
            Assert.Equal("#ЦИКЛ!", cellA1.Error);
        }
    }
}
