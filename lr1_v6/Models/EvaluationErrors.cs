namespace lr1_v6.Models
{
    public class SyntaxErrorException : Exception
    {
        public int Position { get; }
        public string OffendingToken { get; }

        public SyntaxErrorException(string message, int position = -1, string offendingToken = "")
            : base(message)
        {
            Position = position;
            OffendingToken = offendingToken;
        }
    }

    public class CircularReferenceException : Exception
    {
        public string CyclePath { get; }

        public CircularReferenceException(string cyclePath)
            : base($"Виявлено циклічне посилання: {cyclePath}")
        {
            CyclePath = cyclePath;
        }
    }

    public class InvalidCellReferenceException : Exception
    {
        public string CellName { get; }

        public InvalidCellReferenceException(string cellName, string reason)
            : base($"Некоректне посилання на клітинку '{cellName}': {reason}")
        {
            CellName = cellName;
        }
    }
}
