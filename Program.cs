using System;
using KeplerTokenizer;
using System.Collections.Generic;
using Arguments;
using KeplerInterpreter;

namespace KeplerCompiler
{
    class Program
    {
        static Tokenizer tokenizer = new Tokenizer();
        static void Main(string[] args)
        {
            // if (args.Length == 0) throw new Exception("Filename not provided!");
            ArgumentList arguments = new ArgumentList(args);

            if (arguments.HasArgument("help") || arguments.HasArgument("h"))
            {
                Console.WriteLine("\r\nSCode/Kepler Interpreter and Compiler v1.0");
                Console.WriteLine("Release date: June 10th, 2021");

                Console.WriteLine("\r\n--build     Compile the supplied Kepler file.");
                //  Console.WriteLine("--file      Compile the supplied Kepler file.")
                Console.WriteLine("--file      The directory/filename of the Kepler file.");
                Console.WriteLine("--help      Show the list of arguments.");
                Console.WriteLine("--debug     Enable debug logging.");

                Environment.Exit(0);
            }

            try
            {
                if (arguments.HasArgument("file")) Init(arguments);
                else LiveInterpret(arguments);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(" ");
                Console.WriteLine(e.Message);
                Console.ForegroundColor = ConsoleColor.White;
                Console.ResetColor();

                Environment.ExitCode = -1;
            }

        }

        static void Init(ArgumentList arguments)
        {

            // load the file, and tokenize it.
            tokenizer.Load(arguments.GetArgument("file"));

            if (arguments.HasArgument("build"))
            {
                Console.WriteLine("\r\nSCode/Kepler Compiler v1.0");
                Console.WriteLine("Release date: June 10th, 2021");
                Console.WriteLine("\r\nStarting compilation...");
                DateTime started = DateTime.Now;

                // do compilation

                TimeSpan time_elapsed = DateTime.Now - started;

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\r\nDone!");

                Console.ForegroundColor = ConsoleColor.White;
                Console.ResetColor();
                Console.WriteLine(string.Format("Time Elapsed {0}", time_elapsed));
            }
            else
            {
                Interpreter interpreter = new Interpreter();
                interpreter.verbose_debug = arguments.HasArgument("debug");

                // do interpretation
                while (tokenizer.HasNext())
                {
                    interpreter.Interpret(tokenizer.CurrentLine());

                    tokenizer++;
                }

            }
        }

        static void LiveInterpret(ArgumentList arguments)
        {
            Console.WriteLine("");
            Console.WriteLine("Kepler Interpreter v1.0");
            Console.WriteLine("Release date: June 10th, 2021");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Live Interpretation");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("");

            Interpreter interpreter = new Interpreter();
            interpreter.verbose_debug = arguments.HasArgument("debug");

            int line = 1;
            while (true)
            {

                Console.Write(string.Format("{0}: ", line));
                string input = Console.ReadLine();

                if (input.StartsWith("."))
                {
                    switch (input.Substring(1))
                    {
                        case "DUMP":
                            interpreter.DUMP();
                            break;
                    }
                }
                else
                {
                    interpreter.Interpret(tokenizer.TokenizeLine(line, input));
                    line++;
                }
            }
        }
    }
}
