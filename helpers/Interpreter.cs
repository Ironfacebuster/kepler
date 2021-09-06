using System;
using System.Collections.Generic;
using KeplerTokenizer;
using KeplerTokens.Tokens;
using StateMachine;
using KeplerVariables;

using System.Linq;

namespace KeplerInterpreter
{
    public class Interpreter
    {
        // TODO: SCOPES!!!!!
        public TokenStateLevels levels = new TokenStateLevels();
        public bool verbose_debug = false;
        public bool has_parent = false;
        public Interpreter parent;
        public TokenState c_state = new TokenState(TokenType.UNRECOGNIZED, null, null);
        public Token c_token = new Token(TokenType.UNRECOGNIZED, 0, "NUL");
        // bool assigned_token = false;
        bool inside_function = false;
        bool inside_conditional = false;
        bool inside_interrupt = false;
        int conditional_indentation = 0;
        bool skip_conditional = false;

        List<KeplerInterrupt> interrupts = new List<KeplerInterrupt>();
        public KeplerFunction c_function = new KeplerFunction("NUL");
        public KeplerInterrupt c_interrupt = new KeplerInterrupt(-1, new KeplerFunction("NUL"));
        public KeplerFunction c_conditional = new KeplerFunction("CONDITIONAL");
        public LineIterator c_line = new LineIterator("", 0, 0);

        static string PrintLine(LineIterator line)
        {
            string[] split_line = line.GetString().Split(" ");
            // string[] split_line = Regex.Split(line.GetString(), "('.*?'|\".*?\"|\\S+)");
            string line_header = string.Format("Line <{0}>: ", line.line);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write(line_header);
            Console.ForegroundColor = ConsoleColor.White;

            return line_header;
        }

        public void Interpret(LineIterator line)
        {
            this.levels.interpreter = this;
            levels.verbose_debug = verbose_debug;
            try
            {
                if (levels.has_linked_file)
                {
                    if (verbose_debug) Console.WriteLine("Linked file loaded!");
                    // file was linked, so we copy all the global variables and functions

                    foreach (KeyValuePair<string, KeplerVariable> pair in levels.linked_variables.list)
                    {
                        if (verbose_debug) Console.WriteLine(string.Format("Transferring {0}", pair.Key));
                        if (levels.variables.list.ContainsKey(pair.Key)) throw new LinkedFileException(string.Format("{0} has already been declared.", pair.Key));
                        levels.variables.list.Add(pair.Key, pair.Value);
                    }

                    foreach (KeyValuePair<string, KeplerFunction> pair in levels.linked_functions.list)
                    {
                        if (verbose_debug) Console.WriteLine(string.Format("Transferring {0}", pair.Key));
                        if (levels.functions.list.ContainsKey(pair.Key)) throw new LinkedFileException(string.Format("{0} has already been declared.", pair.Key));
                        levels.functions.list.Add(pair.Key, pair.Value);
                    }

                    levels.has_linked_file = false; // reset to false
                }

                internal_int(line);
            }
            catch (TokenException e)
            {

                if (verbose_debug) this.DUMP();
                else Console.WriteLine("");


                string full_line = line.GetString();
                string[] split_line = full_line.Split(" ");
                string line_header = PrintLine(line);

                string spaces = "";

                for (int i = 0; i < c_token.start + 1; i++)
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


                Console.Write(c_line.GetString() + "\r\n");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(spaces + "^ " + e.Message);

                Console.ForegroundColor = ConsoleColor.White;
                Console.ResetColor(); // backup reset color (doesn't work for me, but oh well)
                Environment.Exit(-1);
            }
            catch (InterpreterException e)
            {

                if (verbose_debug) this.DUMP();
                else Console.WriteLine("");


                string full_line = c_line.GetString();
                string[] split_line = full_line.Split(" ");
                string line_header = PrintLine(line);

                string spaces = "";

                for (int i = 0; i < c_token.start + 1; i++)
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

                Console.Write(c_line.GetString() + "\r\n");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(spaces + "^ " + e.Message);

                Console.ForegroundColor = ConsoleColor.White;
                Console.ResetColor(); // backup reset color (doesn't work for me, but oh well)
                Environment.Exit(-1);
            }
            catch (EOPException e)
            {

                if (verbose_debug) this.DUMP();
                else Console.WriteLine("");


                string full_line = c_line.GetString();
                string[] split_line = full_line.Split(" ");
                string line_header = PrintLine(line);

                string spaces = "".PadLeft(line_header.Length, ' ');

                Console.Write(c_line.GetString() + "\r\n");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(spaces + "^ " + e.Message);

                Console.ForegroundColor = ConsoleColor.White;
                Console.ResetColor(); // backup reset color (doesn't work for me, but oh well)
                Environment.Exit(-1);
            }
            catch (LinkedFileException e)
            {

                if (verbose_debug) this.DUMP();
                else Console.WriteLine("");

                string full_line = c_line.GetString();
                string[] split_line = full_line.Split(" ");
                string line_header = PrintLine(line);

                string spaces = "";

                int len = 0;
                while (len < split_line[0].Length)
                {
                    spaces = spaces + " ";
                    len++;
                }

                spaces = spaces + " "; // add space between words

                spaces = spaces.PadLeft(spaces.Length + line_header.Length, ' ');

                Console.Write(c_line.GetString() + "\r\n");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(string.Format("{0}^ Error linking file: {1}", spaces, e.Message));

                Console.ForegroundColor = ConsoleColor.White;
                Console.ResetColor(); // backup reset color (doesn't work for me, but oh well)
                Environment.Exit(-1);
            }
            catch (LevelOneException e)
            {

                if (verbose_debug) this.DUMP();
                else Console.WriteLine("");

                string full_line = c_line.GetString();
                string[] split_line = full_line.Split(" ");
                string line_header = PrintLine(line);

                string spaces = "";

                for (int i = 0; i < c_token.start; i++)
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

                Console.Write(c_line.GetString() + "\r\n");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(spaces + "^ " + e.Message);

                Console.ForegroundColor = ConsoleColor.White;
                Console.ResetColor(); // backup reset color (doesn't work for me, but oh well)
                Environment.Exit(-1);
            }
            catch (Exception e)
            {
                if (verbose_debug) this.DUMP();
                else Console.WriteLine("");

                Console.WriteLine(e);
                Environment.Exit(-1);
            }
        }

        public void DUMP()
        {
            Console.WriteLine("");
            Console.WriteLine(this.levels.functions);
            Console.WriteLine(this.levels.variables);

            Console.WriteLine(this.c_state.a_operand);

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\r\nCurrent State");
            Console.ForegroundColor = ConsoleColor.White;

            foreach (KeyValuePair<string, bool> pair in this.c_state.booleans)
            {
                Console.WriteLine(string.Format("{0} => {1}", pair.Key, pair.Value));
            }
            foreach (KeyValuePair<string, string> pair in this.c_state.strings)
            {
                Console.WriteLine(string.Format("{0} => \"{1}\"", pair.Key, pair.Value));
            }

            Console.WriteLine("");
        }

        void internal_int(LineIterator line)
        {
            if (verbose_debug) Console.WriteLine("Registered Interrupts: " + this.interrupts.Count);
            levels.end_on_eop = levels.end_on_eop && this.interrupts.Count == 0;

            c_line.m_num = 0;
            c_line = line;
            bool assigned_level_one_state = false;

            if (inside_interrupt)
            {
                c_line.m_num = 0;

                if (line.tokens[0].type == TokenType.EOP) throw new EOPException("Unexpected end of program!");

                if (verbose_debug) Console.WriteLine("ADDING TO INTERRUPT -> " + c_line.GetString());
                c_interrupt.function.lines.Add(c_line);

                if (line.CurrentToken().type == TokenType.EndInterval && line.Peek().token_string == "every")
                {
                    c_interrupt.function.lines.RemoveAt(c_interrupt.function.lines.Count - 1);
                    inside_interrupt = false;
                    c_state.booleans["inside_interval"] = false;

                    if (verbose_debug) Console.WriteLine(string.Format("EXIT <{0}>", c_interrupt.id));
                }

                return;
            }

            if (inside_function)
            {

                c_line.m_num = 0;
                if (line.CurrentToken().type == TokenType.EOP) throw new EOPException("Unexpected end of program!");

                c_function.lines.Add(c_line);

                if (line.CurrentToken().type == TokenType.EndFunction && line.Peek().token_string == c_function.name)
                {
                    inside_function = false;
                    c_function.lines.RemoveAt(c_function.lines.Count - 1);

                    if (verbose_debug) Console.WriteLine(string.Format("EXIT <{0}>", c_function.name));
                }

                return;
            }

            if (skip_conditional)
            {
                // check for "endif"
                if (c_line.CurrentToken().type == TokenType.EndConditional && c_line.indentation == conditional_indentation) skip_conditional = false;

                return;
            }

            if (inside_conditional)
            {
                c_line.m_num = 0;

                if (line.CurrentToken().type == TokenType.EOP) throw new EOPException("Unexpected end of program!");

                c_conditional.lines.Add(c_line);

                if (c_line.CurrentToken().type == TokenType.EndConditional && line.indentation == conditional_indentation)
                {
                    inside_conditional = false;

                    Interpreter conditional_int = new Interpreter();

                    conditional_int.has_parent = true;
                    conditional_int.parent = this;

                    conditional_int.levels.is_interrupt = this.levels.is_interrupt;
                    conditional_int.levels.interrupt_id = this.levels.interrupt_id;

                    conditional_int.verbose_debug = verbose_debug;

                    conditional_int.levels.variables = levels.variables.Copy();
                    conditional_int.levels.functions = levels.functions.Copy();

                    for (int i = 0; i < c_conditional.lines.Count; i++)
                        conditional_int.Interpret(c_conditional.lines[i]);
                }

                return;
            }

            if (verbose_debug)
            {
                Console.WriteLine("\r\nINTERP. " + c_line.GetString());

                foreach (Token t in c_line.tokens)
                {
                    Console.Write(string.Format("{0} ({1}) ", t.token_string, t.type));
                }

                Console.Write("\r\n");
            }
            while (line.HasNext())
            {

                c_token = line.CurrentToken();

                if (c_token.type == TokenType.EOL) break;

                if (!assigned_level_one_state)
                {
                    c_state = levels.GetLevelOneToken(c_token);
                    if (verbose_debug) Console.WriteLine("START " + c_token);
                    assigned_level_one_state = true;
                }
                else if (verbose_debug) Console.WriteLine("TOKEN " + c_token);

                if (c_token.type != TokenType.EOL && c_state.type == TokenType.EOL && c_line.Peek().type != TokenType.EOL) throw new TokenException("Unexpected EOL!");
                if (c_state.type != c_token.type) throw new TokenException(string.Format("Unexpected token {0} {1}", c_token.type, c_state.type));

                c_state = c_state.DoAction(c_token, c_line.Peek()).Shift(c_state);

                // TODO: change booleans in StateMachine so that "start" doesn't have the "inside_function" bool
                if (c_state.booleans["inside_function"] && c_token.token_string != "start")
                {
                    inside_function = true;
                    c_state.c_function.lines = new List<LineIterator>(); // clear lines in case of redefinition
                    c_function = c_state.c_function;

                    if (verbose_debug) Console.WriteLine(string.Format("ENTER <{0}>", c_function.name));
                }

                if (c_state.booleans["inside_interval"] && c_token.token_string == "start")
                {
                    int int_id = this.interrupts.Count;
                    KeplerInterrupt interrupt = new KeplerInterrupt(int_id, new KeplerFunction("interval_" + int_id));

                    c_state.c_interrupt = interrupt;
                    interrupts.Add(interrupt);

                    inside_interrupt = true;
                    c_interrupt = c_state.c_interrupt;

                    if (verbose_debug) Console.WriteLine(string.Format("ENTER <{0}>", c_interrupt.id));
                }

                if (c_state.type == TokenType.EOL)
                {
                    if (c_state.booleans["console_print"]) Console.WriteLine(c_state.strings["print_string"]);
                    if (c_state.booleans["inside_conditional"])
                    {
                        c_conditional.lines = new List<LineIterator>();
                        inside_conditional = true;
                        conditional_indentation = c_line.indentation;
                    }
                    if (c_state.booleans["validate_conditional"] && !c_state.booleans["inside_conditional"])
                    {
                        conditional_indentation = c_line.indentation;
                        skip_conditional = true;
                    }

                    break;
                }

                line++;
            }
        }

        public void HandleInterrupts()
        {
            // do interrupts
            List<KeplerInterrupt> interrupts = this.GetInterrupts();

            for (int i = 0; i < interrupts.Count; ++i)
            {
                KeplerFunction int_function = interrupts[i].function;

                int_function.ResetLines();

                Interpreter f_interpreter = new Interpreter();

                f_interpreter.has_parent = true;
                f_interpreter.parent = this;

                f_interpreter.levels.variables = levels.variables.Copy();
                f_interpreter.levels.functions = levels.functions.Copy();


                f_interpreter.levels.is_interrupt = true;
                f_interpreter.levels.interrupt_id = interrupts[i].id;

                f_interpreter.verbose_debug = this.verbose_debug;

                // do interpretation
                foreach (LineIterator line in int_function.lines)
                {
                    if (HasInterrupt(interrupts[i].id))
                        f_interpreter.Interpret(line);
                    else break;
                }

                interrupts[i].Reset();
            }
        }

        public bool HasAnyInterrupts()
        {
            // cleanup pass
            int i = 0;
            while (i < this.interrupts.Count)
            {
                if (this.interrupts[i].IsDisabled()) this.interrupts.RemoveAt(i);
                else ++i;
            }

            return this.interrupts.Count > 0;
        }
        public bool HasInterrupt()
        {
            for (int i = 0; i < this.interrupts.Count; ++i)
            {
                if (this.interrupts[i].isValidInterrupt()) return true;
            }

            return false;
        }

        public bool HasInterrupt(int id)
        {
            for (int i = 0; i < this.interrupts.Count; ++i)
            {
                if (this.interrupts[i].id == id && this.interrupts[i].isValidInterrupt()) return true;
            }

            return false;
        }

        public KeplerInterrupt GetInterrupt(int id)
        {
            for (int i = 0; i < this.interrupts.Count; ++i)
                if (this.interrupts[i].id == id) return this.interrupts[i];

            if (this.has_parent)
                return this.parent.GetInterrupt(id);

            throw new InterpreterException("Unable to find interrupt with ID " + id);
        }

        public List<KeplerInterrupt> GetInterrupts()
        {
            List<KeplerInterrupt> valid_interrupts = new List<KeplerInterrupt>();

            for (int i = 0; i < this.interrupts.Count; ++i)
            {
                if (this.interrupts[i].isValidInterrupt()) valid_interrupts.Add(this.interrupts[i]);
            }

            return valid_interrupts.OrderBy(i => i.Overage()).ToList();
        }
    }

    public class InterpreterException : Exception
    {

        public InterpreterException() { }

        public InterpreterException(string message)
            : base(message) { }

        public InterpreterException(string message, Exception inner)
            : base(message, inner) { }
    }

    public class EOPException : Exception
    {
        public EOPException(string message)
            : base(message) { }
    }

    public class LinkedFileException : Exception
    {
        public LinkedFileException() { }
        public LinkedFileException(string message)
            : base(message) { }
        public LinkedFileException(string message, Exception inner)
            : base(message, inner) { }
    }
}