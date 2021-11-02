using System;
using KeplerVariables;

namespace Kepler.Modules
{
    public static class KInput
    {
        public static Module module;

        static KInput()
        {

            // INPUT MODULE
            KeplerFunction get_input = new KeplerFunction("input", true);
            get_input.SetType(KeplerType.String);
            get_input.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();
                res.SetStringValue(Console.ReadLine());
                res.SetModifier(KeplerModifier.Constant);

                return res;
            };

            KeplerFunction get_key = new KeplerFunction("get_key", true);
            get_key.SetType(KeplerType.String);
            string last_key = "";
            get_key.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();

                if (Console.KeyAvailable)
                {
                    last_key = Console.ReadKey().Key.ToString();

                }

                res.SetStringValue(last_key);
                res.SetModifier(KeplerModifier.Constant);

                return res;
            };

            KeplerFunction resetkey = new KeplerFunction("reset_key", true);
            resetkey.SetType(KeplerType.String);
            resetkey.internal_call = (interpreter, args) =>
            {
                last_key = "";
                return null;
            };

            module = new Module("input", new KeplerFunction[] { get_input, get_key, resetkey });
        }
    }
}