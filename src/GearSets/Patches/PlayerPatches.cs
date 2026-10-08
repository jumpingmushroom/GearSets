using System;
using GearSets.Core;
using GearSets.UI;
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

    /// <summary>While the radial is open, mouse movement aims the radial instead of the camera.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.SetMouseLook))]
    internal static class RadialLookPatch
    {
        private static bool Prefix()
        {
            return !RadialPicker.IsOpen;
        }
    }

    /// <summary>No attacks, blocks, jumps or dodges while picking a set; walking still works.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
    internal static class RadialControlsPatch
    {
        private static void Prefix(ref bool attack, ref bool attackHold, ref bool secondaryAttack, ref bool secondaryAttackHold,
            ref bool block, ref bool blockHold, ref bool jump, ref bool dodge)
        {
            if (!RadialPicker.IsOpen)
                return;
            attack = attackHold = secondaryAttack = secondaryAttackHold = false;
            block = blockHold = jump = dodge = false;
        }
    }
}
