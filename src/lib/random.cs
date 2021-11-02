using System;
using KeplerVariables;
using System.Collections.Generic;
using Kepler.Versioning;

namespace Kepler.Modules
{
    public static class KRandom
    {
        public static Module module;
        static Random r = new Random();

        static KRandom()
        {

            // RANDOM MODULE
            KeplerFunction f_random = new KeplerFunction("random", true);
            f_random.SetType(KeplerType.Float);
            f_random.internal_call = (interpreter, args) =>
            {
                // TODO: add range arguments
                KeplerVariable res = new KeplerVariable();
                res.SetFloatValue((decimal)((float)r.Next(513) / 512f));
                res.SetModifier(KeplerModifier.Constant);

                return res;
            };

            KeplerFunction random_int = new KeplerFunction("random_int", true);
            random_int.SetType(KeplerType.Int);
            random_int.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();
                res.SetIntValue(r.Next(255));
                res.SetModifier(KeplerModifier.Constant);

                return res;
            };

            module = new Module("random", new KeplerFunction[] { f_random, random_int });
        }
    }
}