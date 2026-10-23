using Antlr4.Runtime;
using System.Text;

namespace lr1_v6.Grammar
{
    public class LabCalculatorLexer : ITokenSource
    {
        public const int
            MAX = 1,
            MIN = 2,
            MOD = 3,
            DIV = 4,
            NUMBER = 5,
            IDENTIFIER = 6,
            EXPONENT = 7,
            MULTIPLY = 8,
            DIVIDE = 9,
            SUBTRACT = 10,
            ADD = 11,
            LPAREN = 12,
            RPAREN = 13,
            COMMA = 14,
            SEMICOLON = 15,
            WS = 16;

        private readonly ICharStream _input;
        private readonly string _text;
        private int _pos;
        private int _line = 1;
        private int _charPosInLine;
        private readonly List<IAntlrErrorListener<int>> _errorListeners = new();

        public LabCalculatorLexer(ICharStream input)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _text = input.GetText(new Antlr4.Runtime.Misc.Interval(0, input.Size - 1));
            _pos = 0;
        }

        public int Line => _line;
        public int Column => _charPosInLine;
        public ICharStream InputStream => _input;
        public string SourceName => _input.SourceName;
        public ITokenFactory TokenFactory { get; set; } = CommonTokenFactory.Default;

        public void RemoveErrorListeners() => _errorListeners.Clear();

        public void AddErrorListener(IAntlrErrorListener<int> listener)
        {
            if (listener != null)
            {
                _errorListeners.Add(listener);
            }
        }

        public IToken NextToken()
        {
            while (_pos < _text.Length)
            {
                char c = _text[_pos];

                if (char.IsWhiteSpace(c))
                {
                    if (c == '\n')
                    {
                        _line++;
                        _charPosInLine = 0;
                    }
                    else
                    {
                        _charPosInLine++;
                    }
                    _pos++;
                    continue;
                }

                int startPos = _pos;
                int startCol = _charPosInLine;

                switch (c)
                {
                    case '^':
                        Advance(1);
                        return CreateToken(EXPONENT, "^", startPos, startPos, startCol);
                    case '*':
                        Advance(1);
                        return CreateToken(MULTIPLY, "*", startPos, startPos, startCol);
                    case '/':
                        Advance(1);
                        return CreateToken(DIVIDE, "/", startPos, startPos, startCol);
                    case '-':
                        Advance(1);
                        return CreateToken(SUBTRACT, "-", startPos, startPos, startCol);
                    case '+':
                        Advance(1);
                        return CreateToken(ADD, "+", startPos, startPos, startCol);
                    case '(':
                        Advance(1);
                        return CreateToken(LPAREN, "(", startPos, startPos, startCol);
                    case ')':
                        Advance(1);
                        return CreateToken(RPAREN, ")", startPos, startPos, startCol);
                    case ',':
                        Advance(1);
                        return CreateToken(COMMA, ",", startPos, startPos, startCol);
                    case ';':
                        Advance(1);
                        return CreateToken(SEMICOLON, ";", startPos, startPos, startCol);
                }

                if (char.IsDigit(c))
                {
                    var sb = new StringBuilder();
                    while (_pos < _text.Length && char.IsDigit(_text[_pos]))
                    {
                        sb.Append(_text[_pos]);
                        Advance(1);
                    }

                    if (_pos < _text.Length && _text[_pos] == '.')
                    {
                        sb.Append('.');
                        Advance(1);
                        if (_pos >= _text.Length || !char.IsDigit(_text[_pos]))
                        {
                            NotifyLexerError(startPos, startCol, $"Некоректний запис числа '{sb}': очікувалася цифра після крапки.");
                        }
                        while (_pos < _text.Length && char.IsDigit(_text[_pos]))
                        {
                            sb.Append(_text[_pos]);
                            Advance(1);
                        }
                    }

                    return CreateToken(NUMBER, sb.ToString(), startPos, _pos - 1, startCol);
                }

                if (IsAsciiLetter(c))
                {
                    var letters = new StringBuilder();
                    while (_pos < _text.Length && IsAsciiLetter(_text[_pos]))
                    {
                        letters.Append(_text[_pos]);
                        Advance(1);
                    }

                    var digits = new StringBuilder();
                    while (_pos < _text.Length && char.IsDigit(_text[_pos]))
                    {
                        digits.Append(_text[_pos]);
                        Advance(1);
                    }

                    string word = letters.ToString();
                    if (digits.Length == 0)
                    {
                        string lower = word.ToLowerInvariant();
                        int? keywordType = lower switch
                        {
                            "max" => MAX,
                            "min" => MIN,
                            "mod" => MOD,
                            "div" => DIV,
                            _ => null
                        };

                        if (keywordType.HasValue)
                        {
                            return CreateToken(keywordType.Value, word, startPos, _pos - 1, startCol);
                        }

                        NotifyLexerError(startPos, startCol,
                            $"Невідомий ідентифікатор або функція '{word}'. Дозволені операції/функції: mod, div, max, min або посилання на клітинку (напр. A1).");
                        return CreateToken(IDENTIFIER, word, startPos, _pos - 1, startCol);
                    }

                    if (_pos < _text.Length && IsAsciiLetter(_text[_pos]))
                    {
                        var bad = new StringBuilder(word).Append(digits);
                        while (_pos < _text.Length && (IsAsciiLetter(_text[_pos]) || char.IsDigit(_text[_pos])))
                        {
                            bad.Append(_text[_pos]);
                            Advance(1);
                        }
                        NotifyLexerError(startPos, startCol, $"Некоректне ім'я клітинки '{bad}'. Формат клітинки: літери стовпчика та номер рядка (напр. A1, B3).");
                    }

                    string fullCellId = word + digits;
                    return CreateToken(IDENTIFIER, fullCellId, startPos, _pos - 1, startCol);
                }

                Advance(1);
                string hint = IsCyrillicLetter(c)
                    ? $"Неприпустимий кириличний символ '{c}'. Використовуйте латинські літери для імен клітинок (A, B, C...) та функцій (max, min, mod, div)."
                    : $"Невідомий символ '{c}' у виразі.";
                NotifyLexerError(startPos, startCol, hint);
            }

            return CreateToken(TokenConstants.EOF, "<EOF>", _pos, _pos, _charPosInLine);
        }

        private static bool IsAsciiLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

        private static bool IsCyrillicLetter(char c) => c >= '\u0400' && c <= '\u04FF';

        private void Advance(int count)
        {
            _pos += count;
            _charPosInLine += count;
        }

        private CommonToken CreateToken(int type, string text, int start, int stop, int col)
        {
            var token = new CommonToken(new Tuple<ITokenSource, ICharStream>(this, _input), type, TokenConstants.DefaultChannel, start, stop)
            {
                Text = text,
                Line = _line,
                Column = col
            };
            return token;
        }

        private void NotifyLexerError(int charIndex, int column, string message)
        {
            foreach (var listener in _errorListeners)
            {
                listener.SyntaxError(TextWriter.Null, null, charIndex, _line, column, message, null);
            }
        }
    }
}
