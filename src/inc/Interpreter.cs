using Kepler.Exceptions;
using Kepler.Lexer;
using Kepler.Lexer.Tokens;
using Kepler.LogicControl;
using Kepler.Tracing;
using KeplerVariables;
using System;
using System.Collections.Generic;

namespace Kepler.Interpreting
{
    public class Interpreter
    {
        public int ID = 0;
        public string filename = "[LIVE]";
        public KeplerErrorStack tracer;
        public StateMachine statemachine = new StateMachine();
        public bool verbose_debug = false;
        public bool debug = false;
        public bool has_parent = false;
        public bool is_function = false;
        public Interpreter parent;
        public bool is_global = false;
        public Interpreter global;
        public TokenState c_state = null;
        public Token c_token = null;
        bool inside_function = false;
        bool inside_conditional = false, inside_conditional_else = false, inside_conditional_elseif = false;
        bool inside_interrupt = false;
        bool inside_loop = false;
        // bool has_return_value = false;
        int desired_intendation = 0;
        bool skip_conditional = false;
        public bool killed = false;
        public KeplerInterruptManager interrupts = new KeplerInterruptManager();
        public KeplerFunction c_function = null;
        // new KeplerFunction("NUL")
        public KeplerInterrupt c_interrupt = null;
        // new KeplerInterrupt(-1, new KeplerFunction("NUL"), null)
        // public KeplerFunction c_conditional = new KeplerFunction("CONDITIONAL");
        public KeplerConditional c_conditional = new KeplerConditional();
        // 
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
                this.interrupts.parent = parent.interrupts;
                this.interrupts.global = this.global.interrupts;
                // this.parent
                // this.interrupts = this.global.interrupts;
            }

            if (parent == null) this.has_parent = false;
            else
            {
                this.ID = parent.ID + 1;
                this.has_parent = true;
                this.parent = parent;
            }

            this.statemachine.interpreter = this;
            if (c_interrupt != null)
                c_interrupt.parent = this;
        }

        public TokenState Interpret(LineIterator line)
        {
            statemachine.verbose_debug = verbose_debug;
            // statemachine.debug = debug;

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

                if (debug || verbose_debug) PrintFormattedLine(line);

                return internal_int(line);
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

                Console.WriteLine(e);

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

        TokenState internal_int(LineIterator line)
        {
            if (this.killed) return c_state;

            if (verbose_debug)
            {
                Console.WriteLine(string.Format("\r\nINTERPRETER ID: {0}", this.ID));
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("Registered Interrupts: " + this.interrupts.Count);
                Console.ResetColor();
            }
            statemachine.end_on_eop = statemachine.end_on_eop && this.interrupts.Count == 0;

            c_line.m_num = 0;
            c_line = line;
            bool assigned_level_one_state = false;

            if (c_state != null)
            {
                c_state.booleans["inside_conditional"] = inside_conditional || inside_conditional_else;
                c_state.booleans["inside_function"] = inside_function;
                c_state.booleans["inside_interval"] = inside_interrupt;
                c_state.booleans["inside_loop"] = inside_loop;

                if (verbose_debug) foreach (KeyValuePair<string, bool> p in c_state.booleans)
                        Console.WriteLine(String.Format("{0} -> {1}", p.Key, p.Value));
            }

            if (inside_interrupt || inside_loop)
            {
                c_line.m_num = 0;

                if (line.tokens[0].type == TokenType.EOP) throw new KeplerError(KeplerErrorCode.UNEXP_EOP);

                if (debug) Console.WriteLine("ADDING TO INTERRUPT -> " + c_line.GetString());
                c_interrupt.function.lines.Add(c_line);

                if (inside_loop && line.CurrentToken().type == TokenType.EndLoop && line.Peek().token_string == "forever" && line.indentation == desired_intendation)
                {
                    c_interrupt.function.lines.RemoveAt(c_interrupt.function.lines.Count - 1);
                    c_interrupt.SetForever();
                    c_interrupt.Enable();
                    inside_loop = false;
                    c_state.booleans["inside_loop"] = false;

                    if (debug) Console.WriteLine(string.Format("EXIT LOOP <{0}>", c_interrupt.id));

                    this.HandleInterrupts();
                }
                else if (inside_interrupt && line.CurrentToken().type == TokenType.EndInterval && line.Peek().token_string == "every" && line.indentation == desired_intendation)
                {
                    c_interrupt.function.lines.RemoveAt(c_interrupt.function.lines.Count - 1);
                    inside_interrupt = false;
                    c_state.booleans["inside_interval"] = false;

                    if (debug) Console.WriteLine(string.Format("EXIT <{0}>", c_interrupt.id));

                    c_interrupt.Enable();
                    this.HandleInterrupts();
                }

                return c_state;
            }

            if (inside_function)
            {

                c_line.m_num = 0;
                if (line.CurrentToken().type == TokenType.EOP) throw new KeplerError(KeplerErrorCode.UNEXP_EOP);

                c_function.lines.Add(c_line);
                c_state.booleans["inside_function"] = true;

                if (line.CurrentToken().type == TokenType.EndFunction && line.Peek().token_string == c_function.name && line.indentation == desired_intendation)
                {
                    inside_function = false;
                    c_function.lines.RemoveAt(c_function.lines.Count - 1);

                    if (verbose_debug) Console.WriteLine(string.Format("EXIT <{0}>", c_function.name));
                }

                return c_state;
            }

            if (skip_conditional)
            {
                // check for "endif"
                if (c_line.CurrentToken().type == TokenType.EndConditional && c_line.indentation == desired_intendation) skip_conditional = false;

                return c_state;
            }

            if (inside_conditional || inside_conditional_else)
            {
                c_line.m_num = 0;

                if (line.CurrentToken().type == TokenType.EOP) throw new KeplerError(KeplerErrorCode.UNEXP_EOP);
                if (line.CurrentToken().type == TokenType.ConditionalElse && c_line.indentation == desired_intendation)
                {
                    if (line.tokens.Count > 1 && line.tokens[1].type != TokenType.EOL) throw new KeplerException(c_line, new KeplerError(KeplerErrorCode.UNEXP_TOKEN, new string[] { line.tokens[1].type.ToString() }).GetErrorString(), this.tracer, 1);
                    inside_conditional_else = true;
                }
                if (line.CurrentToken().type == TokenType.ConditionalElseIf && c_line.indentation == desired_intendation)
                {
                    if (line.tokens.Count < 2) throw new KeplerException(c_line, new KeplerError(KeplerErrorCode.UNEXP_EOL).GetErrorString(), this.tracer, 2);
                    if (line.tokens[1].type != TokenType.GenericOperation) throw new KeplerException(c_line, new KeplerError(KeplerErrorCode.UNEXP_TOKEN, new string[] { line.tokens[1].type.ToString() }).GetErrorString(), this.tracer, 1);
                    c_conditional.elseifs.Add(new KeplerFunction(c_line.line.ToString()));
                    inside_conditional_elseif = true;
                }

                if (inside_conditional_else)
                    c_conditional.else_function.lines.Add(c_line);
                else if (inside_conditional_elseif)
                    c_conditional.elseifs[c_conditional.elseifs.Count - 1].lines.Add(c_line);
                else
                    c_conditional.main_function.lines.Add(c_line);

                if (c_line.CurrentToken().type == TokenType.EndConditional && line.indentation == desired_intendation)
                {
                    // int stack_id = this.tracer.PushStack(String.Format("at conditional ({0}:{1}:{2})", this.filename, c_line.line - c_conditional.lines.Count, c_line.CurrentToken().start + 1));

                    Interpreter conditional_int = new Interpreter(this.global, this);

                    conditional_int.statemachine.is_interrupt = this.statemachine.is_interrupt;
                    conditional_int.statemachine.interrupt_id = this.statemachine.interrupt_id;

                    if (this.is_function)
                    {
                        conditional_int.statemachine.function_type = this.statemachine.function_type;
                        conditional_int.statemachine.function_id = this.statemachine.function_id;
                        conditional_int.is_function = true;
                    }

                    conditional_int.verbose_debug = verbose_debug;
                    conditional_int.tracer = this.tracer;
                    conditional_int.filename = this.filename;

                    conditional_int.statemachine.variables = statemachine.variables.Copy();
                    conditional_int.statemachine.functions = statemachine.functions.Copy();

                    if (inside_conditional_else)
                        // if we're still in an "else" statement, remove the "endif" in the else statement
                        c_conditional.else_function.lines.RemoveAt(c_conditional.else_function.lines.Count - 1);
                    else if (inside_conditional_elseif)
                        // if we're in an elseif remove the "endif" in the last elseif
                        c_conditional.elseifs[c_conditional.elseifs.Count - 1].lines.RemoveAt(c_conditional.elseifs[c_conditional.elseifs.Count - 1].lines.Count - 1);
                    else
                        // otherwise remove the "endif" in the main conditional
                        c_conditional.main_function.lines.RemoveAt(c_conditional.main_function.lines.Count - 1);

                    bool do_main = this.statemachine.DoGenericOperation(c_conditional.main_function.lines[0].tokens[1]).BoolValue;
                    bool check_elseifs = c_conditional.elseifs.Count > 0;
                    bool do_else = (!do_main && c_conditional.else_function.lines.Count > 1);

                    if (do_main)
                    {
                        c_conditional.main_function.Reset();
                        for (int i = 1; i < c_conditional.main_function.lines.Count; i++)
                        {
                            conditional_int.Interpret(c_conditional.main_function.lines[i]);
                        }
                    }
                    else
                    {
                        if (check_elseifs)
                        {
                            for (int i = 0; i < c_conditional.elseifs.Count; i++)
                            {
                                bool do_elseif = this.statemachine.DoGenericOperation(c_conditional.elseifs[i].lines[0].tokens[1]).BoolValue;
                                if (do_elseif)
                                {
                                    c_conditional.elseifs[i].Reset();
                                    for (int j = 1; j < c_conditional.elseifs[i].lines.Count; j++)
                                    {
                                        conditional_int.Interpret(c_conditional.elseifs[i].lines[j]);
                                    }

                                    do_else = false;
                                    break;
                                }
                            }
                        }
                        if (do_else)
                        {
                            c_conditional.else_function.Reset();
                            for (int i = 1; i < c_conditional.else_function.lines.Count; i++)
                                conditional_int.Interpret(c_conditional.else_function.lines[i]);
                        }
                    }

                    inside_conditional = false;
                    inside_conditional_else = false;
                    inside_conditional_elseif = false;
                }

                return c_state;
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
                    desired_intendation = c_line.indentation;

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
                    desired_intendation = c_line.indentation;

                    c_interrupt.Disable();

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
                    desired_intendation = c_line.indentation;

                    if (verbose_debug) Console.WriteLine(string.Format("ENTER <{0}>", c_interrupt.id));
                }

                if (c_state.type == TokenType.EOL)
                {
                    if (c_state.booleans["throw_error"]) throw new KeplerError(KeplerErrorCode.GENERIC_ERROR, new string[] { c_state.strings["error_string"] });
                    if (c_state.booleans["console_print"]) Console.WriteLine(c_state.strings["print_string"]);
                    if (c_state.booleans["inside_conditional"])
                    {

                        this.c_conditional.main_function = new KeplerFunction("conditional_main");
                        this.c_conditional.else_function = new KeplerFunction("conditional_else");
                        this.c_conditional.elseifs.Clear();

                        inside_conditional = true;
                        inside_conditional_else = false;

                        this.c_conditional.main_function.lines.Add(c_line);

                        desired_intendation = c_line.indentation;
                    }
                    if (c_state.booleans["validate_conditional"] && !c_state.booleans["inside_conditional"])
                    {
                        desired_intendation = c_line.indentation;
                        skip_conditional = true;
                    }

                    break;
                }

                line++;
            }

            return c_state;
        }

        public void Kill()
        {
            if (verbose_debug) Console.WriteLine("INTERRUPT " + this.ID + " KILLED ON LINE" + this.c_line.line);

            // hmm...
            if (this.is_global)
                throw new KeplerError(KeplerErrorCode.GENERIC_ERROR, new string[] { "Attempted to kill global!" });

            this.killed = true;
            this.c_line.Kill();
        }

        public void HandleInterrupts(bool only_infinite = false)
        {

            // do interrupts
            while (this.interrupts.HasInterrupts())
            {
                List<KeplerInterrupt> interrupts = this.interrupts.GetInterrupts();
                for (int i = 0; i < interrupts.Count; ++i)
                {
                    KeplerInterrupt interrupt = interrupts[i];
                    if (!interrupt.isInfinite() && only_infinite) continue;

                    if (verbose_debug) Console.WriteLine("DOING INTERRUPT " + interrupt.id);

                    string stack_id = this.tracer.PushStack(String.Format("at {0} (#{1}) ({2}:{3}:{4})", interrupt.isInfinite() ? "forever" : "interval", interrupt.id, this.filename, this.c_line.line, this.c_line.CurrentToken().start));

                    KeplerFunction int_function = interrupt.function;

                    // int_function.ResetLines();
                    int_function.Reset();

                    // maybe move this to when the interrupt is created?
                    // seems like creating a new interpreter could cause a bit of delay
                    Interpreter f_interpreter = new Interpreter(this.global, interrupt.parent);

                    f_interpreter.statemachine.variables = interrupt.parent.statemachine.variables.Copy();
                    f_interpreter.statemachine.functions = interrupt.parent.statemachine.functions.Copy();

                    f_interpreter.statemachine.is_interrupt = true;
                    f_interpreter.statemachine.interrupt_id = interrupts[i].id;

                    f_interpreter.verbose_debug = this.verbose_debug;
                    f_interpreter.debug = this.debug;
                    f_interpreter.tracer = this.tracer;
                    f_interpreter.filename = this.filename;

                    // Console.WriteLine("START INTERP. INTERRUPT");
                    // do interpretation
                    for (int l = 0; l < int_function.lines.Count; ++l)
                    {
                        LineIterator line = int_function.lines[l];

                        if (this.interrupts.HasInterrupt(interrupts[i].id))
                            f_interpreter.Interpret(line);
                        else break;
                    }

                    interrupts[i].Reset();

                    this.tracer.PopStack(stack_id);
                }

            }
        }

        static void PrintFormattedLine(LineIterator line)
        {
            if (line == null) return;

            // int indentation = 0;
            for (int i = 0; i < line.tokens.Count; ++i)
            {
                Token t = line.tokens[i];

                PrintToken(t, i);
            }

            Console.WriteLine("");
        }

        static void PrintToken(Token token, int indentation = 0, string prefix = "")
        {
            string pad_string = "";

            while (pad_string.Length < indentation * 2) pad_string = pad_string + " ";

            string print_string = String.Format("{0}{1} ({2})", prefix, (string)(token.a == null ? token.token_string : token.operation.ToString()), token.type);
            Console.WriteLine(pad_string + print_string);

            // a is only assigned if b is assigned, so we only need to check one of them
            if (token.a != null)
            {
                PrintToken(token.a, indentation + 1, " A: ");
                if (token.b != null) PrintToken(token.b, indentation + 1, " B: ");
            }
        }
    }
}