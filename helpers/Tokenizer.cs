using System;
using System.Collections.Generic;
using KeplerTokens.Tokens;
using KeplerTokens.DataTypes;
using System.Text.RegularExpressions;
using System.Linq;

namespace KeplerTokenizer
{
    public class Tokenizer
    {
        private List<LineIterator> lines;
        public int current_line = 0;

        // automatically load and tokenize the code.
        public void Load(string filename)
        {
            string fileExt = System.IO.Path.GetExtension(filename);
            if (fileExt != ".kep") throw new Exception("This file is not a valid .kep file!");

            string[] lines = System.IO.File.ReadAllLines(@filename);
            List<LineIterator> filtered_lines = new List<LineIterator>();

            Boolean in_comment_block = false;

            for (int i = 0; i < lines.Length; ++i)
            {
                char[] charsToTrim = { ' ', '\n', '\r', '\t' };

                Regex r = new Regex(@"^(    )");
                Regex double_quotes = new Regex("(?!\\B\"[^\"]*),(?![^\"]*\"\\B)");
                // Regex eol_comment = new Regex()
                // convert leading 4 spaces to a tab and trim end spaces
                // .Replace(",", " and") <- important: CANNOT split by commas within quotation marks!
                string line = double_quotes.Replace(r.Replace(lines[i], "\t").TrimEnd(charsToTrim), " and");
                // trim leading spaces
                string m_line = line.Trim(charsToTrim);

                int indentation = (line.Length - m_line.Length);

                // check for block comments
                if (m_line.StartsWith("!--"))
                {
                    in_comment_block = true;
                }
                if (m_line.EndsWith("--!") && in_comment_block)
                {
                    in_comment_block = false;
                    continue;
                }

                // if the line starts with a "!", or we're currently in a block comment
                if (m_line.StartsWith("!") || in_comment_block) continue;

                // if the length of the trimmed line isn't zero
                if (m_line.Length != 0) filtered_lines.Add(new LineIterator(m_line, i + 1, indentation));
            }

            filtered_lines.Add(new LineIterator("EOP", lines.Length + 1, 0));

            // Console.WriteLine(string.Format("Loaded {0} line(s).", lines.Length));
            // Console.WriteLine(string.Format("Tokenized {0} line(s).", filtered_lines.Count - 1));
            this.lines = filtered_lines;
        }

        public LineIterator TokenizeLine(int line, string text)
        {
            return new LineIterator(text, line, 0);
        }

        public static Tokenizer operator ++(Tokenizer tokenizer)
        {
            tokenizer.current_line = tokenizer.current_line + 1;
            return tokenizer;
        }

        public void SetLine(int line)
        {
            this.current_line = line;
        }

        public LineIterator CurrentLine()
        {
            return this.lines[this.current_line];
        }

        public Boolean HasNext()
        {
            return this.current_line < this.lines.Count;
        }

        public List<LineIterator> Lines()
        {
            return this.lines;
        }
    }

    public class LineIterator
    {
        public int line = 0;
        public int indentation = 0;
        public int m_num = 0;
        public List<Token> tokens = new List<Token>();
        public bool killed = false;
        public LineIterator(string line, int index, int indentation)
        {
            this.line = index;
            this.indentation = indentation;

            Regex inline_comment = new Regex("((!--)[\\w|\\s]*(--!))"); // remove inline comments
            // split by spaces, unless inside quotation marks.
            List<string> final_split = Regex.Matches(inline_comment.Replace(line.Replace("\t", " "), ""), @"[\""].+?[\""]|[^ ]+")
                            .Cast<Match>()
                            .Select(m => m.Value)
                            .ToList();

            List<Token> m_tokens = new List<Token>();

            try
            {
                Boolean in_string = false; // store if entered a string token

                for (int i = 0; i < final_split.Count; ++i)
                {
                    string tokenized = final_split[i];
                    if (tokenized.Length == 0) continue;

                    TokenMatch pair = new TokenMatch(TokenType.UNRECOGNIZED, null, null, null, 0);

                    // if we're at the beginning of this line.
                    if (i == 0)
                    {
                        if (final_split.Count == 1)
                            pair = GetTokenType(tokenized, null, null);
                        else
                            pair = GetTokenType(tokenized, final_split[Math.Min(i + 1, final_split.Count - 1)], null);
                    }
                    else
                    {
                        if (i + 1 < final_split.Count)
                            pair = GetTokenType(tokenized, final_split[i + 1], final_split[i - 1]);
                        else
                            // handle tokens at the end of the line
                            pair = GetTokenType(tokenized, null, final_split[i - 1]);
                    }

                    // if (pair.type == TokenType.DoubleQuote) in_string = !in_string;
                    if (tokenized.StartsWith("\"")) in_string = true;

                    int count = 1;
                    while (count <= pair.increment)
                    {
                        // Console.WriteLine(count);
                        tokenized = tokenized + " " + final_split[i + count];
                        count++;
                    }

                    if (in_string) m_tokens.Add(new Token(TokenType.StaticString, i, tokenized));
                    else { m_tokens.Add(new Token(pair.type, i, tokenized)); i += pair.increment; }

                    if (tokenized.EndsWith("\"")) in_string = false;
                    // throw new Exception(string.Format("Unable to tokenize line {0}: unrecognizable token!\r\nToken: {1}", this.line, tokenized));
                }

                // Operations pass
                for (int i = 0; i < m_tokens.Count;)
                {
                    if (i == m_tokens.Count - 2 || i == m_tokens.Count - 1)
                    {
                        i++;
                        continue;
                    }

                    Token peek = m_tokens[i + 1];
                    Token far_peek = m_tokens[i + 2];

                    Token operation_token = new Token(TokenType.GenericOperation, -1, "NUL");
                    operation_token.start = i;
                    operation_token.token_string = m_tokens[i].token_string + " " + peek.token_string + " " + far_peek.token_string;
                    bool clean_up = false;

                    switch (peek.type)
                    {
                        case TokenType.GenericAdd:
                            operation_token.operation = OperationType.Add;
                            clean_up = true;
                            break;
                        case TokenType.GenericSubtract:
                            operation_token.operation = OperationType.Subtract;
                            clean_up = true;
                            break;
                        case TokenType.GenericMultiply:
                            operation_token.operation = OperationType.Multiply;
                            clean_up = true;
                            break;
                        case TokenType.GenericPower:
                            operation_token.operation = OperationType.Power;
                            clean_up = true;
                            break;
                        case TokenType.GenericDivide:
                            operation_token.operation = OperationType.Divide;
                            clean_up = true;
                            break;
                        case TokenType.GenericModulo:
                            operation_token.operation = OperationType.Modulo;
                            clean_up = true;
                            break;
                        case TokenType.GenericEquality:
                            operation_token.operation = OperationType.Equality;
                            clean_up = true;
                            break;
                        case TokenType.GenericGreaterThan:
                            operation_token.operation = OperationType.GreaterThan;
                            clean_up = true;
                            break;
                        case TokenType.GenericGreaterThanEqual:
                            operation_token.operation = OperationType.GreaterThanEqual;
                            clean_up = true;
                            break;
                        case TokenType.GenericLessThan:
                            operation_token.operation = OperationType.LessThan;
                            clean_up = true;
                            break;
                        case TokenType.GenericLessThanEqual:
                            operation_token.operation = OperationType.LessThanEqual;
                            clean_up = true;
                            break;
                        default:
                            i++;
                            break;
                    }

                    if (clean_up)
                    {
                        // assign tokens
                        operation_token.a = m_tokens[i];
                        operation_token.b = m_tokens[i + 2];

                        // remove combined tokens
                        m_tokens.RemoveAt(i + 1);
                        m_tokens.RemoveAt(i + 1);

                        // assign combined token
                        m_tokens[i] = operation_token;
                    }
                }

                // Combine
                for (int i = 1; i < m_tokens.Count;)
                {

                    if (i == m_tokens.Count - 2 || i == m_tokens.Count - 1)
                    {
                        i++;
                        continue;
                    }

                    Token peek = m_tokens[i + 1];
                    Token far_peek = m_tokens[i + 2];

                    Token operation_token = new Token(TokenType.GenericOperation, -1, "NUL");
                    operation_token.start = i;
                    operation_token.token_string = m_tokens[i].token_string + " " + peek.token_string + " " + far_peek.token_string;
                    bool clean_up = false;

                    switch (peek.type)
                    {
                        // AND
                        case TokenType.BooleanOperator:
                            if ((m_tokens[i].type == TokenType.GenericOperation || m_tokens[i].type == TokenType.StaticBoolean) && (far_peek.type == TokenType.GenericOperation || far_peek.type == TokenType.StaticBoolean))
                            {
                                operation_token.operation = OperationType.And;
                                clean_up = true;
                            }
                            else i++;
                            break;
                        // OR
                        case TokenType.OrOperator:
                            if ((m_tokens[i].type == TokenType.GenericOperation || m_tokens[i].type == TokenType.StaticBoolean) && (far_peek.type == TokenType.GenericOperation || far_peek.type == TokenType.StaticBoolean))
                            {
                                operation_token.operation = OperationType.Or;
                                clean_up = true;
                            }
                            else i++;
                            break;
                        default:
                            i++;
                            break;
                    }

                    if (clean_up)
                    {
                        // assign tokens
                        operation_token.a = m_tokens[i];
                        operation_token.b = m_tokens[i + 2];

                        // remove combined tokens
                        m_tokens.RemoveAt(i + 1);
                        m_tokens.RemoveAt(i + 1);

                        // assign combined token
                        m_tokens[i] = operation_token;
                    }
                }

                // conditional virtual equality
                if (m_tokens.Count == 2 && m_tokens[0].type == TokenType.StartConditional)
                {
                    Token equality = new Token(TokenType.GenericOperation, 1, m_tokens[1].token_string);
                    equality.operation = OperationType.Equality;
                    equality.a = m_tokens[1];
                    equality.b = new Token(TokenType.StaticBoolean, 3, "True");

                    m_tokens[1] = equality;
                }

                this.tokens = m_tokens;
            }
            catch (SystemException e)
            {
                Console.WriteLine("Error while tokenizing\r\n" + e);
                Environment.ExitCode = -1;
            }
        }

        private TokenMatch GetTokenType(string token, string peek, string previous)
        {

            TokenMatch[] tokens = new TokenMatch[] {
                new TokenMatch(TokenType.EOP, "EOP", "EOP", null, 0), // End of Program token
                new TokenMatch(TokenType.EOP, "EOP", null, null, 0), // End of Program token

                // looping things
                new TokenMatch(TokenType.StartInterval, "start", "every", null, 0),
                new TokenMatch(TokenType.EndInterval, "end", "every", null, 0),
                new TokenMatch(TokenType.DeclareInterval, "every", TokenMatch.any_string, "start", 0),
                new TokenMatch(TokenType.DeclareInterval, "every", TokenMatch.any_string, "end", 0),

                new TokenMatch(TokenType.StartLoop, "start", "forever", null, 0),
                new TokenMatch(TokenType.EndLoop, "end", "forever", null, 0),
                new TokenMatch(TokenType.DeclareLoop, "forever", TokenMatch.any_string, "start", 0),
                new TokenMatch(TokenType.DeclareLoop, "forever", TokenMatch.any_string, "end", 0),
                new TokenMatch(TokenType.BreakOut, "breakout", null, null, 0),

                // new TokenMatch(TokenType.DoubleQuote, "\"", TokenMatch.any_string, TokenMatch.any_string, 0), // handle doublequote
                // new TokenMatch(TokenType.DoubleQuote, "\"", null, TokenMatch.any_string, 0), // handle doublequote at end of line
                new TokenMatch(TokenType.StartConditional, "if", TokenMatch.any_string, null, 0),
                new TokenMatch(TokenType.EndConditional, "endif", null, null, 0),

                new TokenMatch(TokenType.StartHeader, "start", "Header", null, 0),
                new TokenMatch(TokenType.EndHeader, "end", "Header", null, 0),
                new TokenMatch(TokenType.DeclareHeader, "Header", null, "start", 0),
                new TokenMatch(TokenType.DeclareHeader, "Header", null, "end", 0),
                new TokenMatch(TokenType.LinkFile, "link", TokenMatch.any_string, null, 0),  // linking files
                new TokenMatch(TokenType.ConsolePrint, "print", TokenMatch.any_string, null, 0), // print to console command
                new TokenMatch(TokenType.StartFunction, "start", TokenMatch.any_string, null, 0),
                new TokenMatch(TokenType.EndFunction, "end", TokenMatch.any_string, null, 0),
                new TokenMatch(TokenType.DeclareFunction, TokenMatch.any_string, null, "start", 0), // handle functions with no arguments
                new TokenMatch(TokenType.DeclareFunction, TokenMatch.any_string, null, "end", 0), // handle functions with no arguments
                new TokenMatch(TokenType.DeclareFunction, TokenMatch.any_string, TokenMatch.any_string, "start", 0),
                new TokenMatch(TokenType.DeclareFunction, TokenMatch.any_string, TokenMatch.any_string, "end", 0),
                new TokenMatch(TokenType.DeclareFunction, TokenMatch.any_string, "uses", null, 0), // any string that comes before "uses" is a function declaration
                new TokenMatch(TokenType.DeclareFunction, TokenMatch.any_string, "returns", null, 0),

                new TokenMatch(TokenType.StartStaticArray, "[", null, null, 0),
                new TokenMatch(TokenType.EndStaticArray, "]", null, null, 0),
                new TokenMatch(TokenType.StartStaticList, "{", null, null, 0),
                new TokenMatch(TokenType.EndStaticList, "}", null, null, 0),

                new TokenMatch(TokenType.BooleanOperator, "and", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.OrOperator, "or", TokenMatch.any_string, TokenMatch.any_string, 0),

                // new TokenMatch(TokenType.DeclareVariable, TokenMatch.any_string, "equals", TokenMatch.any_string, 0),
                // new TokenMatch(TokenType.DeclareVariable, TokenMatch.any_string, "is", TokenMatch.any_string, 0),

                // static types
                new TokenMatch(TokenType.StaticVariableType, "Float", null, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticVariableType, "uFloat", null, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticVariableType, "Int", null, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticVariableType, "uInt", null, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticVariableType, "String", null, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticVariableType, "Array", null, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticVariableType, "List", null, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticVariableType, "Boolean", null, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticVariableType, "Float", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticVariableType, "uFloat", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticVariableType, "Int", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticVariableType, "uInt", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticVariableType, "String", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticVariableType, "Array", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticVariableType, "List", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticVariableType, "Boolean", TokenMatch.any_string, TokenMatch.any_string, 0),

                new TokenMatch(TokenType.StartArguments, "with", TokenMatch.any_string, TokenMatch.any_string, 0), // "with" defines the start of StartArguments
                new TokenMatch(TokenType.StartPositionalArguments, "using", TokenMatch.any_string, TokenMatch.any_string, 0), // "using" defines the start of positional arguments
                new TokenMatch(TokenType.PositionalArgumentAssignment, "as", TokenMatch.any_string, TokenMatch.any_string, 0), // "as" is a PositionalArgumentAssignment
                new TokenMatch(TokenType.PositionalArgument, TokenMatch.any_string, TokenMatch.any_string, "as", 0), // any string AFTER "as" is a PositionalArgument
                // new TokenMatch(TokenType.DeclareVariable, TokenMatch.any_string, "and", TokenMatch.any_string, 0), // any string before "and" is a DeclareVariable, if it isn't after "as"
                new TokenMatch(TokenType.DeclareVariable, TokenMatch.any_string, "as", TokenMatch.any_string, 0), // any string BEFORE "as" is a DeclareVariable
                new TokenMatch(TokenType.AssignFunctionType, "returns", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.AssignNonPositionalArgument, "uses", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.FunctionReturn, "return", TokenMatch.any_string, null, 0),
                // new TokenMatch(TokenType.ConditionalIf, "if", TokenMatch.any_string, null, 0),
                // new TokenMatch(TokenType.ConditionalElse, "else", "else", null, 0),
                // new TokenMatch(TokenType.ConditionalElseIf, "else", "if", null, 1), // i++
                new TokenMatch(TokenType.GenericAssign, "is", TokenMatch.any_string, TokenMatch.any_string, 0),

                // modifiers
                new TokenMatch(TokenType.StaticModifier, "Constant", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticModifier, "Variable", TokenMatch.any_string, TokenMatch.any_string, 0),

                // static values
                new TokenMatch(TokenType.StaticBoolean, "True", null, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticBoolean, "False", null, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticInt, TokenMatch.eval_int, null, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticFloat, TokenMatch.eval_float, null, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticUnsignedInt, TokenMatch.eval_uint, null, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticUnsignedFloat, TokenMatch.eval_ufloat, null, TokenMatch.any_string, 0),

                // static values before operators
                new TokenMatch(TokenType.StaticBoolean, "True", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticBoolean, "False", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticInt, TokenMatch.eval_int, TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticFloat, TokenMatch.eval_float, TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticUnsignedInt, TokenMatch.eval_uint, TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticUnsignedFloat, TokenMatch.eval_ufloat, TokenMatch.any_string, TokenMatch.any_string, 0),

                // operations
                new TokenMatch(TokenType.GenericAdd, "+", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.GenericSubtract, "-", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.GenericMultiply, "*", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.GenericPower, "^", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.GenericDivide, "/", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.GenericModulo, "%", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.GenericEquality, "equals", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.GenericLessThan, "<", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.GenericGreaterThan, ">", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.GenericLessThanEqual, "<=", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.GenericGreaterThanEqual, ">=", TokenMatch.any_string, TokenMatch.any_string, 0),

                new TokenMatch(TokenType.CallFunction, "call", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.DeclareFunction, TokenMatch.any_string, TokenMatch.any_string, "call", 0),

                new TokenMatch(TokenType.NonPositionalArgument, TokenMatch.any_string, "as", null, 0),
                new TokenMatch(TokenType.PositionalArgumentAssignment, "as", null, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.PositionalArgumentAssignment, TokenMatch.any_string, null, "as", 0),
                new TokenMatch(TokenType.PositionalArgumentAssignment, TokenMatch.any_string, TokenMatch.any_string, "as", 0),
                new TokenMatch(TokenType.DeclareVariable, TokenMatch.any_string, TokenMatch.any_string, "return", 0), // any text following a "return" that isn't tokenized as a StaticType
                new TokenMatch(TokenType.DeclareVariable, TokenMatch.any_string, null, TokenMatch.any_string, 0), // any string at the end of a line is assumed to be a variable name
                new TokenMatch(TokenType.DeclareVariable, TokenMatch.any_string, TokenMatch.any_string, TokenMatch.any_string, 0)
                // since these "pairs" are checked from top to bottom, this final DeclareVariable is just in case
        };

            // if (tokenized == "True" || tokenized == "False") m_tokens.Add(new Token(TokenType.StaticBoolean, i));
            // else if (tokenized == "Int" || tokenized == "uInt") m_tokens.Add(new Token(TokenType.StaticVariableType, i));
            // else if (tokenized == "Float" || tokenized == "uFloat") m_tokens.Add(new Token(TokenType.StaticVariableType, i));
            // else if (tokenized == "String") m_tokens.Add(new Token(TokenType.StaticVariableType, i));
            // else if (tokenized == "Array") m_tokens.Add(new Token(TokenType.StaticVariableType, i));
            // else if (tokenized == "List") m_tokens.Add(new Token(TokenType.StaticVariableType, i));
            // else if (tokenized == "Boolean") m_tokens.Add(new Token(TokenType.StaticVariableType, i));
            // else if (tokenized == "]") m_tokens.Add(new Token(TokenType.EndStaticArray, i));
            // else if (tokenized == "}") m_tokens.Add(new Token(TokenType.EndStaticList, i));
            // else if (tokenized == "Header") m_tokens.Add(new Token(TokenType.DeclareHeader, i));

            foreach (var pair in tokens)
            {
                if (pair.Match(token, peek, previous)) return pair;
            }

            return new TokenMatch(TokenType.UNRECOGNIZED, null, null, null, 0);
        }

        public string GetString()
        {
            string tostring = "";

            foreach (var item in this.tokens)
            {
                tostring = tostring + item.token_string + " ";
            }

            return tostring;
        }

        public override string ToString()
        {
            string tab = "";
            int i = 0;
            while (i < this.indentation)
            {
                tab = "\t" + tab;
                i++;
            }

            // string tostring = tab + "LineIterator:\r\nLine: " + this.line + "\r\nTokens:\r\n";
            string tostring = string.Format("{0}LineIterator (Line {1}) [m_num: {2}]:\r\n{0}Tokens:\r\n", tab, this.line, this.m_num);

            foreach (var item in this.tokens)
            {
                tostring = tostring + tab + item.ToString() + "\r\n";
            }

            return tostring;
        }

        public static LineIterator operator ++(LineIterator iterator)
        {
            iterator.m_num = iterator.m_num + 1;
            return iterator;
        }

        public Boolean HasNext()
        {
            if (killed) return false;
            return m_num < tokens.Count;
        }

        public Token CurrentToken()
        {
            return tokens[m_num];
        }

        public Token Peek()
        {
            if (m_num + 1 >= tokens.Count) return new Token(TokenType.EOL, m_num + 1, "EOL");

            return tokens[m_num + 1];
            // return tokens[m_num];
        }

        public void Kill()
        {
            this.killed = true;
        }
    }
}