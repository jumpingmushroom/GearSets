using System.Collections.Generic;
using GearSets.Core.Model;
using Xunit;

namespace GearSets.Tests
{
    public class RadialTextTests
    {
        private static string L(string token)
        {
            var names = new Dictionary<string, string>
            {
                ["$item_capewolf"] = "Wolf Fur Cape",
                ["$item_pickaxeiron"] = "Iron Pickaxe",
                ["$item_shieldbanded"] = "Banded Shield"
            };
            return names.TryGetValue(token, out string n) ? n : token;
        }

        [Fact]
        public void NothingToDoIsAlreadyWearing()
        {
            RadialText.Entry e = RadialText.For(new SwapPlan(), null, L);
            Assert.Equal(SetState.Equipped, e.State);
            Assert.Equal("Already wearing", e.Subtitle);
            Assert.Equal("Nothing to change.", e.Description);
        }

        [Fact]
        public void ReadyCountsSteps()
        {
            var plan = new SwapPlan();
            ItemFacts helm = T.Item("HelmetIron", FitSlot.Helmet);
            plan.Steps.Add(new Step(StepKind.Equip, helm));
            plan.Lines.Add(new ReportLine("Helmet", Outcome.Equip, T.Ref(helm), helm, null));
            RadialText.Entry e = RadialText.For(plan, null, L);
            Assert.Equal(SetState.Ready, e.State);
            Assert.Equal("Ready", e.Subtitle);
            Assert.Equal("1 change.", e.Description);
        }

        [Fact]
        public void HotbarMovesCountAsChanges()
        {
            var plan = new SwapPlan();
            ItemFacts helm = T.Item("HelmetIron", FitSlot.Helmet);
            plan.Steps.Add(new Step(StepKind.Equip, helm));
            var hot = new HotbarPlan();
            hot.Moves.Add(new Move(T.Item("PickaxeIron", FitSlot.RightHand), 0, 0));
            hot.Moves.Add(new Move(T.Item("ShieldBanded", FitSlot.LeftHand), 1, 0));
            RadialText.Entry e = RadialText.For(plan, hot, L);
            Assert.Equal("Ready", e.Subtitle);
            Assert.Equal("3 changes.", e.Description);
        }

        [Fact]
        public void OneMissingIsNamed()
        {
            var plan = new SwapPlan();
            plan.Lines.Add(new ReportLine("Cape", Outcome.Missing, T.Ref("CapeWolf", "c"), null, null));
            RadialText.Entry e = RadialText.For(plan, null, L);
            Assert.Equal(SetState.Missing, e.State);
            Assert.Equal("1 item missing", e.Subtitle);
            Assert.Equal("Missing: Wolf Fur Cape", e.Description);
        }

        [Fact]
        public void HotbarMissingCountsAndIsNamed()
        {
            var plan = new SwapPlan();
            plan.Lines.Add(new ReportLine("Cape", Outcome.Missing, T.Ref("CapeWolf", "c"), null, null));
            var hot = new HotbarPlan();
            hot.Lines.Add(new ReportLine("Hotbar 1", Outcome.Missing, T.Ref("PickaxeIron", "p"), null, null));
            hot.Lines.Add(new ReportLine("Hotbar 2", Outcome.Missing, T.Ref("ShieldBanded", "s"), null, null));
            RadialText.Entry e = RadialText.For(plan, hot, L);
            Assert.Equal("3 items missing", e.Subtitle);
            Assert.Equal("Missing: Wolf Fur Cape, Iron Pickaxe and Banded Shield", e.Description);
        }

        [Fact]
        public void SameMissingItemIsNamedOnce()
        {
            var plan = new SwapPlan();
            plan.Lines.Add(new ReportLine("Right hand", Outcome.Missing, T.Ref("PickaxeIron", "p"), null, null));
            var hot = new HotbarPlan();
            hot.Lines.Add(new ReportLine("Hotbar 1", Outcome.Missing, T.Ref("PickaxeIron", "p"), null, null));
            Assert.Equal("Missing: Iron Pickaxe", RadialText.For(plan, hot, L).Description);
        }

        [Fact]
        public void HotbarWithNoMovesIsStillAlreadyWearing()
        {
            Assert.Equal("Already wearing", RadialText.For(new SwapPlan(), new HotbarPlan(), L).Subtitle);
        }
    }
}
