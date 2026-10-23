using lr1_v6.Calculator;
using System.Numerics;

namespace lr1_v6.Models
{
    public enum TableDisplayMode
    {
        Value,
        Expression
    }

    public class SpreadsheetTable
    {
        public Dictionary<string, Cell> Cells { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public int Rows { get; set; }
        public int Columns { get; set; }

        public TableDisplayMode DisplayMode { get; set; } = TableDisplayMode.Value;
        public int RowCount => Rows;
        public int ColumnCount => Columns;

        public SpreadsheetTable(int initialRows = 8, int initialColumns = 6)
        {
            if (initialRows < 1) initialRows = 1;
            if (initialColumns < 1) initialColumns = 1;

            Rows = initialRows;
            Columns = initialColumns;

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    var cell = new Cell(r, c);
                    Cells[cell.Name] = cell;
                }
            }
        }

        public void UpdateCell(string name, string expression)
        {
            string norm = name.ToUpperInvariant();
            if (!Cells.TryGetValue(norm, out var cell))
            {
                if (Cell.TryParseCellName(norm, out int r, out int c))
                {
                    if (r >= Rows) Rows = r + 1;
                    if (c >= Columns) Columns = c + 1;
                    cell = new Cell(r, c);
                    Cells[norm] = cell;
                }
                else
                {
                    throw new InvalidCellReferenceException(norm, "Некоректний формат імені клітинки.");
                }
            }

            cell.SetExpression(expression);
            RecalculateAll();
        }

        public BigInteger GetValue(string name)
        {
            string norm = name.ToUpperInvariant();
            if (!Cells.TryGetValue(norm, out var cell))
            {
                throw new InvalidCellReferenceException(norm, "Клітинка не знайдена в поточній таблиці.");
            }

            if (!string.IsNullOrEmpty(cell.Error))
            {
                throw new InvalidOperationException($"Клітинка '{norm}' містить помилку ({cell.Error}).");
            }

            return cell.Value;
        }

        public bool HasCycle(string name)
        {
            string target = name.ToUpperInvariant();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var recursionStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            bool HasCycleDfs(string current)
            {
                visited.Add(current);
                recursionStack.Add(current);

                if (Cells.TryGetValue(current, out var cell))
                {
                    foreach (var neighbor in cell.References)
                    {
                        string normNeighbor = neighbor.ToUpperInvariant();
                        if (string.Equals(normNeighbor, target, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }

                        if (!visited.Contains(normNeighbor))
                        {
                            if (HasCycleDfs(normNeighbor)) return true;
                        }
                        else if (recursionStack.Contains(normNeighbor))
                        {
                            return true;
                        }
                    }
                }

                recursionStack.Remove(current);
                return false;
            }

            return HasCycleDfs(target);
        }

        public void RecalculateAll()
        {
            foreach (var cell in Cells.Values)
            {
                if (string.IsNullOrWhiteSpace(cell.Expression))
                {
                    cell.Value = BigInteger.Zero;
                    cell.Error = string.Empty;
                    cell.ParsedTree = null;
                    cell.References.Clear();
                    continue;
                }

                try
                {
                    cell.ParsedTree = SpreadsheetCalculator.Parse(cell.Expression, out var refs);
                    cell.References = refs;
                    cell.Error = string.Empty;
                }
                catch (Exception ex)
                {
                    cell.Error = ex.Message;
                    cell.ParsedTree = null;
                }
            }

            foreach (var cell in Cells.Values)
            {
                if (string.IsNullOrWhiteSpace(cell.Expression)) continue;
                if (!string.IsNullOrEmpty(cell.Error) && cell.ParsedTree == null) continue;

                if (HasCycle(cell.Name))
                {
                    cell.Error = "#ЦИКЛ!";
                }
            }

            var states = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var callStack = new List<string>();

            BigInteger EvaluateCell(Cell cell)
            {
                if (cell.Error == "#ЦИКЛ!")
                {
                    throw new CircularReferenceException(cell.Name);
                }

                if (string.IsNullOrWhiteSpace(cell.Expression))
                {
                    states[cell.Name] = 2;
                    cell.Value = BigInteger.Zero;
                    cell.Error = string.Empty;
                    return BigInteger.Zero;
                }

                if (!string.IsNullOrEmpty(cell.Error) && cell.ParsedTree == null)
                {
                    throw new InvalidOperationException(cell.Error);
                }

                if (states.TryGetValue(cell.Name, out int state))
                {
                    if (state == 1)
                    {
                        throw new CircularReferenceException(cell.Name);
                    }
                    if (state == 2)
                    {
                        if (!string.IsNullOrEmpty(cell.Error))
                        {
                            throw new InvalidOperationException(cell.Error);
                        }
                        return cell.Value;
                    }
                }

                states[cell.Name] = 1;
                callStack.Add(cell.Name);

                try
                {
                    foreach (var refName in cell.References)
                    {
                        if (Cells.TryGetValue(refName, out var refCell))
                        {
                            EvaluateCell(refCell);
                        }
                        else
                        {
                            throw new InvalidCellReferenceException(refName, "Клітинка не знайдена.");
                        }
                    }

                    var evaluator = new EvaluatorVariant6(this);
                    BigInteger result = evaluator.Visit(cell.ParsedTree!);
                    cell.Value = result;
                    cell.Error = string.Empty;
                    states[cell.Name] = 2;
                    return result;
                }
                catch (CircularReferenceException)
                {
                    cell.Error = "#ЦИКЛ!";
                    states[cell.Name] = 2;
                    throw;
                }
                catch (DivideByZeroException)
                {
                    cell.Error = "#ДІЛ/0!";
                    states[cell.Name] = 2;
                    throw;
                }
                catch (InvalidCellReferenceException)
                {
                    cell.Error = "#ПОСИЛ!";
                    states[cell.Name] = 2;
                    throw;
                }
                catch (Exception ex)
                {
                    cell.Error = ex.Message;
                    states[cell.Name] = 2;
                    throw;
                }
                finally
                {
                    callStack.RemoveAt(callStack.Count - 1);
                }
            }

            foreach (var cell in Cells.Values)
            {
                if (string.IsNullOrWhiteSpace(cell.Expression) || (!string.IsNullOrEmpty(cell.Error) && cell.ParsedTree == null))
                {
                    continue;
                }

                if (!states.ContainsKey(cell.Name))
                {
                    try
                    {
                        EvaluateCell(cell);
                    }
                    catch
                    {
                    }
                }
            }
        }

        public void AddRow()
        {
            int newRow = Rows;
            Rows++;
            for (int c = 0; c < Columns; c++)
            {
                var cell = new Cell(newRow, c);
                Cells[cell.Name] = cell;
            }
            RecalculateAll();
        }

        public bool RemoveRow()
        {
            if (Rows <= 1) return false;
            int removeRow = Rows - 1;
            Rows--;
            for (int c = 0; c < Columns; c++)
            {
                string name = Cell.ToCellName(removeRow, c);
                Cells.Remove(name);
            }
            RecalculateAll();
            return true;
        }

        public void AddColumn()
        {
            int newCol = Columns;
            Columns++;
            for (int r = 0; r < Rows; r++)
            {
                var cell = new Cell(r, newCol);
                Cells[cell.Name] = cell;
            }
            RecalculateAll();
        }

        public bool RemoveColumn()
        {
            if (Columns <= 1) return false;
            int removeCol = Columns - 1;
            Columns--;
            for (int r = 0; r < Rows; r++)
            {
                string name = Cell.ToCellName(r, removeCol);
                Cells.Remove(name);
            }
            RecalculateAll();
            return true;
        }

        public Cell GetCell(int r, int c)
        {
            string name = Cell.ToCellName(r, c);
            if (!Cells.TryGetValue(name, out var cell))
            {
                cell = new Cell(r, c);
                Cells[name] = cell;
            }
            return cell;
        }

        public Cell? TryGetCell(string name)
        {
            string norm = name.ToUpperInvariant();
            return Cells.TryGetValue(norm, out var cell) ? cell : null;
        }

        public void SetCellExpression(int r, int c, string? expr)
        {
            string name = Cell.ToCellName(r, c);
            UpdateCell(name, expr ?? string.Empty);
        }

        public void SetCellExpression(string name, string? expr)
        {
            UpdateCell(name, expr ?? string.Empty);
        }

        public string GetDisplayText(int r, int c)
        {
            var cell = GetCell(r, c);
            if (DisplayMode == TableDisplayMode.Expression)
            {
                return cell.Expression;
            }
            return string.IsNullOrEmpty(cell.Error)
                ? (string.IsNullOrWhiteSpace(cell.Expression) ? string.Empty : cell.Value.ToString())
                : cell.Error;
        }

        public void ClearAll()
        {
            foreach (var cell in Cells.Values)
            {
                cell.Clear();
            }
        }

        public void ResizeAndClear(int rows, int cols)
        {
            if (rows < 1) rows = 1;
            if (cols < 1) cols = 1;
            Rows = rows;
            Columns = cols;
            Cells.Clear();
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    var cell = new Cell(r, c);
                    Cells[cell.Name] = cell;
                }
            }
        }
    }
}
