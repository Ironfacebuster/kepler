using KeplerVariables;
using Kepler.Exceptions;
using System;
using System.Collections.Generic;

namespace Kepler.Modules
{
    public static class KObjects
    {
        public static Module module;
        static Dictionary<string, KeplerObject> objects;
        static KObjects()
        {
            objects = new Dictionary<string, KeplerObject>();
            // OBJECTS MODULE
            KeplerFunction object_create = new KeplerFunction("object_create", true);
            object_create.SetType(KeplerType.String);
            object_create.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();
                string object_id = "guid{" + Guid.NewGuid().ToString() + "}";
                objects.Add(object_id, new KeplerObject(object_id));

                // return object ID
                res.SetStringValue(object_id);
                res.SetModifier(KeplerModifier.Constant);
                return res;
            };

            KeplerFunction object_delete = new KeplerFunction("object_delete", true);
            object_delete.AssignNonPositional("id", KeplerType.String);
            object_delete.SetType(KeplerType.Int);
            object_delete.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();
                string object_id = args.GetArgument("id").GetValueAsString();


                if (!objects.ContainsKey(object_id))
                    throw new KeplerError(KeplerErrorCode.GENERIC_ERROR, new string[] { $"Object with ID {object_id} does not exist" });


                objects.Remove(object_id);
                return null;
            };

            KeplerFunction object_exists = new KeplerFunction("object_exists", true);
            object_exists.SetType(KeplerType.Boolean);
            object_exists.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();
                string object_id = "null";

                if (!objects.ContainsKey(object_id))
                    res.SetBoolValue(false);
                else
                    res.SetBoolValue(true);

                res.SetModifier(KeplerModifier.Constant);
                return res;
            };

            KeplerFunction object_get = new KeplerFunction("object_get", true);
            object_get.AssignNonPositional("id", KeplerType.String);
            object_get.AssignNonPositional("key", KeplerType.String);
            object_get.SetType(KeplerType.Any);
            object_get.internal_call = (interpreter, args) =>
            {
                // Console.WriteLine("ARGS: " + args.ToString());
                // TODO: check for object with given ID
                KeplerVariable res = new KeplerVariable();
                string object_id = args.GetArgument("id").GetValueAsString();
                string key = args.GetArgument("key").GetValueAsString();

                if (!objects.ContainsKey(object_id))
                    throw new KeplerError(KeplerErrorCode.GENERIC_ERROR, new string[] { $"Object with ID \"{object_id}\" does not exist." });

                Dictionary<string, KeplerVariable> obj = objects[object_id].properties;

                if (!obj.ContainsKey(key))
                    throw new KeplerError(KeplerErrorCode.GENERIC_ERROR, new string[] { $"Object does not contain ${key}." });

                res = obj[key].Clone();
                res.SetModifier(KeplerModifier.Constant);
                return res;
            };

            KeplerFunction object_set = new KeplerFunction("object_set", true);
            object_set.AssignNonPositional("id", KeplerType.String);
            object_set.AssignNonPositional("key", KeplerType.String);
            object_set.AssignNonPositional("value", KeplerType.Any);
            object_set.SetType(KeplerType.Any);
            object_set.internal_call = (interpreter, args) =>
            {
                // TODO: check for object with given ID
                KeplerVariable res = new KeplerVariable();
                string object_id = args.GetArgument("id").GetValueAsString();
                string key = args.GetArgument("key").GetValueAsString();
                KeplerVariable value = args.GetArgument("value");

                if (!objects.ContainsKey(object_id))
                    throw new KeplerError(KeplerErrorCode.GENERIC_ERROR, new string[] { $"Object with ID \"{object_id}\" does not exist." });

                Dictionary<string, KeplerVariable> obj = objects[object_id].properties;

                if (!obj.ContainsKey(key))
                    obj.Add(key, value.Clone());
                else
                    obj[key].AssignValue(value);

                return null;
            };

            module = new Module("objects", new KeplerFunction[] { object_create, object_delete, object_exists, object_get, object_set });
        }

        class KeplerObject
        {
            public string id;
            public Dictionary<string, KeplerVariable> properties;
            public KeplerObject(string id)
            {
                this.id = id;
                properties = new Dictionary<string, KeplerVariable>();
            }
        }
    }
}