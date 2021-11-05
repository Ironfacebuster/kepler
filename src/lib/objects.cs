/*
 *   Copyright (c) 2021 William Huddleston
 *   All rights reserved.
 *   License: Apache 2.0
 */

using Kepler.Exceptions;
using KeplerVariables;
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

                // just check if it exists
                GetObject(object_id);

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
                KeplerVariable res_value = new KeplerVariable();
                string object_id = args.GetArgument("id").GetValueAsString();
                string key = args.GetArgument("key").GetValueAsString();

                res_value = GetObject(object_id).GetProperty(key).Clone();
                res_value.SetModifier(KeplerModifier.Constant);
                return res_value;
            };

            KeplerFunction object_keys = new KeplerFunction("object_keys", true);
            object_keys.AssignNonPositional("id", KeplerType.String);
            object_keys.SetType(KeplerType.Any);
            object_keys.internal_call = (interpreter, args) =>
            {
                KeplerVariable res_value = new KeplerVariable();
                string object_id = args.GetArgument("id").GetValueAsString();

                res_value.SetStringValue(string.Join(",", GetObject(object_id).properties.Keys));
                res_value.SetModifier(KeplerModifier.Constant);
                return res_value;
            };

            KeplerFunction object_set = new KeplerFunction("object_set", true);
            object_set.AssignNonPositional("id", KeplerType.String);
            object_set.AssignNonPositional("key", KeplerType.String);
            object_set.AssignNonPositional("value", KeplerType.Any);
            object_set.SetType(KeplerType.Any);
            object_set.internal_call = (interpreter, args) =>
            {
                // TODO: check for object with given ID
                string object_id = args.GetArgument("id").GetValueAsString();
                string key = args.GetArgument("key").GetValueAsString();
                KeplerVariable value = args.GetArgument("value");

                GetObject(object_id).SetKey(key, value);

                return null;
            };

            module = new Module("objects", new KeplerFunction[] { object_create, object_delete, object_exists, object_get, object_set, object_keys });
        }

        private static KeplerObject GetObject(string object_id)
        {
            if (!objects.ContainsKey(object_id))
                throw new KeplerError(KeplerErrorCode.GENERIC_ERROR, new string[] { $"Object with ID \"{object_id}\" does not exist." });

            return objects[object_id];
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

            public KeplerVariable GetProperty(string key)
            {
                if (!properties.ContainsKey(key))
                    throw new KeplerError(KeplerErrorCode.GENERIC_ERROR, new string[] { $"Object does not contain ${key}." });

                return properties[key];
            }

            public void SetKey(string key, KeplerVariable value)
            {
                KeplerVariable deref = value.Clone();
                deref.modifier = KeplerModifier.Variable;

                if (properties.ContainsKey(key))
                    properties[key] = deref;
                else
                    properties.Add(key, deref);
            }
        }
    }
}