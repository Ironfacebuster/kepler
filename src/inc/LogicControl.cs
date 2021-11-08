/*
 *   Copyright (c) 2021 William Huddleston
 *   All rights reserved.
 *   License: Apache 2.0
 */

using Kepler.Exceptions;
using Kepler.Interpreting;
using Kepler.Lexer;
using Kepler.Lexer.Tokens;
using Kepler.Modules;
using KeplerVariables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Kepler.LogicControl
{
    public class StateMachine
    {
        // static InternalLibraries internalLibraries;
        public bool verbose_debug = false;
        public bool end_on_eop = true;
        public bool linked_file = false;
        public bool is_interrupt = false;
        public int interrupt_id = -1;
        public KeplerVariableManager variables = new KeplerVariableManager();
        public KeplerFunctionManager functions = new KeplerFunctionManager();

        // every line starts with a level 1 token
        public List<TokenState> level1 = new List<TokenState>();

        public bool has_linked_file;
        public KeplerVariableManager linked_variables;
        public KeplerFunctionManager linked_functions;

        // function things
        bool schedule_execute_function = false;
        public KeplerVariable function_return_value;
        public KeplerType function_type;
        public string function_id;
        public Dictionary<string, Interpreter> interpreter_cache;


        bool inside_header = false;
        KeplerFunction scheduled_function = new KeplerFunction("NUL");
        public Interpreter interpreter;
        // level 1 tokens
        // EOP - EOP
        // if - ConditionalIf
        // else if - ConditionalElseIf
        // else - ConditionalElse
        // variable - DeclareVariable

        List<TokenState> GetTokenLevels()
        {
            List<TokenState> levels = new List<TokenState>();
            // final states
            TokenState EOL = new TokenState(TokenType.EOL, HandleEOL);
            TokenState EOP = new TokenState(TokenType.EOP, HandleEOP);

            // level 3 states
            TokenState StaticModifier = new TokenState(TokenType.StaticModifier, HandleStaticModifier);
            StaticModifier.booleans["declared_variable"] = true;
            StaticModifier.booleans["variable_assign"] = true;

            // level 2 states
            TokenState DeclareHeader = new TokenState(TokenType.DeclareHeader, new TokenState[] { EOL }, NullHandle);
            TokenState DeclareFunction = new TokenState(TokenType.DeclareFunction, HandleDeclareFunction);
            TokenState DeclareVariable = new TokenState(TokenType.DeclareVariable, HandleDeclareVariable);
            TokenState AssignFunctionType = new TokenState(TokenType.AssignFunctionType, HandleAssignFunctionType);

            // level n states
            TokenState GenericAssign = new TokenState(TokenType.GenericAssign, HandleGenericAssign);
            TokenState BooleanOperator = new TokenState(TokenType.BooleanOperator, NullHandle);
            TokenState OrOperator = new TokenState(TokenType.OrOperator, NullHandle);
            TokenState StaticVariableType = new TokenState(TokenType.StaticVariableType, HandleStaticVariableType);    // variable static type assignment
            TokenState StaticFunctionType = new TokenState(TokenType.StaticVariableType, HandleStaticVariableType);    // variable static type assignment
            TokenState StartArguments = new TokenState(TokenType.StartArguments, HandleStartArguments);
            TokenState StartDefineArguments = new TokenState(TokenType.StartDefineArguments, HandleStartDefineArguments);
            TokenState SetNonPositionalArgument = new TokenState(TokenType.SetNonPositionalArgument, new TokenState[] { BooleanOperator, EOL }, HandleSetNonPositionalArgument);

            // static values
            TokenState StaticFloat = new TokenState(TokenType.StaticFloat, new TokenState[] { BooleanOperator, EOL }, HandleStaticFloat);
            TokenState StaticInt = new TokenState(TokenType.StaticInt, new TokenState[] { BooleanOperator, EOL }, HandleStaticInt);
            TokenState StaticString = new TokenState(TokenType.StaticString, new TokenState[] { BooleanOperator, EOL }, HandleStaticString);
            TokenState StaticUnsignedInt = new TokenState(TokenType.StaticUnsignedInt, new TokenState[] { BooleanOperator, EOL }, HandleStaticUnsignedInt);
            TokenState StaticBool = new TokenState(TokenType.StaticBoolean, new TokenState[] { BooleanOperator, EOL }, HandleStaticBool);

            // loop stuff
            TokenState DeclareInterval = new TokenState(TokenType.DeclareInterval, new TokenState[] { StaticInt, StaticFloat, StaticUnsignedInt, DeclareVariable, EOL }, NullHandle);
            TokenState StartInterval = new TokenState(TokenType.StartInterval, new TokenState[] { DeclareInterval }, HandleStartInterval);
            TokenState EndInterval = new TokenState(TokenType.EndInterval, new TokenState[] { DeclareInterval }, HandleEndInterval);

            TokenState BreakOut = new TokenState(TokenType.BreakOut, new TokenState[] { EOL }, HandleBreakOut);

            TokenState DeclareLoop = new TokenState(TokenType.DeclareLoop, new TokenState[] { EOL }, NullHandle);
            TokenState StartLoop = new TokenState(TokenType.StartLoop, new TokenState[] { DeclareLoop }, HandleStartLoop);
            TokenState EndLoop = new TokenState(TokenType.EndLoop, new TokenState[] { DeclareLoop }, HandleEndLoop);



            // Function,
            // List,
            // Array,
            // Boolean,
            // Unassigned


            TokenState StartCallFunction = new TokenState(TokenType.CallFunction, new TokenState[] { DeclareFunction }, HandleStartCallFunction);
            TokenState AssignVariable = new TokenState(TokenType.DeclareVariable, new TokenState[] { GenericAssign }, HandleDeclareVariableAndAssign);    // level 1 declare variable is always assigning a value/creating a variable
            TokenState AssignFunction = new TokenState(TokenType.DeclareFunction, HandleDeclareFunctionAndAssign);    // level 1 declare function is always assigning a static type
            TokenState ConsolePrint = new TokenState(TokenType.ConsolePrint, HandleConsolePrint);

            TokenState GenericOperation = new TokenState(TokenType.GenericOperation, HandleGenericOperation);
            GenericOperation.child_states = new TokenState[] { GenericOperation, BooleanOperator, EOL };

            // linking stuff
            TokenState LinkFile = new TokenState(TokenType.LinkFile, new TokenState[] { StaticString }, HandleLinkFile);
            TokenState ThrowError = new TokenState(TokenType.ThrowError, new TokenState[] { StaticString, DeclareVariable, GenericOperation }, HandleThrowError);


            TokenState StartAssertion = new TokenState(TokenType.StartAssertion, new TokenState[] { GenericOperation }, HandleStartAssertion);

            // function things
            DeclareFunction.child_states = new TokenState[] { StartArguments, EOL };
            StartDefineArguments.child_states = new TokenState[] { SetNonPositionalArgument, EOL };
            StartArguments.child_states = new TokenState[] { SetNonPositionalArgument, GenericOperation, EOL };

            // ASSIGN RECURSIVE CHILD STATES
            GenericAssign.child_states = new TokenState[] { StartCallFunction, DeclareVariable, StaticFloat, StaticString, StaticInt, StaticUnsignedInt, StaticBool, StaticModifier, StaticVariableType, StaticFunctionType, GenericOperation };
            AssignFunction.child_states = new TokenState[] { AssignFunctionType, StartDefineArguments };
            AssignFunctionType.child_states = new TokenState[] { StaticVariableType, BooleanOperator };
            ConsolePrint.child_states = new TokenState[] { GenericOperation, DeclareVariable, StaticFloat, StaticString, StaticInt, StaticUnsignedInt, StaticBool, StaticModifier, StaticVariableType };

            DeclareVariable.child_states = new TokenState[] { EOL };

            StaticVariableType.child_states = new TokenState[] { BooleanOperator, EOL };
            StaticFunctionType.child_states = new TokenState[] { BooleanOperator, EOL };
            StaticModifier.child_states = new TokenState[] { BooleanOperator, /*StaticVariableType,*/ EOL };

            // handle "and is" & assigning arguments
            BooleanOperator.child_states = new TokenState[] { GenericAssign, DeclareVariable, GenericOperation, SetNonPositionalArgument, StartDefineArguments };

            EOL.child_states = new TokenState[] { EOL };
            EOP.child_states = new TokenState[] { EOP, EOL };

            // set up level 1 token states
            levels.Add(new TokenState(TokenType.ConditionalIf, HandleConditionalIf));
            levels.Add(new TokenState(TokenType.ConditionalElseIf, HandleConditionalElseIf));
            levels.Add(new TokenState(TokenType.ConditionalElse, new TokenState[] { EOL }, HandleConditionalElse));
            levels.Add(new TokenState(TokenType.FunctionReturn, new TokenState[] { StartCallFunction, GenericOperation, DeclareVariable, StaticBool, StaticFloat, StaticInt, StaticString, StaticUnsignedInt }, HandleFunctionReturn));
            levels.Add(StartInterval);
            levels.Add(EndInterval);
            levels.Add(StartLoop);
            levels.Add(EndLoop);
            levels.Add(BreakOut);
            levels.Add(LinkFile); // link file
            levels.Add(ThrowError); // throw error
            levels.Add(new TokenState(TokenType.StartHeader, new TokenState[] { DeclareHeader }, HandleStartHeader));    // start header token
            levels.Add(new TokenState(TokenType.EndHeader, new TokenState[] { DeclareHeader }, HandleEndHeader));        // end header token
            levels.Add(new TokenState(TokenType.StartFunction, new TokenState[] { DeclareFunction }, HandleStartFunction));     // start function token
            levels.Add(new TokenState(TokenType.EndFunction, new TokenState[] { DeclareFunction }, HandleEndFunction));         // end function token
            levels.Add(new TokenState(TokenType.StartConditional, new TokenState[] { StaticBool, GenericOperation }, HandleStartConditional));  // if
            levels.Add(new TokenState(TokenType.EndConditional, new TokenState[] { EOL }, HandleEndConditional));
            levels.Add(AssignVariable);
            levels.Add(AssignFunction);
            levels.Add(StartCallFunction);
            levels.Add(StartAssertion);
            levels.Add(ConsolePrint);
            levels.Add(EOP); // END OF PROGRAM token

            return levels;
        }

        public StateMachine(Interpreter interpreter)
        {
            this.interpreter = interpreter;

            if (this.interpreter.is_global) interpreter_cache = new Dictionary<string, Interpreter>();
            else interpreter_cache = this.interpreter.global.statemachine.interpreter_cache;

            this.level1 = GetTokenLevels();
        }

        public TokenState GetLevelOneToken(Token token)
        {
            for (int i = 0; i < level1.Count; ++i)
            {
                if (level1[i].type == token.type) return level1[i];
            }

            throw new KeplerError(KeplerErrorCode.UNEXP_START_TOKEN, new string[] { token.token_string });
        }
        void NullHandle(Token token, TokenState state)
        {
            // do nothing...
        }

        void HandleFunctionReturn(Token token, TokenState state)
        {
            if (!this.interpreter.is_function) throw new KeplerError(KeplerErrorCode.UNEXP_RETURN);

            state.booleans["return_value"] = true;
        }

        void HandleStartConditional(Token token, TokenState state)
        {
            // enter "validate_conditional"
            state.booleans["validate_conditional"] = true;
            state.booleans["inside_conditional"] = true;
        }

        void HandleStartAssertion(Token token, TokenState state)
        {
            state.booleans["validate_assertion"] = true;
        }

        void HandleEndConditional(Token token, TokenState state)
        {
            if (!state.booleans["inside_conditional"]) throw new KeplerError(KeplerErrorCode.UNEXP_END_COND);
            // enter "validate_conditional"
            // state.booleans["validate_conditional"] = false;
            state.booleans["inside_conditional"] = false;
        }

        void HandleConsolePrint(Token token, TokenState state)
        {
            state.strings["print_string"] = ""; // clear print_string
            state.booleans["console_print"] = true;
        }
        void HandleGenericOperation(Token token, TokenState state)
        {
            KeplerVariable result = DoGenericOperation(token);

            if (state.booleans["variable_assign"])
                state.left_side_operator.AssignValue(result);
            if (state.booleans["console_print"])
                state.strings["print_string"] = state.strings["print_string"] + result.GetValueAsString();
            if (state.booleans["validate_conditional"])
                state.booleans["validated_conditional"] = result.GetValueAsBool();
            if (state.booleans["validate_assertion"] && result.GetValueAsBool() != true)
                throw new KeplerError(KeplerErrorCode.FALSE_ASSERTION, new string[] { result.GetValueAsBool().ToString() });
            if (state.booleans["throw_error"])
                state.strings["error_string"] = result.GetValueAsString();
            if (state.booleans["return_value"])
                this.SetReturnValue(result);
        }
        public KeplerVariable DoGenericOperation(Token token)
        {
            if (verbose_debug)
            {
                Console.WriteLine(String.Format("Doing Generic Operation \"{0}\"", token.operation));
            }

            KeplerVariable result = new KeplerVariable();
            KeplerVariable a_operand = CreateTemporaryVariable(token.a);

            if (token.operation == OperationType.CastType)
            {
                if (token.b.type != TokenType.StaticVariableType) throw new KeplerError(KeplerErrorCode.UNEXP_TOKEN, new string[] { token.b.token_string });
                Enum.TryParse(token.b.token_string, out KeplerType m_type);

                switch (m_type)
                {
                    case KeplerType.Float:
                        result.SetFloatValue(a_operand.GetValueAsFloat(true));
                        break;
                    case KeplerType.Int:
                        result.SetIntValue(a_operand.GetValueAsInt(true));
                        break;
                    case KeplerType.uInt:
                        result.SetUnsignedIntValue(a_operand.GetValueAsUnsignedInt(true));
                        break;
                    case KeplerType.Boolean:
                        result.SetBoolValue(a_operand.GetValueAsBool(true));
                        break;
                    case KeplerType.String:
                        result.SetStringValue(a_operand.GetValueAsString(true));
                        break;
                    default:
                        throw new KeplerError(KeplerErrorCode.EXPLICIT_CAST, new string[] { a_operand.type.ToString(), m_type.ToString() });
                }

                return result;
            }

            if (token.operation == OperationType.Invert)
            {
                result.SetBoolValue(!a_operand.GetValueAsBool());
                return result;
            }
            // create b_operand after making sure we're not casting
            KeplerVariable b_operand = CreateTemporaryVariable(token.b);

            if (verbose_debug)
            {
                Console.WriteLine(a_operand);
                Console.WriteLine(b_operand);
            }

            if (token.operation == OperationType.StrictEquality)
            {
                if (a_operand.type != b_operand.type)
                    throw new KeplerError(KeplerErrorCode.STRICT_TYPE_EQUALITY, new string[] { a_operand.type.ToString(), b_operand.type.ToString() });
                else
                {
                    switch (a_operand.type)
                    {
                        case KeplerType.Float:
                            result.SetBoolValue(a_operand.FloatValue == b_operand.FloatValue);
                            break;
                        case KeplerType.Int:
                            result.SetBoolValue(a_operand.IntValue == b_operand.IntValue);
                            break;
                        case KeplerType.uInt:
                            result.SetBoolValue(a_operand.uIntValue == b_operand.uIntValue);
                            break;
                        case KeplerType.Boolean:
                            result.SetBoolValue(a_operand.BoolValue == b_operand.BoolValue);
                            break;
                        case KeplerType.String:
                            result.SetBoolValue(a_operand.StringValue == b_operand.StringValue);
                            break;
                        case KeplerType.NaN:
                            result.SetBoolValue(true);
                            break;
                        default:
                            result.SetBoolValue(false);
                            break;
                    }
                }

                return result;
            }

            if (token.operation == OperationType.Equality)
            {
                switch (a_operand.type)
                {
                    case KeplerType.Float:
                        result.SetBoolValue(a_operand.GetValueAsFloat() == b_operand.GetValueAsFloat());
                        break;
                    case KeplerType.Int:
                        result.SetBoolValue(a_operand.GetValueAsInt() == b_operand.GetValueAsInt());
                        break;
                    case KeplerType.uInt:
                        result.SetBoolValue(a_operand.GetValueAsUnsignedInt() == b_operand.GetValueAsUnsignedInt());
                        break;
                    case KeplerType.Boolean:
                        result.SetBoolValue(a_operand.GetValueAsBool() == b_operand.GetValueAsBool());
                        break;
                    case KeplerType.String:
                        result.SetBoolValue(a_operand.GetValueAsString() == b_operand.GetValueAsString());
                        break;
                    case KeplerType.NaN:
                        result.SetBoolValue(true);
                        break;
                    default:
                        result.SetBoolValue(false);
                        break;
                }

                return result;
            }

            if (a_operand.type == KeplerType.NaN || b_operand.type == KeplerType.NaN)
            {
                // doing any operation to a NaN results in a NaN
                result.SetType(KeplerType.NaN);
                return result;
            }

            // string handling
            // cast both operands to String if adding
            if ((a_operand.type == KeplerType.String || b_operand.type == KeplerType.String) && token.operation == OperationType.Add)
            {
                result.SetStringValue(a_operand.GetValueAsString() + b_operand.GetValueAsString());
                return result;
            }
            else if ((a_operand.type == KeplerType.String || b_operand.type == KeplerType.String) &&
            (token.operation != OperationType.GreaterThan &&
            token.operation != OperationType.GreaterThanEqual &&
            token.operation != OperationType.LessThan &&
            token.operation != OperationType.LessThanEqual &&
            token.operation != OperationType.And &&
            token.operation != OperationType.Or))
            {
                result.SetType(KeplerType.NaN);
                return result;
            }

            switch (token.operation)
            {
                case OperationType.Add:
                    switch (a_operand.type)
                    {
                        case KeplerType.Float:
                            result.SetFloatValue(a_operand.GetValueAsFloat() + b_operand.GetValueAsFloat());
                            break;
                        case KeplerType.Int:
                            result.SetIntValue(a_operand.GetValueAsInt() + b_operand.GetValueAsInt());
                            break;
                        case KeplerType.uInt:
                            result.SetUnsignedIntValue(a_operand.GetValueAsUnsignedInt() + b_operand.GetValueAsUnsignedInt());
                            break;
                    }
                    break;
                case OperationType.Subtract:
                    switch (a_operand.type)
                    {
                        case KeplerType.Float:
                            result.SetFloatValue(a_operand.GetValueAsFloat() - b_operand.GetValueAsFloat());
                            break;
                        case KeplerType.Int:
                            result.SetIntValue(a_operand.GetValueAsInt() - b_operand.GetValueAsInt());
                            break;
                        case KeplerType.uInt:
                            result.SetUnsignedIntValue(a_operand.GetValueAsUnsignedInt() - b_operand.GetValueAsUnsignedInt());
                            break;
                    }
                    break;
                case OperationType.Multiply:
                    switch (a_operand.type)
                    {
                        case KeplerType.Float:
                            result.SetFloatValue(a_operand.GetValueAsFloat() * b_operand.GetValueAsFloat());
                            break;
                        case KeplerType.Int:
                            result.SetIntValue(a_operand.GetValueAsInt() * b_operand.GetValueAsInt());
                            break;
                        case KeplerType.uInt:
                            result.SetUnsignedIntValue(a_operand.GetValueAsUnsignedInt() * b_operand.GetValueAsUnsignedInt());
                            break;
                    }
                    break;
                case OperationType.Power:
                    switch (a_operand.type)
                    {
                        case KeplerType.Float:
                            result.SetFloatValue((decimal)Math.Pow((double)a_operand.GetValueAsFloat(), (double)b_operand.GetValueAsFloat()));
                            break;
                        case KeplerType.Int:
                            result.SetIntValue((int)Math.Pow(a_operand.GetValueAsInt(), b_operand.GetValueAsInt()));
                            break;
                        case KeplerType.uInt:
                            result.SetUnsignedIntValue((uint)Math.Pow(a_operand.GetValueAsUnsignedInt(), b_operand.GetValueAsUnsignedInt()));
                            break;
                    }
                    break;
                case OperationType.Divide:
                    // check for divide by zero
                    if (b_operand.GetValueAsFloat() == 0)
                    {
                        result.SetType(KeplerType.NaN);
                        return result;
                    }
                    switch (a_operand.type)
                    {
                        case KeplerType.Float:
                            result.SetFloatValue(a_operand.GetValueAsFloat() / b_operand.GetValueAsFloat());
                            break;
                        case KeplerType.Int:
                            result.SetIntValue(a_operand.GetValueAsInt() / b_operand.GetValueAsInt());
                            break;
                        case KeplerType.uInt:
                            result.SetUnsignedIntValue(a_operand.GetValueAsUnsignedInt() / b_operand.GetValueAsUnsignedInt());
                            break;
                    }
                    break;
                case OperationType.Modulo:
                    result.SetFloatValue(a_operand.GetValueAsFloat() % b_operand.GetValueAsFloat());
                    break;
                case OperationType.GreaterThan:
                    if (a_operand.type == KeplerType.String) result.SetBoolValue(string.Compare(a_operand.GetValueAsString(), b_operand.GetValueAsString()) == 1);
                    else result.SetBoolValue(a_operand.GetValueAsFloat() > b_operand.GetValueAsFloat());
                    break;
                case OperationType.GreaterThanEqual:
                    if (a_operand.type == KeplerType.String)
                    {
                        int comp = string.Compare(a_operand.GetValueAsString(), b_operand.GetValueAsString());
                        result.SetBoolValue(comp == 1 || comp == 0);
                    }
                    else result.SetBoolValue(a_operand.GetValueAsFloat() >= b_operand.GetValueAsFloat());
                    break;
                case OperationType.LessThan:
                    if (a_operand.type == KeplerType.String) result.SetBoolValue(string.Compare(a_operand.GetValueAsString(), b_operand.GetValueAsString()) == -1);
                    else result.SetBoolValue(a_operand.GetValueAsFloat() < b_operand.GetValueAsFloat());
                    break;
                case OperationType.LessThanEqual:
                    if (a_operand.type == KeplerType.String)
                    {
                        int comp = string.Compare(a_operand.GetValueAsString(), b_operand.GetValueAsString());
                        result.SetBoolValue(comp == -1 || comp == 0);
                    }
                    else result.SetBoolValue(a_operand.GetValueAsFloat() <= b_operand.GetValueAsFloat());
                    break;
                case OperationType.And:
                    result.SetBoolValue(a_operand.GetValueAsBool() && b_operand.GetValueAsBool());
                    break;
                case OperationType.Or:
                    result.SetBoolValue(a_operand.GetValueAsBool() || b_operand.GetValueAsBool());
                    break;
            }

            return result;
        }
        void HandleLinkFile(Token token, TokenState state)
        {
            if (!inside_header) throw new KeplerError(KeplerErrorCode.LINK_OUT_HEADER);
            state.booleans["link_file"] = true;
        }
        void HandleThrowError(Token token, TokenState state)
        {
            state.booleans["throw_error"] = true;
            state.strings["error_string"] = "";
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

        void HandleSetNonPositionalArgument(Token token, TokenState state)
        {
            // this is a hack.
            if (state.booleans["inside_define_arguments"])
            {
                if (token.a.type != TokenType.StaticVariableType) throw new KeplerError(KeplerErrorCode.UNEXP_TOKEN, new string[] { token.a.token_string });
                Enum.TryParse(token.a.token_string, out KeplerType m_type);

                state.c_function.AssignNonPositional(token.b.token_string, m_type);
            }
            else
            {
                scheduled_function.SetNonPositional(token.b.token_string, CreateTemporaryVariable(token.a));
            }
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
                state.left_side_operator.SetFloatValue(decimal.Parse(token.token_string));
            if (state.booleans["console_print"])
                state.strings["print_string"] = state.strings["print_string"] + token.token_string;
            if (state.booleans["inside_interval"])
                state.c_interrupt.SetInterval((int)decimal.Parse(token.token_string));
            if (state.booleans["return_value"])
            {
                KeplerVariable new_variable = new KeplerVariable();
                new_variable.SetFloatValue(decimal.Parse(token.token_string));
                new_variable.SetModifier(KeplerModifier.Constant);

                this.SetReturnValue(new_variable);
            }
        }
        void HandleStaticInt(Token token, TokenState state)
        {
            if (state.booleans["variable_assign"])
                state.left_side_operator.SetIntValue(int.Parse(token.token_string));
            if (state.booleans["console_print"])
                state.strings["print_string"] = state.strings["print_string"] + token.token_string;
            if (state.booleans["inside_interval"])
                state.c_interrupt.SetInterval(int.Parse(token.token_string));
            if (state.booleans["return_value"])
            {
                KeplerVariable new_variable = new KeplerVariable();
                new_variable.SetIntValue(int.Parse(token.token_string));
                new_variable.SetModifier(KeplerModifier.Constant);

                this.SetReturnValue(new_variable);
            }
        }
        void HandleStaticUnsignedInt(Token token, TokenState state)
        {
            if (state.booleans["variable_assign"])
                state.left_side_operator.SetUnsignedIntValue(uint.Parse(token.token_string.Substring(1)));
            if (state.booleans["console_print"])
                state.strings["print_string"] = state.strings["print_string"] + token.token_string;
            if (state.booleans["inside_interval"])
                state.c_interrupt.SetInterval((int)uint.Parse(token.token_string.Substring(1))); // cast to int
            if (state.booleans["return_value"])
            {
                KeplerVariable new_variable = new KeplerVariable();
                new_variable.SetUnsignedIntValue(uint.Parse(token.token_string));
                new_variable.SetModifier(KeplerModifier.Constant);

                this.SetReturnValue(new_variable);
            }
        }
        void HandleStaticBool(Token token, TokenState state)
        {
            if (state.booleans["variable_assign"])
                state.left_side_operator.SetBoolValue(bool.Parse(token.token_string));
            if (state.booleans["console_print"])
                state.strings["print_string"] = state.strings["print_string"] + bool.Parse(token.token_string);
            if (state.booleans["return_value"])
            {
                KeplerVariable new_variable = new KeplerVariable();
                new_variable.SetBoolValue(bool.Parse(token.token_string));
                new_variable.SetModifier(KeplerModifier.Constant);

                this.SetReturnValue(new_variable);
            }
            // if (state.booleans["validate_conditional"])
            //     state.booleans["inside_conditional"] = bool.Parse(token.token_string);
        }
        void HandleStaticString(Token token, TokenState state)
        {
            string string_value = token.token_string.Substring(1, token.token_string.Length - 2);
            if (state.booleans["variable_assign"])
                state.left_side_operator.SetStringValue(string_value);
            if (state.booleans["link_file"])
            {
                if (verbose_debug) Console.WriteLine(string.Format("LINKING \"{0}\"", string_value));

                // check if the internal modules have something by this name

                if (InternalLibraries.HasModule(string_value))
                {
                    this.LoadModule(string_value);
                }
                else
                {
                    // load file and interpret
                    Tokenizer m_tokenizer = new Tokenizer();
                    m_tokenizer.Load(string_value);

                    Interpreter m_interpreter = new Interpreter(this.interpreter.global, this.interpreter);
                    m_interpreter.verbose_debug = this.interpreter.verbose_debug;
                    m_interpreter.debug = this.interpreter.debug;
                    m_interpreter.statemachine.linked_file = true;
                    m_interpreter.tracer = this.interpreter.tracer;
                    m_interpreter.filename = Path.GetFileName(string_value);

                    // do interpretation
                    while (m_tokenizer.HasNext())
                    {
                        m_interpreter.Interpret(m_tokenizer.CurrentLine());

                        m_tokenizer++;
                    }

                    // transfer all global variables and functions
                    linked_variables = m_interpreter.statemachine.variables;
                    linked_functions = m_interpreter.statemachine.functions;

                    has_linked_file = true;
                }
            }
            else if (state.booleans["throw_error"])
            {
                state.strings["error_string"] = state.strings["error_string"] + string_value;
            }
            else if (state.booleans["console_print"])
                state.strings["print_string"] = state.strings["print_string"] + string_value;
            if (state.booleans["return_value"])
            {
                KeplerVariable new_variable = new KeplerVariable();
                new_variable.SetStringValue(string_value);
                new_variable.SetModifier(KeplerModifier.Constant);

                this.SetReturnValue(new_variable);
            }
        }

        List<string> loaded_modules = new List<string>();

        public void LoadModule(string name)
        {
            if (loaded_modules.Contains(name)) return; // if this module is already loaded, just return.
            if (!InternalLibraries.HasModule(name)) throw new KeplerError(KeplerErrorCode.GENERIC_ERROR, new string[] { $"No module named '{name}' was found." });

            try
            {
                Module loaded_module = InternalLibraries.GetModule(name);

                if (loaded_module.required_modules != null && loaded_module.required_modules.Length > 0)
                {
                    for (int i = 0; i < loaded_module.required_modules.Length; ++i)
                    {
                        this.LoadModule(loaded_module.required_modules[i].name);
                    }
                }

                if (loaded_module.variables != null && loaded_module.variables.Count > 0)
                {
                    foreach (KeyValuePair<string, KeplerVariable> var_pair in loaded_module.variables)
                    {
                        this.variables.Load(var_pair);
                    }
                }

                if (loaded_module.functions != null && loaded_module.functions.Length > 0)
                {
                    for (int i = 0; i < loaded_module.functions.Length; i++)
                    {
                        this.functions.Load(loaded_module.functions[i]);
                    }
                }

                loaded_modules.Add(name);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"An error occurred while loading module \"{name}\".\r\n");
                throw e;
            }
        }
        void HandleStaticModifier(Token token, TokenState state)
        {
            Enum.TryParse(token.token_string, out KeplerModifier m_type);
            state.left_side_operator.SetModifier(m_type);
        }
        void HandleStaticVariableType(Token token, TokenState state)
        {
            Enum.TryParse(token.token_string, out KeplerType m_type);
            if (state.booleans["declared_variable"] && state.booleans["variable_assign"])
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
                schedule_execute_function = false;
                ExecuteFunction(scheduled_function, state);
                scheduled_function = null;
            }
        }
        void HandleEOP(Token token, TokenState state)
        {
            if (verbose_debug) Console.WriteLine("EOP!");

            if (!linked_file && end_on_eop) Environment.Exit(0); // exit with code 0 if NOT a linked file
        }
        void HandleConditionalIf(Token token, TokenState state)
        {
            state.booleans["inside_if_statement"] = true;
        }
        void HandleConditionalElseIf(Token token, TokenState state)
        {
            if (!state.booleans["inside_if_statement"]) throw new KeplerError(KeplerErrorCode.UNEXP_START_COND);
        }
        void HandleConditionalElse(Token token, TokenState state)
        {
            if (!state.booleans["inside_if_statement"]) throw new KeplerError(KeplerErrorCode.UNEXP_START_COND);
        }
        void HandleDeclareVariable(Token token, TokenState state)
        {
            if (state.booleans["variable_assign"])
            {
                state.left_side_operator.AssignValue(variables.GetVariable(token.token_string));
            }
            // if (state.booleans["inside_arguments"])
            // {
            //     state.strings["nonpositional_argument_name"] = token.token_string;
            // }
            else if (state.booleans["console_print"])
            {
                state.strings["print_string"] = state.strings["print_string"] + variables.GetVariable(token.token_string).GetValueAsString();
            }
            else if (state.booleans["throw_error"])
            {
                state.strings["error_string"] = variables.GetVariable(token.token_string).GetValueAsString();
            }
            else if (state.booleans["return_value"])
            {
                this.SetReturnValue(variables.GetVariable(token.token_string));
            }
            else
            {
                state.c_variable = variables.DeclareVariable(token.token_string, this.interpreter.is_global ? true : false);
                state.booleans["declared_variable"] = true;
            }
        }
        void HandleDeclareVariableAndAssign(Token token, TokenState state)
        {
            state.c_variable = variables.DeclareVariable(token.token_string, this.interpreter.is_global ? true : false);
            state.booleans["declared_variable"] = true;
            state.booleans["variable_assign"] = true;
        }
        void HandleDeclareFunctionAndAssign(Token token, TokenState state)
        {
            state.c_function = functions.DeclareFunction(token.token_string, this.interpreter.is_global ? true : false);
            state.booleans["declared_function"] = true;
        }
        void HandleDeclareFunction(Token token, TokenState state)
        {
            KeplerFunction c_function = functions.GetFunction(token.token_string);

            // TODO: execute function after arguments are assigned
            if (state.booleans["calling_function"])
            {
                if (c_function.type == KeplerType.Unassigned) throw new KeplerError(KeplerErrorCode.CALL_UNDEF_FUNCT_TYPE, new string[] { c_function.name });

                // reset arguments
                c_function.Reset();

                if (state.booleans["declared_variable"])
                {
                    state.c_variable.type = c_function.type;
                    c_function.target = state.c_variable; // assign target (for return values)
                }


                // always schedule function so that arguments that get unececcary assigned can be caught
                schedule_execute_function = true;
                scheduled_function = c_function;
            }
            else
            {
                state.c_function = functions.DeclareFunction(token.token_string, this.interpreter.is_global ? true : false);
                state.booleans["declared_function"] = true;
            }
        }

        void HandleStartArguments(Token token, TokenState state)
        {

            if (!state.booleans["calling_function"]) throw new KeplerError(KeplerErrorCode.UNEXP_TOKEN);

            state.booleans["inside_arguments"] = true;
        }

        void HandleStartDefineArguments(Token token, TokenState state)
        {
            // if (state.booleans["calling_function"]) throw new KeplerError(KeplerErrorCode.UNEXP_TOKEN);

            state.booleans["inside_define_arguments"] = true;
        }

        void HandleStartInterval(Token token, TokenState state)
        {
            if (state.booleans["inside_interval"]) throw new KeplerError(KeplerErrorCode.UNEXP_START_INT);
            state.booleans["inside_interval"] = true;
        }
        void HandleEndInterval(Token token, TokenState state)
        {
            if (!state.booleans["inside_interval"]) throw new KeplerError(KeplerErrorCode.UNEXP_END_INT);
            state.booleans["inside_interval"] = false;
        }
        void HandleStartLoop(Token token, TokenState state)
        {
            if (state.booleans["inside_loop"]) throw new KeplerError(KeplerErrorCode.UNEXP_START_LOOP);
            state.booleans["inside_loop"] = true;
        }
        void HandleEndLoop(Token token, TokenState state)
        {
            if (!state.booleans["inside_loop"]) throw new KeplerError(KeplerErrorCode.UNEXP_END_LOOP);
            state.booleans["inside_loop"] = false;
        }
        void HandleBreakOut(Token token, TokenState state)
        {
            if (!this.is_interrupt) throw new KeplerError(KeplerErrorCode.UNEXP_BREAKOUT);

            if (verbose_debug) Console.WriteLine("BREAK ON LINE " + interpreter.c_line.line);

            KillAll();
        }
        void KillAll()
        {
            Interpreter parent_interpreter = this.interpreter;
            KeplerInterrupt interrupt = interpreter.interrupts.GetInterrupt(this.interrupt_id);
            interrupt.Disable();

            while (true)
            {
                // Console.WriteLine(string.Format("{0} -> {1}", parent_interpreter.ID, parent_interpreter.is_global));
                if (parent_interpreter.ID == interrupt.parent.ID) break;

                parent_interpreter.Kill();
                parent_interpreter = parent_interpreter.parent;
            }
        }

        void KillAllByFunctionID(string function_id)
        {
            Interpreter parent_interpreter = this.interpreter;

            while (true)
            {
                if (parent_interpreter.statemachine.function_id != function_id) break;

                parent_interpreter.Kill();
                parent_interpreter = parent_interpreter.parent;
            }

            this.interpreter.Kill();
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
        void ExecuteFunction(KeplerFunction function, TokenState state)
        {
            string stack_id = this.interpreter.tracer.PushStack(String.Format("at {0} ({1}:{2}:{3})", function.name, this.interpreter.filename, this.interpreter.c_line.line, this.interpreter.c_line.CurrentToken().start));

            // get interpreter from cache
            Interpreter f_interpreter;
            string funct_id = function.id.ToString();

            if (this.interpreter_cache.ContainsKey(funct_id))
            {
                f_interpreter = this.interpreter_cache[funct_id];
                f_interpreter.killed = false;
            }
            else
            {
                this.interpreter_cache[funct_id] = new Interpreter(this.interpreter.global, this.interpreter);
                f_interpreter = this.interpreter_cache[funct_id];

                f_interpreter.verbose_debug = this.interpreter.verbose_debug;
                f_interpreter.debug = this.interpreter.debug;
                f_interpreter.tracer = this.interpreter.tracer;
                f_interpreter.filename = this.interpreter.filename;
                f_interpreter.is_function = true;

                f_interpreter.statemachine.function_type = function.type;
                f_interpreter.statemachine.function_id = function.id;
            }

            f_interpreter.statemachine.function_return_value = null;
            f_interpreter.statemachine.variables = this.variables.Copy();
            f_interpreter.statemachine.functions = this.functions.Copy();

            if (function.is_internal)
            {
                if (this.verbose_debug)
                    Console.WriteLine("EXECUTING INTERNAL FUNCT!");
                // call with null argument list, since arguments aren't properly implemented yet
                KeplerVariable result = function.internal_call(f_interpreter, function.arguments);

                if (result != null)
                    f_interpreter.statemachine.SetReturnValue(result);
            }
            else
            {
                if (function.HasTarget() && function.type == KeplerType.Unassigned) throw new KeplerError(KeplerErrorCode.ASSIGN_UNDEF_FUNCT_TYPE, new string[] { function.name, function.GetTarget().ToString() });

                // load arguments
                if (function.HasArguments())
                {
                    if (verbose_debug) Console.WriteLine("LOADING ARGUMENTS");
                    foreach (KeyValuePair<string, KeplerType> pair in function.arguments.required_non_positionals)
                    {

                        // if (verbose_debug) Console.WriteLine("LOADING ARGUMENT " + argument.name);
                        KeplerVariable var = function.arguments.GetArgument(pair.Key);
                        KeplerVariable n_var = f_interpreter.statemachine.variables.DeclareVariable(pair.Key, false);
                        n_var.AssignValue(var);
                    }
                }

                // do interpretation
                for (int i = 0; i < function.lines.Count; ++i)
                {
                    f_interpreter.Interpret(function.lines[i]);
                    if (f_interpreter.killed) break;
                }

                function.Reset(); // reset target, argument assignments

            }

            this.interpreter.tracer.PopStack(stack_id);

            if (f_interpreter.statemachine.HasReturnValue())
            {
                if (this.verbose_debug)
                {
                    Console.WriteLine("RETURN VALUE!");
                    Console.WriteLine(f_interpreter.statemachine.function_return_value);
                    Console.WriteLine(function.target.id);
                }

                if (state.booleans["variable_assign"] && state.left_side_operator != null)
                {
                    state.left_side_operator.AssignValue(f_interpreter.statemachine.function_return_value);
                    // KeplerVariable target = this.variables.GetVariableByID(function.target.id);
                }
            }
        }

        void SetReturnValue(KeplerVariable return_value)
        {
            if (this.function_type != return_value.type && this.function_type != KeplerType.Any) throw new KeplerError(KeplerErrorCode.INVALID_TYPE_ASSIGN, new string[] { return_value.type.ToString(), this.function_type.ToString() });

            this.function_return_value = return_value;

            // simple recursive loop for now
            Interpreter parent_interpreter = this.interpreter;

            while (true)
            {
                if (parent_interpreter.statemachine.function_id != this.function_id) break;

                parent_interpreter = parent_interpreter.parent;
                parent_interpreter.statemachine.function_return_value = return_value;
            }

            // break out
            KillAllByFunctionID(this.function_id);
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
                    var.SetFloatValue(decimal.Parse(token.token_string));
                    break;
                case TokenType.StaticBoolean:
                    var.SetBoolValue(bool.Parse(token.token_string));
                    break;
                case TokenType.StaticString:
                    var.SetStringValue(token.token_string.Substring(1, token.token_string.Length - 2));
                    break;
                default:
                    throw new KeplerError(KeplerErrorCode.NULL_TEMP_VAR, new string[] { token.type.ToString() });
            }

            return var;
        }

        public bool HasReturnValue()
        {
            return !(this.function_return_value == null);
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
        public KeplerVariable left_side_operator; // "left_side_operator" is
        public List<KeplerVariable> add_operands = new List<KeplerVariable>();
        public KeplerFunction c_function;
        public KeplerInterrupt c_interrupt;

        public TokenState(TokenType type, Action<Token, TokenState> action)
        {
            AssignDefaultBools();
            // AssignDefaultStrings();

            this.type = type;
            this.action = action;
        }

        public TokenState(TokenType type, TokenState[] child_states, Action<Token, TokenState> action)
        {
            AssignDefaultBools();
            // AssignDefaultStrings();

            this.type = type;
            this.child_states = child_states;
            this.action = action;
        }

        void AssignDefaultBools()
        {
            booleans["declared_variable"] = false;
            booleans["declared_function"] = false;

            booleans["variable_assign"] = false;
            booleans["validate_conditional"] = false;
            booleans["validated_conditional"] = false;
            booleans["validate_assertion"] = false;
            booleans["inside_conditional"] = false;

            booleans["inside_function"] = false;
            booleans["inside_interval"] = false;
            booleans["inside_loop"] = false;

            booleans["function_assign"] = false;
            booleans["calling_function"] = false;
            booleans["inside_arguments"] = false;
            booleans["inside_define_arguments"] = false;

            booleans["inside_if_statement"] = false;

            booleans["inside_header"] = false;
            booleans["link_file"] = false;

            booleans["console_print"] = false;
            booleans["throw_error"] = false;
            booleans["return_value"] = false;
        }

        void AssignDefaultStrings()
        {
            strings["nonpositional_variable_name"] = "";
            strings["print_string"] = "";
            strings["error_string"] = "";
        }

        public TokenState Shift(TokenState previous_token)
        {
            // pass through tracked strings and booleans
            this.booleans = previous_token.booleans;
            this.strings = previous_token.strings;

            this.add_operands = previous_token.add_operands;
            this.left_side_operator = previous_token.left_side_operator;

            this.c_variable = previous_token.c_variable;
            this.c_function = previous_token.c_function;
            this.c_interrupt = previous_token.c_interrupt;

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
            for (int i = 0; i < current_state.child_states.Length; ++i)
            {
                if (current_state.child_states[i].type == peek.type) return current_state.child_states[i];
            }

            if (peek.type == TokenType.EOL) throw new KeplerError(KeplerErrorCode.UNEXP_EOL);

            throw new KeplerError(KeplerErrorCode.UNEXP_TOKEN, new string[] { peek.token_string });
        }

        public override string ToString()
        {
            return string.Format("TokenState {0}", this.type);
        }
    }

    public class Token
    {
        public TokenType type;
        public int start;
        public string token_string;

        // only for GenericOperation
        public Token a;
        public Token b;
        public OperationType operation;

        public Token(TokenType type, int index, string token_string)
        {
            this.type = type;
            this.start = index;
            this.token_string = token_string;
        }

        public override string ToString()
        {
            // if(this.type == TokenType.GenericOperation)
            //     return string.Format("{0} {1} {2}", token_string, type, start);
            return string.Format("{0} => {1} [{2}]", token_string, type, start);
        }
    }

    public class TokenMatch
    {
        public TokenType type;
        public string string_token;
        public string peek;
        public string prev;
        public int increment = 1;

        public TokenMatch(TokenType type, string string_token, string peek, string prev, int increment)
        {
            this.type = type;
            this.string_token = string_token;
            this.peek = peek;
            this.prev = prev;
            this.increment = increment;
        }

        public Boolean Match(string token, string peek, string previous)
        {
            Boolean match_token = false;
            Boolean match_previous = false;
            Boolean match_peek = false;

            // if match_peek is already true, don't check the next time
            if ((string.IsNullOrEmpty(this.peek) && string.IsNullOrEmpty(peek)) || this.peek == peek) match_peek = true;
            if (!match_peek && (!string.IsNullOrEmpty(this.peek) && this.peek == any_string)) match_peek = true;
            if (!match_peek && (!string.IsNullOrEmpty(this.peek) && (this.peek == valid_variable && IsValidIdentifier(peek)))) match_peek = true;

            // if match_previous is already true, don't check the next time
            if (!match_previous && ((string.IsNullOrEmpty(this.prev) && string.IsNullOrEmpty(previous)) || this.prev == previous)) match_previous = true;
            if (!match_previous && (!string.IsNullOrEmpty(this.prev) && this.prev == any_string)) match_previous = true;
            if (!match_previous && (!string.IsNullOrEmpty(this.prev) && (this.prev == valid_variable && IsValidIdentifier(previous)))) match_previous = true;

            // if match_token is already true, don't check the next time
            if (!match_token && ((string.IsNullOrEmpty(this.string_token) && string.IsNullOrEmpty(token)) || this.string_token == token)) match_token = true;
            if (!match_token && !string.IsNullOrEmpty(this.string_token))
            {
                if (this.string_token == valid_variable && IsValidIdentifier(token)) match_token = true;
                if (!match_token && this.string_token == any_string) match_token = true;
                if (!match_token && (this.string_token == eval_float && Decimal.TryParse(token, out decimal f) && token.IndexOf(".") != -1)) match_token = true;
                if (!match_token && (this.string_token == eval_int && Int32.TryParse(token, out int i) && token.IndexOf(".") == -1)) match_token = true;
                if (!match_token && (this.string_token == eval_ufloat && Decimal.TryParse(token.Substring(1), out decimal uf) && token.IndexOf(".") != -1 && token[0] == 'u')) match_token = true;
                if (!match_token && (this.string_token == eval_uint && Int32.TryParse(token.Substring(1), out int ui) && token.IndexOf(".") == -1 && token[0] == 'u')) match_token = true;
            }

            if (match_token && match_peek && match_previous) return true;

            return false;
        }

        static bool IsValidIdentifier(string token)
        {
            if (string.IsNullOrEmpty(token) || token.Length == 0) return false;

            if (disallowed_start.IsMatch(token[0].ToString())) return false;
            if (disallowed_characters.IsMatch(token)) return false;

            return true;
        }

        static Regex disallowed_characters = new Regex("[!@#%^&*()[\\]{}\\\\/]"); // disallowed characters in a variable name
        static Regex disallowed_start = new Regex("(\\d)"); // disallowed start of a variable name
        public static string any_string = "$ ANYSTRING"; // if the string is any string
        public static string valid_variable = "$ VALIDVAR"; // check if the string is a valid variable
        public static string eval_int = "$ EVALINT"; // if the string parses to an valid integer
        public static string eval_float = "$ EVALFLOAT"; // if the string parses to a valid float
        public static string eval_string = "$ EVALSTRING"; // if the string starts and ends with a quotation mark (strings are properly created later)
        public static string eval_uint = "$ EVALUINT"; // if the string parses to an valid integer
        public static string eval_ufloat = "$ EVALUFLOAT"; // if the string parses to a valid float
    }

}