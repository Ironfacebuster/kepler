using System;
using System.Collections.Generic;
using Kepler.Versioning;

namespace Help
{
    public class HelpOption
    {
        public string argument;
        public string description;

        public HelpOption(string argument, string description)
        {
            this.argument = argument;
            this.description = description;
        }
    }
    public class HelpList
    {
        List<HelpOption> options = new List<HelpOption>();
        public int spacing = 4;

        public void Print()
        {
            Console.WriteLine(String.Format("\r\nKepler {0}", StaticValues._VERSION));
            Console.WriteLine(String.Format("Release date: {0}\r\n", StaticValues._RELEASE));

            int max_length = 0;
            string spacer = "";

            while (spacer.Length < spacing) spacer = spacer + " ";

            // find longest line
            foreach (HelpOption opt in options)
            {
                if (opt.argument.Length > max_length) max_length = opt.argument.Length;
            }

            // print out arguments
            foreach (HelpOption opt in options)
            {
                string normalized = "";
                while (normalized.Length < max_length - opt.argument.Length + spacing) normalized = normalized + " ";

                Console.WriteLine(opt.argument + normalized + opt.description);
            }

            Console.WriteLine("");
        }

        public void AddOption(string argument, string description)
        {
            this.options.Add(new HelpOption(argument, description));
        }
    }
}