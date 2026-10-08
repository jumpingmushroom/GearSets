using System;
using System.Collections.Generic;
using System.Text;

namespace GearSets.Core.Model
{
    /// <summary>The top-left message after a swap. Missing items are named, never just counted.</summary>
    public static class ReportText
    {
        public static string Compose(string setName, SwapPlan plan, SwapPlan remaining, HotbarPlan hotbar,
            Func<string, string> localize)
        {
            var lines = new List<ReportLine>(plan.Lines);
            if (hotbar != null)
                lines.AddRange(hotbar.Lines);

            var notDone = new List<string>();
            if (remaining != null)
                foreach (Step s in remaining.Steps)
                    if (s.Kind != StepKind.CancelQueued)
                        AddOnce(notDone, localize(s.Item.Name));

            var missing = new List<string>();
            var skipped = new List<string>();
            var cleared = new List<string>();
            foreach (ReportLine l in lines)
            {
                switch (l.Outcome)
                {
                    case Outcome.Missing:
                        AddOnce(missing, localize(l.Wanted.Name));
                        break;
                    case Outcome.Substitute:
                    {
                        string want = localize(l.Wanted.Name);
                        string used = localize(l.Used.Name);
                        AddOnce(missing, want + " (used " + (used == want ? "another copy" : used) + ")");
                        break;
                    }
                    case Outcome.Skipped:
                        AddOnce(skipped, localize(l.Wanted != null ? l.Wanted.Name : l.Slot) + " (" + l.Note + ")");
                        break;
                    case Outcome.Cleared:
                        AddOnce(cleared, l.Slot);
                        break;
                }
            }

            bool hotbarMoved = hotbar != null && hotbar.Moves.Count > 0;
            var sb = new StringBuilder();
            if (notDone.Count > 0)
                sb.Append(setName).Append(" interrupted: ").Append(JoinAnd(notDone)).Append(" not equipped. Use the set again to finish.");
            else if (plan.NothingToDo && !hotbarMoved && missing.Count == 0 && skipped.Count == 0)
                sb.Append(setName).Append(" is already equipped.");
            else
                sb.Append(setName).Append(" equipped.");
            if (missing.Count > 0)
                sb.Append(" Missing: ").Append(string.Join(", ", missing.ToArray())).Append('.');
            if (skipped.Count > 0)
                sb.Append(" Skipped: ").Append(string.Join("; ", skipped.ToArray())).Append('.');
            foreach (string slot in cleared)
                sb.Append(' ').Append(slot).Append(" cleared by a two-handed item.");
            return sb.ToString();
        }

        public static string JoinAnd(IList<string> items)
        {
            if (items.Count == 0)
                return "";
            if (items.Count == 1)
                return items[0];
            var head = new string[items.Count - 1];
            for (int i = 0; i < head.Length; i++)
                head[i] = items[i];
            return string.Join(", ", head) + " and " + items[items.Count - 1];
        }

        private static void AddOnce(List<string> list, string s)
        {
            if (!list.Contains(s))
                list.Add(s);
        }
    }
}
