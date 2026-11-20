using lr1_v6.Models;
using System.Numerics;

namespace lr1_v6.Tests
{
    public class SpreadsheetTableTests
    {
        [Fact]
        public void Constructor_SmallDimensions_ClampsToMinimumOneByOne()
        {
            var table = new SpreadsheetTable(0, -3);

            Assert.Equal(1, table.Rows);
            Assert.Equal(1, table.Columns);
            Assert.Equal(1, table.RowCount);
            Assert.Equal(1, table.ColumnCount);
            Assert.NotNull(table.TryGetCell("A1"));
        }

        [Fact]
        public void UpdateCell_LowercaseAndOutOfBoundsValidName_ExpandsTableAndEvaluates()
        {
            var table = new SpreadsheetTable(2, 2);
            table.UpdateCell("d5", "10 + 15");

            Assert.Equal(5, table.Rows);
            Assert.Equal(4, table.Columns);
            Assert.Equal(new BigInteger(25), table.TryGetCell("D5")?.Value);
        }

        [Fact]
        public void UpdateCell_InvalidCellName_ThrowsInvalidCellReferenceException()
        {
            var table = new SpreadsheetTable(2, 2);
            Assert.Throws<InvalidCellReferenceException>(() => table.UpdateCell("INVALID", "5"));
        }

        [Fact]
        public void GetValue_MissingOrErrorCell_ThrowsExpectedException()
        {
            var table = new SpreadsheetTable(2, 2);
            table.UpdateCell("A1", "10 / 0");

            Assert.Throws<InvalidCellReferenceException>(() => table.GetValue("Z99"));
            Assert.Throws<InvalidOperationException>(() => table.GetValue("A1"));
        }

        [Fact]
        public void RecalculateAll_DiamondDependencyGraph_IsNotTreatedAsCycle()
        {
            var table = new SpreadsheetTable(4, 4);
            table.UpdateCell("A1", "2");
            table.UpdateCell("B1", "A1 + 3");
            table.UpdateCell("C1", "A1 * 4");
            table.UpdateCell("D1", "B1 + C1");

            var d1 = table.TryGetCell("D1")!;
            Assert.False(d1.HasError);
            Assert.False(table.HasCycle("D1"));
            Assert.Equal(new BigInteger(13), d1.Value);

            table.UpdateCell("A1", "10");
            Assert.Equal(new BigInteger(53), d1.Value);
        }

        [Fact]
        public void RecalculateAll_SelfReferenceAndCycleRecovery_UpdatesErrorsAndValues()
        {
            var table = new SpreadsheetTable(3, 3);
            table.UpdateCell("A1", "A1 + 1");

            var a1 = table.TryGetCell("A1")!;
            Assert.True(a1.HasError);
            Assert.Equal("#ЦИКЛ!", a1.Error);

            table.UpdateCell("A1", "42");
            Assert.False(a1.HasError);
            Assert.Equal(new BigInteger(42), a1.Value);
        }

        [Fact]
        public void RecalculateAll_OutOfBoundsReference_MarksRefError()
        {
            var table = new SpreadsheetTable(3, 3);
            table.UpdateCell("A1", "Z99 + 1");

            var a1 = table.TryGetCell("A1")!;
            Assert.True(a1.HasError);
            Assert.Equal("#ПОСИЛ!", a1.Error);
        }

        [Fact]
        public void RecalculateAll_EmptyReferencedCell_EvaluatesAsZeroAndUpdatesWhenCleared()
        {
            var table = new SpreadsheetTable(3, 3);
            table.UpdateCell("B1", "A1 + 5");

            Assert.Equal(new BigInteger(5), table.TryGetCell("B1")?.Value);

            table.UpdateCell("A1", "20");
            Assert.Equal(new BigInteger(25), table.TryGetCell("B1")?.Value);

            table.UpdateCell("A1", "");
            Assert.Equal(new BigInteger(5), table.TryGetCell("B1")?.Value);
        }

        [Fact]
        public void RecalculateAll_ErrorPropagation_MarksDependentCellWithError()
        {
            var table = new SpreadsheetTable(3, 3);
            table.UpdateCell("A1", "10 / 0");
            table.UpdateCell("B1", "A1 + 1");

            Assert.Equal("#ДІЛ/0!", table.TryGetCell("A1")?.Error);
            Assert.True(table.TryGetCell("B1")?.HasError);
        }

        [Fact]
        public void AddAndRemoveRowsAndColumns_UpdatesDimensionsAndRecalculatesDependents()
        {
            var table = new SpreadsheetTable(2, 2);
            table.AddRow();
            table.AddColumn();

            Assert.Equal(3, table.Rows);
            Assert.Equal(3, table.Columns);
            Assert.NotNull(table.TryGetCell("C3"));

            table.UpdateCell("A1", "C3 + 1");
            table.UpdateCell("C3", "9");
            Assert.Equal(new BigInteger(10), table.TryGetCell("A1")?.Value);

            Assert.True(table.RemoveRow());
            Assert.Equal(2, table.Rows);
            Assert.Null(table.TryGetCell("C3"));
            Assert.Equal("#ПОСИЛ!", table.TryGetCell("A1")?.Error);

            Assert.True(table.RemoveColumn());
            Assert.Equal(2, table.Columns);

            Assert.True(table.RemoveRow());
            Assert.False(table.RemoveRow());
            Assert.Equal(1, table.Rows);

            Assert.True(table.RemoveColumn());
            Assert.False(table.RemoveColumn());
            Assert.Equal(1, table.Columns);
        }

        [Fact]
        public void GetDisplayText_HandlesEmptyValueAndErrorInBothModes()
        {
            var table = new SpreadsheetTable(2, 2);
            table.SetCellExpression("A1", "10 / 0");
            table.SetCellExpression(0, 1, null);

            table.DisplayMode = TableDisplayMode.Value;
            Assert.Equal("#ДІЛ/0!", table.GetDisplayText(0, 0));
            Assert.Equal(string.Empty, table.GetDisplayText(0, 1));

            table.DisplayMode = TableDisplayMode.Expression;
            Assert.Equal("10 / 0", table.GetDisplayText(0, 0));
            Assert.Equal(string.Empty, table.GetDisplayText(0, 1));
        }

        [Fact]
        public void ClearAllAndResizeAndClear_ResetCellsAndDimensions()
        {
            var table = new SpreadsheetTable(3, 3);
            table.UpdateCell("A1", "123");
            table.ClearAll();

            Assert.Equal(3, table.Rows);
            Assert.Equal(string.Empty, table.TryGetCell("A1")?.Expression);
            Assert.Equal(BigInteger.Zero, table.TryGetCell("A1")?.Value);

            table.UpdateCell("B2", "45");
            table.ResizeAndClear(0, 2);

            Assert.Equal(1, table.Rows);
            Assert.Equal(2, table.Columns);
            Assert.Null(table.TryGetCell("B2"));
            Assert.Equal(string.Empty, table.TryGetCell("A1")?.Expression);
        }
    }
}
