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

        public Module(string name, KeplerFunction[] functions = null, Dictionary<string, KeplerVariable> variables = null, Module[] required_modules = null)
        {
            this.name = name;
            this.functions = functions;
            this.variables = variables;
            this.required_modules = required_modules;
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

            KeplerVariable e = new KeplerVariable();
            e.SetFloatValue(2.7182818284590451m);
            e.SetModifier(KeplerModifier.Constant);
            main_vars.Add("E", e);

            KeplerVariable pi = new KeplerVariable();
            pi.SetFloatValue(3.141592653589793m);
            pi.SetModifier(KeplerModifier.Constant);
            main_vars.Add("PI", e);

            KeplerVariable tau = new KeplerVariable();
            tau.SetFloatValue(6.2831853071795862m);
            tau.SetModifier(KeplerModifier.Constant);
            main_vars.Add("TAU", tau);

            KeplerVariable nan = new KeplerVariable();
            nan.SetType(KeplerType.NaN);
            nan.SetModifier(KeplerModifier.Constant);
            main_vars.Add("NaN", nan);

            KeplerFunction clearscreen = new KeplerFunction("clear", true);
            clearscreen.SetType(KeplerType.String);
            clearscreen.internal_call = (interpreter, args) =>
            {
                Console.Clear();
                return null;
            };

            Module main = new Module("main", new KeplerFunction[] { clearscreen }, main_vars);

            // assign all modules
            modules = new Module[] { main, KMath.module, KInput.module, KTime.module, KRandom.module, KFilesystem.module, KObjects.module };
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
}