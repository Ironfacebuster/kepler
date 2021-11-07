/*
 *   Copyright (c) 2021 William Huddleston
 *   All rights reserved.
 *   License: Apache 2.0
 */

using Arguments;
using Help;
using Kepler.Exceptions;
using Kepler.Interpreting;
using Kepler.Lexer;
using Kepler.LogicControl;
using Kepler.Tracing;
using Kepler.Versioning;
using System;
using System.IO;

namespace KeplerCompiler
{
    class Program
    {
        static Tokenizer tokenizer = new Tokenizer();
        static bool verbose_debug = false;
        static bool debug = false;

        static void Main(string[] args)
        {
            ArgumentList arguments = new ArgumentList();

            arguments.AddArgument(new ArgType("file", ArgType.AnyValue));
            arguments.AddArgument(new ArgType("filename", ArgType.AnyValue));
            arguments.AddArgument(new ArgType("help"));
            arguments.AddArgument(new ArgType("headless"));
            arguments.AddArgument(new ArgType("version"));
            arguments.AddArgument(new ArgType("debug", new string[] { "verbose", ArgType.BoolTrue }));

            arguments.Parse(args);

            if (arguments.HasArgument("help"))
            {
                // HelpList is a custom class that lets me print a nice looking help menu.
                HelpList list = new HelpList();

                // It automatically finds the longest argument and uses that to calculate the padding.
                list.AddOption("--file", "The pathlike directory/filename of the kepler file.");
                list.AddOption("--headless", "Run in \"headless\" mode, for integration.");
                list.AddOption("--help", "Show the list of arguments.");
                list.AddOption("--version", "Display the currently installed Kepler version.");
                list.AddOption("--debug", "Enable debug logging.");

                list.Print();

                Environment.Exit(0);
            }

            if (arguments.HasArgument("version"))
            {
                Console.WriteLine(StaticValues._VERSION);
                Environment.Exit(0);
            }

            debug = arguments.HasArgument("debug");
            verbose_debug = arguments.GetArgument("debug") == "verbose";

            KeplerErrorStack tracer = new KeplerErrorStack();

            try
            {
                if (arguments.HasArgument("file") || arguments.HasArgument("filename")) Init(arguments, tracer);
                else LiveInterpret(arguments, tracer);
            }
            catch (KeplerException e)
            {
                LogKeplerException(e, true);
                Environment.Exit(-1);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine("");
#if DEBUG
                Console.Write(e);
#else
                Console.Write(e.Message);
#endif

                Console.ResetColor(); // reset the color back to default
                Console.WriteLine("");

                Environment.ExitCode = -1;
            }
        }

        static void Init(ArgumentList arguments, KeplerErrorStack tracer)
        {

            // load the file, and tokenize it.
            string filename = arguments.HasArgument("filename") ? arguments.GetArgument("filename") : arguments.GetArgument("file");
            tokenizer.Load(filename);

            Interpreter interpreter = new Interpreter(null, null);
            interpreter.filename = Path.GetFileName(filename);
            interpreter.debug = debug;
            interpreter.verbose_debug = verbose_debug;
            interpreter.tracer = tracer;

            // "load" required static values
            LoadStaticValues(interpreter);

            // this doesn't work right now because the executable is standalone
            // load static values from file if the static directory exists
            // if (Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "kepler_static\\")) LoadStaticFile(interpreter);

            // do interpretation
            while (tokenizer.HasNext() || interpreter.interrupts.HasAnyInterrupts())
            {
                if (interpreter.interrupts.HasInterrupts())
                    interpreter.HandleInterrupts(false);
                if (tokenizer.HasNext())
                {
                    interpreter.Interpret(tokenizer.CurrentLine());
                    tokenizer++;
                }
            }
        }

        static void LiveInterpret(ArgumentList arguments, KeplerErrorStack tracer)
        {
            bool headless_mode = arguments.HasArgument("headless");

            if (!headless_mode)
            {
                Console.WriteLine(String.Format("\r\nKepler {0} ({1})", StaticValues._VERSION, StaticValues._TYPE));
                Console.WriteLine(String.Format("Build date: {0}\r\n", StaticValues._RELEASE));
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("Live Interpretation");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("");
                Console.WriteLine("Type \".help\" for help");
                Console.WriteLine("");
            }

            Interpreter interpreter = new Interpreter(null, null);
            interpreter.tracer = tracer;


            interpreter.verbose_debug = verbose_debug;
            interpreter.debug = debug;

            // "load" required static values
            LoadStaticValues(interpreter);

            // this doesn't work right now because the executable is standalone
            // load static values from file if the static directory exists
            // if (Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "kepler_static\\")) LoadStaticFile(interpreter);

            int line = 1;
            while (true)
            {
                try
                {
                    if (interpreter.interrupts.HasInterrupts())
                        interpreter.HandleInterrupts(false);

                    if (!headless_mode) Console.Write("> ");
                    string input = Console.ReadLine();

                    if (!headless_mode && input.StartsWith("."))
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
                catch (KeplerException e)
                {
                    LogKeplerException(e, false);

                    if (headless_mode) Environment.Exit(-1);
                }
            }
        }

        /// <summary>
        /// "Loads" various constant values as variables.
        /// </summary>
        static void LoadStaticValues(Interpreter interpreter)
        {
            // load "main" module
            interpreter.statemachine.LoadModule("main");
        }

        // static void LoadStaticFile(Interpreter interpreter)
        // {
        //     Tokenizer t = new Tokenizer();

        //     string directory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        //     t.Load(directory + "\\kepler_static\\static_values.kep");

        //     interpreter.statemachine.end_on_eop = false;

        //     while (t.HasNext())
        //     {
        //         LineIterator line = StaticValues.ReplaceMacros(t.CurrentLine());
        //         interpreter.Interpret(line);

        //         t++;
        //     }

        //     interpreter.statemachine.end_on_eop = true;
        // }

        static void LogKeplerException(KeplerException e, bool show_trace)
        {
            LineIterator c_line = e.line;
            Token c_token = e.line.CurrentToken();

            string full_line = c_line.GetString();
            string[] split_line = full_line.Split(" ");
            string line_header = String.Format("<{0}>: ", e.line.line);

            string spaces = "";

            int token_start = Math.Min(c_token.start + e.token_offset, c_line.tokens.Count - 1);

            for (int i = 0; i < token_start; i++)
            {
                int len = 0;
                while (len < split_line[i].Length)
                {
                    spaces = spaces + " ";
                    len++;
                }

                spaces = spaces + " "; // add space between words
            }

            spaces = spaces.PadLeft(spaces.Length + line_header.Length, ' ');

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write(line_header);

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(c_line.GetString());

            Console.ForegroundColor = ConsoleColor.Red;

            Console.Write(spaces);
            int marker_length = c_line.tokens.Count > 0 ? token_start > c_line.tokens.Count - 1 ? 1 : c_line.tokens[token_start].token_string.Length : 0;

            if (marker_length == 1)
                Console.Write("^");
            else for (int i = 0; i < marker_length; i++)
                    Console.Write("~");

            Console.WriteLine("");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(e.message);

            if (show_trace && e.stack != null)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(e.stack.GetStack());
            }
            else Console.WriteLine("");

            Console.ResetColor(); // reset the color back to default
            Console.WriteLine("");
        }
    }
}
