using System.Collections.Generic;

namespace GearSets.Core.Model
{
    /// <summary>
    /// Turns a set and a snapshot into ordered equip/unequip steps and report lines. Already worn
    /// items produce no step, so planning again after an interruption finishes the swap.
    /// </summary>
    public static class SwapPlanner
    {
        private static readonly SlotKind[] Armour = { SlotKind.Helmet, SlotKind.Chest, SlotKind.Legs, SlotKind.Shoulder };
        private static readonly SlotKind[] Tail = { SlotKind.Trinket, SlotKind.Ammo };

        public static SwapPlan Plan(GearSet set, InventorySnapshot snap)
        {
            var plan = new SwapPlan();
            var taken = new HashSet<int>();
            var unequips = new List<Step>();
            var hands = new List<Step>();
            var rest = new List<Step>();

            PlanHands(set, snap, taken, plan, unequips, hands);
            foreach (SlotKind k in Armour)
                PlanSingle(set, k, snap, taken, plan, unequips, rest);
            PlanUtilities(set, snap, taken, plan, unequips, rest);
            foreach (SlotKind k in Tail)
                PlanSingle(set, k, snap, taken, plan, unequips, rest);

            plan.Steps.AddRange(unequips);
            plan.Steps.AddRange(hands);
            plan.Steps.AddRange(rest);
            return plan;
        }

        /// <summary>
        /// Hands as a pair. A two-handed item can't share: the right hand wins. Order is left then
        /// right (so AutoShield sees the shield already on), except a torch, which only goes to the
        /// left hand once a one-handed weapon is in the right.
        /// </summary>
        private static void PlanHands(GearSet set, InventorySnapshot snap, HashSet<int> taken, SwapPlan plan,
            List<Step> unequips, List<Step> hands)
        {
            SlotEntry left = set.Slots[SlotKind.LeftHand];
            SlotEntry right = set.Slots[SlotKind.RightHand];
            ItemFacts wornL = snap.WornIn(WornSlot.LeftHand);
            ItemFacts wornR = snap.WornIn(WornSlot.RightHand);

            bool rs = false, ls = false;
            ItemFacts r = right.Mode == EntryMode.Item
                ? ItemMatcher.Find(right.Item, snap, taken, out rs, WornSlot.RightHand) : null;
            var takenWithR = new HashSet<int>(taken);
            if (r != null)
                takenWithR.Add(r.Index);
            ItemFacts l = left.Mode == EntryMode.Item
                ? ItemMatcher.Find(left.Item, snap, takenWithR, out ls, WornSlot.LeftHand) : null;

            bool leftSkipped = false;
            if (r != null && l != null && (r.TwoHanded || l.TwoHanded))
            {
                plan.Lines.Add(new ReportLine("Left hand", Outcome.Skipped, left.Item, null,
                    "can't be held together with a two-handed item"));
                l = null;
                leftSkipped = true;
            }

            var rightSteps = new List<Step>();
            var leftSteps = new List<Step>();
            if (right.Mode == EntryMode.Item)
            {
                if (r == null)
                    plan.Lines.Add(new ReportLine("Right hand", Outcome.Missing, right.Item, null, null));
                else
                    Commit(right.Item, r, rs, "Right hand", WornSlot.RightHand, taken, plan, rightSteps);
            }
            if (left.Mode == EntryMode.Item && !leftSkipped)
            {
                if (l == null)
                    plan.Lines.Add(new ReportLine("Left hand", Outcome.Missing, left.Item, null, null));
                else
                    Commit(left.Item, l, ls, "Left hand", WornSlot.LeftHand, taken, plan, leftSteps);
            }

            if (right.Mode == EntryMode.Empty && wornR != null && !taken.Contains(wornR.Index))
                Remove(wornR, "Right hand", plan, unequips);
            if (left.Mode == EntryMode.Empty && wornL != null && !taken.Contains(wornL.Index))
                Remove(wornL, "Left hand", plan, unequips);

            if (r != null && r.TwoHanded && left.Mode == EntryMode.Ignore && rightSteps.Count > 0
                && wornL != null && !taken.Contains(wornL.Index))
                plan.Lines.Add(new ReportLine("Left hand", Outcome.Cleared, null, wornL, null));
            if (l != null && l.TwoHanded && right.Mode == EntryMode.Ignore && leftSteps.Count > 0
                && wornR != null && !taken.Contains(wornR.Index))
                plan.Lines.Add(new ReportLine("Right hand", Outcome.Cleared, null, wornR, null));

            if (l != null && l.Fits == FitSlot.Torch)
            {
                hands.AddRange(rightSteps);
                hands.AddRange(leftSteps);
            }
            else
            {
                hands.AddRange(leftSteps);
                hands.AddRange(rightSteps);
            }
        }

        private static void PlanSingle(GearSet set, SlotKind k, InventorySnapshot snap, HashSet<int> taken,
            SwapPlan plan, List<Step> unequips, List<Step> into)
        {
            SlotEntry e = set.Slots[k];
            string label = Slots.Label(k);
            WornSlot ws = Slots.Worn(k);
            if (e.Mode == EntryMode.Ignore)
                return;
            if (e.Mode == EntryMode.Empty)
            {
                ItemFacts worn = snap.WornIn(ws);
                if (worn != null && !taken.Contains(worn.Index))
                    Remove(worn, label, plan, unequips);
                return;
            }
            bool sub;
            ItemFacts item = ItemMatcher.Find(e.Item, snap, taken, out sub, ws);
            if (item == null)
            {
                plan.Lines.Add(new ReportLine(label, Outcome.Missing, e.Item, null, null));
                return;
            }
            Commit(e.Item, item, sub, label, ws, taken, plan, into);
        }

        /// <summary>Utilities not in the set come off first; otherwise ExtraSlots would add the new one beside them.</summary>
        private static void PlanUtilities(GearSet set, InventorySnapshot snap, HashSet<int> taken, SwapPlan plan,
            List<Step> unequips, List<Step> rest)
        {
            UtilityEntry u = set.Utilities;
            if (u.Mode == EntryMode.Ignore)
                return;
            var keep = new HashSet<int>();
            var equips = new List<Step>();
            if (u.Mode == EntryMode.Item)
            {
                int n = 0;
                foreach (ItemRef want in u.Items)
                {
                    if (++n > snap.UtilitySlots)
                    {
                        plan.Lines.Add(new ReportLine("Utility", Outcome.Skipped, want, null, "no free utility slot"));
                        continue;
                    }
                    bool sub;
                    ItemFacts item = ItemMatcher.Find(want, snap, taken, out sub, WornSlot.Utility);
                    if (item == null)
                    {
                        plan.Lines.Add(new ReportLine("Utility", Outcome.Missing, want, null, null));
                        continue;
                    }
                    keep.Add(item.Index);
                    Commit(want, item, sub, "Utility", WornSlot.Utility, taken, plan, equips);
                }
            }
            foreach (ItemFacts w in snap.WornUtilities())
                if (!keep.Contains(w.Index) && !taken.Contains(w.Index))
                    Remove(w, "Utility", plan, unequips);
            rest.AddRange(equips);
        }

        private static void Commit(ItemRef want, ItemFacts item, bool substitute, string label, WornSlot target,
            HashSet<int> taken, SwapPlan plan, List<Step> into)
        {
            taken.Add(item.Index);
            bool worn = item.Worn == target || (Slots.IsHand(target) && Slots.IsHand(item.Worn));
            if (worn)
            {
                if (item.Queued == QueuedAs.Unequip)
                    into.Add(new Step(StepKind.CancelQueued, item));
                plan.Lines.Add(new ReportLine(label, substitute ? Outcome.Substitute : Outcome.AlreadyWorn, want, item, null));
                return;
            }
            if (item.Queued != QueuedAs.Equip)
                into.Add(new Step(StepKind.Equip, item));
            plan.Lines.Add(new ReportLine(label, substitute ? Outcome.Substitute : Outcome.Equip, want, item, null));
        }

        private static void Remove(ItemFacts worn, string label, SwapPlan plan, List<Step> unequips)
        {
            if (worn.Queued != QueuedAs.Unequip)
                unequips.Add(new Step(StepKind.Unequip, worn));
            plan.Lines.Add(new ReportLine(label, Outcome.Unequip, null, worn, null));
        }
    }
}
