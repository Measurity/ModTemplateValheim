using System.Linq;
using BepInEx;
using HarmonyLib;

namespace ModTemplateValheim
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Mod : BaseUnityPlugin
    {
        public const string PluginAuthor = "Measurity";
        public const string PluginGuid = "com.github.measurity.modtemplatevalheim";
        public const string PluginName = "ModTemplateValheim";
        public const string PluginVersion = "1.0.0.0";

        private static readonly Harmony harmony = new(typeof(Mod).GetCustomAttributes(typeof(BepInPlugin), false)
            .Cast<BepInPlugin>()
            .First()
            .GUID);

        private void Awake()
        {
            harmony.PatchAll();
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
}
