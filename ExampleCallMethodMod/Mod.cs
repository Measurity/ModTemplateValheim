using BepInEx;

namespace ExampleCallMethodMod;

public partial class Mod : BaseUnityPlugin
{
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