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
    public static class KGraphics
    {
        public static Module module;
        public static float active_r = 255, active_g = 255, active_b = 255;

        static KGraphics()
        {
            KeplerFunction window_create = new KeplerFunction("window_create", true);
            window_create.SetType(KeplerType.String);
            window_create.AssignNonPositional("title", KeplerType.String);
            window_create.AssignNonPositional("width", KeplerType.Int);
            window_create.AssignNonPositional("height", KeplerType.Int);
            window_create.internal_call = (interpreter, args) =>
            {
                KeplerVariable res = new KeplerVariable();
                res.SetModifier(KeplerModifier.Constant);
                return res;
            };

            KeplerFunction window_update = new KeplerFunction("window_update", true);
            window_update.SetType(KeplerType.Int);
            // window_update.AssignNonPositional("id", KeplerType.String);
            window_update.internal_call = (interpreter, args) =>
            {
                return null;
            };

            KeplerFunction window_clear = new KeplerFunction("window_clear", true);
            window_clear.SetType(KeplerType.Int);
            // window_clear.AssignNonPositional("id", KeplerType.String);
            window_clear.internal_call = (interpreter, args) =>
            {
                return null;
            };

            KeplerFunction window_draw = new KeplerFunction("window_draw", true);
            window_draw.SetType(KeplerType.Int);
            // window_draw.AssignNonPositional("id", KeplerType.String);
            window_draw.AssignNonPositional("x", KeplerType.Int);
            window_draw.AssignNonPositional("y", KeplerType.Int);
            window_draw.internal_call = (interpreter, args) =>
            {

                int x = args.GetArgument("x").GetValueAsInt(),
                    y = args.GetArgument("y").GetValueAsInt();

                return null;
            };

            KeplerFunction window_destroy = new KeplerFunction("window_destroy", true);
            window_destroy.SetType(KeplerType.Int);
            window_destroy.AssignNonPositional("id", KeplerType.String);
            window_destroy.internal_call = (interpreter, args) =>
            {
                string id = args.GetArgument("id").GetValueAsString();

                return null;
            };

            KeplerFunction set_color = new KeplerFunction("set_color", true);
            set_color.SetType(KeplerType.Int);
            set_color.AssignNonPositional("r", KeplerType.Int);
            set_color.AssignNonPositional("g", KeplerType.Int);
            set_color.AssignNonPositional("b", KeplerType.Int);
            set_color.internal_call = (interpreter, args) =>
            {
                active_r = (float)args.GetArgument("r").GetValueAsInt() / 255.0f;
                active_g = (float)args.GetArgument("g").GetValueAsInt() / 255.0f;
                active_b = (float)args.GetArgument("b").GetValueAsInt() / 255.0f;

                return null;
            };

            module = new Module("graphics", new KeplerFunction[] { window_create, window_update, set_color, window_draw, window_clear, window_destroy });

            module.events.AddListener("exit", () =>
            {
                return 0;
            });
        }
    }
}