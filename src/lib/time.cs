using KeplerVariables;
using System;

namespace Kepler.Modules
{
    public static class KTime
    {
        public static Module module;
        static KTime()
        {
            // TIME MODULE
            KeplerFunction get_start = new KeplerFunction("get_start", true);
            get_start.SetType(KeplerType.String);
            string START = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString();
            get_start.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();
                res.SetStringValue(START);
                res.SetModifier(KeplerModifier.Constant);

                return res;
            };

            KeplerFunction get_time = new KeplerFunction("get_time", true);
            get_time.SetType(KeplerType.String);
            get_time.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();
                res.SetStringValue((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString());
                res.SetModifier(KeplerModifier.Constant);

                return res;
            };

            module = new Module("time", new KeplerFunction[] { get_start, get_time });
        }
    }
}