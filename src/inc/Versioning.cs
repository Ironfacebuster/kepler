/*
 *   Copyright (c) 2021 William Huddleston
 *   All rights reserved.
 *   License: Apache 2.0
 */

using kepler.Properties;
using Kepler.Lexer;
using Kepler.Lexer.Tokens;
using Kepler.LogicControl;

namespace Kepler.Versioning
{
    public class StaticValues
    {
        public static string _VERSION = "v1a1.5";
        public static string _TYPE = Resources.type;
        // public static string _RELEASE = "October 13th, 2021";
        public static string _RELEASE = Resources.buildtime;

        public static LineIterator ReplaceMacros(LineIterator line)
        {
            // use the static values above
            // and replace macros like $_VERSION

            // TODO: make this more elegant
            for (var i = 0; i < line.tokens.Count; ++i)
            {
                Token t = line.tokens[i];

                if (t.start != 0)
                {
                    switch (t.token_string)
                    {
                        case "$_VERSION":
                            line.tokens[i] = new Token(TokenType.StaticString, t.start, EscapeString(_VERSION));
                            break;
                        case "NaN":
                            // this is ugly
                            // maybe add a StaticNaN?
                            line.tokens[i] = new Token(TokenType.GenericOperation, t.start + 1, "NaN");
                            line.tokens[i].a = new Token(TokenType.StaticString, t.start, "null");
                            line.tokens[i].b = new Token(TokenType.StaticInt, t.start + 2, "0");
                            line.tokens[i].operation = OperationType.Subtract;
                            break;
                    }
                }
            }

            return line;
        }

        private static string EscapeString(string value)
        {
            return "\"" + value + "\"";
        }
    }
}