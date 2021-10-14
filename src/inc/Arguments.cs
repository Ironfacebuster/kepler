using System;
using System.Collections.Generic;

namespace Arguments
{

    public class Argument
    {
        public string argument;
        public string value;

        public Argument(string argument, string value)
        {
            this.argument = argument;
            this.value = value;
        }

        public override string ToString()
        {
            return string.Format("{0} -> {1}", argument, value);
        }
    }

    public class ArgType
    {
        public static string AnyValue = "$ANYVALUE";
        public static string NoValue = "$NOVALUE";
        public static string BoolTrue = "true";

        public string argument;
        public string[] values;

        public ArgType(string arg)
        {
            argument = arg;
            values = new string[] { BoolTrue };
        }

        public ArgType(string arg, string value)
        {
            argument = arg;
            values = new string[] { value };
        }

        public ArgType(string arg, string[] vals)
        {
            argument = arg;
            values = vals;
        }
    }

    public class ArgumentList
    {

        List<Argument> arguments = new List<Argument>();
        List<string> invalid_arguments = new List<string>();
        List<ArgType> validators = new List<ArgType>();
        public ArgumentList(string[] args)
        {
            this.Parse(args);
        }

        public ArgumentList()
        {

        }

        public override string ToString()
        {
            string output = "\r\nArgumentList\r\n";
            output = output + string.Format("{0} argument(s)", arguments.Count);

            foreach (Argument arg in arguments)
            {
                output = output + "\r\n" + arg.argument + ": " + arg.value;
            }

            return output;
        }

        /// <summary>
        /// Check if an argument exists.
        /// </summary>
        public Boolean HasArgument(string argument)
        {
            foreach (Argument arg in arguments)
            {
                if (arg.argument == argument)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get an argument's value, if it exists.
        /// </summary>
        public string GetArgument(string argument)
        {
            foreach (Argument arg in arguments)
            {
                if (arg.argument == argument)
                {
                    return arg.value;
                }
            }

            return null;
        }
        public void AddArgument(ArgType validator)
        {
            validators.Add(validator);
        }

        /// <summary>
        /// Parse an array of string arguments to a usable set of data, and return any unrecognized arguments.
        /// </summary>
        public string[] Parse(string[] args)
        {
            for (var i = 0; i < args.Length;)
            {
                // create the argument
                if (args[i].StartsWith("--"))
                {
                    // if the next argument starts with two dashes, this argument has no value
                    if (i + 1 >= args.Length || args[i + 1].StartsWith("--"))
                    {
                        arguments.Add(new Argument(args[i].Substring(2), "true"));
                        i++;
                    }
                    // otherwise, this argument has a value associated with it
                    else
                    {
                        arguments.Add(new Argument(args[i].Substring(2), args[i + 1]));
                        i += 2;
                    }
                }
                else
                {
                    arguments.Add(new Argument("filename", args[i]));
                    i++;
                }

                // last_index = i;
            }

            // validate arguments
            List<string> invalid_arguments = new List<string>();
            if (validators.Count > 0)
            {
                foreach (Argument arg in arguments)
                {
                    ArgType validator = null;
                    foreach (ArgType v in validators)
                    {
                        if (v.argument == arg.argument)
                        {
                            validator = v;
                            break;
                        }
                    }
                    bool valid = false;

                    // validate argument if a validator is found
                    if (validator != null)
                    {
                        foreach (string val in validator.values)
                        {
                            if (val == ArgType.AnyValue) valid = true;
                            // if (val == ArgType.NoValue && arg.value == ArgType.BoolTrue) valid = true; // BoolTrue is the default when there is no specified value
                            if (val == arg.value) valid = true;

                            if (valid) break;
                        }
                    }

                    if (!valid) invalid_arguments.Add(arg.argument);
                }
            }

            return invalid_arguments.ToArray();
        }

        ArgType GetValidator(Argument arg)
        {
            ArgType validator = null;
            foreach (ArgType v in validators)
            {
                if (v.argument == arg.argument)
                {
                    validator = v;
                    break;
                }
            }

            return validator;
        }

        /// <summary>
        /// Using Levenshtein distance, this method finds the closest registered argument and returns it.
        /// </summary>
        public string GetClosestArgument(string arg)
        {
            string closest = null;
            double percentage = 0;

            foreach (ArgType v in validators)
            {
                int distance = LevenshteinDistance(arg, v.argument);
                double new_percentage = (1.0 - ((double)distance / (double)Math.Max(arg.Length, v.argument.Length)));

                if (new_percentage > percentage && new_percentage > 0.5)
                {
                    closest = v.argument;
                    percentage = new_percentage;
                }
            }

            return closest;
        }

        /// <summary>
        /// Compute the distance between two strings. https://www.dotnetperls.com/levenshtein
        /// </summary>
        static int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }
    }
}