using System;
using System.IO;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using KeplerTokenizer;
using KeplerInterpreter;

namespace KeplerVariables
{

    public class KeplerVariableManager
    {
        public IDictionary<string, KeplerVariable> list = new Dictionary<string, KeplerVariable>();

        public KeplerVariable DeclareVariable(string name)
        {
            // TODO: handle redeclarations
            // if the dictionary has this variable already
            if (list.ContainsKey(name)) return list[name];

            // variables are created the first time they're seen
            KeplerVariable n_var = new KeplerVariable();
            list[name] = n_var;
            return n_var;
        }

        public KeplerVariable GetVariable(string name)
        {
            if (list.ContainsKey(name))
            {
                KeplerVariable found = list[name];

                if (found.type == KeplerType.Unassigned) throw new InterpreterException(string.Format("[154] {0} does not have a defined type", name));

                return list[name];
            }

            throw new InterpreterException(string.Format("[154] {0} has not yet been declared", name));
        }

        // allow copying of global for "scoping"
        public KeplerVariableManager Copy()
        {
            KeplerVariableManager copy = new KeplerVariableManager();
            copy.list = new Dictionary<string, KeplerVariable>(this.list);
            return copy;
        }

        public override string ToString()
        {
            string output = "VariableManager\r\n";
            foreach (KeyValuePair<string, KeplerVariable> pair in list)
            {
                output = output + pair.Key + " => " + pair.Value + "\r\n";
            }
            return output;
            // return base.ToString();
        }
    }

    public class KeplerVariable
    {
        public KeplerType type = KeplerType.Unassigned;
        public KeplerModifier modifier = KeplerModifier.Variable;
        public double FloatValue = 0;
        public uint uIntValue = 0;
        public int IntValue = 0;
        public string StringValue = "";
        public bool BoolValue = false;


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

        public void SetFloatValue(double value)
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
            if (this.type != type) throw new InterpreterException(string.Format("[101] Invalid type assignment {0} to {1}", type, this.type));
        }
        public void ValidateConstant()
        {
            if (this.modifier == KeplerModifier.Constant) throw new InterpreterException("[105] Assignment to constant variable");
        }

        public override string ToString()
        {
            string output = string.Format("{0} {1}", this.modifier, this.type);

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
                    return FloatValue.ToString();
                case KeplerType.Int:
                    return IntValue.ToString();
                case KeplerType.uInt:
                    return string.Format("u{0}", uIntValue);
                case KeplerType.Boolean:
                    return BoolValue.ToString();
                case KeplerType.String:
                    return StringValue;
            }

            return "";
        }
    }

    public class KeplerFunctionManager
    {
        public IDictionary<string, KeplerFunction> list = new Dictionary<string, KeplerFunction>();

        public KeplerFunction DeclareFunction(string name)
        {
            // TODO: handle redeclarations
            // if the dictionary has this variable already
            if (list.ContainsKey(name)) return list[name];

            // functions are created the first time they're seen
            KeplerFunction n_funct = new KeplerFunction(name);
            list[name] = n_funct;
            return n_funct;
        }

        public KeplerFunction GetFunction(string name)
        {
            if (list.ContainsKey(name)) return list[name];

            throw new InterpreterException(string.Format("[154] {0} has not yet been declared", name));
        }

        public KeplerFunctionManager Copy()
        {
            KeplerFunctionManager copy = new KeplerFunctionManager();
            copy.list = new Dictionary<string, KeplerFunction>(this.list);
            return copy;
        }

        public override string ToString()
        {
            string output = "FunctionManager\r\n";
            foreach (KeyValuePair<string, KeplerFunction> pair in list)
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
        public IDictionary<string, KeplerType> positional_arguments = new Dictionary<string, KeplerType>(); // name = type, assigned by position
        public IDictionary<string, KeplerType> non_positional_arguments = new Dictionary<string, KeplerType>(); // name = type, assigned by name ref
        public IDictionary<string, KeplerVariable> arguments = new Dictionary<string, KeplerVariable>();
        public KeplerVariable target = new KeplerVariable();
        bool has_target = false;
        public KeplerType type;
        public string name;

        public KeplerFunction(string name)
        {
            this.name = name;
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
            for (int i = 0; i < this.lines.Count; ++i) this.lines[i].m_num = 0;
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
            if (non_positional_arguments.ContainsKey(name)) throw new InterpreterException(string.Format("{0} already has a non-positional argument named {1}", this.name, name));
        }

        void AssertNPArgument(string name)
        {
            if (!non_positional_arguments.ContainsKey(name)) throw new InterpreterException(string.Format("{0} does not have a non-positional argument named {1}", this.name, name));
        }

        public override string ToString()
        {
            // string output = "KeplerFunction";
            // foreach (LineIterator line in lines)
            // {
            //     output = output + line + "\r\n";
            // }
            return string.Format("KeplerFunction {0}", type);
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
        Unassigned
    }
}