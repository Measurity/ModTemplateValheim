using BepInEx;
using HarmonyLib;

namespace ExampleCallMethodMod;

public partial class Mod : BaseUnityPlugin
{
    private static readonly Harmony harmony = new(PluginGuid);
    
    private void Awake()
    {
        // Add or change your patches in "Patches" folder.
        harmony.PatchAll();
    }

    private void OnDestroy()
    {
        harmony.UnpatchSelf();
    }
}