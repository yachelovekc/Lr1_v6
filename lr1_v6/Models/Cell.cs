using lr1_v6.Grammar;
using System.Numerics;
using System.Text;

namespace lr1_v6.Models
{
    public class Cell
    {
        public string Name { get; set; } = string.Empty;
        public string Expression { get; set; } = string.Empty;
        public BigInteger Value { get; set; } = BigInteger.Zero;
        public string Error { get; set; } = string.Empty;
        public List<string> References { get; set; } = new();

        public int Row { get; set; }
        public int Column { get; set; }
        public LabCalculatorParser.CompileUnitContext? ParsedTree { get; set; }

        public bool HasError => !string.IsNullOrEmpty(Error);
        public string DisplayValue => string.IsNullOrEmpty(Error) ? (string.IsNullOrWhiteSpace(Expression) ? string.Empty : Value.ToString()) : Error;

        public Cell(string name)
        {
            Name = name.ToUpperInvariant();
            if (TryParseCellName(Name, out int r, out int c))
            {
                Row = r;
                Column = c;
            }
        }

        public Cell(int row, int column)
        {
            Row = row;
            Column = column;
            Name = ToCellName(row, column);
        }

        public void SetExpression(string text)
        {
            Expression = text?.Trim() ?? string.Empty;
            Error = string.Empty;
            References.Clear();
            ParsedTree = null;

            if (string.IsNullOrWhiteSpace(Expression))
            {
                Value = BigInteger.Zero;
                return;
            }

            try
            {
                ParsedTree = Calculator.SpreadsheetCalculator.Parse(Expression, out var refs);
                References = refs;
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
        }

        public void Clear()
        {
            Expression = string.Empty;
            Value = BigInteger.Zero;
            Error = string.Empty;
            References.Clear();
            ParsedTree = null;
        }

        public (BigInteger Value, bool Success) CheckSyntaxAndCycle(SpreadsheetTable? table = null)
        {
            if (string.IsNullOrWhiteSpace(Expression))
            {
                Value = BigInteger.Zero;
                Error = string.Empty;
                return (BigInteger.Zero, true);
            }

            try
            {
                ParsedTree = Calculator.SpreadsheetCalculator.Parse(Expression, out var refs);
                References = refs;

                if (table != null && table.HasCycle(Name))
                {
                    Error = "#ЦИКЛ!";
                    return (Value, false);
                }

                Value = Calculator.SpreadsheetCalculator.Evaluate(Expression, table);
                Error = string.Empty;
                return (Value, true);
            }
            catch (CircularReferenceException)
            {
                Error = "#ЦИКЛ!";
                return (Value, false);
            }
            catch (DivideByZeroException)
            {
                Error = "#ДІЛ/0!";
                return (Value, false);
            }
            catch (InvalidCellReferenceException)
            {
                Error = "#ПОСИЛ!";
                return (Value, false);
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                return (Value, false);
            }
        }

        public static string ColumnIndexToName(int colIndex)
        {
            if (colIndex < 0) throw new ArgumentOutOfRangeException(nameof(colIndex));
            var sb = new StringBuilder();
            int n = colIndex;
            while (n >= 0)
            {
                sb.Insert(0, (char)('A' + (n % 26)));
                n = (n / 26) - 1;
            }
            return sb.ToString();
        }

        public static string ToCellName(int row, int column) => $"{ColumnIndexToName(column)}{row + 1}";

        public static bool TryParseCellName(string cellName, out int row, out int column)
        {
            row = -1;
            column = -1;
            if (string.IsNullOrWhiteSpace(cellName)) return false;
            string trimmed = cellName.Trim().ToUpperInvariant();
            int i = 0, colAcc = 0;
            while (i < trimmed.Length && trimmed[i] >= 'A' && trimmed[i] <= 'Z')
            {
                colAcc = colAcc * 26 + (trimmed[i] - 'A' + 1);
                i++;
            }
            if (i == 0 || i == trimmed.Length) return false;
            if (!int.TryParse(trimmed[i..], out int oneBasedRow) || oneBasedRow <= 0) return false;
            column = colAcc - 1;
            row = oneBasedRow - 1;
            return true;
        }
    }
}
