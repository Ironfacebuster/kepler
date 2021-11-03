using KeplerVariables;

namespace Kepler.Modules
{
    public static class KFilesystem
    {
        public static Module module;
        static KFilesystem()
        {

            // FILESYSTEM MODULE
            KeplerFunction fs_load = new KeplerFunction("fs_load", true);
            fs_load.SetType(KeplerType.String);
            fs_load.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();
                res.SetStringValue("null");
                res.SetModifier(KeplerModifier.Constant);

                // TODO: actually implement this

                return res;
            };

            KeplerFunction fs_exists = new KeplerFunction("fs_exists", true);
            fs_exists.SetType(KeplerType.Boolean);
            fs_exists.AssignNonPositional("file", KeplerType.String);
            fs_exists.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();
                res.SetBoolValue(false);
                res.SetModifier(KeplerModifier.Constant);

                // TODO: actually implement this

                return res;
            };

            KeplerFunction fs_write = new KeplerFunction("fs_write", true);
            fs_write.SetType(KeplerType.Int);
            fs_write.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();
                // codes 0 = success, 1 = general error
                res.SetIntValue(0);
                res.SetModifier(KeplerModifier.Constant);

                // TODO: actually implement this

                return res;
            };

            module = new Module("filesystem", new KeplerFunction[] { fs_load, fs_write, fs_exists });
            module.AddRequiredModule(KObjects.module);
        }
    }
}