using System;
using KeplerTracing;
using KeplerTokenizer;

namespace KeplerExceptions
{
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

    public class GenericException : Exception
    {
        // public string Message;
        public int token_offset;
        public GenericException(string message, int token_offset) : base(message)
        {
            // this.Message = message;
            this.token_offset = token_offset;
        }

    }

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
}