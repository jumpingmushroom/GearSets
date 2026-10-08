using System;
using GearSets.Core;
using HarmonyLib;

namespace GearSets.Patches
{
    /// <summary>Loads the local player's sets once m_customData is populated (login and after death).</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    internal static class PlayerSpawnPatch
    {
        private static void Postfix(Player __instance)
        {
            try
            {
                if (__instance == Player.m_localPlayer)
                    SetStore.Load(__instance);
            }
            catch (Exception e)
            {
                GearSetsPlugin.WarnOnce("GearSets: loading sets failed", e);
            }
        }
    }
}
