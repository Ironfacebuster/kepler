using KeplerVariables;
using System;

namespace Kepler.Modules
{
    public static class KUtilities
    {
        public static Module module;
        static KUtilities()
        {
            KeplerFunction get_type = new KeplerFunction("get_type", true);
            get_type.SetType(KeplerType.String);
            get_type.internal_call = (interpreter, args) =>
            {
                // get variable type
                KeplerType type = KeplerType.Int;

                KeplerVariable res = new KeplerVariable();
                res.SetStringValue(type.ToString());
                res.SetModifier(KeplerModifier.Constant);

                return res;
            };

            module = new Module("utilities", new KeplerFunction[] { get_type });
        }
    }
}