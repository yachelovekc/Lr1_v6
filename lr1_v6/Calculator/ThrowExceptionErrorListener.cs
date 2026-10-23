using Antlr4.Runtime;
using lr1_v6.Models;

namespace lr1_v6.Calculator
{
    public class ThrowExceptionErrorListener : BaseErrorListener, IAntlrErrorListener<int>
    {
        public override void SyntaxError(
            TextWriter output,
            IRecognizer recognizer,
            IToken offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException e)
        {
            string tokenText = offendingSymbol?.Text ?? "";
            int humanPos = charPositionInLine + 1;
            throw new SyntaxErrorException(
                $"Синтаксична помилка (позиція {humanPos}): {msg}",
                charPositionInLine,
                tokenText);
        }

        public void SyntaxError(
            TextWriter output,
            IRecognizer recognizer,
            int offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException e)
        {
            int humanPos = charPositionInLine + 1;
            throw new SyntaxErrorException(
                $"Лексична помилка (позиція {humanPos}): {msg}",
                charPositionInLine,
                offendingSymbol.ToString());
        }
    }
}
