using System;
using KeplerTokenizer;
using Arguments;
using KeplerInterpreter;
using KeplerVersioning;
using KeplerStateMachine;
using KeplerExceptions;
using KeplerTracing;
using Help;
using System.IO;

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
            interpreter.verbose_debug = arguments.HasArgument("debug");
            interpreter.tracer = tracer;

            // load static values from file
            if (AppDomain.CurrentDomain.BaseDirectory.Replace("\\", "/").EndsWith("kepler/")) LoadStaticValues(interpreter);
            // do interpretation
            while (tokenizer.HasNext() || interpreter.interrupts.HasAnyInterrupts())
            {
                // Console.WriteLine(tokenizer.HasNext());
                if (interpreter.interrupts.HasInterrupts())
                    interpreter.HandleInterrupts(false);
                else if (tokenizer.current_line < tokenizer.Lines().Count)
                {
                    interpreter.Interpret(tokenizer.CurrentLine());
                    tokenizer++;
                }
            }
        }

        static void LiveInterpret(ArgumentList arguments, KeplerErrorStack tracer)
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
            interpreter.tracer = tracer;


            interpreter.verbose_debug = arguments.HasArgument("debug");

            // load static values from file
            if (AppDomain.CurrentDomain.BaseDirectory.Replace("\\", "/").EndsWith("kepler/")) LoadStaticValues(interpreter);

            int line = 1;
            while (true)
            {
                try
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
                catch (KeplerException e)
                {
                    LogKeplerException(e, false);
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

        static void LogKeplerException(KeplerException e, bool show_trace)
        {
            LineIterator c_line = e.line;
            Token c_token = e.line.CurrentToken();

            string full_line = c_line.GetString();
            string[] split_line = full_line.Split(" ");
            string line_header = String.Format("<{0}>: ", e.line.line);

            string spaces = "";

            int token_start = c_token.start + e.token_offset;

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
            // Console.WriteLine(spaces + "^ ");
            Console.Write(spaces);
            int marker_length = c_line.tokens.Count > 0 ? token_start > c_line.tokens.Count - 1 ? 1 : c_line.tokens[token_start].token_string.Length : 0;

            if (marker_length == 1)
                Console.Write("^");
            else
                for (int i = 0; i < marker_length; i++)
                {
                    Console.Write("~");
                }

            Console.WriteLine("");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(e.message);

            if (show_trace)
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
