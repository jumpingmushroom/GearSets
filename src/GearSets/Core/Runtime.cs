using System;

namespace GearSets.Core
{
    /// <summary>Per-frame work. Each part is isolated so one failure can't stop the others.</summary>
    internal static class Runtime
    {
        public static void Tick()
        {
            if (PluginConfig.Enabled == null || !PluginConfig.Enabled.Value)
                return;
            Safe("GearSets: swap tick failed", SwapExecutor.Tick);
            if (InventoryGui.IsVisible())
            {
                Safe("GearSets: tab tick failed", () => UI.GearSetsTab.Place(InventoryGui.instance));
                Safe("GearSets: window tick failed", UI.GearSetsWindow.Tick);
            }
        }

        private static void Safe(string key, Action a)
        {
            try
            {
                a();
            }
            catch (Exception e)
            {
                GearSetsPlugin.WarnOnce(key, e);
            }
        }
    }
}
