using System.Collections.Generic;
using GearSets.Core.Model;
using Xunit;

namespace GearSets.Tests
{
    public class ReportTextTests
    {
        private static string L(string token)
        {
            var names = new Dictionary<string, string>
            {
                ["$item_pickaxeiron"] = "Iron Pickaxe",
                ["$item_pickaxeantler"] = "Antler Pickaxe",
                ["$item_armorironlegs"] = "Iron Greaves",
                ["$item_capewolf"] = "Wolf Fur Cape",
                ["$item_shieldbanded"] = "Banded Shield"
            };
            return names.TryGetValue(token, out string n) ? n : token;
        }

        [Fact]
        public void Equipped()
        {
            var plan = new SwapPlan();
            ItemFacts helm = T.Item("HelmetIron", FitSlot.Helmet);
            plan.Steps.Add(new Step(StepKind.Equip, helm));
            plan.Lines.Add(new ReportLine("Helmet", Outcome.Equip, T.Ref(helm), helm, null));
            Assert.Equal("Mining equipped.", ReportText.Compose("Mining", plan, new SwapPlan(), null, L));
        }

        [Fact]
        public void AlreadyEquipped()
        {
            Assert.Equal("Combat is already equipped.", ReportText.Compose("Combat", new SwapPlan(), new SwapPlan(), new HotbarPlan(), L));
        }

        [Fact]
        public void MissingAndSubstitutesAreNamed()
        {
            var plan = new SwapPlan();
            ItemFacts antler = T.Item("PickaxeAntler", FitSlot.RightHand);
            ItemFacts iron = T.Item("PickaxeIron", FitSlot.RightHand);
            plan.Steps.Add(new Step(StepKind.Equip, antler));
            plan.Lines.Add(new ReportLine("Right hand", Outcome.Substitute, T.Ref("PickaxeIron", "p"), antler, null));
            plan.Lines.Add(new ReportLine("Cape", Outcome.Missing, T.Ref("CapeWolf", "c"), null, null));
            var hotbar = new HotbarPlan();
            hotbar.Lines.Add(new ReportLine("Hotbar 1", Outcome.Substitute, T.Ref("PickaxeIron", "p"), iron, null));
            Assert.Equal("Mining equipped. Missing: Iron Pickaxe (used Antler Pickaxe), Wolf Fur Cape, Iron Pickaxe (used another copy).",
                ReportText.Compose("Mining", plan, new SwapPlan(), hotbar, L));
        }

        [Fact]
        public void InterruptedNamesWhatIsLeft()
        {
            var plan = new SwapPlan();
            ItemFacts legs = T.Item("ArmorIronLegs", FitSlot.Legs);
            ItemFacts cape = T.Item("CapeWolf", FitSlot.Shoulder);
            plan.Steps.Add(new Step(StepKind.Equip, legs));
            plan.Steps.Add(new Step(StepKind.Equip, cape));
            var remaining = new SwapPlan();
            remaining.Steps.Add(new Step(StepKind.Equip, legs));
            remaining.Steps.Add(new Step(StepKind.Equip, cape));
            Assert.Equal("Mining interrupted: Iron Greaves and Wolf Fur Cape not equipped. Use the set again to finish.",
                ReportText.Compose("Mining", plan, remaining, null, L));
        }

        [Fact]
        public void SkippedAndClearedAreExplained()
        {
            var plan = new SwapPlan();
            ItemFacts shield = T.Item("ShieldBanded", FitSlot.LeftHand);
            plan.Lines.Add(new ReportLine("Left hand", Outcome.Skipped, T.Ref(shield), null, "can't be held together with a two-handed item"));
            plan.Lines.Add(new ReportLine("Left hand", Outcome.Cleared, null, shield, null));
            plan.Steps.Add(new Step(StepKind.Equip, T.Item("Battleaxe", FitSlot.RightHand)));
            Assert.Equal("Combat equipped. Skipped: Banded Shield (can't be held together with a two-handed item). Left hand cleared by a two-handed item.",
                ReportText.Compose("Combat", plan, new SwapPlan(), null, L));
        }

        [Fact]
        public void JoinAnd()
        {
            Assert.Equal("", ReportText.JoinAnd(new string[0]));
            Assert.Equal("a", ReportText.JoinAnd(new[] { "a" }));
            Assert.Equal("a and b", ReportText.JoinAnd(new[] { "a", "b" }));
            Assert.Equal("a, b and c", ReportText.JoinAnd(new[] { "a", "b", "c" }));
        }
    }
}
