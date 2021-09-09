using System;
using KeplerTracing;
using KeplerTokenizer;

namespace KeplerExceptions
{
    // public class TokenException : Exception
    // {
    //     public TokenException() { }

    //     public TokenException(string message)
    //         : base(message) { }

    //     public TokenException(string message, Exception inner)
    //         : base(message, inner) { }
    // }
    // public class LevelOneException : Exception
    // {
    //     public LevelOneException() { }

    //     public LevelOneException(string message)
    //         : base(message) { }

    //     public LevelOneException(string message, Exception inner)
    //         : base(message, inner) { }
    // }
    // public class InterpreterException : Exception
    // {

    //     public InterpreterException() { }

    //     public InterpreterException(string message)
    //         : base(message) { }

    //     public InterpreterException(string message, Exception inner)
    //         : base(message, inner) { }
    // }
    // public class EOPException : Exception
    // {
    //     public EOPException(string message)
    //         : base(message) { }
    // }
    // public class LinkedFileException : Exception
    // {
    //     public LinkedFileException() { }
    //     public LinkedFileException(string message)
    //         : base(message) { }
    //     public LinkedFileException(string message, Exception inner)
    //         : base(message, inner) { }
    // }
    // public class GenericException : Exception
    // {
    //     // public string Message;
    //     public int token_offset;
    //     public GenericException(string message, int token_offset) : base(message)
    //     {
    //         // this.Message = message;
    //         this.token_offset = token_offset;
    //     }

    // }

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
            switch (this.code)
            {
                case KeplerErrorCode.INVAL_FILE:
                    return string.Format("[{0}] This file is not a valid .kep file.", this.code);
                case KeplerErrorCode.UNEXP_EOL:
                    return string.Format("[{0}] Syntax Error: Unexpected EOL.", this.code);
                case KeplerErrorCode.UNEXP_START_LOOP:
                    return string.Format("[{0}] Syntax Error: Unexpected start of forever.", this.code);
                case KeplerErrorCode.UNEXP_END_LOOP:
                    return string.Format("[{0}] Syntax Error: Unexpected start of forever.", this.code);
                case KeplerErrorCode.UNEXP_START_INT:
                    return string.Format("[{0}] Syntax Error: Unexpected start of interval.", this.code);
                case KeplerErrorCode.UNEXP_END_INT:
                    return string.Format("[{0}] Syntax Error: Unexpected start of interval.", this.code);
                case KeplerErrorCode.UNEXP_START_COND:
                    return string.Format("[{0}] Syntax Error: Unexpected start of conditional.", this.code);
                case KeplerErrorCode.UNEXP_END_COND:
                    return string.Format("[{0}] Syntax Error: Unexpected start of conditional.", this.code);
                case KeplerErrorCode.LINK_OUT_HEADER:
                    return string.Format("[{0}] Syntax Error: Cannot link file outside of Header.", this.code);
                case KeplerErrorCode.NULL_BREAKOUT:
                    return string.Format("[{0}] Syntax Error: Unexpected breakout, nothing to break out of.", this.code);
                case KeplerErrorCode.UNEXP_START_TOKEN: // fallthrough
                case KeplerErrorCode.UNEXP_TOKEN:
                    return string.Format("[{0}] Syntax Error: Unexpected token \"{1}\"", this.code, this.args[0]);
                case KeplerErrorCode.UNDECLARED_INT_ID:
                    return string.Format("[{0}] Interrupt Error: Unable to find interrupt with ID {1}.", this.code, this.args[0]);
                case KeplerErrorCode.UNEXP_EOP:
                    return string.Format("[{0}] Error: Unexpected end of program.", this.code);
                case KeplerErrorCode.CALL_UNDEF_FUNCT_TYPE:
                    return string.Format("[{0}] Error: Cannot call {1} without a defined return type.", this.code, this.args[0]);
                case KeplerErrorCode.ASSIGN_UNDEF_FUNCT_TYPE:
                    return string.Format("[{0}] Error: Cannot assign to {2} as {1} does not have a defined return type.", this.code, this.args[0], this.args[1]);
                case KeplerErrorCode.NULL_TEMP_VAR:
                    return string.Format("[{0}] Error: Unable to create temporary variable for TokenType {1}.", this.code, this.args[0]);
                case KeplerErrorCode.DUP_NONPOS_ARG:
                    return string.Format("[{0}] Error: {1} already has a non-positional argument named \"{2}\"", this.code, this.args[0], this.args[1]);
                case KeplerErrorCode.UNDECLARED_NONPOS_ARG:
                    return string.Format("[{0}] Error: {1} does not have a non-positional argument named \"{2}\"", this.code, this.args[0], this.args[1]);
                case KeplerErrorCode.INVALID_TYPE_ASSIGN:
                    return string.Format("[{0}] Type Error: Invalid type assignment {1} to {2}.", this.code, this.args[0], this.args[1]);
                case KeplerErrorCode.ASSIGN_CONSTANT_VAR:
                    return string.Format("[{0}] Type Error: Assignment to constant variable.", this.code);
                case KeplerErrorCode.INVALID_CAST:
                    return string.Format("[{0}] Type Error: Unable to cast type {1} to {2}!", this.code, this.args[0], this.args[1]);
                case KeplerErrorCode.UNASSIGNED_TYPE:
                    return string.Format("[{0}] Type Error: {1} does not have a defined type.", this.code, this.args[0]);
                case KeplerErrorCode.UNDECLARED:
                    return string.Format("[{0}] Declaration Error: {1} has not yet been declared.", this.code, this.args[0]);
                case KeplerErrorCode.DECLARE_DUP:
                    return string.Format("[{0}] Declaration Error: {1} has already been declared.", this.code, this.args[0]);
            }

            return string.Format("Unexpected Error {0}", this.code);
        }

        public int GetTokenOffset()
        {
            switch (this.code)
            {
                case KeplerErrorCode.UNEXP_EOL:
                case KeplerErrorCode.UNEXP_EOP:
                case KeplerErrorCode.UNEXP_TOKEN:
                    return 1;
                    // case KeplerErrorCode.INVAL_FILE:
                    // case KeplerErrorCode.UNEXP_START_LOOP:
                    // case KeplerErrorCode.UNEXP_END_LOOP:
                    // case KeplerErrorCode.UNEXP_START_INT:
                    // case KeplerErrorCode.UNEXP_END_INT:
                    // case KeplerErrorCode.UNEXP_START_COND:
                    // case KeplerErrorCode.UNEXP_END_COND:
                    // case KeplerErrorCode.LINK_OUT_HEADER:
                    // case KeplerErrorCode.NULL_BREAKOUT:
                    // case KeplerErrorCode.CALL_UNDEF_FUNCT_TYPE:
                    // case KeplerErrorCode.ASSIGN_UNDEF_FUNCT_TYPE:
                    // case KeplerErrorCode.NULL_TEMP_VAR:
                    // case KeplerErrorCode.UNEXP_START_TOKEN: // fallthrough
                    // case KeplerErrorCode.UNEXP_TOKEN:
                    // case KeplerErrorCode.INVALID_TYPE_ASSIGN:
                    // case KeplerErrorCode.ASSIGN_CONSTANT_VAR:
                    // case KeplerErrorCode.INVALID_CAST:
                    // case KeplerErrorCode.UNDECLARED:
                    // case KeplerErrorCode.UNASSIGNED_TYPE:
                    // case KeplerErrorCode.DUP_NONPOS_ARG:
                    // case KeplerErrorCode.UNDECLARED_NONPOS_ARG:
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
        NULL_BREAKOUT,
        CALL_UNDEF_FUNCT_TYPE,
        ASSIGN_UNDEF_FUNCT_TYPE,
        NULL_TEMP_VAR,
        INVALID_TYPE_ASSIGN,
        ASSIGN_CONSTANT_VAR,
        INVALID_CAST,
        UNDECLARED,
        UNASSIGNED_TYPE,
        DUP_NONPOS_ARG,
        UNDECLARED_NONPOS_ARG,
        DECLARE_DUP
    }
}