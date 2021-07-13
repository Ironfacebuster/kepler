using System;
using System.Text.RegularExpressions;
using KeplerTokenizer;
using KeplerTokens.Tokens;

namespace KeplerVersioning
{
    public class StaticValues
    {
        public static string _VERSION = "v1a1.1";
        public static string _RELEASE = "July 13th, 2021";

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