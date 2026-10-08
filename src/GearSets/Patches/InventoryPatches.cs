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

    /// <summary>
    /// InventoryGui.Update closes the inventory on Use (E), Inventory (Tab) and Esc without checking
    /// the text popup, so typing a set name would close it. Skip it only while our name popup is up.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), "Update")]
    internal static class InventoryUpdateGuard
    {
        private static bool Prefix()
        {
            try
            {
                NameInput.Poll();
                return !(NameInput.Pending && TextInput.IsVisible());
            }
            catch (Exception e)
            {
                GearSetsPlugin.WarnOnce("GearSets: inventory update guard failed", e);
                return true;
            }
        }
    }
}
