using System.Collections.Generic;
using Kepler.Lexer;
using Kepler.Interpreting;
using System.Diagnostics;
using System;
using System.Linq;
using Kepler.Exceptions;

namespace KeplerVariables
{

    public class KeplerVariableManager
    {
        // public IDictionary<string, KeplerVariable> global = new Dictionary<string, KeplerVariable>();
        public KeplerVariableManager global;
        public IDictionary<string, KeplerVariable> local = new Dictionary<string, KeplerVariable>();

        public KeplerVariableManager()
        {
            this.global = this;
        }

        public KeplerVariable DeclareVariable(string name, bool global_override)
        {
            if (global.local.ContainsKey(name)) return global.local[name];
            if (local.ContainsKey(name)) return local[name];

            // variables are created the first time they're seen
            KeplerVariable n_var = new KeplerVariable();
            if (global_override) global.local[name] = n_var;
            else local[name] = n_var;
            return n_var;
        }

        public KeplerVariable GetVariable(string name)
        {
            if (global.local.ContainsKey(name))
            {
                // Console.WriteLine("GLOBAL VAR " + name);
                KeplerVariable found = global.local[name];

                if (found.type == KeplerType.Unassigned) throw new KeplerError(KeplerErrorCode.UNASSIGNED_TYPE, new string[] { name });

                return global.local[name];
            }
            if (local.ContainsKey(name))
            {
                // Console.WriteLine("LOCAL VAR " + name);
                KeplerVariable found = local[name];

                if (found.type == KeplerType.Unassigned) throw new KeplerError(KeplerErrorCode.UNASSIGNED_TYPE, new string[] { name });

                return local[name];
            }

            throw new KeplerError(KeplerErrorCode.UNDECLARED, new string[] { name });
        }

        public KeplerVariable GetVariableByID(string id)
        {
            // search local first
            foreach (KeyValuePair<string, KeplerVariable> var in this.local)
            {
                Console.WriteLine(var.Value);
                if (var.Value.id == id) return var.Value;
            }

            if (this.global != this) return this.global.GetVariableByID(id);

            throw new KeplerError(KeplerErrorCode.GENERIC_ERROR, new string[] { string.Format("#{0} is not a valid variable ID!", id) });
        }

        // allow copying of global for "scoping"
        public KeplerVariableManager Copy()
        {
            KeplerVariableManager copy = new KeplerVariableManager();
            copy.global = this.global;
            copy.local = new Dictionary<string, KeplerVariable>(this.local);
            return copy;
        }

        public override string ToString()
        {
            string output = "VariableManager\r\n";
            foreach (KeyValuePair<string, KeplerVariable> pair in global.local)
            {
                output = output + "GLOBAL " + pair.Key + " => " + pair.Value + "\r\n";
            }
            foreach (KeyValuePair<string, KeplerVariable> pair in local)
            {
                output = output + "LOCAL " + pair.Key + " => " + pair.Value + "\r\n";
            }
            return output;
            // return base.ToString();
        }
    }

    public class KeplerVariable
    {
        public string id = "";
        public KeplerType type = KeplerType.Unassigned;
        public KeplerModifier modifier = KeplerModifier.Variable;
        public decimal FloatValue = 0;
        public uint uIntValue = 0;
        public int IntValue = 0;
        public string StringValue = "";
        public bool BoolValue = false;

        public KeplerVariable()
        {
            this.id = String.Format("{0:X}", DateTime.Now.Ticks);
        }

        public void SetModifier(KeplerModifier modifier)
        {
            ValidateConstant();
            this.modifier = modifier;
        }

        public void SetType(KeplerType type)
        {
            ValidateConstant();
            this.type = type;
        }

        public void SetFloatValue(decimal value)
        {
            ValidateType(KeplerType.Float);
            FloatValue = value;
        }

        public void SetIntValue(int value)
        {
            ValidateType(KeplerType.Int);
            IntValue = value;
        }

        public void SetUnsignedIntValue(uint value)
        {
            ValidateType(KeplerType.uInt);
            uIntValue = value;
        }

        public void SetBoolValue(bool value)
        {
            ValidateType(KeplerType.Boolean);
            BoolValue = value;
        }

        public void SetStringValue(string value)
        {
            ValidateType(KeplerType.String);

            // convert escape characters
            StringValue = /* ToLiteral(value); */ value;
        }

        public void AssignValue(KeplerVariable variable)
        {
            ValidateConstant();
            ValidateType(variable.type);

            this.type = variable.type;

            switch (this.type)
            {
                case KeplerType.Float:
                    this.SetFloatValue(variable.FloatValue);
                    break;
                case KeplerType.Int:
                    this.SetIntValue(variable.IntValue);
                    break;
                case KeplerType.uInt:
                    this.SetUnsignedIntValue(variable.uIntValue);
                    break;
                case KeplerType.Boolean:
                    this.SetBoolValue(variable.BoolValue);
                    break;
                case KeplerType.String:
                    this.SetStringValue(variable.StringValue);
                    break;
            }
        }

        public KeplerVariable Clone()
        {
            KeplerVariable clone = new KeplerVariable();

            clone.type = type;

            clone.FloatValue = FloatValue;
            clone.IntValue = IntValue;
            clone.uIntValue = uIntValue;
            clone.BoolValue = BoolValue;
            clone.StringValue = StringValue;

            return clone;
        }

        private static string ToLiteral(string input)
        {
            return input.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\'", "\'").Replace("\\\"", "\"").Replace("\\", "\\");
        }

        public void ValidateType(KeplerType type)
        {
            ValidateConstant();
            if (this.type == KeplerType.Unassigned) this.type = type;
            if (this.type != type) throw new KeplerError(KeplerErrorCode.INVALID_TYPE_ASSIGN, new string[] { type.ToString(), this.type.ToString() });
        }
        public void ValidateConstant()
        {
            if (this.modifier == KeplerModifier.Constant) throw new KeplerError(KeplerErrorCode.ASSIGN_CONSTANT_VAR);
        }

        public override string ToString()
        {
            string output = string.Format("(#{0}) {1} {2}", this.id, this.modifier, this.type);

            switch (type)
            {
                case KeplerType.Float:
                    output = string.Format("{0} ({1})", output, FloatValue);
                    // output = output + "\r\nValue: " + FloatValue;
                    break;
                case KeplerType.Int:
                    output = string.Format("{0} ({1})", output, IntValue);
                    break;
                case KeplerType.uInt:
                    output = string.Format("{0} (u{1})", output, uIntValue);
                    break;
                case KeplerType.Boolean:
                    output = string.Format("{0} ({1})", output, BoolValue);
                    break;
                case KeplerType.String:
                    output = string.Format("{0} (\"{1}\")", output, StringValue);
                    break;
            }

            return output;
        }

        public string GetValueAsString()
        {
            switch (type)
            {
                case KeplerType.Float:
                    string str = FloatValue.ToString();
                    if (str.IndexOf(".") == -1) str = str + ".0";
                    return str;
                case KeplerType.Int:
                    return IntValue.ToString();
                case KeplerType.uInt:
                    return string.Format("u{0}", uIntValue);
                case KeplerType.Boolean:
                    return BoolValue.ToString();
                case KeplerType.String:
                    return StringValue;
                case KeplerType.NaN:
                    return "NaN";
            }

            throw new KeplerError(KeplerErrorCode.INVALID_CAST, new string[] { this.type.ToString(), "String" });
        }

        public int GetValueAsInt()
        {
            switch (type)
            {
                case KeplerType.Float:
                    return (int)FloatValue;
                case KeplerType.Int:
                    return IntValue;
                case KeplerType.uInt:
                    return (int)uIntValue;
                case KeplerType.Boolean:
                    return BoolValue ? 1 : 0;
                case KeplerType.String:
                    throw new KeplerError(KeplerErrorCode.INVALID_CAST, new string[] { this.type.ToString(), "Int" });
            }

            return 0;
        }

        public uint GetValueAsUnsignedInt()
        {
            switch (type)
            {
                case KeplerType.Float:
                    return (uint)FloatValue;
                case KeplerType.Int:
                    return (uint)IntValue;
                case KeplerType.uInt:
                    return uIntValue;
                case KeplerType.Boolean:
                    return (uint)(BoolValue ? 1 : 0);
                case KeplerType.String:
                    throw new KeplerError(KeplerErrorCode.INVALID_CAST, new string[] { this.type.ToString(), "uInt" });
            }

            return 0;
        }

        public decimal GetValueAsFloat()
        {
            switch (type)
            {
                case KeplerType.Float:
                    return FloatValue;
                case KeplerType.Int:
                    return IntValue;
                case KeplerType.uInt:
                    return uIntValue;
                case KeplerType.Boolean:
                    return BoolValue ? 1.0m : 0.0m;
                case KeplerType.String:
                    throw new KeplerError(KeplerErrorCode.INVALID_CAST, new string[] { this.type.ToString(), "Float" });
            }

            return 0;
        }

        public bool GetValueAsBool()
        {
            switch (type)
            {
                case KeplerType.Float:
                    return (int)FloatValue >= 1;
                case KeplerType.Int:
                    return IntValue >= 1;
                case KeplerType.uInt:
                    return uIntValue >= 1;
                case KeplerType.Boolean:
                    return BoolValue;
                case KeplerType.String:
                    throw new KeplerError(KeplerErrorCode.INVALID_CAST, new string[] { this.type.ToString(), "Boolean" });
            }

            return false;
        }
    }

    public class KeplerFunctionManager
    {
        public IDictionary<string, KeplerFunction> global = new Dictionary<string, KeplerFunction>();
        public IDictionary<string, KeplerFunction> local = new Dictionary<string, KeplerFunction>();

        public KeplerFunction DeclareFunction(string name, bool global_override)
        {
            if (global.ContainsKey(name)) return global[name];
            if (local.ContainsKey(name)) return local[name];

            // functions are created the first time they're seen
            KeplerFunction n_funct = new KeplerFunction(name);
            if (global_override) global[name] = n_funct;
            else local[name] = n_funct;
            return n_funct;
        }

        public KeplerFunction GetFunction(string name)
        {
            if (global.ContainsKey(name)) return global[name];
            if (local.ContainsKey(name)) return local[name];

            throw new KeplerError(KeplerErrorCode.UNDECLARED, new string[] { name });
        }

        public KeplerFunctionManager Copy()
        {
            KeplerFunctionManager copy = new KeplerFunctionManager();
            copy.global = new Dictionary<string, KeplerFunction>(this.global);
            copy.local = new Dictionary<string, KeplerFunction>(this.local);
            return copy;
        }

        public override string ToString()
        {
            string output = "FunctionManager\r\n";
            foreach (KeyValuePair<string, KeplerFunction> pair in global)
            {
                output = output + "GLOBAL " + pair.Key + " => " + pair.Value + "\r\n";
            }
            foreach (KeyValuePair<string, KeplerFunction> pair in local)
            {
                output = output + "LOCAL " + pair.Key + " => " + pair.Value + "\r\n";
            }
            return output;
            // return base.ToString();
        }
    }

    public class KeplerFunction
    {
        public List<LineIterator> lines = new List<LineIterator>();
        public IDictionary<string, KeplerType> positional_arguments = new Dictionary<string, KeplerType>(); // name = type, assigned by position
        public IDictionary<string, KeplerType> non_positional_arguments = new Dictionary<string, KeplerType>(); // name = type, assigned by name ref
        public IDictionary<string, KeplerVariable> arguments = new Dictionary<string, KeplerVariable>();
        public KeplerVariable target = new KeplerVariable();
        bool has_target = false;
        public KeplerType type;
        public string name;
        public string id;

        public KeplerFunction(string name)
        {
            this.name = name;
            this.id = String.Format("{0:X}", DateTime.Now.Ticks);
        }

        public void SetType(KeplerType type)
        {
            this.type = type;
        }

        public bool HasArguments()
        {
            if (positional_arguments.Count > 0) return true;
            if (non_positional_arguments.Count > 0) return true;

            return false;
        }

        public void AddNonPositional(string name)
        {
            AssertNPArgumentNotExists(name);

            non_positional_arguments[name] = KeplerType.Unassigned;
        }

        public void AssignNonPositional(string name, KeplerType type)
        {
            AssertNPArgument(name);

            non_positional_arguments[name] = type;
        }

        public void ResetLines()
        {
            for (int i = 0; i < this.lines.Count; ++i)
            {
                this.lines[i].killed = false;
                this.lines[i].m_num = 0;
            }
        }
        public void Reset()
        {
            ResetLines();
            // foreach(LineIterator line in lines) line.m_num = 0;
            arguments = new Dictionary<string, KeplerVariable>(); // reset assigned arguments
            target = new KeplerVariable(); // reset target to empty variable
            has_target = true;
        }

        public void SetTarget(KeplerVariable target)
        {
            this.target = target;
            has_target = true;
        }

        public KeplerVariable GetTarget()
        {
            return this.target;
        }

        public bool HasTarget()
        {
            return this.has_target;
        }

        void AssertNPArgumentNotExists(string name)
        {
            if (non_positional_arguments.ContainsKey(name)) throw new KeplerError(KeplerErrorCode.DUP_NONPOS_ARG, new string[] { this.name, name });
        }

        void AssertNPArgument(string name)
        {
            if (!non_positional_arguments.ContainsKey(name)) throw new KeplerError(KeplerErrorCode.UNDECLARED_NONPOS_ARG, new string[] { this.name, name });
        }

        public override string ToString()
        {
            // string output = "KeplerFunction";
            // foreach (LineIterator line in lines)
            // {
            //     output = output + line + "\r\n";
            // }
            return string.Format("(#{0}) KeplerFunction {1}", id, type);
        }
    }

    public class KeplerInterrupt
    {
        Stopwatch stopWatch = new Stopwatch();

        public int id = -1; // unassigned ID
        int interval = -1; // unassigned interval
        long last_check = 0;
        long desired_time = 0;
        bool validated = false;
        public KeplerFunction function;
        public Interpreter parent;
        bool disabled = false;
        public KeplerInterrupt(int id, KeplerFunction function, Interpreter parent)
        {
            this.id = id;
            this.parent = parent;
            // this.interval = ms_interval;
            // this.Reset(); // assign last_check
            this.function = function;
        }

        public void Reset()
        {
            this.last_check = this.stopWatch.ElapsedMilliseconds;
            this.desired_time = this.last_check + this.interval;
        }

        public void SetInterval(int interval)
        {
            interval = Math.Max(1, interval);
            if (!this.validated)
            {
                this.stopWatch.Start();
                this.validated = true;
            }
            this.interval = interval;
            this.Reset();
        }

        public void SetForever()
        {
            this.interval = -2;
            this.Reset();
            this.validated = true;
        }

        public bool isValidInterrupt()
        {
            if (this.disabled) return false;
            if (!this.validated) return false;
            if (this.interval == -2) return true;

            return this.stopWatch.ElapsedMilliseconds >= this.desired_time;
        }

        public int Overage()
        {
            return (int)(this.stopWatch.ElapsedMilliseconds - this.last_check);
        }


        public void Disable()
        {
            // if (verbose_debug) Console.WriteLine("DISABLE " + this.id);
            this.disabled = true;
        }

        public bool IsDisabled()
        {
            return this.disabled;
        }

        public bool isInfinite()
        {
            return this.interval == -2;
        }
    }

    public class KeplerInterruptManager
    {
        public List<KeplerInterrupt> interrupts = new List<KeplerInterrupt>();
        // public Interpreter global;

        public void Add(KeplerInterrupt interrupt)
        {
            this.interrupts.Insert(0, interrupt);
        }

        public bool HasAnyInterrupts()
        {
            this.CleanInterrupts();
            return this.interrupts.Count > 0;
        }
        public bool HasInterrupts()
        {
            for (int i = 0; i < this.interrupts.Count; ++i)
            {
                if (this.interrupts[i].isValidInterrupt()) return true;
            }

            return false;
        }

        public bool HasInterrupt(int id)
        {
            for (int i = 0; i < this.interrupts.Count; ++i)
            {
                if (this.interrupts[i].id == id && this.interrupts[i].isValidInterrupt()) return true;
            }

            return false;
        }

        public KeplerInterrupt GetInterrupt(int id)
        {
            for (int i = 0; i < this.interrupts.Count; ++i)
                if (this.interrupts[i].id == id) return this.interrupts[i];

            // if (this.has_parent)
            //     return this.parent.GetInterrupt(id);

            throw new KeplerError(KeplerErrorCode.UNDECLARED_INT_ID, new string[] { id.ToString() });
        }

        public List<KeplerInterrupt> GetInterrupts()
        {
            this.CleanInterrupts();

            bool added_infinite = false;
            List<KeplerInterrupt> valid_interrupts = new List<KeplerInterrupt>();

            int i = 0;
            while (i < this.interrupts.Count)
            {
                if (this.interrupts[i].isValidInterrupt())
                {
                    if (this.interrupts[i].isInfinite())
                    {
                        if (!added_infinite)
                        {
                            // Console.WriteLine("ADDING " + this.interrupts[i].id);
                            valid_interrupts.Add(this.interrupts[i]);
                            added_infinite = true;
                        }
                        // else Console.WriteLine("SKIPPING " + this.interrupts[i].id);
                    }
                    else valid_interrupts.Add(this.interrupts[i]);
                }
                i++;
            }

            return valid_interrupts.ToList();
        }

        void CleanInterrupts()
        {
            int i = 0;
            while (i < this.interrupts.Count)
            {
                if (this.interrupts[i].IsDisabled())
                {
                    // Console.WriteLine("REMOVING " + this.interrupts[i].id);
                    this.interrupts.RemoveAt(i);
                }
                else i++;
            }
        }

        public int Count
        {
            get { return this.interrupts.Count; }
        }
    }

    public enum KeplerModifier
    {
        Variable,
        Constant
    }

    public enum KeplerType
    {
        Float,
        Int,
        uFloat,
        uInt,
        String,
        Function,
        List,
        Array,
        Boolean,
        Unassigned,
        NaN
    }
}