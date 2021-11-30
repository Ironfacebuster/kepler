/*
 *   Copyright (c) 2021 William Huddleston
 *   All rights reserved.
 *   License: Apache 2.0
 */

using KeplerVariables;

namespace Kepler.Modules
{
    public static class KUtilities
    {
        public static Module module;
        static KUtilities()
        {
            KeplerFunction get_type = new KeplerFunction("get_type", true);
            get_type.SetType(KeplerType.String);
            get_type.AssignNonPositional("variable", KeplerType.Any);
            get_type.internal_call = (interpreter, args) =>
            {
                // get variable type
                // KeplerType type = KeplerType.Int;
                KeplerVariable var = args.GetArgument("variable");

                KeplerVariable res = new KeplerVariable();
                res.SetStringValue(var.type.ToString());
                res.SetModifier(KeplerModifier.Constant);

                return res;
            };

            KeplerFunction get_id = new KeplerFunction("get_id", true);
            get_id.SetType(KeplerType.String);
            get_id.AssignNonPositional("variable", KeplerType.Any);
            get_id.internal_call = (interpreter, args) =>
            {
                // get variable type
                // KeplerType type = KeplerType.Int;
                KeplerVariable var = args.GetArgument("variable");
                // Console.WriteLine(var.ToString());

                KeplerVariable res = new KeplerVariable();
                res.SetStringValue(var.id.ToString());
                res.SetModifier(KeplerModifier.Constant);

                return res;
            };

            module = new Module("utilities", new KeplerFunction[] { get_type, get_id });
        }
    }
}