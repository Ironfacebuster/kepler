using System;
using System.Collections.Generic;
using KeplerTokenizer;
using KeplerTokens.Tokens;
using KeplerVariables;
using KeplerInterpreter;

namespace StateMachine
{
    public class TokenStateLevels
    {
        public bool verbose_debug = false;
        public bool linked_file = false;
        public KeplerVariableManager variables = new KeplerVariableManager();
        public KeplerFunctionManager functions = new KeplerFunctionManager();

        // every line starts with a level 1 token
        public List<TokenState> level1 = new List<TokenState>();
        bool schedule_execute_function = false;

        public bool has_linked_file;
        public KeplerVariableManager linked_variables;
        public KeplerFunctionManager linked_functions;
        public KeplerVariable return_value = new KeplerVariable();
        // bool has_return_value = false;

        bool inside_header = false;
        KeplerFunction scheduled_function = new KeplerFunction("NUL");
        // level 1 tokens
        // EOP - EOP
        // if - ConditionalIf
        // else if - ConditionalElseIf
        // else - ConditionalElse
        // variable - DeclareVariable

        public TokenStateLevels()
        {
            // final states
            TokenState EOL = new TokenState(TokenType.EOL, HandleEOL);
            TokenState EOP = new TokenState(TokenType.EOP, HandleEOP);

            // level 3 states
            TokenState StringText = new TokenState(TokenType.StringText, HandleStringText);
            TokenState StaticModifier = new TokenState(TokenType.StaticModifier, HandleStaticModifier);
            StaticModifier.booleans["declared_variable"] = true;
            StaticModifier.booleans["variable_assign"] = true;

            // level 2 states
            TokenState DeclareHeader = new TokenState(TokenType.DeclareHeader, new TokenState[] { EOL }, NullHandle);
            TokenState DeclareFunction = new TokenState(TokenType.DeclareFunction, new TokenState[] { EOL }, HandleDeclareFunction);
            TokenState DeclareVariable = new TokenState(TokenType.DeclareVariable, HandleDeclareVariable);
            TokenState AssignFunctionType = new TokenState(TokenType.AssignFunctionType, HandleAssignFunctionType);

            // level n states
            TokenState GenericAssign = new TokenState(TokenType.GenericAssign, HandleGenericAssign);
            TokenState BooleanOperator = new TokenState(TokenType.BooleanOperator, NullHandle);
            TokenState EnterDoubleQuote = new TokenState(TokenType.DoubleQuote, HandleEnterDoubleQuote);
            TokenState ExitDoubleQuote = new TokenState(TokenType.DoubleQuote, HandleExitDoubleQuote);
            TokenState StaticVariableType = new TokenState(TokenType.StaticVariableType, HandleStaticVariableType);    // variable static type assignment
            TokenState StaticFunctionType = new TokenState(TokenType.StaticVariableType, HandleStaticVariableType);    // variable static type assignment
            TokenState GenericAdd = new TokenState(TokenType.GenericAdd, HandleGenericAdd);

            // static values
            TokenState StaticFloat = new TokenState(TokenType.StaticFloat, new TokenState[] { BooleanOperator, EOL }, HandleStaticFloat);
            TokenState StaticInt = new TokenState(TokenType.StaticInt, new TokenState[] { BooleanOperator, EOL }, HandleStaticInt);
            // TokenState StaticUnsignedFloat = new TokenState(TokenType.StaticUnsignedFloat, new TokenState[] { BooleanOperator, EOL }, HandleStaticUnsignedFloat);
            TokenState StaticUnsignedInt = new TokenState(TokenType.StaticUnsignedInt, new TokenState[] { BooleanOperator, EOL }, HandleStaticUnsignedInt);
            TokenState StaticBool = new TokenState(TokenType.StaticBoolean, new TokenState[] { BooleanOperator, EOL }, HandleStaticBool);

            // Function,
            // List,
            // Array,
            // Boolean,
            // Unassigned

            // set up level 1 token states
            level1.Add(new TokenState(TokenType.ConditionalIf, HandleConditionalIf));
            level1.Add(new TokenState(TokenType.ConditionalElseIf, HandleConditionalElseIf));
            level1.Add(new TokenState(TokenType.ConditionalElse, HandleConditionalElse));
            level1.Add(new TokenState(TokenType.FunctionReturn, new TokenState[] { DeclareFunction, DeclareVariable }, NullHandle)); // TODO: return values!

            TokenState StartCallFunction = new TokenState(TokenType.CallFunction, new TokenState[] { DeclareFunction }, HandleStartCallFunction);
            TokenState AssignVariable = new TokenState(TokenType.DeclareVariable, new TokenState[] { GenericAssign }, HandleDeclareVariableAndAssign);    // level 1 declare variable is always assigning a value/creating a variable
            TokenState AssignFunction = new TokenState(TokenType.DeclareFunction, HandleDeclareFunctionAndAssign);    // level 1 declare function is always assigning a static type
            TokenState ConsolePrint = new TokenState(TokenType.ConsolePrint, HandleConsolePrint);

            TokenState GenericOperation = new TokenState(TokenType.GenericOperation, HandleGenericOperation);
            GenericOperation.child_states = new TokenState[] { GenericOperation, EOL };

            // function argument stuff
            TokenState AssignNonPositionalArgument = new TokenState(TokenType.AssignNonPositionalArgument, HandleAssignNonPositionalArgument);
            TokenState PositionalArgumentAssignment = new TokenState(TokenType.PositionalArgumentAssignment, HandlePositionalArgumentAssignment);

            // linking stuff
            TokenState LinkFile = new TokenState(TokenType.LinkFile, new TokenState[] { EnterDoubleQuote }, HandleLinkFile);

            level1.Add(new TokenState(TokenType.StartHeader, new TokenState[] { DeclareHeader }, HandleStartHeader));    // start header token
            level1.Add(new TokenState(TokenType.EndHeader, new TokenState[] { DeclareHeader }, HandleEndHeader));        // end header token
            level1.Add(LinkFile);                                                                                        // link file

            level1.Add(new TokenState(TokenType.StartFunction, new TokenState[] { DeclareFunction }, HandleStartFunction));     // start function token
            level1.Add(new TokenState(TokenType.EndFunction, new TokenState[] { DeclareFunction }, HandleEndFunction));       // end function token

            // ASSIGN RECURSIVE CHILD STATES
            GenericAssign.child_states = new TokenState[] { StartCallFunction, DeclareVariable, StaticFloat, StaticInt, StaticUnsignedInt, StaticBool, StaticModifier, StaticVariableType, StaticFunctionType, EnterDoubleQuote, GenericOperation };
            GenericAdd.child_states = new TokenState[] { StartCallFunction, DeclareVariable, StaticFloat, StaticInt, StaticUnsignedInt, StaticBool, StaticModifier, StaticVariableType, StaticFunctionType, EnterDoubleQuote };
            AssignFunction.child_states = new TokenState[] { AssignFunctionType, AssignNonPositionalArgument };
            AssignFunctionType.child_states = new TokenState[] { StaticVariableType };
            ConsolePrint.child_states = new TokenState[] { EnterDoubleQuote, DeclareVariable };

            DeclareVariable.child_states = new TokenState[] { PositionalArgumentAssignment, GenericAdd, EOL };
            PositionalArgumentAssignment.child_states = new TokenState[] { StaticVariableType };
            AssignNonPositionalArgument.child_states = new TokenState[] { DeclareVariable };

            StaticVariableType.child_states = new TokenState[] { BooleanOperator, EOL };
            StaticFunctionType.child_states = new TokenState[] { BooleanOperator, EOL };
            StaticModifier.child_states = new TokenState[] { BooleanOperator, /*StaticVariableType,*/ EOL };

            // string handling
            EnterDoubleQuote.child_states = new TokenState[] { StringText, ExitDoubleQuote };  // String
            StringText.child_states = new TokenState[] { StringText, ExitDoubleQuote };        // String
            ExitDoubleQuote.child_states = new TokenState[] { BooleanOperator, EOL };          // String

            // handle "and is" & assigning arguments
            BooleanOperator.child_states = new TokenState[] { GenericAssign, DeclareVariable };

            level1.Add(AssignVariable);
            level1.Add(AssignFunction);
            level1.Add(StartCallFunction);
            level1.Add(ConsolePrint);

            EOP.child_states = new TokenState[] { EOP, EOL };

            level1.Add(EOP); // END OF PROGRAM token
        }

        public TokenState GetLevelOneToken(Token token)
        {
            foreach (TokenState state in level1)
            {
                if (state.type == token.type) return state;
            }

            throw new LevelOneException(string.Format("[141] Unexpected initial token \"{0}\"", token.token_string));
        }
        void NullHandle(Token token, TokenState state)
        {
            // do nothing...
        }
        void HandleGenericAdd(Token token, TokenState state)
        {
            if (state.booleans["declared_variable"])
            {
                state.a_operand = state.c_variable.Clone(); // create a cloned variable (no direct reference)
                state.booleans["assigned_a_operand"] = true;
            }
        }
        void HandleConsolePrint(Token token, TokenState state)
        {
            state.strings["print_string"] = ""; // clear print_string
            state.booleans["console_print"] = true;
        }
        void HandleGenericOperation(Token token, TokenState state)
        {
            if (verbose_debug)
            {
                Console.WriteLine("Doing generic operation");
                Console.WriteLine(token.a);
                Console.WriteLine(token.b);
            }

            KeplerVariable result = DoGenericOperation(token);

            state.left_side_operator.AssignValue(result);
        }

        KeplerVariable DoGenericOperation(Token token)
        {
            if (verbose_debug)
            {
                Console.WriteLine(String.Format("Doing Generic Operation \"{0}\"", token.operation));
            }
            KeplerVariable result = new KeplerVariable();

            KeplerVariable a_operand = CreateTemporaryVariable(token.a);
            KeplerVariable b_operand = CreateTemporaryVariable(token.b);

            if (verbose_debug)
            {
                Console.WriteLine(a_operand);
                Console.WriteLine(b_operand);
            }

            if (a_operand.type != b_operand.type) throw new InterpreterException(string.Format("Cannot \"{0}\" mismatched types! ({1} and {2})", token.operation, a_operand.type, b_operand.type));

            switch (token.operation)
            {
                case KeplerTokens.DataTypes.OperationType.Add:
                    switch (a_operand.type)
                    {
                        case KeplerType.Float:
                            result.SetFloatValue(a_operand.FloatValue + b_operand.FloatValue);
                            break;
                        case KeplerType.Int:
                            result.SetIntValue(a_operand.IntValue + b_operand.IntValue);
                            break;
                        case KeplerType.uInt:
                            result.SetUnsignedIntValue(a_operand.uIntValue + b_operand.uIntValue);
                            break;
                            // TODO: NEW StringText!!!
                            // case TokenType.StringText:
                            //     state.left_side_operator.SetStringValue(a_operand.StringValue + b_operand.StringValue);
                            //     break;
                    }
                    break;
                case KeplerTokens.DataTypes.OperationType.Subtract:
                    switch (a_operand.type)
                    {
                        case KeplerType.Float:
                            result.SetFloatValue(a_operand.FloatValue - b_operand.FloatValue);
                            break;
                        case KeplerType.Int:
                            result.SetIntValue(a_operand.IntValue - b_operand.IntValue);
                            break;
                        case KeplerType.uInt:
                            result.SetUnsignedIntValue(a_operand.uIntValue - b_operand.uIntValue);
                            break;
                            // TODO: NEW StringText!!!
                            // case TokenType.StringText:
                            //     state.left_side_operator.SetStringValue(a_operand.StringValue + b_operand.StringValue);
                            //     break;
                    }
                    break;
            }

            return result;
        }
        void HandleLinkFile(Token token, TokenState state)
        {
            if (!inside_header) throw new InterpreterException("Cannot link file outside of Header");
            state.booleans["link_file"] = true;
        }
        void HandleStartFunction(Token token, TokenState state)
        {
            state.booleans["inside_function"] = true;
        }
        void HandleEndFunction(Token token, TokenState state)
        {
            state.booleans["inside_function"] = false;
        }
        void HandleStartCallFunction(Token token, TokenState state)
        {
            state.booleans["calling_function"] = true;
        }
        void HandleAssignNonPositionalArgument(Token token, TokenState state)
        {
            state.booleans["assigning_function_variables"] = true;
        }
        void HandlePositionalArgumentAssignment(Token token, TokenState state)
        {
            state.booleans["assigning_function_variables_type"] = true;
        }
        void HandleGenericAssign(Token token, TokenState state)
        {

            if (state.booleans["declared_variable"])
            {
                state.booleans["variable_assign"] = true;
                state.booleans["declared_variable"] = false;
                // state.booleans["has_left_side_operator"] = true;
                state.left_side_operator = state.c_variable;
            }
            else if (state.booleans["declared_function"])
            {
                state.booleans["function_assign"] = true;
            }
        }
        void HandleStaticFloat(Token token, TokenState state)
        {
            if (state.booleans["variable_assign"])
                state.left_side_operator.SetFloatValue(double.Parse(token.token_string));
        }
        void HandleStaticInt(Token token, TokenState state)
        {
            if (state.booleans["variable_assign"])
                state.left_side_operator.SetIntValue(int.Parse(token.token_string));
        }
        void HandleStaticUnsignedInt(Token token, TokenState state)
        {
            if (state.booleans["variable_assign"])
                state.left_side_operator.SetUnsignedIntValue(uint.Parse(token.token_string.Substring(1)));
        }
        void HandleStaticBool(Token token, TokenState state)
        {
            if (state.booleans["variable_assign"])
                state.left_side_operator.SetBoolValue(bool.Parse(token.token_string));
        }
        void HandleStaticModifier(Token token, TokenState state)
        {
            Enum.TryParse(token.token_string, out KeplerModifier m_type);
            state.left_side_operator.SetModifier(m_type);
        }
        void HandleEnterDoubleQuote(Token token, TokenState state)
        {
            state.booleans["inside_string"] = true;
            state.strings["build_string"] = "";
        }
        void HandleExitDoubleQuote(Token token, TokenState state)
        {
            state.booleans["inside_string"] = false;

            if (state.booleans["console_print"])
            {

                state.strings["print_string"] = state.strings["print_string"] + state.strings["build_string"];

            }
            else if (inside_header && state.booleans["link_file"])
            {
                if (verbose_debug) Console.WriteLine(string.Format("LINKING \"{0}\"", state.strings["build_string"]));

                // load file and interpret
                Tokenizer m_tokenizer = new Tokenizer();
                m_tokenizer.Load(state.strings["build_string"]);

                Interpreter m_interpreter = new Interpreter();
                m_interpreter.verbose_debug = verbose_debug;
                m_interpreter.levels.linked_file = true;

                // do interpretation
                while (m_tokenizer.HasNext())
                {
                    m_interpreter.Interpret(m_tokenizer.CurrentLine());

                    m_tokenizer++;
                }

                // transfer all global variables and functions
                linked_variables = m_interpreter.levels.variables;
                linked_functions = m_interpreter.levels.functions;

                has_linked_file = true;

            }
            else state.c_variable.SetStringValue(state.strings["build_string"]);
        }
        void HandleStringText(Token token, TokenState state)
        {
            state.strings["build_string"] = state.strings["build_string"].Length == 0 ? token.token_string : state.strings["build_string"] + " " + token.token_string;
            state.booleans["inside_string"] = true;
        }
        void HandleStaticVariableType(Token token, TokenState state)
        {
            Enum.TryParse(token.token_string, out KeplerType m_type);
            if (state.booleans["assigning_function_variables_type"])
            {
                state.c_function.AssignNonPositional(state.strings["nonpositional_variable_name"], m_type);
            }
            else if (state.booleans["declared_variable"] && state.booleans["variable_assign"])
            {
                state.c_variable.SetType(m_type);
            }
            else if (state.booleans["declared_function"] && state.booleans["function_assign"])
            {
                state.c_function.SetType(m_type);
            }
        }
        void HandleEOL(Token token, TokenState state)
        {
            if (schedule_execute_function)
            {
                ExecuteFunction(scheduled_function);
                schedule_execute_function = false;
            }
        }
        void HandleEOP(Token token, TokenState state)
        {
            if (verbose_debug) Console.WriteLine("EOP!");

            if (!linked_file) Environment.Exit(0); // exit with code 0 if NOT a linked file
        }
        void HandleConditionalIf(Token token, TokenState state)
        {
            state.booleans["inside_if_statement"] = true;
        }
        void HandleConditionalElseIf(Token token, TokenState state)
        {
            state.booleans["inside_if_statement"] = true;
        }
        void HandleConditionalElse(Token token, TokenState state)
        {
            state.booleans["inside_if_statement"] = true;
        }
        void HandleDeclareVariable(Token token, TokenState state)
        {
            if (state.booleans["assigned_a_operand"])
            {
                // on the other side of a "GenericOperation"
                // TODO: check operation type after adding other operations
                if (verbose_debug) Console.WriteLine(token.token_string);
                KeplerVariable b_operand = variables.GetVariable(token.token_string);

                if (verbose_debug) Console.WriteLine(state.a_operand);
                if (verbose_debug) Console.WriteLine(b_operand);

                if (state.a_operand.type != b_operand.type) throw new InterpreterException(string.Format("Mismatched types! ({0} and {1})", state.a_operand.type, b_operand.type));
                KeplerVariable a_operand = state.a_operand;
                return_value.SetType(b_operand.type);

                switch (b_operand.type)
                {
                    case KeplerType.Float:
                        state.left_side_operator.SetFloatValue(a_operand.FloatValue + b_operand.FloatValue);
                        break;
                    case KeplerType.Int:
                        state.left_side_operator.SetIntValue(a_operand.IntValue + b_operand.IntValue);
                        break;
                    case KeplerType.uInt:
                        state.left_side_operator.SetUnsignedIntValue(a_operand.uIntValue + b_operand.uIntValue);
                        break;
                    case KeplerType.String:
                        state.left_side_operator.SetStringValue(a_operand.StringValue + b_operand.StringValue);
                        break;
                }

                state.ResetOperands();
                // state.a_operand = b_operand; // shift for chained adds
                // state.booleans["assigned_a_operand"] = true;
            }
            else if (state.booleans["console_print"])
            {
                state.strings["print_string"] = state.strings["print_string"] + variables.GetVariable(token.token_string).GetValueAsString();
            }
            else if (state.booleans["assigning_function_variables"])
            {
                state.c_function.AddNonPositional(token.token_string);
                state.strings["nonpositional_variable_name"] = token.token_string;
            }
            else
            {
                state.c_variable = variables.DeclareVariable(token.token_string);
                state.booleans["declared_variable"] = true;
            }
        }
        void HandleDeclareVariableAndAssign(Token token, TokenState state)
        {
            state.c_variable = variables.DeclareVariable(token.token_string);
            state.booleans["declared_variable"] = true;
            state.booleans["variable_assign"] = true;
        }
        void HandleDeclareFunctionAndAssign(Token token, TokenState state)
        {
            state.c_function = functions.DeclareFunction(token.token_string);
            state.booleans["declared_function"] = true;
        }
        void HandleDeclareFunction(Token token, TokenState state)
        {
            KeplerFunction c_function = functions.GetFunction(token.token_string);
            // TODO: execute function after arguments are assigned
            if (state.booleans["calling_function"])
            {
                if (c_function.type == KeplerType.Unassigned) throw new InterpreterException(string.Format("Cannot call {0} without a defined return type", c_function.name));

                if (state.booleans["declared_variable"])
                {
                    state.c_variable.type = c_function.type;
                    c_function.target = state.c_variable; // assign target (for return values)
                }

                if (c_function.HasArguments())
                {
                    schedule_execute_function = true;
                    scheduled_function = c_function;

                    if (verbose_debug) Console.WriteLine(string.Format("Scheduling call of {0} to EOL", c_function.name));
                }
                else ExecuteFunction(c_function);
                // foreach(KeyValuePair<string,KeplerType> pair in c_function.non_positional_arguments)
                //     Console.WriteLine(string.Format("{0} -> {1}",pair.Key,pair.Value));



                // interpret lines within function
                // interpret until "ReturnFunction" is encountered
            }
            else
            {
                state.c_function = functions.DeclareFunction(token.token_string);
                state.booleans["declared_function"] = true;
            }
        }
        void HandleAssignFunctionType(Token token, TokenState state)
        {
            state.booleans["function_assign"] = true;
        }
        void HandleStartHeader(Token token, TokenState state)
        {
            if (verbose_debug) Console.WriteLine("ENTER HEADER");
            // state.c_function = functions.DeclareFunction(token.token_string);
            inside_header = true;
            // Console.WriteLine(state.booleans["inside_header"]);
        }
        void HandleEndHeader(Token token, TokenState state)
        {
            // state.c_function = functions.DeclareFunction(token.token_string);
            if (verbose_debug) Console.WriteLine("EXIT HEADER");
            inside_header = false;
        }

        void ExecuteFunction(KeplerFunction function)
        {
            if (function.HasTarget() && function.type == KeplerType.Unassigned) throw new InterpreterException(string.Format("Cannot assign to {1} as {0} does not have a defined return type", function.name, function.GetTarget()));

            function.ResetLines(); // reset line token indexes to zero

            Interpreter f_interpreter = new Interpreter();
            f_interpreter.levels.variables = this.variables.Copy();
            f_interpreter.levels.functions = this.functions.Copy();

            // f_interpreter.verbose_debug = true;
            // do interpretation
            foreach (LineIterator line in function.lines)
                f_interpreter.Interpret(line);

            function.Reset(); // reset target, argument assignments
        }

        KeplerVariable CreateTemporaryVariable(Token token)
        {
            KeplerVariable var = new KeplerVariable();

            switch (token.type)
            {
                case TokenType.GenericOperation:
                    // it's another operation!
                    return DoGenericOperation(token);
                case TokenType.DeclareVariable:
                    // it's already a variable
                    return variables.GetVariable(token.token_string);
                case TokenType.StaticInt:
                    var.SetIntValue(int.Parse(token.token_string));
                    break;
                case TokenType.StaticUnsignedInt:
                    var.SetUnsignedIntValue(uint.Parse(token.token_string.Substring(1)));
                    break;
                case TokenType.StaticFloat:
                    var.SetFloatValue(float.Parse(token.token_string));
                    break;
                default:
                    throw new Exception(String.Format("Unable to create temporary variable for TokenType {0}", token.type));
            }



            return var;
        }
    }

    public class TokenState
    {
        // name declaration
        public bool override_state = false;
        public IDictionary<string, bool> booleans = new Dictionary<string, bool>();
        public IDictionary<string, string> strings = new Dictionary<string, string>();

        public TokenType type = TokenType.UNRECOGNIZED;
        public Action<Token, TokenState> action;

        public TokenState[] child_states = new TokenState[0];

        public KeplerVariable c_variable;
        public KeplerVariable a_operand; // "a_operand" + "b_operand"
        public KeplerVariable left_side_operator; // "left_side_operator" is
        public List<KeplerVariable> add_operands = new List<KeplerVariable>();
        public KeplerFunction c_function;

        public TokenState(TokenType type, Action<Token, TokenState> action)
        {
            AssignDefaultBools();
            AssignDefaultStrings();

            this.type = type;
            this.action = action;
        }

        public TokenState(TokenType type, TokenState[] child_states, Action<Token, TokenState> action)
        {
            AssignDefaultBools();
            AssignDefaultStrings();

            this.type = type;
            this.child_states = child_states;
            this.action = action;
        }

        void AssignDefaultBools()
        {
            booleans["declared_variable"] = false;
            booleans["declared_function"] = false;
            booleans["variable_assign"] = false;
            booleans["inside_string"] = false;
            booleans["function_assign"] = false;
            booleans["assigning_function_variables"] = false;
            booleans["assigning_function_variables_type"] = false;
            booleans["closing_function"] = false;
            booleans["inside_function"] = false;
            booleans["calling_function"] = false;
            booleans["inside_if_statement"] = false;
            booleans["inside_header"] = false;
            booleans["link_file"] = false;
            booleans["console_print"] = false;
            booleans["assigned_a_operand"] = false;
        }

        void AssignDefaultStrings()
        {
            strings["build_string"] = "";
            strings["nonpositional_variable_name"] = "";
            strings["print_string"] = "";
        }

        public void ResetOperands()
        {
            booleans["assigned_a_operand"] = false;
            a_operand = new KeplerVariable();
            // left_side_operator = 
        }

        public TokenState Shift(TokenState previous_token)
        {
            // pass through tracked strings and booleans
            this.booleans = previous_token.booleans;
            this.strings = previous_token.strings;

            this.add_operands = previous_token.add_operands;
            this.a_operand = previous_token.a_operand;
            this.left_side_operator = previous_token.left_side_operator;

            this.c_variable = previous_token.c_variable;
            this.c_function = previous_token.c_function;

            return this;
        }

        public TokenState DoAction(Token token, Token peek)
        {
            // do action, then get next state
            this.action(token, this);
            return GetNextState(this, peek);
        }

        TokenState GetNextState(TokenState current_state, Token peek)
        {
            foreach (TokenState state in current_state.child_states)
            {
                // Console.WriteLine(string.Format("{0} {1}", state.type, peek.type));
                // if (this.MatchState(state, peek)) return state;
                if (state.type == peek.type) return state;
            }

            if (peek.type == TokenType.EOL) throw new TokenException("[554] Unexpected EOL");

            // Console.WriteLine(peek.type);
            throw new TokenException(string.Format("[557] Unexpected token \"{0}\"", peek.token_string));
        }

        public override string ToString()
        {
            // string output = "TokenState\r\n";
            return string.Format("TokenState {0}", this.type);
        }
    }

    public class TokenException : Exception
    {
        public TokenException() { }

        public TokenException(string message)
            : base(message) { }

        public TokenException(string message, Exception inner)
            : base(message, inner) { }
    }
    public class LevelOneException : Exception
    {
        public LevelOneException() { }

        public LevelOneException(string message)
            : base(message) { }

        public LevelOneException(string message, Exception inner)
            : base(message, inner) { }
    }
}