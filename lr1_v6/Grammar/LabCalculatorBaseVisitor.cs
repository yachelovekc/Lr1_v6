using Antlr4.Runtime.Tree;

namespace lr1_v6.Grammar
{
    public class LabCalculatorBaseVisitor<Result> : AbstractParseTreeVisitor<Result>, ILabCalculatorVisitor<Result>
    {
        public virtual Result VisitCompileUnit(LabCalculatorParser.CompileUnitContext context) => VisitChildren(context);
        public virtual Result VisitParenthesizedExpr(LabCalculatorParser.ParenthesizedExprContext context) => VisitChildren(context);
        public virtual Result VisitUnaryExpr(LabCalculatorParser.UnaryExprContext context) => VisitChildren(context);
        public virtual Result VisitExponentialExpr(LabCalculatorParser.ExponentialExprContext context) => VisitChildren(context);
        public virtual Result VisitMultiplicativeExpr(LabCalculatorParser.MultiplicativeExprContext context) => VisitChildren(context);
        public virtual Result VisitAdditiveExpr(LabCalculatorParser.AdditiveExprContext context) => VisitChildren(context);
        public virtual Result VisitMaxExpr(LabCalculatorParser.MaxExprContext context) => VisitChildren(context);
        public virtual Result VisitMinExpr(LabCalculatorParser.MinExprContext context) => VisitChildren(context);
        public virtual Result VisitNumberExpr(LabCalculatorParser.NumberExprContext context) => VisitChildren(context);
        public virtual Result VisitIdentifierExpr(LabCalculatorParser.IdentifierExprContext context) => VisitChildren(context);
    }
}
