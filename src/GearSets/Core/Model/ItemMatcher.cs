using System.Collections.Generic;

namespace GearSets.Core.Model
{
    /// <summary>Finds the item that satisfies a set entry: the exact copy first, else the best substitute.</summary>
    public static class ItemMatcher
    {
        /// <summary>
        /// Tagged refs look for that exact copy (several copies with one tag: the higher durability).
        /// Otherwise, and always for stackables, the best item of the same prefab:
        /// substitutes prefer one already worn in <paramref name="preferWorn"/>, then highest quality,
        /// then the same variant, then highest durability; stackables prefer the same quality, then
        /// the biggest stack. Broken items and items in <paramref name="taken"/> never match.
        /// </summary>
        public static ItemFacts Find(ItemRef want, InventorySnapshot snap, ICollection<int> taken, out bool substitute,
            WornSlot preferWorn = WornSlot.None)
        {
            substitute = false;
            if (want.Tag != null)
            {
                ItemFacts exact = null;
                foreach (ItemFacts f in snap.Items)
                {
                    if (f.Tag != want.Tag || f.Broken || taken.Contains(f.Index))
                        continue;
                    if (exact == null || f.Durability > exact.Durability)
                        exact = f;
                }
                if (exact != null)
                    return exact;
            }

            ItemFacts best = null;
            foreach (ItemFacts f in snap.Items)
            {
                if (f.Prefab != want.Prefab || f.Broken || taken.Contains(f.Index))
                    continue;
                if (best == null || Better(f, best, want, preferWorn))
                    best = f;
            }
            substitute = best != null && want.Tag != null;
            return best;
        }

        private static bool Better(ItemFacts a, ItemFacts b, ItemRef want, WornSlot prefer)
        {
            if (want.Tag == null)
            {
                bool aq = a.Quality == want.Quality, bq = b.Quality == want.Quality;
                if (aq != bq)
                    return aq;
                return a.Stack > b.Stack;
            }
            bool aw = Prefers(a.Worn, prefer), bw = Prefers(b.Worn, prefer);
            if (aw != bw)
                return aw;
            if (a.Quality != b.Quality)
                return a.Quality > b.Quality;
            bool av = a.Variant == want.Variant, bv = b.Variant == want.Variant;
            if (av != bv)
                return av;
            return a.Durability > b.Durability;
        }

        private static bool Prefers(WornSlot worn, WornSlot prefer)
        {
            if (prefer == WornSlot.None || worn == WornSlot.None)
                return false;
            return worn == prefer || (Slots.IsHand(prefer) && Slots.IsHand(worn));
        }
    }
}
