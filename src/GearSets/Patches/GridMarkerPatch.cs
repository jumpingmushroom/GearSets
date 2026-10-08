using System;
using System.Collections.Generic;
using GearSets.Core;
using GearSets.Integrations;
using GearSets.UI;
using HarmonyLib;
using UnityEngine;

namespace GearSets.Patches
{
    /// <summary>
    /// A small mint diamond on player-inventory cells holding set items (including ExtraSlots' cells,
    /// which are elements of the same grid). Parented to the element itself, not m_icon, which
    /// MyLittleUI scales and CoolCount dims. Bottom-left by default: top-right holds MyLittleUI's
    /// quality stars and Larder's food icon.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGrid), "UpdateGui")]
    [HarmonyAfter(ModIds.MyLittleUI, ModIds.QuickStack, ModIds.ExtraSlots)]
    [HarmonyPriority(Priority.Low)]
    internal static class GridMarkerPatch
    {
        private const string MarkerName = "GearSets.Marker";
        private static readonly Dictionary<int, ItemDrop.ItemData> ByCell = new Dictionary<int, ItemDrop.ItemData>();

        private static void Postfix(InventoryGrid __instance)
        {
            try
            {
                InventoryGui gui = InventoryGui.instance;
                if (gui == null || __instance != gui.m_playerGrid || __instance.m_elements == null)
                    return;
                bool on = PluginConfig.Enabled.Value && PluginConfig.ShowCellMarker.Value;
                ByCell.Clear();
                Inventory inv = __instance.m_inventory;
                if (on && inv != null)
                    foreach (ItemDrop.ItemData it in inv.GetAllItems())
                        ByCell[it.m_gridPos.y * 1000 + it.m_gridPos.x] = it;

                foreach (InventoryElement el in __instance.m_elements)
                {
                    if (el == null)
                        continue;
                    ItemDrop.ItemData item;
                    bool mark = on && ByCell.TryGetValue(el.Position.y * 1000 + el.Position.x, out item)
                        && SetStore.SetsFor(Tags.Get(item)).Count > 0;
                    Transform marker = el.transform.Find(MarkerName);
                    if (!mark)
                    {
                        if (marker != null && marker.gameObject.activeSelf)
                            marker.gameObject.SetActive(false);
                        continue;
                    }
                    if (marker == null)
                        marker = UiKit.Diamond(el.transform, MarkerName, 8f, UiKit.Mint).transform;
                    Place((RectTransform)marker, PluginConfig.CellMarkerCorner.Value);
                    if (!marker.gameObject.activeSelf)
                        marker.gameObject.SetActive(true);
                }
            }
            catch (Exception e)
            {
                GearSetsPlugin.WarnOnce("GearSets: cell marker failed", e);
            }
        }

        private static void Place(RectTransform rt, MarkerCorner corner)
        {
            Vector2 a;
            Vector2 pos;
            switch (corner)
            {
                case MarkerCorner.BottomRight: a = new Vector2(1f, 0f); pos = new Vector2(-7f, 10f); break;
                case MarkerCorner.TopLeft: a = new Vector2(0f, 1f); pos = new Vector2(7f, -7f); break;
                case MarkerCorner.TopRight: a = new Vector2(1f, 1f); pos = new Vector2(-7f, -7f); break;
                default: a = new Vector2(0f, 0f); pos = new Vector2(7f, 10f); break; // above the durability bar
            }
            rt.anchorMin = rt.anchorMax = a;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
        }
    }
}
