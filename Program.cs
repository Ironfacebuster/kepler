using System;
using KeplerTokenizer;
using Arguments;
using KeplerInterpreter;
using KeplerVersioning;
using System.Collections.Generic;
using KeplerVariables;
using Help;

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
                HelpList list = new HelpList();

                list.AddOption("--file", "The pathlike directory/filename of the kepler file.");
                list.AddOption("--help", "Show the list of arguments.");
                list.AddOption("--version", "Display the currently installed Kepler version.");
                list.AddOption("--debug", "Enable debug logging.");

                list.Print();

                Environment.Exit(0);
            }

            if (arguments.HasArgument("version") || arguments.HasArgument("v"))
            {
                Console.WriteLine(StaticValues._VERSION);
                Environment.Exit(0);
            }

            try
            {
                if (arguments.HasArgument("file") || arguments.HasArgument("filename")) Init(arguments);
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
            tokenizer.Load(arguments.HasArgument("filename") ? arguments.GetArgument("filename") : arguments.GetArgument("file"));

            Interpreter interpreter = new Interpreter(null, null);
            interpreter.verbose_debug = arguments.HasArgument("debug");

            // load static values from file
            if (AppDomain.CurrentDomain.BaseDirectory.Replace("\\", "/").EndsWith("kepler/")) LoadStaticValues(interpreter);

            // do interpretation
            while (tokenizer.HasNext() || interpreter.interrupts.HasAnyInterrupts())
            {
                if (interpreter.interrupts.HasInterrupts())
                    interpreter.HandleInterrupts(false);
                else if (tokenizer.current_line < tokenizer.Lines().Count) { interpreter.Interpret(tokenizer.CurrentLine()); tokenizer++; }
            }
        }

        static void LiveInterpret(ArgumentList arguments)
        {
            Console.WriteLine(String.Format("\r\nKepler {0}", StaticValues._VERSION));
            Console.WriteLine(String.Format("Release date: {0}", StaticValues._RELEASE));
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Live Interpretation");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("");
            Console.WriteLine("Type \".help\" for help");
            Console.WriteLine("");

            Interpreter interpreter = new Interpreter(null, null);


            interpreter.verbose_debug = arguments.HasArgument("debug");

            // load static values from file
            if (AppDomain.CurrentDomain.BaseDirectory.Replace("\\", "/").EndsWith("kepler/")) LoadStaticValues(interpreter);

            int line = 1;
            while (true)
            {
                if (interpreter.interrupts.HasInterrupts())
                    interpreter.HandleInterrupts(false);
                else
                {
                    Console.Write("> ");
                    string input = Console.ReadLine();

                    if (input.StartsWith("."))
                    {
                        switch (input.Substring(1).ToLower())
                        {
                            case "help":
                                Console.WriteLine(" "); // padding
                                Console.WriteLine(".HELP    show this help menu");
                                // Console.WriteLine(".DUMP    dump some debug information"); it's a secret to everybody!
                                Console.WriteLine(".EXIT    exit immediately");
                                Console.WriteLine(" "); // padding
                                break;
                            case "dump":
                                interpreter.DUMP();
                                break;
                            case "exit":
                                Console.Write("Exiting live interpretation...");
                                Environment.Exit(0); // exit without error
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

        static void LoadStaticValues(Interpreter interpreter)
        {
            interpreter.statemachine.end_on_eop = false;

            Tokenizer t = new Tokenizer();

            string directory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            t.Load(directory + "\\kepler_static\\static_values.kep");

            while (t.HasNext())
            {
                LineIterator line = StaticValues.ReplaceMacros(t.CurrentLine());
                interpreter.Interpret(line);

                t++;
            }

            interpreter.statemachine.end_on_eop = true;
        }
    }
}
