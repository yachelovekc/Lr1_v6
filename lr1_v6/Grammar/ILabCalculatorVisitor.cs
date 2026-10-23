using Antlr4.Runtime.Tree;

namespace lr1_v6.Grammar
{
    public interface ILabCalculatorVisitor<Result> : IParseTreeVisitor<Result>
    {
        Result VisitCompileUnit(LabCalculatorParser.CompileUnitContext context);
        Result VisitParenthesizedExpr(LabCalculatorParser.ParenthesizedExprContext context);
        Result VisitUnaryExpr(LabCalculatorParser.UnaryExprContext context);
        Result VisitExponentialExpr(LabCalculatorParser.ExponentialExprContext context);
        Result VisitMultiplicativeExpr(LabCalculatorParser.MultiplicativeExprContext context);
        Result VisitAdditiveExpr(LabCalculatorParser.AdditiveExprContext context);
        Result VisitMaxExpr(LabCalculatorParser.MaxExprContext context);
        Result VisitMinExpr(LabCalculatorParser.MinExprContext context);
        Result VisitNumberExpr(LabCalculatorParser.NumberExprContext context);
        Result VisitIdentifierExpr(LabCalculatorParser.IdentifierExprContext context);
    }
}
