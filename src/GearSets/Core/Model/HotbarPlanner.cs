using System.Collections.Generic;

namespace GearSets.Core.Model
{
    /// <summary>
    /// Moves set items into hotbar positions (row 0). The item swaps places with the occupant;
    /// when the item came from an ExtraSlots cell the occupant goes to the first free normal cell
    /// below the hotbar instead. Gear worn in an ExtraSlots equipment cell is never moved.
    /// Run from a fresh snapshot after the equip steps have finished.
    /// </summary>
    public static class HotbarPlanner
    {
        public static HotbarPlan Plan(GearSet set, InventorySnapshot snap)
        {
            var plan = new HotbarPlan();
            var taken = new HashSet<int>();
            int n = snap.Items.Count;
            var px = new int[n];
            var py = new int[n];
            foreach (ItemFacts f in snap.Items)
            {
                px[f.Index] = f.X;
                py[f.Index] = f.Y;
            }

            for (int i = 0; i < GearSet.HotbarSize; i++)
            {
                SlotEntry e = set.Hotbar[i];
                if (e.Mode != EntryMode.Item)
                    continue;
                string label = "Hotbar " + (i + 1);
                bool sub;
                ItemFacts item = ItemMatcher.Find(e.Item, snap, taken, out sub);
                if (item == null)
                {
                    plan.Lines.Add(new ReportLine(label, Outcome.Missing, e.Item, null, null));
                    continue;
                }
                taken.Add(item.Index);
                int fx = px[item.Index], fy = py[item.Index];
                if (fx == i && fy == 0)
                {
                    plan.Lines.Add(new ReportLine(label, Outcome.AlreadyWorn, e.Item, item, null));
                    continue;
                }
                bool fromSlotCell = fy >= snap.PlayerRows;
                if (fromSlotCell && item.Worn != WornSlot.None)
                {
                    plan.Lines.Add(new ReportLine(label, Outcome.Skipped, e.Item, item, "worn in an equipment slot"));
                    continue;
                }

                int occ = Occupant(px, py, i, 0);
                if (occ >= 0)
                {
                    int tx = fx, ty = fy;
                    if (fromSlotCell && !FreeCell(snap, px, py, out tx, out ty))
                    {
                        plan.Lines.Add(new ReportLine(label, Outcome.Skipped, e.Item, item, "no room in the inventory"));
                        continue;
                    }
                    plan.Moves.Add(new Move(snap.Items[occ], tx, ty));
                    px[occ] = tx;
                    py[occ] = ty;
                }
                plan.Moves.Add(new Move(item, i, 0));
                px[item.Index] = i;
                py[item.Index] = 0;
                plan.Lines.Add(new ReportLine(label, sub ? Outcome.Substitute : Outcome.Equip, e.Item, item, null));
            }
            return plan;
        }

        private static int Occupant(int[] px, int[] py, int x, int y)
        {
            for (int j = 0; j < px.Length; j++)
                if (px[j] == x && py[j] == y)
                    return j;
            return -1;
        }

        /// <summary>First empty cell in the normal grid below the hotbar row.</summary>
        private static bool FreeCell(InventorySnapshot snap, int[] px, int[] py, out int x, out int y)
        {
            for (y = 1; y < snap.PlayerRows; y++)
                for (x = 0; x < snap.Width; x++)
                    if (Occupant(px, py, x, y) < 0)
                        return true;
            x = y = -1;
            return false;
        }
    }
}
