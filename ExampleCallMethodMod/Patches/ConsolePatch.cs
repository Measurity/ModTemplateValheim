using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using ExampleCallMethodMod.Utils;
using HarmonyLib;

namespace ExampleCallMethodMod.Patches;

public static class ConsolePatch
{
    /// <summary>
    ///     TODO: Remove this patch and write your mod :)
    ///     Example harmony Prefix patch for in-game console input.
    /// </summary>
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InputText))]
    public static class InputText
    {
        public static bool Prefix()
        {
            Console console = Console.instance;
            var input = console.m_input.text;
            if (!input.StartsWith("call", true, CultureInfo.InvariantCulture))
            {
                return true;
            }
            var parts = CommandParser.Parse(input).ToArray();
            if (parts.Length < 3)
            {
                console.AddString(
                    "call command requires at least 3 arguments: call <class> <methodName> [arg1] [arg2]");
                return false;
            }
            console.AddString(input);
            Type targetClass = typeof(Console).Assembly.GetTypes()
                .FirstOrDefault(t => t.Name.Equals(parts[1].Text, StringComparison.InvariantCultureIgnoreCase));
            if (targetClass == null)
            {
                console.AddString($"Could not find class with name '{parts[1]}'");
                return false;
            }
            MethodInfo method = targetClass.GetMethods(BindingFlags.Public |
                                                       BindingFlags.Static |
                                                       BindingFlags.NonPublic |
                                                       BindingFlags.Instance |
                                                       BindingFlags.InvokeMethod)
                .FirstOrDefault(m => m.Name.Equals(parts[2].Text, StringComparison.InvariantCultureIgnoreCase));
            if (method == null)
            {
                console.AddString($"Could not find method '{parts[2].Text}' on class '{targetClass.FullName}'");
                return false;
            }
            // Parameters must match target method.
            var methodParams = method.GetParameters();
            if (methodParams.Length != parts.Length - 3) // -3 for command, class and method name args
            {
                console.AddString(
                    $"Command does not match method parameters. Expected parameter types: {string.Join(", ", methodParams.Select(p => p.ParameterType.Name))}");
                return false;
            }
            var methodArgsToSupply = new object[methodParams.Length];
            for (var i = 0; i < methodParams.Length; i++)
            {
                var argText = parts[i + 3].Text;
                Type paramType = methodParams[i].ParameterType;
                try
                {
                    methodArgsToSupply[i] = TypeDescriptor.GetConverter(paramType).ConvertFrom(argText);
                }
                catch (NotSupportedException)
                {
                    console.AddString(
                        $"Cannot convert argument {i + 1} '{argText}' to type of '{paramType.FullName}'");
                    return false;
                }
            }
            // Handle instance method calls by getting singleton instance (if possible).
            object targetInstance = null;
            if (!method.IsStatic)
            {
                targetInstance = targetClass == typeof(Player)
                    ? Player.m_localPlayer
                    : targetClass.GetField("m_instance", BindingFlags.Static | BindingFlags.NonPublic)
                        ?.GetValue(null);
                if (targetInstance == null)
                {
                    console.AddString(
                        $"Could not call method '{method.Name}' because it's an instance method and no static instance field is defined on it");
                    return false;
                }
            }

            // Call method, print result.
            var result = method.Invoke(targetInstance, methodArgsToSupply);
            if (result != null)
            {
                console.AddString(string.Join(", ", ArrayUtils.ToStringArray(result)));
            }
            return false;
        }
    }
}