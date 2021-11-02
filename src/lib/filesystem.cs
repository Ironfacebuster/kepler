using KeplerVariables;

namespace Kepler.Modules
{
    public static class KFilesystem
    {
        public static Module module;
        static KFilesystem()
        {
            KeplerFunction load = new KeplerFunction("load", true);
            load.SetType(KeplerType.String);
            load.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();
                res.SetStringValue("null");
                res.SetModifier(KeplerModifier.Constant);

                return res;
            };

            // FILESYSTEM MODULE
            KeplerFunction write = new KeplerFunction("write", true);
            write.SetType(KeplerType.Int);
            write.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();
                // codes 0 = success, 1 = general error
                res.SetIntValue(0);
                res.SetModifier(KeplerModifier.Constant);

                // TODO: actually implement this

                return res;
            };

            module = new Module("filesystem", new KeplerFunction[] { load, write });
            module.AddRequiredModule(KObjects.module);
        }
    }
}