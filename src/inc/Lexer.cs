using Kepler.Exceptions;
using Kepler.Lexer.Tokens;
using Kepler.LogicControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


namespace Kepler.Lexer
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
            if (this.lines == null) return null;
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
            string math_chars = "[\\/\\+\\*\\%\\^](?=([^\"]*\"[^\"]*\")*[^\"]*$)"; // math character selector
            string comp_chars = "(\\>\\=)|(\\<\\=)|(\\>)|(\\<)(?=([^\"]*\"[^\"]*\")*[^\"]*$)"; // comparison character selector
            string singlets = "[\\,\\!\\@\\#\\&\\(\\)](?=([^\"]*\"[^\"]*\")*[^\"]*$)"; // single character selector
                                                                                       // string single_quote = "\"((?![\\S\\s]*\")|(?=[\\S\\s]*\"))";

            // Transform code so that it's actually parseable
            string modified_line = line.Replace("\t", " "); // replace tabs with spaces
            modified_line = inline_comment.Replace(modified_line, ""); // remove inline block comments
            modified_line = Regex.Replace(modified_line, math_chars, " $0 "); // select math characters and add spaces
            modified_line = Regex.Replace(modified_line, comp_chars, " $0 "); // select math characters and add spaces
            modified_line = Regex.Replace(modified_line, singlets, " $0 "); // select singlet characters and add spaces
                                                                            // modified_line = Regex.Replace(modified_line, single_quote, " $0 ");

            // split by spaces, unless inside quotation marks.
            List<string> final_split = Regex.Matches(modified_line, @"[\""].+?[\""]|[^ ]+")
                            .Cast<Match>()
                            .Select(m => m.Value)
                            .ToList();

            List<Token> m_tokens = new List<Token>();

            try
            {

                // try
                // {
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

                    if (in_string)
                    {
                        m_tokens.Add(new Token(TokenType.StaticString, i, tokenized));

                        if (tokenized.Length < 2)
                        {
                            this.tokens = m_tokens;
                            throw new KeplerException(this, new KeplerError(KeplerErrorCode.MAL_STRING).GetErrorString(), new Kepler.Tracing.KeplerErrorStack(), i);
                        }
                    }
                    else { m_tokens.Add(new Token(pair.type, i, tokenized)); i += pair.increment; }

                    if (tokenized.EndsWith("\"")) in_string = false;
                    if (tokenized.StartsWith("\"") && !tokenized.EndsWith("\""))
                    {
                        // this is not graceful but it'll have to do
                        this.tokens = m_tokens;
                        this.m_num = this.tokens.Count - 1;
                        throw new KeplerException(this, new KeplerError(KeplerErrorCode.UNEXP_EOL).GetErrorString(), new Kepler.Tracing.KeplerErrorStack(), i);
                    }
                }

                // Operations pass
                for (int i = 0; i < m_tokens.Count;)
                {
                    if (i >= m_tokens.Count - 2 || i >= m_tokens.Count - 1)
                    {
                        break;
                    }

                    Token peek = m_tokens[i + 1];
                    Token far_peek = m_tokens[i + 2];

                    Token operation_token = new Token(TokenType.GenericOperation, -1, "NUL");
                    operation_token.start = i;
                    operation_token.token_string = m_tokens[i].token_string + " " + peek.token_string + " " + far_peek.token_string;
                    bool clean_up = true;

                    switch (peek.type)
                    {
                        case TokenType.GenericAdd:
                            operation_token.operation = OperationType.Add;
                            break;
                        case TokenType.GenericSubtract:
                            operation_token.operation = OperationType.Subtract;
                            break;
                        case TokenType.GenericMultiply:
                            operation_token.operation = OperationType.Multiply;
                            break;
                        case TokenType.GenericPower:
                            operation_token.operation = OperationType.Power;
                            break;
                        case TokenType.GenericDivide:
                            operation_token.operation = OperationType.Divide;
                            break;
                        case TokenType.GenericModulo:
                            operation_token.operation = OperationType.Modulo;
                            break;
                        case TokenType.GenericEquality:
                            operation_token.operation = OperationType.Equality;
                            break;
                        case TokenType.GenericStrictEquality:
                            operation_token.operation = OperationType.StrictEquality;
                            break;
                        case TokenType.GenericGreaterThan:
                            operation_token.operation = OperationType.GreaterThan;
                            break;
                        case TokenType.GenericGreaterThanEqual:
                            operation_token.operation = OperationType.GreaterThanEqual;
                            break;
                        case TokenType.GenericLessThan:
                            operation_token.operation = OperationType.LessThan;
                            break;
                        case TokenType.GenericLessThanEqual:
                            operation_token.operation = OperationType.LessThanEqual;
                            break;
                        case TokenType.CastType:
                            operation_token.operation = OperationType.CastType;
                            break;
                        default:
                            i++;
                            clean_up = false;
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
                    if (i >= m_tokens.Count - 2 || i >= m_tokens.Count - 1)
                    {
                        break;
                    }

                    Token peek = m_tokens[i + 1];
                    Token far_peek = m_tokens[i + 2];

                    Token operation_token = new Token(TokenType.GenericOperation, -1, "NUL");
                    operation_token.start = i;
                    operation_token.token_string = m_tokens[i].token_string + " " + peek.token_string + " " + far_peek.token_string;
                    bool clean_up = false;

                    bool current_valid = (m_tokens[i].type == TokenType.GenericOperation || IsStaticValue(m_tokens[i]) || m_tokens[i].type == TokenType.DeclareVariable);
                    bool next_valid = (far_peek.type == TokenType.GenericOperation || IsStaticValue(far_peek) || far_peek.type == TokenType.DeclareVariable);

                    if (current_valid && next_valid)
                    {
                        switch (peek.type)
                        {
                            // AND
                            case TokenType.BooleanOperator:
                                operation_token.operation = OperationType.And;
                                clean_up = true;
                                break;
                            // OR
                            case TokenType.OrOperator:
                                operation_token.operation = OperationType.Or;
                                clean_up = true;
                                break;
                        }
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
                    else i++;
                }

                // conditional virtual equality
                if (m_tokens.Count == 2 && (m_tokens[0].type == TokenType.StartConditional || m_tokens[0].type == TokenType.ConditionalElseIf))
                {
                    Token equality = new Token(TokenType.GenericOperation, 1, m_tokens[1].token_string);
                    equality.operation = OperationType.Equality;
                    equality.a = m_tokens[1];
                    equality.b = new Token(TokenType.StaticBoolean, 3, "True");

                    m_tokens[1] = equality;
                }

                // final pass, for single token operations
                // this pass is reversed
                for (int i = m_tokens.Count - 2; i >= 1; --i)
                {
                    Token peek = m_tokens[i + 1];

                    if (m_tokens[i].type == TokenType.BooleanInvert)
                    {
                        Token operation_token = new Token(TokenType.GenericOperation, -1, "NUL");
                        operation_token.start = i;
                        operation_token.token_string = m_tokens[i].token_string + " " + peek.token_string;
                        operation_token.operation = OperationType.Invert;
                        operation_token.a = m_tokens[i + 1];

                        // remove combined tokens
                        m_tokens.RemoveAt(i + 1);
                        // assign combined token
                        m_tokens[i] = operation_token;
                    }
                }

                if (m_tokens.Count == 0) m_tokens.Add(new Token(TokenType.EOL, 0, "EOL"));

                this.tokens = m_tokens;
            }
            catch (KeplerError e)
            {
                this.tokens = new List<Token>();

                foreach (string s in line.Split(' '))
                {
                    this.tokens.Add(new Token(TokenType.Generic, m_tokens.Count, s));
                }

                throw new KeplerException(this, e.GetErrorString(), null);
            }
        }

        private bool IsStaticValue(Token token)
        {
            switch (token.type)
            {
                case TokenType.StaticBoolean:
                    return true;
                case TokenType.StaticFloat:
                    return true;
                case TokenType.StaticInt:
                    return true;
                case TokenType.StaticString:
                    return true;
                case TokenType.StaticUnsignedInt:
                    return true;
                default:
                    return false;
            }
        }

        private TokenMatch GetTokenType(string token, string peek, string previous)
        {
            for (int i = 0; i < match_tokens.Length; ++i)
            {
                if (match_tokens[i].Match(token, peek, previous)) return match_tokens[i];
            }

            throw new KeplerError(KeplerErrorCode.GENERIC_ERROR, new string[] { "Unrecognized token: " + token });
        }

        public string GetString()
        {
            string tostring = "";

            for (int i = 0; i < this.tokens.Count; ++i)
            {
                tostring = tostring + this.tokens[i].token_string + " ";
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

            for (int c = 0; c < this.tokens.Count; ++c)
            {
                tostring = tostring + tab + this.tokens[c].ToString() + "\r\n";
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
            if (m_num >= tokens.Count) return new Token(TokenType.EOL, 0, "EOL");
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

        static TokenMatch[] match_tokens = new TokenMatch[] {
                new TokenMatch(TokenType.EOP, "EOP", "EOP", null, 0), // End of Program token
                new TokenMatch(TokenType.EOP, "EOP", null, null, 0), // End of Program token

                new TokenMatch(TokenType.StartAssertion, "assert", TokenMatch.any_string, null, 0), // Assertion token

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
                new TokenMatch(TokenType.ThrowError, "throw", TokenMatch.any_string, null, 0),  // throwing errors
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
                new TokenMatch(TokenType.BooleanOperator, ",", TokenMatch.any_string, TokenMatch.any_string, 0), // alias for "and"
                new TokenMatch(TokenType.OrOperator, "or", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.BooleanInvert, "not", TokenMatch.any_string, TokenMatch.any_string, 0),

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

                new TokenMatch(TokenType.AssignFunctionType, "returns", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.FunctionReturn, "return", TokenMatch.any_string, null, 0),
                new TokenMatch(TokenType.ConditionalIf, "if", TokenMatch.any_string, null, 0),
                new TokenMatch(TokenType.ConditionalElse, "else", null, null, 0),
                new TokenMatch(TokenType.ConditionalElseIf, "elseif", TokenMatch.any_string, null, 0),
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
                new TokenMatch(TokenType.StaticString, TokenMatch.eval_string, null, TokenMatch.any_string, 0),

                // static values before operators
                new TokenMatch(TokenType.StaticBoolean, "True", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticBoolean, "False", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticInt, TokenMatch.eval_int, TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticFloat, TokenMatch.eval_float, TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.StaticUnsignedInt, TokenMatch.eval_uint, TokenMatch.any_string, TokenMatch.any_string, 0),

                // operations
                new TokenMatch(TokenType.GenericAdd, "+", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.GenericSubtract, "-", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.GenericMultiply, "*", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.GenericPower, "^", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.GenericDivide, "/", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.GenericModulo, "%", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.GenericEquality, "equals", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.GenericStrictEquality, "==", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.GenericLessThan, "<", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.GenericGreaterThan, ">", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.GenericLessThanEqual, "<=", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.GenericGreaterThanEqual, ">=", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.CastType, "cast", TokenMatch.any_string, TokenMatch.any_string, 0),

                // generic tokens
                new TokenMatch(TokenType.Generic, "!", TokenMatch.any_string,TokenMatch.any_string,0),
                new TokenMatch(TokenType.Generic, "@", TokenMatch.any_string,TokenMatch.any_string,0),
                new TokenMatch(TokenType.Generic, "#", TokenMatch.any_string,TokenMatch.any_string,0),
                new TokenMatch(TokenType.Generic, "$", TokenMatch.any_string,TokenMatch.any_string,0),
                new TokenMatch(TokenType.Generic, "&", TokenMatch.any_string,TokenMatch.any_string,0),
                new TokenMatch(TokenType.Generic, "(", TokenMatch.any_string,TokenMatch.any_string,0),
                new TokenMatch(TokenType.Generic, ")", TokenMatch.any_string,TokenMatch.any_string,0),

                new TokenMatch(TokenType.CallFunction, "call", TokenMatch.any_string, TokenMatch.any_string, 0),
                new TokenMatch(TokenType.DeclareFunction, TokenMatch.any_string, TokenMatch.any_string, "call", 0),

                // variable things
                new TokenMatch(TokenType.DeclareVariable, TokenMatch.valid_variable, TokenMatch.any_string, "return", 0), // any text following a "return" that isn't tokenized as a StaticType
                new TokenMatch(TokenType.DeclareVariable, TokenMatch.valid_variable, null, TokenMatch.any_string, 0), // any string at the end of a line is assumed to be a variable name
                new TokenMatch(TokenType.DeclareVariable, TokenMatch.valid_variable, TokenMatch.any_string, TokenMatch.any_string, 0),

                new TokenMatch(TokenType.Generic, TokenMatch.any_string, TokenMatch.any_string, TokenMatch.any_string, 0) // catch anything that isn't a keyword or valid variable name
        };
    }
}

namespace Kepler.Lexer.Tokens
{
    public enum TokenType
    {
        BooleanOperator,
        BooleanInvert,
        OrOperator,
        ConstantValue,

        // variable things
        DeclareVariable, // context dependant! if DeclareVariable is FIRST token and it doesn't already exist, CREATE the variable. otherwise, access the variable
        StaticVariableType,
        StaticModifier,
        CastType,

        // header things
        DeclareHeader, // context dependant! MUST follow a StartHeader!
        StartHeader,
        EndHeader,


        GenericAssign, // "is" action dependant on context

        Generic,

        // function things
        DeclareFunction, // context dependant! if DeclareFunction follows a StartFunction, create the function! otherwise, it MUST follow a CallFunction.
        StartFunction,
        EndFunction,
        AssignFunctionType,
        FunctionReturn,
        StartArguments,
        StartPositionalArguments,
        StartNonPositionalArguments,
        CallFunction,

        // conditional things
        StartConditional,
        EndConditional,

        // operations
        GenericAdd,
        GenericSubtract,
        GenericMultiply,
        GenericPower,
        GenericDivide,
        GenericOperation,
        GenericModulo,
        GenericEquality, // equality comparison (non strict, will cast)
        GenericStrictEquality, // (if type != type, return false)
        GenericLessThan,
        GenericGreaterThan,
        GenericLessThanEqual,
        GenericGreaterThanEqual,

        // conditions
        ConditionalIf,
        ConditionalElse,
        ConditionalElseIf,

        // type stuff
        StaticBoolean,
        StaticInt,
        StaticFloat,
        StaticUnsignedInt,
        // StaticUnsignedFloat,
        StartStaticArray,
        EndStaticArray,
        StartStaticList,
        EndStaticList,

        StaticString,

        // linking things
        LinkFile,
        ThrowError,

        ConsolePrint, // print to console

        // looping things
        StartInterval,
        DeclareInterval,
        EndInterval,
        StartLoop,
        DeclareLoop,
        EndLoop,
        BreakOut,

        // signaling things
        EOP, // End of Program
        EOL, // End of Line

        StartAssertion,

        UNRECOGNIZED
    }
    public enum VariableType
    {
        Float,
        Int,
        uFloat,
        uInt,
        String,
        Function,
        List,
        Array,
        Boolean
    }
    public enum OperationType
    {
        Add,
        Subtract,
        Divide,
        Multiply,
        Power,
        Modulo,
        StrictEquality,
        Equality,
        GreaterThan,
        GreaterThanEqual,
        LessThan,
        LessThanEqual,
        And,
        Or,
        Invert,
        CastType,
    }
    public class Float
    {
        public VariableType type = VariableType.Float;
        private float value;
        public Float(float value)
        {
            this.value = value;
        }
    }
    public class uFloat
    {
        public VariableType type = VariableType.uFloat;
        private float value;
        public uFloat(float value)
        {
            this.value = Math.Abs(value);
        }
    }
    public class Int
    {
        public VariableType type = VariableType.Int;
        private int value;
        public Int(int value)
        {
            this.value = value;
        }
    }
    public class uInt
    {
        public VariableType type = VariableType.Int;
        private uint value;
        public uInt(uint value)
        {
            this.value = value;
        }
    }

}