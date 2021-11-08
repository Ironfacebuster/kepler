/*
 *   Copyright (c) 2021 William Huddleston
 *   All rights reserved.
 *   License: Apache 2.0
 */

using System;
using System.Collections.Generic;

namespace Kepler.Input
{
    public static class LiveKeyboard
    {
        static int MaxInputLength = 256;
        public static KeyboardInfo GetInput()
        {
            List<ConsoleKeyInfo> input = new List<ConsoleKeyInfo>();

            int keyCount = 0;
            while (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);

                if (++keyCount > MaxInputLength) continue;

                input.Add(key);
            }

            return new KeyboardInfo(input);
        }

        public static void ClearCurrentLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }
    }

    public class KeyboardInfo
    {
        List<ConsoleKeyInfo> pressed_keys;
        public KeyboardInfo(List<ConsoleKeyInfo> keys)
        {
            this.pressed_keys = keys;
        }

        public string GetKeysAsString()
        {
            string keys = "";
            foreach (var key in pressed_keys)
            {
                if (key.Key == ConsoleKey.Enter)
                    break;

                if (key.Key == ConsoleKey.Backspace)
                    continue;
                if (key.KeyChar == '\u0000') continue;

                keys += key.KeyChar;
            }
            return keys;
        }

        public bool PressedEnter()
        {
            foreach (var key in pressed_keys)
            {
                if (key.Key == ConsoleKey.Enter)
                    return true;
            }

            return false;
        }

        public bool PressedKey(ConsoleKey check)
        {
            foreach (var key in pressed_keys)
            {
                if (key.Key == check)
                    return true;
            }

            return false;
        }
    }
}