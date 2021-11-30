/*
 *   Copyright (c) 2021 William Huddleston
 *   All rights reserved.
 *   License: Apache 2.0
 */

using Kepler.Lexer;
using Kepler.Tracing;
using System;

namespace Kepler.Exceptions
{
    public class KeplerException : Exception
    {
        public LineIterator line;
        public string message;
        public KeplerErrorStack stack;
        public int token_offset = 0;
        public KeplerException(LineIterator line, string message, KeplerErrorStack stack)
        {
            this.line = line;
            this.message = message;
            this.stack = stack;
        }

        public KeplerException(LineIterator line, string message, KeplerErrorStack stack, int token_offset)
        {
            this.line = line;
            this.message = message;
            this.stack = stack;
            this.token_offset = token_offset;
        }
    }

    public class KeplerError : Exception
    {
        public KeplerErrorCode code;
        String[] args;
        public KeplerError(KeplerErrorCode ErrorCode)
        {
            this.code = ErrorCode;
        }
        public KeplerError(KeplerErrorCode ErrorCode, String[] args)
        {
            this.code = ErrorCode;
            this.args = args;
        }
        public string GetErrorString()
        {
            string error_string = "Unknown Error";

            switch (this.code)
            {
                case KeplerErrorCode.INVAL_FILE:
                    error_string = "This file is not a valid .kep file.";
                    break;
                case KeplerErrorCode.MAL_STRING:
                    error_string = "Tokenizer Error: Malformed string.";
                    break;
                case KeplerErrorCode.UNEXP_EOL:
                    error_string = "Syntax Error: Unexpected EOL.";
                    break;
                case KeplerErrorCode.UNEXP_START_LOOP:
                    error_string = "Syntax Error: Unexpected start of forever.";
                    break;
                case KeplerErrorCode.UNEXP_END_LOOP:
                    error_string = "Syntax Error: Unexpected end of forever.";
                    break;
                case KeplerErrorCode.UNEXP_START_INT:
                    error_string = "Syntax Error: Unexpected start of interval.";
                    break;
                case KeplerErrorCode.UNEXP_END_INT:
                    error_string = "Syntax Error: Unexpected end of interval.";
                    break;
                case KeplerErrorCode.UNEXP_START_COND:
                    error_string = "Syntax Error: Unexpected start of conditional.";
                    break;
                case KeplerErrorCode.UNEXP_END_COND:
                    error_string = "Syntax Error: Unexpected end of conditional.";
                    break;
                case KeplerErrorCode.LINK_OUT_HEADER:
                    error_string = "Syntax Error: Cannot link module/file outside of Header.";
                    break;
                case KeplerErrorCode.UNEXP_RETURN:
                    error_string = "Syntax Error: Unexpected return, nothing to return from.";
                    break;
                case KeplerErrorCode.UNEXP_BREAKOUT:
                    error_string = "Syntax Error: Unexpected breakout, nothing to break out of.";
                    break;
                case KeplerErrorCode.UNEXP_START_TOKEN: // fallthrough
                case KeplerErrorCode.UNEXP_TOKEN:
                    error_string = string.Format("Syntax Error: Unexpected token \"{0}\"", this.args[0]);
                    break;
                case KeplerErrorCode.CALL_UNDEF_FUNCT_TYPE:
                    error_string = string.Format("Syntax Error: Cannot call {0} without a defined return type.", this.args[0]);
                    break;
                case KeplerErrorCode.ASSIGN_UNDEF_FUNCT_TYPE:
                    error_string = string.Format("Syntax Error: Cannot assign to {1} as {0} does not have a defined return type.", this.args[0], this.args[1]);
                    break;
                case KeplerErrorCode.DUP_NONPOS_ARG:
                    error_string = string.Format("Syntax Error: {0} already has a non-positional argument named \"{1}\"", this.args[0], this.args[1]);
                    break;
                case KeplerErrorCode.UNDECLARED_NONPOS_ARG:
                    error_string = string.Format("Syntax Error: {0} does not have a non-positional argument named \"{1}\"", this.args[0], this.args[1]);
                    break;
                case KeplerErrorCode.UNASSIGNED_NONPOS_ARG:
                    error_string = string.Format("Syntax Error: {0} has not had argument \"{1}\" assigned!", this.args[0], this.args[1]);
                    break;
                case KeplerErrorCode.INVALID_TYPE_ASSIGN:
                    error_string = string.Format("Type Error: Attempted to assign type {0} to {1}.", this.args[0], this.args[1]);
                    break;
                case KeplerErrorCode.STRICT_TYPE_EQUALITY:
                    error_string = string.Format("Type Error: Cannot strictly compare type {0} to {1}.", this.args[0], this.args[1]);
                    break;
                case KeplerErrorCode.ASSIGN_CONSTANT_VAR:
                    error_string = "Type Error: Assignment to constant variable.";
                    break;
                case KeplerErrorCode.EXPLICIT_CAST:
                    error_string = string.Format("Type Error: Unable to explicitly cast type {0} to {1}!", this.args[0], this.args[1]);
                    break;
                case KeplerErrorCode.IMPLICIT_CAST:
                    error_string = string.Format("Type Error: Unable to implicitly cast type {0} to {1}!", this.args[0], this.args[1]);
                    break;
                case KeplerErrorCode.UNASSIGNED_TYPE:
                    error_string = string.Format("Type Error: \"{0}\" does not have a defined type.", this.args[0]);
                    break;
                case KeplerErrorCode.UNEXP_EOP:
                    error_string = "Error: Unexpected end of program.";
                    break;
                case KeplerErrorCode.NULL_TEMP_VAR:
                    error_string = string.Format("Error: Unable to create temporary variable for TokenType {0}.", this.args[0]);
                    break;
                case KeplerErrorCode.UNDECLARED:
                    error_string = string.Format("Declaration Error: \"{0}\" has not been declared yet.", this.args[0]);
                    break;
                case KeplerErrorCode.DECLARE_DUP:
                    error_string = string.Format("Declaration Error: \"{0}\" has already been declared.", this.args[0]);
                    break;
                case KeplerErrorCode.UNDECLARED_INT_ID:
                    error_string = string.Format("Interrupt Error: Unable to find interrupt with ID {0}.", this.args[0]);
                    break;
                case KeplerErrorCode.FALSE_ASSERTION:
                    error_string = string.Format("Assertion Error: \r\n\tactual: {0}\r\n\texpected: True", this.args[0]);
                    break;
                case KeplerErrorCode.GENERIC_ERROR:
                    error_string = string.Format("Error: {0}", this.args[0]);
                    break;
            }

            // return string.Format("{1}", this.code, error_string);
            return error_string;
        }

        public int GetTokenOffset()
        {
            switch (this.code)
            {
                case KeplerErrorCode.UNEXP_EOL:
                case KeplerErrorCode.UNEXP_EOP:
                case KeplerErrorCode.UNEXP_TOKEN:
                    return 1;
                case KeplerErrorCode.ASSIGN_CONSTANT_VAR:
                    return -1;
            }

            return 0;
        }
    }

    public enum KeplerErrorCode
    {
        INVAL_FILE,
        UNEXP_EOL,
        UNEXP_EOP,
        UNEXP_START_TOKEN,
        UNEXP_TOKEN,
        UNEXP_START_COND,
        UNEXP_END_COND,
        UNEXP_START_LOOP,
        UNEXP_END_LOOP,
        UNEXP_START_INT,
        UNEXP_END_INT,
        UNDECLARED_INT_ID,
        LINK_OUT_HEADER,
        UNEXP_RETURN,
        UNEXP_BREAKOUT,
        CALL_UNDEF_FUNCT_TYPE,
        ASSIGN_UNDEF_FUNCT_TYPE,
        NULL_TEMP_VAR,
        INVALID_TYPE_ASSIGN,
        ASSIGN_CONSTANT_VAR,
        STRICT_TYPE_EQUALITY,
        EXPLICIT_CAST,
        IMPLICIT_CAST,
        UNDECLARED,
        UNASSIGNED_TYPE,
        DUP_NONPOS_ARG,
        UNDECLARED_NONPOS_ARG,
        UNASSIGNED_NONPOS_ARG,
        DECLARE_DUP,
        FALSE_ASSERTION,
        GENERIC_ERROR,
        MAL_STRING
    }
}