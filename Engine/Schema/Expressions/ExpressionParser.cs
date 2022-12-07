using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XSP.Engine.Schema.Expressions
{
    internal class ExpressionParser
    {
        internal const string TOKEN_START = "${";
        internal const char TOKEN_END = '}';
        internal const string UNEXPECTED_CHAR = "Unexpected character '{0}'";
        private const char tokenStart0 = '$';
        private const char tokenStart1 = '{';

        private readonly ContainerNode? container;
        private readonly string expression;
        private readonly int length;
        private int ix;

        public ExpressionParser(string expression)
        {
            this.expression = expression;
            this.length = expression.Length;

            StringBuilder? buffer = null;

            while (ix < length)
            {
                char ch = expression[ix];

                // is it a token start?
                if (ix < length - 1 && ch == tokenStart0 && expression[ix + 1] == tokenStart1)
                {
                    if (container == null)
                    {
                        container = new ContainerNode();
                    }

                    if (buffer != null && buffer.Length > 0)
                    {
                        container.Add(new PrimitiveNode<string>(buffer.ToString()));
                        buffer.Length = 0;
                    }

                    ix += 2;
                    Node node = ParseToken(TOKEN_END);
                    container.Add(node);
                }
                else
                {
                    if (buffer == null)
                    {
                        buffer = new StringBuilder(length - ix);
                    }
                    buffer.Append(ch);
                    ix++;
                }
            }

            if (container != null && buffer != null && buffer.Length > 0)
            {
                container.Add(new PrimitiveNode<string>(buffer.ToString()));
            }
        }

        public static Node? Parse(string expression)
        {
            if(expression == null || !expression.Contains(TOKEN_START)) {
                return new PrimitiveNode<string>(expression);
            }

            ContainerNode? container = new ExpressionParser(expression).container;
            if(container != null) {
                return container.FirstOrSelf();
            }

            return null;
        }

        private void SkipWhitespace() {
        while(ix < length) {
            if(!char.IsWhiteSpace(expression[ix])) {
                break;
            }

            ix++;
        }
        }

    private Node ParseToken(char last) {
        WorkingSet workingSet = new();

        SkipWhitespace();

        while(ix < length) {
            bool nameIsEmpty = workingSet.NameIsEmpty;

            char ch = expression[ix];
            if (ch == last) {
                return workingSet.GetResult(ix++);
            }

            switch (ch) {
                case ' ':
                case '\t':
                    if(!nameIsEmpty) {
                        workingSet.Update(false, ix);
                    }
                    workingSet.NameAllowed = false;
                    break;

                case '0': case '1': case '2': case '3': case '4':
                case '5': case '6': case '7': case '8': case '9':
                case '-':
                    ParseNumber(workingSet, ch, last);
                    continue;

                case '\'':
                case '\"':
                    ParseLiteral(workingSet, ch);
                    break;

                case '(':
                    workingSet.DemandExpression(ch, ix);
                    
                    workingSet.SetResult(ParseToken(')'));
                    continue;

                case '?':
                    Node? node = ParseNullable(workingSet, ch, last);
                    if(node != null) {
                        return node;
                    }
                    break;

                case '=':
                case '!':
                        if (!workingSet.IsEmpty && ix + 1 < length && expression[ix + 1] == '=')
                        {
                            // comparison operator!

                            ix += 2; // skip the ??
                            Node rightSide = ParseToken(last);
                            return new CompareNode(
                                workingSet.GetResult(ix),
                                rightSide,
                                ch == '=' ? ComparisonOperator.EQ : ComparisonOperator.NE);
                        }
                        break;

                case '.':
                    if(nameIsEmpty) {
                        // unexpected '.' ...
                        throw new ParseException(string.Format(UNEXPECTED_CHAR, ch), ix);
                    }
                    workingSet.Update(false, ix);

                    workingSet.NameAllowed = true;
                    break;

                case '[':
                    ParseIndexer(workingSet);
                    break;

                default:
                    if(!workingSet.NameAllowed) {
                        throw new ParseException(string.Format(UNEXPECTED_CHAR, ch), ix);
                    }

                    workingSet.AppendName(ch);
                    break;
            }

            ix++;
        }

        throw new ParseException("Unexpected end of expression reached", ix);
    }

    private void ParseLiteral(WorkingSet workingSet, char ch) {
        workingSet.DemandExpression(ch, ix);

        for(ix++; ix < length && expression[ix] != ch; ix++) {
            workingSet.AppendName(expression[ix]);
        }

        if(ix == length) {
            throw new ParseException("string literal not properly closed", ix);
        }

        workingSet.SetPrimitive(workingSet.GetName());
        workingSet.NameAllowed = false;
    }

    private void ParseNumber(WorkingSet workingSet, char ch, char last)  {
        workingSet.DemandExpression(ch, ix);

        if(ch == '-') {
            workingSet.AppendName(ch);
            ix++;
        }

        bool hasDecimal = false;
        while(ix < length) {
            ch = expression[ix];
            if(ch == last) {
                break;
            }

            if(Char.IsDigit(ch)) {
                workingSet.AppendName(ch);
            }
            else if (!hasDecimal && ch == '.')
            {
                workingSet.AppendName(ch);
                hasDecimal = true;
            }
            else
            {
                throw new ParseException(string.Format(UNEXPECTED_CHAR, ch), ix);
            }

            ix++;
        }

        if(hasDecimal) {
            workingSet.SetPrimitive(double.Parse(workingSet.GetName()!));
        } else {
            workingSet.SetPrimitive(int.Parse(workingSet.GetName()!));
        }

        workingSet.NameAllowed = false;
    }

    private int GetOrdinal() {
        int ordinal = 0;
        for(ix++; ix < length && expression[ix] != ']'; ix++) {
            char ch = expression[ix];
            if(!Char.IsDigit(ch)) {
                throw new ParseException(string.Format(UNEXPECTED_CHAR, ch), ix);
            }

            ordinal = (ordinal * 10) + ((int)Char.GetNumericValue(ch));
        }

        return ordinal;
    }

    private void ParseIndexer(WorkingSet workingSet) {
        workingSet.Update(false, ix);
        workingSet.NameAllowed = false;

        workingSet.GetProperty(ix).Ordinal = GetOrdinal();

        if(ix == length) {
            throw new ParseException("Expected ']'", ix);
        }

        if(ix + 1 < length && expression[ix + 1] == '?') {
            ix++;
            workingSet.GetProperty(ix).Nullable = true;
        }

        if(ix + 1 < length && expression[ix + 1] == '.') {
            ix++;
            workingSet.NameAllowed = true;
        }
    }

    private Node? ParseNullable(WorkingSet workingSet, char ch, char last) {
        if (workingSet.NameIsEmpty) {
            if(!workingSet.IsEmpty && ix + 1 < length && expression[ix + 1] == '?') {
                // null-coalescing operator!

                ix += 2; // skip the ??
                Node rightSide = ParseToken(last);
                return new NullCoalescingNode(workingSet.GetResult(ix), rightSide);
            }

            // unexpected '?' ...
            throw new ParseException(string.Format(UNEXPECTED_CHAR, ch), ix);
        }

        workingSet.Update(true, ix);

        ix++;
        if(ix < length && expression[ix] == '.') {
            ix++;
            workingSet.NameAllowed = true;
        } else {
            workingSet.NameAllowed = false;
        }

        return null;
    }
    }
}
