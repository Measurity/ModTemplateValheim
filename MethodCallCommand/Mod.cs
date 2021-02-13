using BepInEx;

namespace MethodCallCommand
{
    [BepInPlugin("com.github.measurity.methodcallcommandplugin", "MethodCallCommand", "1.0.0.0")]
    public class Mod : BaseUnityPlugin
    {
        private void Awake()
        {
            ModApi.Init();
        }

        private void OnDestroy()
        {
            ModApi.Dispose();
        }
    }
}
