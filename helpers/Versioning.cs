using System.Text.RegularExpressions;

namespace KeplerVersioning
{
    public class StaticValues
    {
        public static string _VERSION = "v1a1";
        public static string _RELEASE = "July 12th, 2021";

        public static string ReplaceMacros(string line)
        {
            // use the static values above
            // and replace macros like $_VERSION
            line = new Regex(@"(?<!^)(\$_VERSION)").Replace(line, EscapeString(_VERSION));
            return line;
        }

        private static string EscapeString(string value)
        {
            return "\"" + value + "\"";
        }
    }
}