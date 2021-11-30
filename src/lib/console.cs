/*
 *   Copyright (c) 2021 William Huddleston
 *   All rights reserved.
 *   License: Apache 2.0
 */

using KeplerVariables;
using System;

namespace Kepler.Modules
{
    public static class KConsole
    {
        public static Module module;
        static KConsole()
        {
            KeplerFunction cursor_visible = new KeplerFunction("cursor_visible", true);
            cursor_visible.SetType(KeplerType.Int);
            cursor_visible.AssignNonPositional("state", KeplerType.Boolean);
            cursor_visible.internal_call = (interpreter, args) =>
            {
                Console.CursorVisible = args.GetArgument("state").GetValueAsBool();
                return null;
            };

            KeplerFunction console_clear = new KeplerFunction("console_clear", true);
            console_clear.SetType(KeplerType.String);
            console_clear.internal_call = (interpreter, args) =>
            {
                Console.Clear();
                return null;
            };

            module = new Module("console", new KeplerFunction[] { console_clear, cursor_visible });
        }
    }
}