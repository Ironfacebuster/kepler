using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KeplerTokens
{
    namespace Tokens
    {
        public class Token
        {
            public TokenType type;
            public int start;
            public string token_string;

            // only for GenericOperation
            public Token a;
            public Token b;
            public DataTypes.OperationType operation;
            // 

            public Token(TokenType type, int index, string token_string)
            {
                this.type = type;
                this.start = index;
                this.token_string = token_string;
            }

            public override string ToString()
            {
                // if(this.type == TokenType.GenericOperation)
                //     return string.Format("{0} {1} {2}", token_string, type, start);
                return string.Format("{0} => {1} [{2}]", token_string, type, start);
            }
        }

        public class TokenMatch
        {
            public TokenType type;
            public string string_token;
            public string peek;
            public string prev;
            public int increment = 1;

            public TokenMatch(TokenType type, string string_token, string peek, string prev, int increment)
            {
                this.type = type;
                this.string_token = string_token;
                this.peek = peek;
                this.prev = prev;
                this.increment = increment;
            }

            public Boolean Match(string token, string peek, string previous)
            {
                Boolean match_token = false;
                Boolean match_previous = false;
                Boolean match_peek = false;

                if ((string.IsNullOrEmpty(this.peek) && string.IsNullOrEmpty(peek)) || this.peek == peek) match_peek = true;
                if (!string.IsNullOrEmpty(this.peek) && this.peek == any_string) match_peek = true;

                if ((string.IsNullOrEmpty(this.prev) && string.IsNullOrEmpty(previous)) || this.prev == previous) match_previous = true;
                if (!string.IsNullOrEmpty(this.prev) && this.prev == any_string) match_previous = true;

                if ((string.IsNullOrEmpty(this.string_token) && string.IsNullOrEmpty(token)) || this.string_token == token) match_token = true;


                if (!string.IsNullOrEmpty(this.string_token))
                {
                    if (this.string_token == any_string) match_token = true;
                    if (this.string_token == eval_float && Double.TryParse(token, out double f) && token.IndexOf(".") != -1) match_token = true;
                    if (this.string_token == eval_int && Int32.TryParse(token, out int i) && token.IndexOf(".") == -1) match_token = true;
                    // if (this.string_token == eval_ufloat && Double.TryParse(token.Substring(1), out double uf) && (new Regex(@"u[0-9]*.[0-9]*").Match(token).Length == 1)) match_token = true;
                    // if (this.string_token == eval_uint && Int32.TryParse(token.Substring(1), out int ui) && (new Regex(@"u[0-9]*").Match(token).Length == 1)) match_token = true;
                    if (this.string_token == eval_ufloat && Double.TryParse(token.Substring(1), out double uf) && token.IndexOf(".") != -1 && token[0] == 'u') match_token = true;
                    if (this.string_token == eval_uint && Int32.TryParse(token.Substring(1), out int ui) && token.IndexOf(".") == -1 && token[0] == 'u') match_token = true;
                }

                if (match_token && match_peek && match_previous) return true;

                return false;
            }

            public static string any_string = "$ANYSTRING"; // if the string is any string
            public static string eval_int = "$EVALINT"; // if the string parses to an valid integer
            public static string eval_float = "$EVALFLOAT"; // if the string parses to a valid float
            public static string eval_uint = "$EVALUINT"; // if the string parses to an valid integer
            public static string eval_ufloat = "$EVALUFLOAT"; // if the string parses to a valid float
        }

        public enum TokenType
        {
            BooleanOperator,
            ConstantValue,

            // variable things
            DeclareVariable, // context dependant! if DeclareVariable is FIRST token and it doesn't already exist, CREATE the variable. otherwise, access the variable
            StaticVariableType,
            StaticModifier,

            // header things
            DeclareHeader, // context dependant! MUST follow a StartHeader!
            StartHeader,
            EndHeader,


            GenericEquality, // equality comparison (if type != type, throw TypeError)
            NonToken, // inverter token: invert the result of the following tokens
            GenericAssign, // "is" action dependant on context

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

            // operations
            GenericAdd,
            GenericSubtract,
            GenericMultiply,
            GenericDivide,
            GenericOperation,

            AssignNonPositionalArgument,
            PositionalArgument,
            PositionalArgumentAssignment,
            NonPositionalArgument,

            // conditions
            ConditionalIf,
            ConditionalElse,
            ConditionalElseIf,

            // type stuff
            StaticBoolean,
            StaticInt,
            StaticFloat,
            StaticUnsignedInt,
            StaticUnsignedFloat,
            StartStaticArray,
            EndStaticArray,
            StartStaticList,
            EndStaticList,
            // DoubleQuote, // signifies the toggling of string handling

            // TODO: make StringText a FULL string token
            // rather than toggling modes during interpretation,
            // just create an entire StringText token that contains the
            // string during TOKENIZATION!
            StaticString,
            // StringText, // text that's inside of a string

            // linking things
            LinkFile,

            ConsolePrint, // print to console

            // signaling things
            EOP, // End of Program
            EOL, // End of Line

            UNRECOGNIZED
        }
    }

    namespace DataTypes
    {
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
            Multiply
            // TODO: add more operations (exp, mod, etc...)
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
}