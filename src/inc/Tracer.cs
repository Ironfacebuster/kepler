using System;
using System.Text;
using System.Collections.Generic;
using Kepler.Exceptions;

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
        // Dictionary<string, KeplerTrace> stack = new Dictionary<string, KeplerTrace>();
        static Random gen = new Random();

        public string PushStack(string n)
        {
            string id = new Guid(CreateMD5(n)).ToString();
            bool had_already = false;

            for (int i = 0; i < stack.Count; i++)
            {
                if (stack[i].ID == id)
                {
                    this.stack[i].Increment();
                    had_already = true;
                }
            }

            if (!had_already)
            {
                stack.Add(new KeplerTrace(id, n, 1));
            }

            // if (this.stack.ContainsKey(id))
            //     this.stack[id] = this.stack[id].Increment();
            // else
            //     this.stack[id] = new KeplerTrace(n, 1);

            return id;
        }

        public void PopStack(string id)
        {
            bool found_key = false;
            for (int i = 0; i < stack.Count; ++i)
            {
                if (stack[i].ID == id)
                {
                    if (this.stack[i].count > 1)
                        this.stack[i].Decrement();
                    else
                        stack.RemoveAt(i);

                    found_key = true;
                }
            }

            if (!found_key) throw new KeplerError(KeplerErrorCode.GENERIC_ERROR, new string[] { "ID not found in tracer stack." });
        }

        public string GetStack()
        {
            string stacked = "";
            // int i = 0;
            int max = Math.Min(10, this.stack.Count - 1);

            for (int i = max; i >= 0; --i)
            {
                stacked = stacked + "\r\n\t" + this.stack[i].ToString();
            }

            return stacked;
        }

        static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }

                md5.Dispose();
                return sb.ToString();
            }
        }
    }

    struct KeplerTrace
    {
        public string ID;
        public int count;
        public string message;

        public KeplerTrace(string ID, string message, int count)
        {
            this.ID = ID;
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
            return new KeplerTrace(this.ID, this.message, this.count + 1);
        }

        public KeplerTrace Decrement()
        {
            return new KeplerTrace(this.ID, this.message, this.count - 1);
        }
    }
}