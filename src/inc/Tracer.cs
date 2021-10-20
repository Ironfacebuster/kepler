using System;
using System.Collections.Generic;

namespace Kepler.Tracing
{
    // public class KeplerTracer
    // {
    //     List<KeplerTrace> traces = new List<KeplerTrace>();
    //     // Interpreter base_interpreter = 

    //     void Trace()
    //     {

    //     }

    //     void RemoveTrace()
    //     {

    //     }

    //     // public void TraceFunction(KeplerFunction function)
    //     // {
    //     //     this.traces.Add(new KeplerTrace(function.name, function));
    //     // }

    //     void GetTraceRoute()
    //     {

    //     }

    // }

    // class KeplerTrace
    // {
    //     string name = "$NULL";
    //     string filename = "$NULL";
    //     int line = -1;

    //     public KeplerTrace(string name, int line)
    //     {

    //     }

    //     public override string ToString()
    //     {
    //         return String.Format("at {0} <{1}:{2}>", this.name, this.filename, this.line);
    //     }
    // }

    public class KeplerErrorStack
    {
        List<KeplerTrace> stack = new List<KeplerTrace>();

        public int PushStack(string n)
        {
            if (this.stack.Count > 0 && this.stack[0].message == n) this.stack[0] = this.stack[0].Increment();
            else this.stack.Insert(0, new KeplerTrace(n, 1));

            return this.stack.Count - 1;
        }

        public void PopStack(int id)
        {
            // TODO: better "id" system
            if (id == this.stack.Count) id = id - 1;

            if (this.stack[id].count > 1) this.stack[id].Decrement();
            else this.stack.Remove(this.stack[id]);
        }

        public string GetStack()
        {
            string stacked = "";
            int i = 0;
            int max = Math.Min(10, this.stack.Count);

            while (i < max)
            {
                stacked = stacked + "\r\n\t" + this.stack[i].ToString();
                i++;
            }

            return stacked;
        }
    }

    struct KeplerTrace
    {
        public int count;
        public string message;

        public KeplerTrace(string message, int count)
        {
            this.message = message;
            this.count = count;
        }

        public override string ToString()
        {
            string m = this.message;
            if (this.count > 1) m = m + " x" + count;

            return m;
        }

        public KeplerTrace Increment()
        {
            return new KeplerTrace(this.message, this.count + 1);
        }

        public KeplerTrace Decrement()
        {
            return new KeplerTrace(this.message, this.count - 1);
        }
    }
}