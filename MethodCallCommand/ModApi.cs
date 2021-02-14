using HarmonyLib;

namespace MethodCallCommand
{
    public static class ModApi
    {
        private static readonly Harmony harmony = new("org.github.measurity.methodcallcommandplugin");

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
            if (Console.instance)
            {
                Console.instance.Print(message);
            }
        }
    }
}
