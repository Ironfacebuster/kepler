/*
 *   Copyright (c) 2021 William Huddleston
 *   All rights reserved.
 *   License: Apache 2.0
 */

using KeplerVariables;
using System;

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

            KeplerFunction seed_random = new KeplerFunction("seed_random", true);
            seed_random.SetType(KeplerType.Int);
            seed_random.AssignNonPositional("seed", KeplerType.Int);
            seed_random.internal_call = (interpreter, args) =>
            {
                // TODO: seed the random number generator with arguments
                r = new Random(args.GetArgument("seed").GetValueAsInt());
                // r = new Random(r.Next());
                return null;
            };

            module = new Module("random", new KeplerFunction[] { f_random, random_int, seed_random });
        }
    }
}