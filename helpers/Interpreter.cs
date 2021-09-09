using System;
using System.Collections.Generic;
using KeplerTokenizer;
using KeplerTokens.Tokens;
using KeplerStateMachine;
using KeplerVariables;
using KeplerTracing;
using KeplerExceptions;

namespace KeplerInterpreter
{
    public class Interpreter
    {
        // TODO: SCOPES!!!!!
        public int ID = 0;
        public string filename = "[LIVE]";
        public KeplerErrorStack tracer;
        public StateMachine statemachine = new StateMachine();
        public bool verbose_debug = false;
        public bool has_parent = false;
        public Interpreter parent;
        public bool is_global = false;
        public Interpreter global;
        public TokenState c_state = new TokenState(TokenType.UNRECOGNIZED, null, null);
        public Token c_token = new Token(TokenType.UNRECOGNIZED, 0, "NUL");
        bool inside_function = false;
        bool inside_conditional = false;
        bool inside_interrupt = false;
        bool inside_loop = false;
        int conditional_indentation = 0;
        bool skip_conditional = false;

        bool killed = false;

        public KeplerInterruptManager interrupts = new KeplerInterruptManager();
        public KeplerFunction c_function = new KeplerFunction("NUL");
        public KeplerInterrupt c_interrupt = new KeplerInterrupt(-1, new KeplerFunction("NUL"), null);
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

        public Interpreter(Interpreter global_scope, Interpreter parent)
        {
            if (global_scope == null)
            {
                this.global = this;
                this.is_global = true;
            }
            else
            {
                this.global = global_scope;
                this.interrupts = this.global.interrupts;
            }

            if (parent == null) this.has_parent = false;
            else
            {
                this.ID = parent.ID + 1;
                this.has_parent = true;
                this.parent = parent;
            }

            this.statemachine.interpreter = this;
            c_interrupt.parent = this;
        }

        public void Interpret(LineIterator line)
        {
            statemachine.verbose_debug = verbose_debug;
            try
            {
                if (statemachine.has_linked_file)
                {
                    if (verbose_debug) Console.WriteLine("Linked file loaded!");
                    // file was linked, so we copy all the global variables and functions

                    foreach (KeyValuePair<string, KeplerVariable> pair in statemachine.linked_variables.local)
                    {
                        if (verbose_debug) Console.WriteLine(string.Format("Transferring {0}", pair.Key));
                        if (statemachine.variables.global.local.ContainsKey(pair.Key)) throw new KeplerError(KeplerErrorCode.DECLARE_DUP, new string[] { pair.Key });
                        statemachine.variables.global.local.Add(pair.Key, pair.Value);
                    }

                    foreach (KeyValuePair<string, KeplerFunction> pair in statemachine.linked_functions.global)
                    {
                        if (verbose_debug) Console.WriteLine(string.Format("Transferring {0}", pair.Key));
                        if (statemachine.functions.global.ContainsKey(pair.Key)) throw new KeplerError(KeplerErrorCode.DECLARE_DUP, new string[] { pair.Key });
                        statemachine.functions.global.Add(pair.Key, pair.Value);
                    }

                    statemachine.has_linked_file = false; // reset to false
                }

                internal_int(line);
            }
            catch (KeplerError e)
            {

                int start_offset = e.GetTokenOffset();

                this.tracer.PushStack(String.Format("at ({0}:{1}:{2})", this.filename, line.line, line.CurrentToken().start + 1));
                throw new KeplerException(this.c_line, e.GetErrorString(), this.tracer, start_offset);
            }
            catch (KeplerException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                if (verbose_debug) this.DUMP();
                else Console.WriteLine("");

                // Console.WriteLine(e);

                // this.tracer.PopStack();
                this.tracer.PushStack(String.Format("at ({0}:{1}:{2})", this.filename, line.line, line.CurrentToken().start + 1));
                throw new KeplerException(this.c_line, "Exception: " + e.Message, this.tracer);
            }
        }

        public void DUMP()
        {
            Console.WriteLine("");
            Console.WriteLine(this.statemachine.functions);
            Console.WriteLine(this.statemachine.variables);

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
            if (this.killed) return;

            if (verbose_debug && this.is_global) Console.WriteLine("Registered Interrupts: " + this.interrupts.Count);
            statemachine.end_on_eop = statemachine.end_on_eop && this.interrupts.Count == 0;

            c_line.m_num = 0;
            c_line = line;
            bool assigned_level_one_state = false;

            if (inside_interrupt || inside_loop)
            {
                c_line.m_num = 0;

                if (line.tokens[0].type == TokenType.EOP) throw new KeplerError(KeplerErrorCode.UNEXP_EOP);

                if (verbose_debug) Console.WriteLine("ADDING TO INTERRUPT -> " + c_line.GetString());
                c_interrupt.function.lines.Add(c_line);

                if (inside_loop && line.CurrentToken().type == TokenType.EndLoop && line.Peek().token_string == "forever")
                {
                    c_interrupt.function.lines.RemoveAt(c_interrupt.function.lines.Count - 1);
                    c_interrupt.SetForever();
                    inside_loop = false;
                    c_state.booleans["inide_loop"] = false;

                    if (verbose_debug) Console.WriteLine(string.Format("EXIT LOOP <{0}>", c_interrupt.id));

                    this.HandleInterrupts(true);
                }
                if (inside_interrupt && line.CurrentToken().type == TokenType.EndInterval && line.Peek().token_string == "every")
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
                if (line.CurrentToken().type == TokenType.EOP) throw new KeplerError(KeplerErrorCode.UNEXP_EOP);

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

                if (line.CurrentToken().type == TokenType.EOP) throw new KeplerError(KeplerErrorCode.UNEXP_EOP);

                c_conditional.lines.Add(c_line);

                if (c_line.CurrentToken().type == TokenType.EndConditional && line.indentation == conditional_indentation)
                {
                    // int stack_id = this.tracer.PushStack(String.Format("at conditional ({0}:{1}:{2})", this.filename, c_line.line - c_conditional.lines.Count, c_line.CurrentToken().start + 1));

                    inside_conditional = false;

                    Interpreter conditional_int = new Interpreter(this.global, this);

                    conditional_int.statemachine.is_interrupt = this.statemachine.is_interrupt;
                    conditional_int.statemachine.interrupt_id = this.statemachine.interrupt_id;


                    conditional_int.verbose_debug = verbose_debug;
                    conditional_int.tracer = this.tracer;
                    conditional_int.filename = this.filename;

                    conditional_int.statemachine.variables = statemachine.variables.Copy();
                    conditional_int.statemachine.functions = statemachine.functions.Copy();

                    for (int i = 0; i < c_conditional.lines.Count; i++)
                        conditional_int.Interpret(c_conditional.lines[i]);

                    // this.tracer.PopStack(stack_id);
                }

                return;
            }

            if (verbose_debug)
            {
                Console.WriteLine("\r\nINTERP. " + c_line.line + " " + c_line.GetString());

                foreach (Token t in c_line.tokens)
                {
                    Console.Write(string.Format("{0} ({1}) ", t.token_string, t.type));
                }

                Console.Write("\r\n");
            }
            while (line.HasNext())
            {
                c_token = line.CurrentToken();

                if (this.killed) break;
                if (c_token.type == TokenType.EOL) break;

                if (!assigned_level_one_state)
                {
                    c_state = statemachine.GetLevelOneToken(c_token);
                    if (verbose_debug) Console.WriteLine("START " + c_token);
                    assigned_level_one_state = true;
                }
                else if (verbose_debug) Console.WriteLine("TOKEN " + c_token);

                if (c_token.type != TokenType.EOL && c_state.type == TokenType.EOL && c_line.Peek().type != TokenType.EOL) throw new KeplerError(KeplerErrorCode.UNEXP_EOL);
                if (c_state.type != c_token.type) throw new KeplerError(KeplerErrorCode.UNEXP_TOKEN, new string[] { c_token.token_string });

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
                    KeplerInterrupt interrupt = new KeplerInterrupt(int_id, new KeplerFunction("interval_" + int_id), this);

                    c_state.c_interrupt = interrupt;
                    this.interrupts.Add(interrupt);

                    inside_interrupt = true;
                    c_interrupt = c_state.c_interrupt;

                    if (verbose_debug) Console.WriteLine(string.Format("ENTER <{0}>", c_interrupt.id));
                }

                if (c_state.booleans["inside_loop"] && c_token.token_string == "start")
                {
                    int int_id = this.interrupts.Count;
                    KeplerInterrupt interrupt = new KeplerInterrupt(int_id, new KeplerFunction("loop_" + int_id), this);

                    c_state.c_interrupt = interrupt;
                    this.interrupts.Add(interrupt);

                    inside_loop = true;
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

        public void Kill()
        {
            if (verbose_debug) Console.WriteLine("INTERRUPT " + this.ID + " KILLED ON LINE" + this.c_line.line);
            this.killed = true;
            this.c_line.Kill();
        }

        public void HandleInterrupts(bool only_infinite)
        {
            // do interrupts
            List<KeplerInterrupt> interrupts = this.interrupts.GetInterrupts();

            for (int i = 0; i < interrupts.Count; ++i)
            {
                KeplerInterrupt interrupt = interrupts[i];
                if (!interrupt.isInfinite() && only_infinite) continue;

                if (verbose_debug) Console.WriteLine("DOING INTERRUPT " + interrupt.id);

                int stack_id = this.tracer.PushStack(String.Format("at {0} (#{1}) ({2}:{3}:{4})", interrupt.isInfinite() ? "forever" : "interval", interrupt.id, this.filename, this.c_line.line, this.c_line.CurrentToken().start));

                KeplerFunction int_function = interrupt.function;

                int_function.ResetLines();

                // maybe move this to when the interrupt is created?
                // seems like creating a new interpreter could cause a bit of delay
                Interpreter f_interpreter = new Interpreter(this.global, this);

                f_interpreter.statemachine.variables = statemachine.variables.Copy();
                f_interpreter.statemachine.functions = statemachine.functions.Copy();

                f_interpreter.statemachine.is_interrupt = true;
                f_interpreter.statemachine.interrupt_id = interrupts[i].id;

                f_interpreter.verbose_debug = this.verbose_debug;
                f_interpreter.tracer = this.tracer;
                f_interpreter.filename = this.filename;

                // do interpretation
                foreach (LineIterator line in int_function.lines)
                {
                    if (this.interrupts.HasInterrupt(interrupts[i].id))
                        f_interpreter.Interpret(line);
                    else break;
                }

                interrupts[i].Reset();

                this.tracer.PopStack(stack_id);
            }
        }
    }
}