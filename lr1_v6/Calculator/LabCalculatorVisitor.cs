using lr1_v6.Grammar;

namespace lr1_v6.Calculator
{
    public class CellReferenceCollectorVisitor : LabCalculatorBaseVisitor<int>
    {
        public List<string> ReferencedCells { get; } = new();

        public override int VisitIdentifierExpr(LabCalculatorParser.IdentifierExprContext context)
        {
            string cellName = context.IDENTIFIER().GetText().ToUpperInvariant();
            if (!ReferencedCells.Contains(cellName))
            {
                ReferencedCells.Add(cellName);
            }
            return 0;
        }
    }
}
