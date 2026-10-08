using System;

namespace GearSets.Core
{
    /// <summary>Per-frame work. Each part is isolated so one failure can't stop the others.</summary>
    internal static class Runtime
    {
        public static void Tick()
        {
            if (PluginConfig.Enabled == null)
                return;
            if (!PluginConfig.Enabled.Value)
            {
                if (UI.RadialPicker.IsOpen)
                    UI.RadialPicker.Close();
                return;
            }
            // A failing tick resets its state, so a swap can't stay busy and the radial can't stay open.
            Safe("GearSets: swap tick failed", SwapExecutor.Tick, SwapExecutor.Reset);
            Safe("GearSets: radial tick failed", UI.RadialPicker.Tick, UI.RadialPicker.Close);
            if (InventoryGui.IsVisible())
            {
                Safe("GearSets: tab tick failed", () => UI.GearSetsTab.Place(InventoryGui.instance));
                Safe("GearSets: window tick failed", UI.GearSetsWindow.Tick);
            }
        }

        private static void Safe(string key, Action a, Action onError = null)
        {
            try
            {
                a();
            }
            catch (Exception e)
            {
                GearSetsPlugin.WarnOnce(key, e);
                if (onError == null)
                    return;
                try
                {
                    onError();
                }
                catch (Exception e2)
                {
                    GearSetsPlugin.WarnOnce(key + " (reset)", e2);
                }
            }
        }
    }
}
