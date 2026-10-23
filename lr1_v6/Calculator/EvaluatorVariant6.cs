using Antlr4.Runtime.Tree;
using lr1_v6.Grammar;
using lr1_v6.Models;
using System.Numerics;

namespace lr1_v6.Calculator
{
    public class EvaluatorVariant6 : LabCalculatorBaseVisitor<BigInteger>
    {
        public SpreadsheetTable? Table { get; set; }

        public EvaluatorVariant6(SpreadsheetTable? table = null)
        {
            Table = table;
        }

        public override BigInteger VisitCompileUnit(LabCalculatorParser.CompileUnitContext context)
        {
            return Visit(context.expression());
        }

        public override BigInteger VisitParenthesizedExpr(LabCalculatorParser.ParenthesizedExprContext context)
        {
            return Visit(context.expression());
        }

        public override BigInteger VisitUnaryExpr(LabCalculatorParser.UnaryExprContext context)
        {
            BigInteger operand = Visit(context.expression());
            int opType = context.operatorToken?.Type ?? LabCalculatorLexer.ADD;
            return opType == LabCalculatorLexer.SUBTRACT ? -operand : operand;
        }

        public override BigInteger VisitAdditiveExpr(LabCalculatorParser.AdditiveExprContext context)
        {
            return AddSubtract(context);
        }

        public BigInteger AddSubtract(LabCalculatorParser.AdditiveExprContext context)
        {
            BigInteger left = Visit(context.expression(0));
            BigInteger right = Visit(context.expression(1));
            int opType = context.operatorToken?.Type ?? LabCalculatorLexer.ADD;

            return opType switch
            {
                LabCalculatorLexer.ADD => left + right,
                LabCalculatorLexer.SUBTRACT => left - right,
                _ => throw new InvalidOperationException($"Невідома операція: {context.operatorToken?.Text}")
            };
        }

        public override BigInteger VisitMultiplicativeExpr(LabCalculatorParser.MultiplicativeExprContext context)
        {
            return MultiplyDivide(context);
        }

        public BigInteger MultiplyDivide(LabCalculatorParser.MultiplicativeExprContext context)
        {
            BigInteger left = Visit(context.expression(0));
            BigInteger right = Visit(context.expression(1));
            int opType = context.operatorToken?.Type ?? LabCalculatorLexer.MULTIPLY;

            if ((opType == LabCalculatorLexer.DIVIDE ||
                 opType == LabCalculatorLexer.DIV ||
                 opType == LabCalculatorLexer.MOD) && right.IsZero)
            {
                throw new DivideByZeroException("Ділення на нуль в операції /, div або mod.");
            }

            return opType switch
            {
                LabCalculatorLexer.MULTIPLY => left * right,
                LabCalculatorLexer.DIVIDE => left / right,
                LabCalculatorLexer.DIV => BigInteger.Divide(left, right),
                LabCalculatorLexer.MOD => BigInteger.Remainder(left, right),
                _ => throw new InvalidOperationException($"Невідома операція: {context.operatorToken?.Text}")
            };
        }

        public override BigInteger VisitExponentialExpr(LabCalculatorParser.ExponentialExprContext context)
        {
            return Power(context);
        }

        public BigInteger Power(LabCalculatorParser.ExponentialExprContext context)
        {
            BigInteger @base = Visit(context.expression(0));
            BigInteger exp = Visit(context.expression(1));

            if (exp.IsZero)
            {
                return BigInteger.One;
            }

            if (exp < BigInteger.Zero)
            {
                if (@base.IsZero)
                {
                    throw new DivideByZeroException("Піднесення нуля до від'ємного степеня.");
                }
                if (@base == BigInteger.One) return BigInteger.One;
                if (@base == -BigInteger.One) return exp % 2 == 0 ? BigInteger.One : -BigInteger.One;
                return BigInteger.Zero;
            }

            if (exp > 10000)
            {
                throw new OverflowException("Показник степеня перевищує 10000.");
            }

            return BigInteger.Pow(@base, (int)exp);
        }

        public override BigInteger VisitMaxExpr(LabCalculatorParser.MaxExprContext context)
        {
            return MaxMin(context);
        }

        public override BigInteger VisitMinExpr(LabCalculatorParser.MinExprContext context)
        {
            return MaxMin(context);
        }

        public BigInteger MaxMin(LabCalculatorParser.ExpressionContext context)
        {
            if (context is LabCalculatorParser.MaxExprContext maxCtx)
            {
                BigInteger a = Visit(maxCtx.expression(0));
                BigInteger b = Visit(maxCtx.expression(1));
                return a >= b ? a : b;
            }
            if (context is LabCalculatorParser.MinExprContext minCtx)
            {
                BigInteger a = Visit(minCtx.expression(0));
                BigInteger b = Visit(minCtx.expression(1));
                return a <= b ? a : b;
            }
            throw new ArgumentException("Очікувався контекст MaxExpr або MinExpr.");
        }

        public override BigInteger VisitNumberExpr(LabCalculatorParser.NumberExprContext context)
        {
            return GetNumber(context);
        }

        public BigInteger GetNumber(LabCalculatorParser.NumberExprContext context)
        {
            string text = context.NUMBER().GetText();
            if (text.Contains('.'))
            {
                text = text.Split('.')[0];
            }
            return BigInteger.Parse(text);
        }

        public override BigInteger VisitIdentifierExpr(LabCalculatorParser.IdentifierExprContext context)
        {
            return GetCell(context);
        }

        public BigInteger GetCell(LabCalculatorParser.IdentifierExprContext context)
        {
            string cellName = context.IDENTIFIER().GetText().ToUpperInvariant();
            if (Table == null)
            {
                throw new InvalidCellReferenceException(cellName, "Таблицю не передано для розв'язання посилань.");
            }
            return Table.GetValue(cellName);
        }
    }
}
