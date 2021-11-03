using KeplerVariables;

namespace Kepler.Modules
{
    public static class KFilesystem
    {
        public static Module module;
        static KFilesystem()
        {

            // FILESYSTEM MODULE
            KeplerFunction fs_read = new KeplerFunction("fs_read", true);
            fs_read.SetType(KeplerType.String);
            fs_read.AssignNonPositional("file", KeplerType.String);
            fs_read.internal_call = (interpreter, args) =>
            {
                string filename = args.GetArgument("file").GetValueAsString();

                KeplerFunction create_obj = interpreter.statemachine.functions.GetFunction("object_create", true);
                KeplerVariable object_id = create_obj.internal_call(interpreter, create_obj.arguments);

                KeplerFunction set_obj = interpreter.statemachine.functions.GetFunction("object_set", true);

                // Set arguments
                set_obj.SetNonPositional("id", object_id.Clone()); // for some reason I have to clone this variable to avoid a type error
                set_obj.SetNonPositional("key", new KeplerVariable() { StringValue = "name", type = KeplerType.String });
                set_obj.SetNonPositional("value", new KeplerVariable() { StringValue = filename, type = KeplerType.String });
                set_obj.internal_call(interpreter, set_obj.arguments); // set name key

                set_obj.SetNonPositional("key", new KeplerVariable() { StringValue = "data", type = KeplerType.String });
                set_obj.SetNonPositional("value", new KeplerVariable() { StringValue = "", type = KeplerType.String });
                set_obj.internal_call(interpreter, set_obj.arguments); // set data key

                // TODO: actually finish this

                return object_id;
            };

            KeplerFunction fs_exists = new KeplerFunction("fs_exists", true);
            fs_exists.SetType(KeplerType.Boolean);
            fs_exists.AssignNonPositional("file", KeplerType.String);
            fs_exists.internal_call = (interpreter, args) =>
            {
                string filename = args.GetArgument("file").GetValueAsString();
                KeplerVariable res = new KeplerVariable();
                res.SetBoolValue(false);
                res.SetModifier(KeplerModifier.Constant);

                // TODO: actually implement this

                return res;
            };

            KeplerFunction fs_write = new KeplerFunction("fs_write", true);
            fs_write.SetType(KeplerType.Int);
            fs_write.AssignNonPositional("file", KeplerType.String);
            fs_write.AssignNonPositional("data", KeplerType.Any);
            fs_write.internal_call = (interpreter, args) =>
            {
                string filename = args.GetArgument("file").GetValueAsString();

                // convert all data to a string
                string data = args.GetArgument("data").GetValueAsString();

                KeplerVariable res = new KeplerVariable();
                // codes 0 = success, 1 = general error
                res.SetIntValue(0);
                res.SetModifier(KeplerModifier.Constant);

                // TODO: actually implement this

                return res;
            };

            module = new Module("filesystem", new KeplerFunction[] { fs_read, fs_write, fs_exists });
            module.AddRequiredModule(KObjects.module);
        }
    }
}