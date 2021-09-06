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
    public class ArgumentList
    {
        List<Argument> arguments = new List<Argument>();
        public ArgumentList(string[] args)
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
        public Boolean HasArgument(string argument)
        {
            Boolean hasArgument = false;
            foreach (Argument arg in arguments)
            {
                if (arg.argument == argument)
                {
                    hasArgument = true;
                    break;
                }
            }

            return hasArgument;
        }
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
    }
}