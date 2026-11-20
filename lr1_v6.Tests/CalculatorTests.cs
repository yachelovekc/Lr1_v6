using lr1_v6.Calculator;
using lr1_v6.Models;
using System.Numerics;

namespace lr1_v6.Tests
{
    public class CalculatorTests
    {
        [Theory]
        [InlineData("= 2 + 3", "5")]
        [InlineData("=max(10, 20)", "20")]
        [InlineData("  =  (7 - 2) * 3  ", "15")]
        public void ParseAndEvaluate_LeadingEqualsSign_EvaluatesExpression(string expr, string expected)
        {
            BigInteger actual = SpreadsheetCalculator.Evaluate(expr);
            Assert.Equal(BigInteger.Parse(expected), actual);
        }

        [Theory]
        [InlineData("+42", "42")]
        [InlineData("-42", "-42")]
        [InlineData("--5", "5")]
        [InlineData("-+5", "-5")]
        [InlineData("-(2 + 3) * 4", "-20")]
        [InlineData("-2 ^ 2", "-4")]
        [InlineData("(-2) ^ 2", "4")]
        [InlineData("2 ^ -3", "0")]
        public void Evaluate_UnaryOperatorsAndPrecedence_ReturnsExpectedResult(string expr, string expected)
        {
            BigInteger actual = SpreadsheetCalculator.Evaluate(expr);
            Assert.Equal(BigInteger.Parse(expected), actual);
        }

        [Theory]
        [InlineData("-25 div 7", "-3")]
        [InlineData("-25 mod 7", "-4")]
        [InlineData("25 div -7", "-3")]
        [InlineData("25 mod -7", "4")]
        [InlineData("-25 / 7", "-3")]
        public void Evaluate_DivisionAndModuloWithNegatives_UsesTruncatingIntegerArithmetic(string expr, string expected)
        {
            BigInteger actual = SpreadsheetCalculator.Evaluate(expr);
            Assert.Equal(BigInteger.Parse(expected), actual);
        }

        [Theory]
        [InlineData("0 ^ 0", "1")]
        [InlineData("1 ^ -10", "1")]
        [InlineData("(-1) ^ -2", "1")]
        [InlineData("(-1) ^ -3", "-1")]
        [InlineData("5 ^ -2", "0")]
        [InlineData("1 ^ 10000", "1")]
        public void Evaluate_PowerEdgeCases_ReturnsExpectedResult(string expr, string expected)
        {
            BigInteger actual = SpreadsheetCalculator.Evaluate(expr);
            Assert.Equal(BigInteger.Parse(expected), actual);
        }

        [Fact]
        public void Evaluate_ZeroToNegativePower_ThrowsDivideByZeroException()
        {
            Assert.Throws<DivideByZeroException>(() => SpreadsheetCalculator.Evaluate("0 ^ -1"));
        }

        [Fact]
        public void Evaluate_ExponentAboveLimit_ThrowsOverflowException()
        {
            Assert.Throws<OverflowException>(() => SpreadsheetCalculator.Evaluate("2 ^ 10001"));
        }

        [Theory]
        [InlineData("10 / 0")]
        [InlineData("10 div 0")]
        [InlineData("10 mod 0")]
        public void Evaluate_DivideOrModuloByZero_ThrowsDivideByZeroException(string expr)
        {
            Assert.Throws<DivideByZeroException>(() => SpreadsheetCalculator.Evaluate(expr));
        }

        [Theory]
        [InlineData("max(10; 25)", "25")]
        [InlineData("min(10; 25)", "10")]
        [InlineData("MaX(-5, -12)", "-5")]
        [InlineData("mIn(-5, -12)", "-12")]
        [InlineData("max(7, 7)", "7")]
        [InlineData("min(7, 7)", "7")]
        public void Evaluate_MaxAndMinWithSemicolonAndNegatives_ReturnsExpectedResult(string expr, string expected)
        {
            BigInteger actual = SpreadsheetCalculator.Evaluate(expr);
            Assert.Equal(BigInteger.Parse(expected), actual);
        }

        [Theory]
        [InlineData("12.99", "12")]
        [InlineData("100.05 + 5.9", "105")]
        public void Evaluate_DecimalNumberLiteral_TruncatesFractionalPart(string expr, string expected)
        {
            BigInteger actual = SpreadsheetCalculator.Evaluate(expr);
            Assert.Equal(BigInteger.Parse(expected), actual);
        }

        [Fact]
        public void Evaluate_CellReferenceWithoutTable_ThrowsInvalidCellReferenceException()
        {
            var ex = Assert.Throws<InvalidCellReferenceException>(() => SpreadsheetCalculator.Evaluate("a1 + 5"));
            Assert.Equal("A1", ex.CellName);
        }

        [Theory]
        [InlineData("2 + 3 * 4", true)]
        [InlineData("= max(A1; B2) ^ 2", true)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("=", false)]
        [InlineData("2 + * 3", false)]
        [InlineData("()", false)]
        public void CheckSyntax_VariousExpressions_ReturnsExpectedValidity(string expr, bool expected)
        {
            Assert.Equal(expected, SpreadsheetCalculator.CheckSyntax(expr));
        }

        [Fact]
        public void FindReferences_ValidExpression_ReturnsDistinctUppercaseReferences()
        {
            var refs = SpreadsheetCalculator.FindReferences("= a1 + max(A1, b12) * c3");
            Assert.Equal(new[] { "A1", "B12", "C3" }, refs);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("10 + * 5")]
        public void FindReferences_EmptyOrInvalidExpression_ReturnsEmptyList(string expr)
        {
            var refs = SpreadsheetCalculator.FindReferences(expr);
            Assert.Empty(refs);
        }

        [Fact]
        public void CellReferenceCollectorVisitor_VisitsParsedTree_CollectsUniqueUppercaseNames()
        {
            var tree = SpreadsheetCalculator.Parse("b2 + B2 + aa10", out _);
            var visitor = new CellReferenceCollectorVisitor();
            visitor.Visit(tree);

            Assert.Equal(new[] { "B2", "AA10" }, visitor.ReferencedCells);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("=")]
        [InlineData("=   ")]
        [InlineData("()")]
        [InlineData("2 + 3 4")]
        [InlineData("max()")]
        [InlineData("max(1, )")]
        [InlineData("12.")]
        [InlineData("A1B + 2")]
        [InlineData("А1 + 2")]
        public void Parse_InvalidExpressions_ThrowsSyntaxErrorException(string badExpr)
        {
            var ex = Assert.Throws<SyntaxErrorException>(() => SpreadsheetCalculator.Parse(badExpr, out _));
            Assert.True(ex.Position >= 0);
            Assert.False(string.IsNullOrWhiteSpace(ex.Message));
        }

        [Fact]
        public void Parse_LexerErrorAtKnownPosition_ReportsZeroBasedPosition()
        {
            var ex = Assert.Throws<SyntaxErrorException>(() => SpreadsheetCalculator.Parse("10 @ 2", out _));
            Assert.Equal(3, ex.Position);
        }
    }
}
