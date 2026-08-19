using MelonLoader;

[assembly: MelonInfo(typeof(ModsPanel.Core), "ModsPanel", "1.1.0", "Rusty", null)]
[assembly: MelonGame("NestedLoop", "BOXROOM")]

namespace ModsPanel
{
    /// <summary>
    /// Starts the shared Mods settings screen. Other mods may register their
    /// sections before BOXROOM's Main scene exists; the registry keeps those
    /// definitions until the Mods tab is ready.
    /// </summary>
    public sealed class Core : MelonMod
    {
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            ModsPanelRuntime.Ensure().RequestInstall();
        }
    }
}
