using System;
using System.Collections.Generic;
using KeplerInterpreter;
using KeplerVariables;
using KeplerExceptions;
using KeplerTokenizer;

namespace KeplerTracing
{
    public class KeplerTracer
    {
        List<KeplerTrace> traces = new List<KeplerTrace>();
        // Interpreter base_interpreter = 

        void Trace()
        {

        }

        void RemoveTrace()
        {

        }

        // public void TraceFunction(KeplerFunction function)
        // {
        //     this.traces.Add(new KeplerTrace(function.name, function));
        // }

        void GetTraceRoute()
        {

        }

    }

    class KeplerTrace
    {
        string name = "$NULL";
        string filename = "$NULL";
        int line = -1;

        public KeplerTrace(string name, int line)
        {

        }

        public override string ToString()
        {
            return String.Format("at {0} <{1}:{2}>", this.name, this.filename, this.line);
        }
    }

    public class KeplerErrorStack
    {
        List<string> stack = new List<string>();

        public int PushStack(string n)
        {
            this.stack.Insert(0, n);
            return this.stack.Count - 1;
        }

        public void PopStack(int index)
        {
            this.stack.Remove(this.stack[index]);
        }

        public string GetStack()
        {
            string stacked = "";
            this.stack.ForEach(str => stacked = stacked + "\r\n\t" + str);
            return stacked;
        }

        // public void Throw(string message)
        // {
        //     throw new KeplerException(current_line, message, this);
        // }
    }
}