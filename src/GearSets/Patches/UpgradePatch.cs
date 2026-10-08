using System;
using System.Collections.Generic;
using GearSets.Core;
using HarmonyLib;

namespace GearSets.Patches
{
    /// <summary>
    /// Upgrading at a station removes the item and adds a fresh ItemData of the same prefab at the
    /// same grid position (InventoryGui.DoCrafting), which loses m_customData. Carry the set tag
    /// over to the new copy so sets keep following it.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
    internal static class UpgradePatch
    {
        internal sealed class State
        {
            public string Tag;
            public string Prefab;
            public int X;
            public int Y;
            public HashSet<ItemDrop.ItemData> Before;
        }

        private static void Prefix(InventoryGui __instance, Player player, out State __state)
        {
            __state = null;
            try
            {
                ItemDrop.ItemData up = __instance.m_craftUpgradeItem;
                string tag = Tags.Get(up);
                if (tag == null || player == null || player.GetInventory() == null)
                    return;
                __state = new State
                {
                    Tag = tag,
                    Prefab = Prefab(up),
                    X = up.m_gridPos.x,
                    Y = up.m_gridPos.y,
                    Before = new HashSet<ItemDrop.ItemData>(player.GetInventory().GetAllItems())
                };
            }
            catch (Exception e)
            {
                __state = null;
                GearSetsPlugin.WarnOnce("GearSets: upgrade tag capture failed", e);
            }
        }

        private static void Postfix(Player player, State __state)
        {
            if (__state == null)
                return;
            try
            {
                if (player == null || player.GetInventory() == null)
                    return;
                foreach (ItemDrop.ItemData it in __state.Before)
                    if (Tags.Get(it) == __state.Tag && player.GetInventory().ContainsItem(it))
                        return; // not upgraded (requirements, cancelled): the old copy is still there
                ItemDrop.ItemData pick = null;
                foreach (ItemDrop.ItemData it in player.GetInventory().GetAllItems())
                {
                    if (__state.Before.Contains(it) || Tags.Get(it) != null || Prefab(it) != __state.Prefab)
                        continue;
                    if (it.m_gridPos.x == __state.X && it.m_gridPos.y == __state.Y)
                    {
                        pick = it;
                        break;
                    }
                    pick = it; // else the newest one added
                }
                if (pick == null)
                    return;
                Tags.Set(pick, __state.Tag);
            }
            catch (Exception e)
            {
                GearSetsPlugin.WarnOnce("GearSets: upgrade tag carry-over failed", e);
            }
        }

        private static string Prefab(ItemDrop.ItemData it)
        {
            return it.m_dropPrefab != null ? it.m_dropPrefab.name : it.m_shared.m_name;
        }
    }
}
