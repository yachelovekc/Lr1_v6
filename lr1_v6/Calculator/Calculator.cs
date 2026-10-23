using Antlr4.Runtime;
using lr1_v6.Grammar;
using lr1_v6.Models;
using System.Numerics;

namespace lr1_v6.Calculator
{
    public static class SpreadsheetCalculator
    {
        public static bool CheckSyntax(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) return false;
            try
            {
                Parse(expression, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static List<string> FindReferences(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) return new List<string>();
            try
            {
                Parse(expression, out var refs);
                return refs;
            }
            catch
            {
                return new List<string>();
            }
        }

        public static BigInteger Evaluate(string expression, SpreadsheetTable? table = null)
        {
            var tree = Parse(expression, out _);
            var evaluator = new EvaluatorVariant6(table);
            return evaluator.Visit(tree);
        }

        public static LabCalculatorParser.CompileUnitContext Parse(string expression, out List<string> references)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                references = new List<string>();
                throw new SyntaxErrorException("Порожній вираз.", 0);
            }

            string normalized = expression.Trim();
            if (normalized.StartsWith('='))
            {
                normalized = normalized[1..].Trim();
                if (string.IsNullOrWhiteSpace(normalized))
                {
                    references = new List<string>();
                    throw new SyntaxErrorException("Синтаксична помилка: після знаку '=' очікується вираз.", 0, "=");
                }
            }

            var errorListener = new ThrowExceptionErrorListener();
            var inputStream = new AntlrInputStream(normalized);

            var lexer = new LabCalculatorLexer(inputStream);
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(errorListener);

            var commonTokenStream = new CommonTokenStream(lexer);
            var parser = new LabCalculatorParser(commonTokenStream);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);

            var tree = parser.compileUnit();

            var collector = new CellReferenceCollectorVisitor();
            collector.Visit(tree);
            references = collector.ReferencedCells;

            return tree;
        }
    }
}
