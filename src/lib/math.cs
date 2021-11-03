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
            math_vars.Add("PI", e);

            KeplerVariable tau = new KeplerVariable();
            tau.SetFloatValue(6.2831853071795862m);
            tau.SetModifier(KeplerModifier.Constant);
            math_vars.Add("TAU", tau);

            KeplerFunction sin = new KeplerFunction("sin", true);
            sin.SetType(KeplerType.Float);
            sin.internal_call = (interpreter, args) =>
            {
                // TODO: actually implement this
                KeplerVariable res = new KeplerVariable();
                res.SetFloatValue(0);
                res.SetModifier(KeplerModifier.Constant);

                return res;
            };

            KeplerFunction cos = new KeplerFunction("cos", true);
            cos.SetType(KeplerType.Float);
            // sin.AssignNonPositional("value", KeplerType.Float);
            cos.internal_call = (interpreter, args) =>
            {
                // TODO: actually implement this
                KeplerVariable res = new KeplerVariable();
                res.SetFloatValue(0);
                res.SetModifier(KeplerModifier.Constant);

                return res;
            };

            KeplerFunction round = new KeplerFunction("round", true);
            round.SetType(KeplerType.Int);
            round.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();
                res.SetIntValue(0);
                res.SetModifier(KeplerModifier.Constant);

                return res;
            };

            KeplerFunction ceil = new KeplerFunction("ceil", true);
            ceil.SetType(KeplerType.Int);
            ceil.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();
                res.SetIntValue(0);
                res.SetModifier(KeplerModifier.Constant);

                return res;
            };

            KeplerFunction floor = new KeplerFunction("floor", true);
            floor.SetType(KeplerType.Int);
            floor.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();
                res.SetIntValue(0);
                res.SetModifier(KeplerModifier.Constant);

                return res;
            };

            module = new Module("math", new KeplerFunction[] { sin, cos, round, floor, ceil }, math_vars);
        }
    }
}