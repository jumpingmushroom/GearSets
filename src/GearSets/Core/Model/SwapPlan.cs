using System.Collections.Generic;

namespace GearSets.Core.Model
{
    public enum StepKind { Unequip, Equip, CancelQueued }

    /// <summary>One action for the executor. CancelQueued removes a pending vanilla queue entry.</summary>
    public sealed class Step
    {
        public readonly StepKind Kind;
        public readonly ItemFacts Item;

        public Step(StepKind kind, ItemFacts item)
        {
            Kind = kind;
            Item = item;
        }
    }

    public enum Outcome { AlreadyWorn, Equip, Substitute, Missing, Unequip, Skipped, Cleared }

    /// <summary>What happens to one entry, for the message after a swap and the window's statuses.</summary>
    public sealed class ReportLine
    {
        public readonly string Slot;
        public readonly Outcome Outcome;
        /// <summary>The set's ref, or null for Unequip and Cleared.</summary>
        public readonly ItemRef Wanted;
        /// <summary>The item acted on or found, if any.</summary>
        public readonly ItemFacts Used;
        /// <summary>Reason, for Skipped.</summary>
        public readonly string Note;

        public ReportLine(string slot, Outcome outcome, ItemRef wanted, ItemFacts used, string note)
        {
            Slot = slot;
            Outcome = outcome;
            Wanted = wanted;
            Used = used;
            Note = note;
        }
    }

    public sealed class SwapPlan
    {
        public readonly List<Step> Steps = new List<Step>();
        public readonly List<ReportLine> Lines = new List<ReportLine>();

        public bool NothingToDo { get { return Steps.Count == 0; } }

        public int Count(Outcome o)
        {
            int n = 0;
            foreach (ReportLine l in Lines)
                if (l.Outcome == o)
                    n++;
            return n;
        }
    }

    /// <summary>Put Item at (X, Y) by setting its grid position.</summary>
    public sealed class Move
    {
        public readonly ItemFacts Item;
        public readonly int X;
        public readonly int Y;

        public Move(ItemFacts item, int x, int y)
        {
            Item = item;
            X = x;
            Y = y;
        }
    }

    public sealed class HotbarPlan
    {
        public readonly List<Move> Moves = new List<Move>();
        public readonly List<ReportLine> Lines = new List<ReportLine>();

        public int Count(Outcome o)
        {
            int n = 0;
            foreach (ReportLine l in Lines)
                if (l.Outcome == o)
                    n++;
            return n;
        }
    }

    public enum SetState { Equipped, Ready, Missing }

    public static class SetStatus
    {
        public static SetState Of(SwapPlan plan, HotbarPlan hotbar, out int missing)
        {
            missing = plan.Count(Outcome.Missing) + (hotbar != null ? hotbar.Count(Outcome.Missing) : 0);
            if (missing > 0)
                return SetState.Missing;
            bool hotbarDone = hotbar == null || hotbar.Moves.Count == 0;
            return plan.NothingToDo && hotbarDone ? SetState.Equipped : SetState.Ready;
        }

        public static string Label(SetState state, int missing)
        {
            switch (state)
            {
                case SetState.Equipped: return "Equipped";
                case SetState.Missing: return missing == 1 ? "1 item missing" : missing + " items missing";
                default: return "Ready";
            }
        }
    }
}
