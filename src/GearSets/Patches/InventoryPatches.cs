using System;
using GearSets.UI;
using HarmonyLib;

namespace GearSets.Patches
{
    /// <summary>Postfix only: vanilla Show always runs; the tab and window follow it.</summary>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    internal static class InventoryShowPatch
    {
        private static void Postfix(InventoryGui __instance)
        {
            try
            {
                if (!PluginConfig.Enabled.Value)
                    return;
                GearSetsTab.Ensure(__instance);
                GearSetsWindow.OnInventoryShow(__instance);
                WindowContent.EnsureBuilt();
            }
            catch (Exception e)
            {
                GearSetsPlugin.WarnOnce("GearSets: inventory UI failed to open", e);
            }
        }
    }
}
