using System;
using KeplerVariables;
using System.Collections.Generic;
using Kepler.Modules;

namespace Kepler.Modules
{
    public static class KMath
    {
        public static Module module;

        static KMath()
        {
            // MATH MODULE
            Dictionary<string, KeplerVariable> math_vars = new Dictionary<string, KeplerVariable>();

            KeplerVariable e = new KeplerVariable();
            e.SetFloatValue(2.7182818284590451m);
            e.SetModifier(KeplerModifier.Constant);
            math_vars.Add("E", e);

            KeplerVariable pi = new KeplerVariable();
            pi.SetFloatValue(3.141592653589793m);
            pi.SetModifier(KeplerModifier.Constant);
            math_vars.Add("PI", pi);

            KeplerVariable tau = new KeplerVariable();
            tau.SetFloatValue(6.2831853071795862m);
            tau.SetModifier(KeplerModifier.Constant);
            math_vars.Add("TAU", tau);

            KeplerFunction sin = new KeplerFunction("sin", true);
            sin.SetType(KeplerType.Float);
            sin.AssignNonPositional("value", KeplerType.Float);
            sin.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();
                res.SetFloatValue((decimal)Math.Sin((double)args.GetArgument("value").GetValueAsFloat()));
                res.SetModifier(KeplerModifier.Constant);

                return res;
            };

            KeplerFunction cos = new KeplerFunction("cos", true);
            cos.SetType(KeplerType.Float);
            cos.AssignNonPositional("value", KeplerType.Any);
            cos.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();
                res.SetFloatValue((decimal)Math.Cos((double)args.GetArgument("value").GetValueAsFloat()));
                res.SetModifier(KeplerModifier.Constant);

                return res;
            };

            KeplerFunction round = new KeplerFunction("round", true);
            round.SetType(KeplerType.Int);
            round.AssignNonPositional("value", KeplerType.Float);
            round.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();
                res.SetIntValue((int)Math.Round((double)args.GetArgument("value").GetValueAsFloat()));
                res.SetModifier(KeplerModifier.Constant);

                return res;
            };

            KeplerFunction ceil = new KeplerFunction("ceil", true);
            ceil.SetType(KeplerType.Int);
            ceil.AssignNonPositional("value", KeplerType.Float);
            ceil.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();
                res.SetIntValue((int)Math.Ceiling((double)args.GetArgument("value").GetValueAsFloat()));
                res.SetModifier(KeplerModifier.Constant);

                return res;
            };

            KeplerFunction abs = new KeplerFunction("abs", true);
            abs.SetType(KeplerType.Int);
            abs.AssignNonPositional("value", KeplerType.Any);
            abs.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();
                // res.SetIntValue((int)Math.Abs((double)args.GetArgument("value").GetValueAsFloat()));
                res.SetModifier(KeplerModifier.Constant);

                return res;
            };

            KeplerFunction floor = new KeplerFunction("floor", true);
            floor.SetType(KeplerType.Int);
            floor.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();
                res.SetIntValue((int)Math.Floor((double)args.GetArgument("value").GetValueAsFloat()));
                res.SetModifier(KeplerModifier.Constant);

                return res;
            };

            KeplerFunction to_binary = new KeplerFunction("to_binary", true);
            to_binary.SetType(KeplerType.String);
            to_binary.AssignNonPositional("value", KeplerType.Int);
            to_binary.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();
                res.SetStringValue(Convert.ToString(args.GetArgument("value").GetValueAsInt(), 2));
                res.SetModifier(KeplerModifier.Constant);

                return res;
            };

            KeplerFunction from_binary = new KeplerFunction("from_binary", true);
            from_binary.SetType(KeplerType.Int);
            from_binary.AssignNonPositional("value", KeplerType.String);
            from_binary.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();
                res.SetIntValue(Convert.ToInt32(args.GetArgument("value").GetValueAsString(), 2));
                res.SetModifier(KeplerModifier.Constant);

                return res;
            };

            module = new Module("math", new KeplerFunction[] { sin, cos, round, floor, ceil, abs, to_binary, from_binary }, math_vars);
        }
    }
}