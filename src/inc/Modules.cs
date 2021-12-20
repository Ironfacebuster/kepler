/*
 *   Copyright (c) 2021 William Huddleston
 *   All rights reserved.
 *   License: Apache 2.0
 */

using System;
using KeplerVariables;
using System.Collections.Generic;
using Kepler.Versioning;

namespace Kepler.Modules
{
    public struct Module
    {
        public string name;
        public Module[] required_modules;
        public KeplerFunction[] functions;
        public Dictionary<string, KeplerVariable> variables;

        public EventManager events;

        public Module(string name, KeplerFunction[] functions = null, Dictionary<string, KeplerVariable> variables = null, Module[] required_modules = null)
        {
            this.name = name;
            this.functions = functions;
            this.variables = variables;
            this.required_modules = required_modules;

            this.events = new EventManager();
        }

        public void AddFunctions(KeplerFunction[] functions)
        {

            List<KeplerFunction> list = new List<KeplerFunction>(this.functions);
            list.AddRange(functions);
            this.functions = list.ToArray();
        }

        public void AddFunction(KeplerFunction function)
        {
            List<KeplerFunction> list = new List<KeplerFunction>(this.functions);
            list.Add(function);
            this.functions = list.ToArray();
        }

        public void AddVariable(string name, KeplerVariable variable)
        {
            this.variables.Add(name, variable);
        }

        public void AddVariables(Dictionary<string, KeplerVariable> variables)
        {
            foreach (KeyValuePair<string, KeplerVariable> variable in variables)
            {
                this.variables.Add(variable.Key, variable.Value);
            }
        }

        public void AddRequiredModule(Module module)
        {
            List<Module> list = new List<Module>(this.required_modules == null ? new Module[0] : this.required_modules);
            list.Add(module);
            this.required_modules = list.ToArray();
        }

        public KeplerFunction GetFunction(string name)
        {
            foreach (KeplerFunction function in this.functions)
            {
                if (function.name == name)
                {
                    return function;
                }
            }
            return null;
        }

        public KeplerVariable CallFunction(string name, Kepler.Interpreting.Interpreter interpreter, KeplerArguments args)
        {
            foreach (KeplerFunction function in this.functions)
            {
                if (function.name == name)
                {
                    return function.internal_call(interpreter, args);
                }
            }
            return null;
        }
    }

    public static class InternalLibraries
    {
        public static Module[] modules;

        static InternalLibraries()
        {

            // MAIN MODULE
            Dictionary<string, KeplerVariable> main_vars = new Dictionary<string, KeplerVariable>();

            KeplerVariable version_var = new KeplerVariable();
            version_var.SetStringValue(StaticValues._VERSION);
            version_var.SetModifier(KeplerModifier.Constant);
            main_vars.Add("$_VERSION", version_var);

            KeplerVariable nan = new KeplerVariable();
            nan.SetType(KeplerType.NaN);
            nan.SetModifier(KeplerModifier.Constant);
            main_vars.Add("NaN", nan);

            Module main = new Module("main", null, main_vars);

            // assign all modules
            modules = new Module[] { main, KMath.module, KInput.module, KTime.module, KRandom.module, KFilesystem.module, KObjects.module, KUtilities.module, KConsole.module, KGraphics.module };
        }

        public static bool HasModule(string name)
        {
            for (int i = 0; i < modules.Length; i++)
            {
                if (modules[i].name == name)
                    return true;
            }

            return false;
        }

        public static Module GetModule(string name)
        {
            for (int i = 0; i < modules.Length; i++)
            {
                if (modules[i].name == name)
                    return modules[i];
            }

            return modules[0];
        }
    }

    public class EventManager
    {
        public IDictionary<string, List<Func<int>>> events = new Dictionary<string, List<Func<int>>>();

        public void AddListener(string e, Func<int> function)
        {
            // events.Add(func);
            if (!events.ContainsKey(e)) events.Add(e, new List<Func<int>>());
            events[e].Add(function);
        }

        public void Remove(string e, Func<int> function)
        {
            events[e].Remove(function);
        }

        public void RemoveAll(string e)
        {
            events.Remove(e);
        }

        public void Activate(string e)
        {
            if (!events.ContainsKey(e) || events[e].Count == 0)
            {
                return;
            }

            foreach (Func<int> func in events[e])
            {
                int result = func();

                if (result != 0)
                {
                    throw new Exception($"A listener for event \"{e}\" returned {result}");
                }
            }
        }
    }
}