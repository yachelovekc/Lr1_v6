using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace lr1_v6.Grammar
{
    public class LabCalculatorParser
    {
        public const int
            MAX = LabCalculatorLexer.MAX,
            MIN = LabCalculatorLexer.MIN,
            MOD = LabCalculatorLexer.MOD,
            DIV = LabCalculatorLexer.DIV,
            NUMBER = LabCalculatorLexer.NUMBER,
            IDENTIFIER = LabCalculatorLexer.IDENTIFIER,
            EXPONENT = LabCalculatorLexer.EXPONENT,
            MULTIPLY = LabCalculatorLexer.MULTIPLY,
            DIVIDE = LabCalculatorLexer.DIVIDE,
            SUBTRACT = LabCalculatorLexer.SUBTRACT,
            ADD = LabCalculatorLexer.ADD,
            LPAREN = LabCalculatorLexer.LPAREN,
            RPAREN = LabCalculatorLexer.RPAREN,
            COMMA = LabCalculatorLexer.COMMA,
            SEMICOLON = LabCalculatorLexer.SEMICOLON,
            WS = LabCalculatorLexer.WS;

        private readonly ITokenStream _input;
        private readonly List<IAntlrErrorListener<IToken>> _errorListeners = new();

        public LabCalculatorParser(ITokenStream input)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
        }

        public void RemoveErrorListeners() => _errorListeners.Clear();

        public void AddErrorListener(IAntlrErrorListener<IToken> listener)
        {
            if (listener != null)
            {
                _errorListeners.Add(listener);
            }
        }

        private IToken CurrentToken => _input.LT(1);

        public CompileUnitContext compileUnit()
        {
            var ctx = new CompileUnitContext(null, 0)
            {
                Start = CurrentToken
            };

            if (CurrentToken.Type == TokenConstants.EOF)
            {
                NotifySyntaxError(CurrentToken, "Порожній вираз.");
                return ctx;
            }

            var expr = ParseAdditive();
            ctx.AddChild(expr);

            if (CurrentToken.Type != TokenConstants.EOF)
            {
                NotifySyntaxError(CurrentToken,
                    $"Неочікуваний токен '{CurrentToken.Text}' після завершення виразу.");
            }
            else
            {
                var eofToken = Consume();
                ctx.AddChild(new TerminalNodeImpl(eofToken));
                ctx.Stop = eofToken;
            }

            return ctx;
        }

        private ExpressionContext ParseAdditive()
        {
            var left = ParseMultiplicative();

            while (CurrentToken.Type == ADD || CurrentToken.Type == SUBTRACT)
            {
                var opToken = Consume();
                var right = ParseMultiplicative();

                var next = new AdditiveExprContext(new ExpressionContext())
                {
                    Start = left.Start,
                    Stop = right.Stop,
                    operatorToken = opToken
                };
                next.AddChild(left);
                next.AddChild(new TerminalNodeImpl(opToken));
                next.AddChild(right);
                left = next;
            }

            return left;
        }

        private ExpressionContext ParseMultiplicative()
        {
            var left = ParseUnary();

            while (CurrentToken.Type == MULTIPLY ||
                   CurrentToken.Type == DIVIDE ||
                   CurrentToken.Type == DIV ||
                   CurrentToken.Type == MOD)
            {
                var opToken = Consume();
                var right = ParseUnary();

                var next = new MultiplicativeExprContext(new ExpressionContext())
                {
                    Start = left.Start,
                    Stop = right.Stop,
                    operatorToken = opToken
                };
                next.AddChild(left);
                next.AddChild(new TerminalNodeImpl(opToken));
                next.AddChild(right);
                left = next;
            }

            return left;
        }

        private ExpressionContext ParseUnary()
        {
            if (CurrentToken.Type == ADD || CurrentToken.Type == SUBTRACT)
            {
                var opToken = Consume();
                var operand = ParseUnary();

                var ctx = new UnaryExprContext(new ExpressionContext())
                {
                    Start = opToken,
                    Stop = operand.Stop,
                    operatorToken = opToken
                };
                ctx.AddChild(new TerminalNodeImpl(opToken));
                ctx.AddChild(operand);
                return ctx;
            }

            return ParseExponential();
        }

        private ExpressionContext ParseExponential()
        {
            var left = ParsePrimary();

            if (CurrentToken.Type == EXPONENT)
            {
                var caretToken = Consume();
                var right = ParseUnary();

                var ctx = new ExponentialExprContext(new ExpressionContext())
                {
                    Start = left.Start,
                    Stop = right.Stop
                };
                ctx.AddChild(left);
                ctx.AddChild(new TerminalNodeImpl(caretToken));
                ctx.AddChild(right);
                return ctx;
            }

            return left;
        }

        private ExpressionContext ParsePrimary()
        {
            var token = CurrentToken;

            switch (token.Type)
            {
                case LPAREN:
                {
                    var lparen = Consume();
                    if (CurrentToken.Type == RPAREN)
                    {
                        NotifySyntaxError(CurrentToken, "Порожні дужки '()' не допускаються.");
                    }
                    var inner = ParseAdditive();
                    var rparen = Expect(RPAREN, "Очікувалася закриваюча кругла дужка ')'.");

                    var ctx = new ParenthesizedExprContext(new ExpressionContext())
                    {
                        Start = lparen,
                        Stop = rparen
                    };
                    ctx.AddChild(new TerminalNodeImpl(lparen));
                    ctx.AddChild(inner);
                    ctx.AddChild(new TerminalNodeImpl(rparen));
                    return ctx;
                }

                case MAX:
                case MIN:
                {
                    bool isMax = token.Type == MAX;
                    string funcName = token.Text;
                    var fnToken = Consume();
                    var lparen = Expect(LPAREN, $"Після імені функції '{funcName}' очікувалася відкриваюча дужка '('.");

                    if (CurrentToken.Type == RPAREN)
                    {
                        NotifySyntaxError(CurrentToken, $"Функція '{funcName}(x, y)' потребує рівно 2 аргументи, але не передано жодного.");
                    }

                    var arg1 = ParseAdditive();

                    IToken sepToken;
                    if (CurrentToken.Type == COMMA || CurrentToken.Type == SEMICOLON)
                    {
                        sepToken = Consume();
                    }
                    else
                    {
                        NotifySyntaxError(CurrentToken, $"У функції '{funcName}(x, y)' очікувалася кома ',' між першим і другим аргументом.");
                        sepToken = CurrentToken;
                    }

                    var arg2 = ParseAdditive();

                    if (CurrentToken.Type == COMMA || CurrentToken.Type == SEMICOLON)
                    {
                        NotifySyntaxError(CurrentToken,
                            $"Функція '{funcName}(x, y)' приймає рівно 2 аргументи.");
                    }

                    var rparen = Expect(RPAREN, $"Очікувалася закриваюча дужка ')' для функції '{funcName}'.");

                    if (isMax)
                    {
                        var ctx = new MaxExprContext(new ExpressionContext())
                        {
                            Start = fnToken,
                            Stop = rparen
                        };
                        ctx.AddChild(new TerminalNodeImpl(fnToken));
                        ctx.AddChild(new TerminalNodeImpl(lparen));
                        ctx.AddChild(arg1);
                        ctx.AddChild(new TerminalNodeImpl(sepToken));
                        ctx.AddChild(arg2);
                        ctx.AddChild(new TerminalNodeImpl(rparen));
                        return ctx;
                    }
                    else
                    {
                        var ctx = new MinExprContext(new ExpressionContext())
                        {
                            Start = fnToken,
                            Stop = rparen
                        };
                        ctx.AddChild(new TerminalNodeImpl(fnToken));
                        ctx.AddChild(new TerminalNodeImpl(lparen));
                        ctx.AddChild(arg1);
                        ctx.AddChild(new TerminalNodeImpl(sepToken));
                        ctx.AddChild(arg2);
                        ctx.AddChild(new TerminalNodeImpl(rparen));
                        return ctx;
                    }
                }

                case NUMBER:
                {
                    var numToken = Consume();
                    var ctx = new NumberExprContext(new ExpressionContext())
                    {
                        Start = numToken,
                        Stop = numToken
                    };
                    ctx.AddChild(new TerminalNodeImpl(numToken));
                    return ctx;
                }

                case IDENTIFIER:
                {
                    var idToken = Consume();
                    if (CurrentToken.Type == LPAREN)
                    {
                        NotifySyntaxError(idToken,
                            $"Невідома функція '{idToken.Text}'. Підтримуються лише функції max(x, y) та min(x, y).");
                    }
                    var ctx = new IdentifierExprContext(new ExpressionContext())
                    {
                        Start = idToken,
                        Stop = idToken
                    };
                    ctx.AddChild(new TerminalNodeImpl(idToken));
                    return ctx;
                }

                case TokenConstants.EOF:
                    NotifySyntaxError(token, "Неочікуваний кінець виразу: очікувалося число, ім'я клітинки або вираз у дужках.");
                    return new NumberExprContext(new ExpressionContext()) { Start = token, Stop = token };

                default:
                    NotifySyntaxError(token, $"Неочікуваний токен '{token.Text}': очікувався операнд.");
                    Consume();
                    return new NumberExprContext(new ExpressionContext()) { Start = token, Stop = token };
            }
        }

        private IToken Consume()
        {
            var tok = CurrentToken;
            if (tok.Type != TokenConstants.EOF)
            {
                _input.Consume();
            }
            return tok;
        }

        private IToken Expect(int expectedType, string errorMessage)
        {
            if (CurrentToken.Type == expectedType)
            {
                return Consume();
            }

            NotifySyntaxError(CurrentToken, errorMessage);
            return CurrentToken;
        }

        private void NotifySyntaxError(IToken offendingToken, string message)
        {
            int line = offendingToken.Line;
            int col = offendingToken.Column;
            foreach (var listener in _errorListeners)
            {
                listener.SyntaxError(TextWriter.Null, null, offendingToken, line, col, message, null);
            }
        }

        public class CompileUnitContext : ParserRuleContext
        {
            public CompileUnitContext(ParserRuleContext? parent, int invokingStateNumber)
                : base(parent, invokingStateNumber)
            {
            }

            public ExpressionContext expression() => GetRuleContext<ExpressionContext>(0);
            public ITerminalNode Eof() => GetToken(TokenConstants.EOF, 0);

            public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor)
            {
                if (visitor is ILabCalculatorVisitor<TResult> typedVisitor)
                    return typedVisitor.VisitCompileUnit(this);
                return visitor.VisitChildren(this);
            }
        }

        public class ExpressionContext : ParserRuleContext
        {
            public ExpressionContext() { }
            public ExpressionContext(ParserRuleContext? parent, int invokingStateNumber)
                : base(parent, invokingStateNumber)
            {
            }

            public void CopyFrom(ExpressionContext ctx) => base.CopyFrom(ctx);
        }

        public class ParenthesizedExprContext : ExpressionContext
        {
            public ParenthesizedExprContext(ExpressionContext context) => CopyFrom(context);
            public ExpressionContext expression() => GetRuleContext<ExpressionContext>(0);

            public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor)
            {
                if (visitor is ILabCalculatorVisitor<TResult> typedVisitor)
                    return typedVisitor.VisitParenthesizedExpr(this);
                return visitor.VisitChildren(this);
            }
        }

        public class UnaryExprContext : ExpressionContext
        {
            public IToken? operatorToken;
            public UnaryExprContext(ExpressionContext context) => CopyFrom(context);
            public ExpressionContext expression() => GetRuleContext<ExpressionContext>(0);

            public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor)
            {
                if (visitor is ILabCalculatorVisitor<TResult> typedVisitor)
                    return typedVisitor.VisitUnaryExpr(this);
                return visitor.VisitChildren(this);
            }
        }

        public class ExponentialExprContext : ExpressionContext
        {
            public ExponentialExprContext(ExpressionContext context) => CopyFrom(context);
            public ExpressionContext[] expression() => GetRuleContexts<ExpressionContext>();
            public ExpressionContext expression(int i) => GetRuleContext<ExpressionContext>(i);

            public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor)
            {
                if (visitor is ILabCalculatorVisitor<TResult> typedVisitor)
                    return typedVisitor.VisitExponentialExpr(this);
                return visitor.VisitChildren(this);
            }
        }

        public class MultiplicativeExprContext : ExpressionContext
        {
            public IToken? operatorToken;
            public MultiplicativeExprContext(ExpressionContext context) => CopyFrom(context);
            public ExpressionContext[] expression() => GetRuleContexts<ExpressionContext>();
            public ExpressionContext expression(int i) => GetRuleContext<ExpressionContext>(i);

            public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor)
            {
                if (visitor is ILabCalculatorVisitor<TResult> typedVisitor)
                    return typedVisitor.VisitMultiplicativeExpr(this);
                return visitor.VisitChildren(this);
            }
        }

        public class AdditiveExprContext : ExpressionContext
        {
            public IToken? operatorToken;
            public AdditiveExprContext(ExpressionContext context) => CopyFrom(context);
            public ExpressionContext[] expression() => GetRuleContexts<ExpressionContext>();
            public ExpressionContext expression(int i) => GetRuleContext<ExpressionContext>(i);

            public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor)
            {
                if (visitor is ILabCalculatorVisitor<TResult> typedVisitor)
                    return typedVisitor.VisitAdditiveExpr(this);
                return visitor.VisitChildren(this);
            }
        }

        public class MaxExprContext : ExpressionContext
        {
            public MaxExprContext(ExpressionContext context) => CopyFrom(context);
            public ExpressionContext[] expression() => GetRuleContexts<ExpressionContext>();
            public ExpressionContext expression(int i) => GetRuleContext<ExpressionContext>(i);

            public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor)
            {
                if (visitor is ILabCalculatorVisitor<TResult> typedVisitor)
                    return typedVisitor.VisitMaxExpr(this);
                return visitor.VisitChildren(this);
            }
        }

        public class MinExprContext : ExpressionContext
        {
            public MinExprContext(ExpressionContext context) => CopyFrom(context);
            public ExpressionContext[] expression() => GetRuleContexts<ExpressionContext>();
            public ExpressionContext expression(int i) => GetRuleContext<ExpressionContext>(i);

            public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor)
            {
                if (visitor is ILabCalculatorVisitor<TResult> typedVisitor)
                    return typedVisitor.VisitMinExpr(this);
                return visitor.VisitChildren(this);
            }
        }

        public class NumberExprContext : ExpressionContext
        {
            public NumberExprContext(ExpressionContext context) => CopyFrom(context);
            public ITerminalNode NUMBER() => GetToken(LabCalculatorParser.NUMBER, 0);

            public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor)
            {
                if (visitor is ILabCalculatorVisitor<TResult> typedVisitor)
                    return typedVisitor.VisitNumberExpr(this);
                return visitor.VisitChildren(this);
            }
        }

        public class IdentifierExprContext : ExpressionContext
        {
            public IdentifierExprContext(ExpressionContext context) => CopyFrom(context);
            public ITerminalNode IDENTIFIER() => GetToken(LabCalculatorParser.IDENTIFIER, 0);

            public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor)
            {
                if (visitor is ILabCalculatorVisitor<TResult> typedVisitor)
                    return typedVisitor.VisitIdentifierExpr(this);
                return visitor.VisitChildren(this);
            }
        }
    }
}
