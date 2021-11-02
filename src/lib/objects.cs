using KeplerVariables;
using System;

namespace Kepler.Modules
{
    public static class KObjects
    {
        public static Module module;
        static KObjects()
        {
            // OBJECTS MODULE
            KeplerFunction createobject = new KeplerFunction("create_object", true);
            createobject.SetType(KeplerType.String);
            createobject.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();
                string object_id = "guid{" + Guid.NewGuid().ToString() + "}";
                // return object ID
                res.SetStringValue(object_id);
                res.SetModifier(KeplerModifier.Constant);

                return res;
            };

            KeplerFunction deleteobject = new KeplerFunction("delete_object", true);
            deleteobject.SetType(KeplerType.Int);
            deleteobject.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();

                // codes 0 = success, 1 = general error
                res.SetIntValue(0);
                res.SetModifier(KeplerModifier.Constant);

                return res;
            };

            KeplerFunction objectget = new KeplerFunction("object_get", true);
            objectget.SetType(KeplerType.Any);
            objectget.internal_call = (interpreter, args) =>
            {
                // TODO: check for object with given ID
                KeplerVariable res = new KeplerVariable();

                res.SetStringValue("null");
                res.SetModifier(KeplerModifier.Constant);

                return res;
            };

            module = new Module("objects", new KeplerFunction[] { createobject, deleteobject, objectget });
        }
    }
}