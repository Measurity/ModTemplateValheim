using System;
using System.Reflection;
using HarmonyLib;
using MethodCallCommand.Utils;

namespace MethodCallCommand
{
    public static class ModApi
    {
        private static readonly Harmony harmony = new Harmony("org.github.measurity.methodcallcommandplugin");
        private static Action<string> print;

        public static void Init()
        {
            harmony.PatchAll();
        }

        public static void Dispose()
        {
            harmony.UnpatchAll();
        }

        /// <summary>
        ///     Prints a message to the game console.
        /// </summary>
        /// <param name="message">Message to print.</param>
        public static void Print(string message)
        {
            if (print == null && Console.instance)
            {
                print = (Action<string>) typeof(Console)
                    .GetMethod("AddString", BindingFlags.Instance | BindingFlags.NonPublic)
                    .CreateDelegate(Console.instance);
            }
            print?.Invoke(message);
        }
    }
}
