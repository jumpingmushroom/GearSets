namespace GearSets.Core
{
    internal static class SwapGate
    {
        public const string CantChange = "Can't change gear right now";

        /// <summary>
        /// Null when a swap may start. Swimming is refused too: ToggleMovementMod re-equips the old
        /// weapon on leaving water, which would undo the swap.
        /// </summary>
        public static string Refusal(Player p)
        {
            if (p == null)
                return CantChange;
            if (p.IsDead() || p.IsTeleporting() || p.InCutscene() || p.InAttack() || p.InDodge() || p.IsSwimming())
                return CantChange;
            return null;
        }
    }
}
