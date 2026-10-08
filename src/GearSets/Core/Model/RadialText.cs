using System;
using System.Collections.Generic;

namespace GearSets.Core.Model
{
    /// <summary>The status line and detail shown for a set in the radial (built-in and Classic).</summary>
    public static class RadialText
    {
        public sealed class Entry
        {
            public readonly SetState State;
            /// <summary>"Already wearing", "Ready", "1 item missing" or "N items missing".</summary>
            public readonly string Subtitle;
            /// <summary>"Missing: …" naming each missing item, else "N changes." or "Nothing to change."</summary>
            public readonly string Description;

            public Entry(SetState state, string subtitle, string description)
            {
                State = state;
                Subtitle = subtitle;
                Description = description;
            }
        }

        public static Entry For(SwapPlan plan, HotbarPlan hotbar, Func<string, string> localize)
        {
            int missing;
            SetState state = SetStatus.Of(plan, hotbar, out missing);
            string subtitle = state == SetState.Equipped ? "Already wearing" : SetStatus.Label(state, missing);

            var names = new List<string>();
            AddMissing(names, plan.Lines, localize);
            if (hotbar != null)
                AddMissing(names, hotbar.Lines, localize);
            int changes = plan.Steps.Count + (hotbar != null ? hotbar.Moves.Count : 0);
            string description = names.Count > 0 ? "Missing: " + ReportText.JoinAnd(names)
                : changes == 0 ? "Nothing to change." : changes == 1 ? "1 change." : changes + " changes.";
            return new Entry(state, subtitle, description);
        }

        private static void AddMissing(List<string> names, List<ReportLine> lines, Func<string, string> localize)
        {
            foreach (ReportLine l in lines)
            {
                if (l.Outcome != Outcome.Missing || l.Wanted == null)
                    continue;
                string n = localize(l.Wanted.Name);
                if (!names.Contains(n))
                    names.Add(n);
            }
        }
    }
}
