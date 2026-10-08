using System;
using System.Collections.Generic;
using GearSets.UI;
using HarmonyLib;
using Valheim.UI;

namespace GearSets.Patches
{
    /// <summary>
    /// Adds the Gear Sets group to Valheim's main radial. ValheimRadialConfig.InitRadialConfig builds
    /// its element list locally and hands it to ConstructRadial, so a prefix there (while the main
    /// config is current) can append to that list without a transpiler.
    /// </summary>
    [HarmonyPatch(typeof(RadialBase), nameof(RadialBase.ConstructRadial))]
    internal static class MainRadialGroupPatch
    {
        private static void Prefix(RadialBase __instance, List<RadialMenuElement> elements)
        {
            try
            {
                if (elements != null && __instance != null && __instance.CurrentConfig is ValheimRadialConfig)
                    BuiltInRadial.AddToMain(__instance, elements);
            }
            catch (Exception e)
            {
                GearSetsPlugin.WarnOnce("GearSets: adding the Gear Sets group to the radial failed", e);
            }
        }
    }
}
