using System;
using System.Collections.Generic;
using GearSets.Core;
using GearSets.Core.Model;
using GearSets.UI;
using HarmonyLib;

namespace GearSets.Patches
{
    /// <summary>
    /// Humanoid.DropItem is the one path every drop takes (drag out of the window, the drop modifier,
    /// moving with no container open). A set item asks first; on yes the drop is re-run with a bypass.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.DropItem))]
    internal static class DropGuard
    {
        private static bool _bypass;

        private static bool Prefix(Humanoid __instance, Inventory inventory, ItemDrop.ItemData item, int amount, ref bool __result)
        {
            try
            {
                if (_bypass || item == null || __instance != Player.m_localPlayer || !PluginConfig.Enabled.Value || !PluginConfig.ConfirmDrop.Value)
                    return true;
                IList<string> sets = SetStore.SetsFor(Tags.Get(item));
                if (sets.Count == 0)
                    return true;
                if (InventoryGui.instance != null)
                    InventoryGui.instance.SetupDragItem(null, null, 1);
                string name = Loc.T(item.m_shared.m_name);
                Confirm.Ask("Drop " + name + "?",
                    "It's in your gear sets " + ReportText.JoinAnd(ToList(sets)) + ". Those sets will report it missing until you pick it up again.",
                    () =>
                    {
                        Player p = Player.m_localPlayer;
                        if (p == null)
                            return;
                        _bypass = true;
                        try
                        {
                            p.DropItem(inventory, item, amount);
                        }
                        finally
                        {
                            _bypass = false;
                        }
                    });
                __result = false;
                return false;
            }
            catch (Exception e)
            {
                GearSetsPlugin.WarnOnce("GearSets: drop guard failed", e);
                return true;
            }
        }

        internal static List<string> ToList(IList<string> names)
        {
            return new List<string>(names);
        }
    }

    /// <summary>The obliterator's lever asks first when its container holds set items.</summary>
    [HarmonyPatch(typeof(Incinerator), "OnIncinerate")]
    internal static class ObliterateGuard
    {
        private static bool _bypass;

        private static bool Prefix(Incinerator __instance, Switch sw, Humanoid user, ItemDrop.ItemData item, ref bool __result)
        {
            try
            {
                if (_bypass || user != Player.m_localPlayer || __instance.m_container == null
                    || !PluginConfig.Enabled.Value || !PluginConfig.ConfirmObliterate.Value)
                    return true;
                var names = new List<string>();
                foreach (ItemDrop.ItemData it in __instance.m_container.GetInventory().GetAllItems())
                    if (SetStore.SetsFor(Tags.Get(it)).Count > 0)
                        names.Add(Loc.T(it.m_shared.m_name));
                if (names.Count == 0)
                    return true;
                Confirm.Ask("Obliterate " + names.Count + " gear set item" + (names.Count == 1 ? "" : "s") + "?",
                    ReportText.JoinAnd(names) + (names.Count == 1 ? " is" : " are") + " in your gear sets and will be destroyed.",
                    () =>
                    {
                        _bypass = true;
                        try
                        {
                            __instance.OnIncinerate(sw, user, item);
                        }
                        finally
                        {
                            _bypass = false;
                        }
                    });
                __result = false;
                return false;
            }
            catch (Exception e)
            {
                GearSetsPlugin.WarnOnce("GearSets: obliterator guard failed", e);
                return true;
            }
        }
    }
}
