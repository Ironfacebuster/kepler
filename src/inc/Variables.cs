using Kepler.Exceptions;
using Kepler.Interpreting;
using Kepler.Lexer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

        public void Load(KeyValuePair<string, KeplerVariable> var_pair)
        {
            if (this.GetVariable(var_pair.Key, true) != null)
                throw new KeplerError(KeplerErrorCode.DECLARE_DUP, new string[] { var_pair.Key });

            this.local.Add(var_pair.Key, var_pair.Value);
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

        public KeplerVariable GetVariable(string name, bool no_error = false)
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

            if (!no_error)
                throw new KeplerError(KeplerErrorCode.UNDECLARED, new string[] { name });
            else
                return null;
        }

        public KeplerVariable GetVariableByID(string id)
        {
            // search local first
            foreach (KeyValuePair<string, KeplerVariable> var in this.local)
            {
                // Console.WriteLine(var.Value);
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
            string output = "Variables\r\nScope: " + (this.global == this.local ? "global" : "local") + "\r\n";
            foreach (KeyValuePair<string, KeplerVariable> pair in global.local)
            {
                output = output + pair.Key + " => " + pair.Value + "\r\n";
            }

            return output;
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
            this.id = Guid.NewGuid().ToString("N");
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
            StringValue = ToLiteral(value);
            // StringValue = value;
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
            if (this.type == KeplerType.Unassigned) this.type = type;
            // ValidateConstant();
            if (this.type != type && (this.type != KeplerType.Any && type != KeplerType.Any)) throw new KeplerError(KeplerErrorCode.INVALID_TYPE_ASSIGN, new string[] { this.type.ToString(), type.ToString() });
        }
        public void ValidateConstant()
        {
            if (this.modifier == KeplerModifier.Constant) throw new KeplerError(KeplerErrorCode.ASSIGN_CONSTANT_VAR);
        }

        public override string ToString()
        {
            string output = string.Format("({0}) {1} {2}", this.id, this.modifier, this.type);

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

        public string GetValueAsString(bool is_explicit = false)
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

            throw new KeplerError(is_explicit ? KeplerErrorCode.EXPLICIT_CAST : KeplerErrorCode.IMPLICIT_CAST, new string[] { this.type.ToString(), "String" });
        }

        public int GetValueAsInt(bool is_explicit = false)
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
                    decimal new_float = 0;
                    if (decimal.TryParse(this.StringValue, out new_float)) return (int)new_float;
                    break;
            }

            throw new KeplerError(is_explicit ? KeplerErrorCode.EXPLICIT_CAST : KeplerErrorCode.IMPLICIT_CAST, new string[] { this.type.ToString(), "Int" });
        }

        public uint GetValueAsUnsignedInt(bool is_explicit = false)
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
                    decimal new_float = 0;
                    if (decimal.TryParse(this.StringValue, out new_float)) return (uint)new_float;
                    break;
            }

            throw new KeplerError(is_explicit ? KeplerErrorCode.EXPLICIT_CAST : KeplerErrorCode.IMPLICIT_CAST, new string[] { this.type.ToString(), "uInt" });
        }

        public decimal GetValueAsFloat(bool is_explicit = false)
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
                    decimal new_float = 0;
                    if (decimal.TryParse(this.StringValue, out new_float)) return new_float;
                    break;

            }

            throw new KeplerError(is_explicit ? KeplerErrorCode.EXPLICIT_CAST : KeplerErrorCode.IMPLICIT_CAST, new string[] { this.type.ToString(), "Float" });
        }

        public bool GetValueAsBool(bool is_explicit = false)
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
                    if (this.StringValue.ToLower() == "true") return true;
                    if (this.StringValue.ToLower() == "false") return false;
                    break;
            }

            throw new KeplerError(is_explicit ? KeplerErrorCode.EXPLICIT_CAST : KeplerErrorCode.IMPLICIT_CAST, new string[] { this.type.ToString(), "Boolean" });
        }
    }

    public class KeplerFunctionManager
    {
        public IDictionary<string, KeplerFunction> global = new Dictionary<string, KeplerFunction>();
        public IDictionary<string, KeplerFunction> local = new Dictionary<string, KeplerFunction>();

        public KeplerFunction DeclareFunction(string name, bool global_override, bool is_internal = false)
        {
            if (global.ContainsKey(name)) return global[name];
            if (local.ContainsKey(name)) return local[name];

            // functions are created the first time they're seen
            KeplerFunction n_funct = new KeplerFunction(name);
            n_funct.is_internal = is_internal;
            if (global_override) global[name] = n_funct;
            else local[name] = n_funct;
            return n_funct;
        }

        public void Load(KeplerFunction function)
        {
            if (this.GetFunction(function.name, true) != null)
                throw new KeplerError(KeplerErrorCode.DECLARE_DUP, new string[] { function.name });

            this.local.Add(function.name, function);
        }

        public KeplerFunction GetFunction(string name, bool no_error = false)
        {
            KeplerFunction res = null;
            if (global.ContainsKey(name)) res = global[name];
            if (local.ContainsKey(name)) res = local[name];

            if (no_error && res != null)
                res.Reset();

            if (!no_error && res == null)
                throw new KeplerError(KeplerErrorCode.UNDECLARED, new string[] { name });

            return res;
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
            string output = "Functions\r\nScope: " + (this.global == this.local ? "global" : "local") + "\r\n";
            // foreach (KeyValuePair<string, KeplerFunction> pair in global)
            // {
            //     output = output + "GLOBAL " + pair.Key + " => " + pair.Value + "\r\n";
            // }
            foreach (KeyValuePair<string, KeplerFunction> pair in local)
            {
                output = output + pair.Key + " => " + pair.Value + "\r\n";
            }
            return output;
            // return base.ToString();
        }
    }

    public class KeplerFunction
    {
        public List<LineIterator> lines = new List<LineIterator>();
        // public IDictionary<string, KeplerType> positional_arguments = new Dictionary<string, KeplerType>(); // name = type, assigned by position
        // public IDictionary<string, KeplerType> non_positional_arguments = new Dictionary<string, KeplerType>(); // name = type, assigned by name ref
        // public IDictionary<string, KeplerVariable> arguments = new Dictionary<string, KeplerVariable>();
        public KeplerArguments arguments;
        public KeplerVariable target = new KeplerVariable();
        bool has_target = false;
        public KeplerType type;
        public string name;
        public string id;
        public bool is_internal = false;
        public Func<Kepler.Interpreting.Interpreter, KeplerArguments, KeplerVariable> internal_call;

        public KeplerFunction(string name, bool is_internal = false)
        {
            this.arguments = new KeplerArguments(this);
            this.name = name;
            this.id = Guid.NewGuid().ToString("N");
            this.is_internal = is_internal;
        }

        public void SetType(KeplerType type)
        {
            this.type = type;
        }

        public bool HasArguments()
        {
            return this.arguments.HasArguments();
        }

        /// <summary>
        /// Assigns a positional argument to the function.
        /// This sets a function's required arguments.
        /// </summary>
        public void AssignNonPositional(string name, KeplerType type)
        {
            AssertNPArgumentNotExists(name);
            arguments.RequireNonPositionalArgument(name, type);
        }

        /// <summary>
        /// Sets the value of a non-positional argument.
        /// This sets the temporary value of a non-positional argument, which is used during execution.
        /// </summary>
        public void SetNonPositional(string name, KeplerVariable value)
        {
            AssertNPArgument(name);

            value.ValidateType(arguments.GetNonPositionalArgumentType(name));

            this.arguments.SetNonPositionalArgument(name, value);
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

            this.arguments.Reset();
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
            if (arguments.HasNonPositionalArgument(name)) throw new KeplerError(KeplerErrorCode.DUP_NONPOS_ARG, new string[] { this.name, name });
        }

        void AssertNPArgument(string name)
        {
            if (!arguments.HasNonPositionalArgument(name)) throw new KeplerError(KeplerErrorCode.UNDECLARED_NONPOS_ARG, new string[] { this.name, name });
        }

        public override string ToString()
        {
            string format_string = "";

            if (this.is_internal) format_string = string.Format("({0}) INTERNAL KeplerFunction {1}", id, type);
            else format_string = string.Format("({0}) KeplerFunction {1}", id, type);

            if (this.arguments.HasArguments()) format_string += " " + this.arguments.ToString();

            return format_string;
        }
    }

    public class KeplerArguments
    {
        public IDictionary<string, KeplerType> required_non_positionals;
        public IDictionary<string, KeplerVariable> non_positional_arguments;
        public List<KeplerVariable> positional_arguments;
        private KeplerFunction function;

        public KeplerArguments(KeplerFunction function)
        {
            this.function = function;
            this.required_non_positionals = new Dictionary<string, KeplerType>();
            this.non_positional_arguments = new Dictionary<string, KeplerVariable>();
            this.positional_arguments = new List<KeplerVariable>();
        }

        public KeplerVariable GetArgument(string name)
        {
            if (non_positional_arguments.ContainsKey(name)) return non_positional_arguments[name];
            throw new KeplerError(KeplerErrorCode.UNASSIGNED_NONPOS_ARG, new string[] { this.function.name, name });
            // throw new KeplerError(KeplerErrorCode.UNDECLARED_NONPOS_ARG, new string[] { this.function.name, name });
        }

        public KeplerVariable GetArgument(int index)
        {
            if (index < positional_arguments.Count) return positional_arguments[index];
            else return null;
        }

        public Boolean HasArguments(bool only_assigned = false)
        {
            if (only_assigned)
                return non_positional_arguments.Count > 0 || positional_arguments.Count > 0;

            return this.required_non_positionals.Count > 0;
        }

        public Boolean HasNonPositionalArgument(string name)
        {
            return required_non_positionals.ContainsKey(name);
        }

        public void RequireNonPositionalArgument(string name, KeplerType type)
        {
            this.required_non_positionals.Add(name, type);
        }

        public KeplerType GetNonPositionalArgumentType(string name)
        {
            if (required_non_positionals.ContainsKey(name)) return required_non_positionals[name];
            // else return null;
            throw new KeplerError(KeplerErrorCode.UNDECLARED_NONPOS_ARG, new string[] { this.function.name, name });
        }

        public void SetNonPositionalArgument(string name, KeplerVariable value)
        {
            if (this.required_non_positionals.ContainsKey(name))
            {
                if (this.non_positional_arguments.ContainsKey(name))
                {
                    this.non_positional_arguments[name] = value;
                }
                else
                {
                    this.non_positional_arguments.Add(name, value);
                }
            }
            else throw new KeplerError(KeplerErrorCode.UNDECLARED_NONPOS_ARG, new string[] { this.function.name, name });
        }

        public void Reset()
        {
            this.non_positional_arguments.Clear();
        }

        public override string ToString()
        {
            string format_string = "";

            if (this.required_non_positionals.Count > 0)
            {
                format_string += "[";
                foreach (KeyValuePair<string, KeplerType> kvp in this.required_non_positionals)
                {
                    format_string += kvp.Key + ":" + kvp.Value.ToString() + ", ";
                }
                format_string = format_string.Substring(0, format_string.Length - 2) + "]";
            }

            return format_string;
        }
    }
    public class KeplerConditional
    {
        public KeplerFunction main_function;
        public List<KeplerFunction> elseifs;
        public KeplerFunction else_function;

        public KeplerConditional()
        {
            main_function = new KeplerFunction("conditional_main");
            else_function = new KeplerFunction("conditional_else");
            elseifs = new List<KeplerFunction>();
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
            // Console.WriteLine(string.Format("CHECKING IF {0} IS VALID", this.id));
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
            this.disabled = true;
            this.stopWatch.Stop();
        }

        public void Enable()
        {
            this.disabled = false;

            this.stopWatch.Start();
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
        public KeplerInterruptManager global;
        public KeplerInterruptManager parent;
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
            {
                if (this.interrupts[i].id == id) return this.interrupts[i];
            }

            if (this.parent != null)
            {
                return this.parent.GetInterrupt(id);
            }
            else if (this.global != null)
            {
                return this.global.GetInterrupt(id);
            }
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
        Any,
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