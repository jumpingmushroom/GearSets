using System;
using GearSets.Core;
using GearSets.Integrations;
using HarmonyLib;

namespace GearSets.Patches
{
    /// <summary>
    /// Appends "Gear sets: A, B" to the static tooltip builder. Runs after MyLittleUI, which regroups
    /// lines appended before it. Skips crafting previews and nested (appended) tooltips.
    /// </summary>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
        new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool) })]
    [HarmonyAfter(ModIds.MyLittleUI)]
    [HarmonyPriority(Priority.Low)]
    internal static class TooltipPatch
    {
        private static void Postfix(ItemDrop.ItemData item, bool crafting, bool appending, ref string __result)
        {
            try
            {
                if (crafting || appending || item == null || !PluginConfig.Enabled.Value || !PluginConfig.ShowTooltipLine.Value)
                    return;
                var names = SetStore.SetsFor(Tags.Get(item));
                if (names.Count == 0)
                    return;
                string[] arr = new string[names.Count];
                names.CopyTo(arr, 0);
                __result += "\n\n<color=#7ee0c3>Gear sets: " + string.Join(", ", arr) + "</color>";
            }
            catch (Exception e)
            {
                GearSetsPlugin.WarnOnce("GearSets: tooltip line failed", e);
            }
        }
    }
}
