using System.Linq;
using BepInEx;
using HarmonyLib;

namespace ModTemplateValheim
{
    [BepInPlugin("com.github.measurity.modtemplatevalheim", "ModTemplateValheim", "1.0.0.0")]
    public class Mod : BaseUnityPlugin
    {
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
