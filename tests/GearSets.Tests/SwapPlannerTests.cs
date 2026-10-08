using System.Linq;
using GearSets.Core.Model;
using Xunit;

namespace GearSets.Tests
{
    public class SwapPlannerTests
    {
        private static string Describe(SwapPlan p)
        {
            return string.Join(" ", p.Steps.Select(s => s.Kind + ":" + s.Item.Prefab));
        }

        [Fact]
        public void EquipsHandsFirstThenArmourInOrder()
        {
            ItemFacts sword = T.Item("SwordIron", FitSlot.RightHand, tag: "s");
            ItemFacts shield = T.Item("ShieldBanded", FitSlot.LeftHand, tag: "sh");
            ItemFacts helm = T.Item("HelmetIron", FitSlot.Helmet, tag: "h");
            ItemFacts chest = T.Item("ArmorIronChest", FitSlot.Chest, tag: "c");
            InventorySnapshot s = T.Snap(chest, helm, sword, shield);
            GearSet set = T.Set();
            set.Slots[SlotKind.RightHand] = SlotEntry.Of(T.Ref(sword));
            set.Slots[SlotKind.LeftHand] = SlotEntry.Of(T.Ref(shield));
            set.Slots[SlotKind.Helmet] = SlotEntry.Of(T.Ref(helm));
            set.Slots[SlotKind.Chest] = SlotEntry.Of(T.Ref(chest));

            SwapPlan p = SwapPlanner.Plan(set, s);

            Assert.Equal("Equip:ShieldBanded Equip:SwordIron Equip:HelmetIron Equip:ArmorIronChest", Describe(p));
            Assert.Equal(4, p.Count(Outcome.Equip));
        }

        [Fact]
        public void TorchInLeftHandGoesAfterRightHand()
        {
            ItemFacts sword = T.Item("SwordIron", FitSlot.RightHand, tag: "s");
            ItemFacts torch = T.Item("Torch", FitSlot.Torch, tag: "t");
            GearSet set = T.Set();
            set.Slots[SlotKind.RightHand] = SlotEntry.Of(T.Ref(sword));
            set.Slots[SlotKind.LeftHand] = SlotEntry.Of(T.Ref(torch));
            Assert.Equal("Equip:SwordIron Equip:Torch", Describe(SwapPlanner.Plan(set, T.Snap(torch, sword))));
        }

        [Fact]
        public void SwordAndShieldToSwordAndTorchTakesTheShieldOffFirst()
        {
            ItemFacts sword = T.Item("SwordIron", FitSlot.RightHand, tag: "s", worn: WornSlot.RightHand);
            ItemFacts shield = T.Item("ShieldBanded", FitSlot.LeftHand, tag: "sh", worn: WornSlot.LeftHand);
            ItemFacts torch = T.Item("Torch", FitSlot.Torch, tag: "t");
            GearSet set = T.Set();
            set.Slots[SlotKind.RightHand] = SlotEntry.Of(T.Ref(sword));
            set.Slots[SlotKind.LeftHand] = SlotEntry.Of(T.Ref(torch));
            SwapPlan p = SwapPlanner.Plan(set, T.Snap(sword, shield, torch));
            Assert.Equal("Unequip:ShieldBanded Equip:Torch", Describe(p));
            Assert.Equal(1, p.Count(Outcome.Unequip));
        }

        [Fact]
        public void AxeAndShieldToSwordAndTorchEquipsSwordThenClearsLeftThenTorch()
        {
            ItemFacts axe = T.Item("AxeIron", FitSlot.RightHand, tag: "a", worn: WornSlot.RightHand);
            ItemFacts shield = T.Item("ShieldBanded", FitSlot.LeftHand, tag: "sh", worn: WornSlot.LeftHand);
            ItemFacts sword = T.Item("SwordIron", FitSlot.RightHand, tag: "s");
            ItemFacts torch = T.Item("Torch", FitSlot.Torch, tag: "t");
            GearSet set = T.Set();
            set.Slots[SlotKind.RightHand] = SlotEntry.Of(T.Ref(sword));
            set.Slots[SlotKind.LeftHand] = SlotEntry.Of(T.Ref(torch));
            SwapPlan p = SwapPlanner.Plan(set, T.Snap(axe, shield, sword, torch));
            Assert.Equal("Equip:SwordIron Unequip:ShieldBanded Equip:Torch", Describe(p));
        }

        [Fact]
        public void TorchInRightHandIsNotWornForTheLeftWhenTheSetWantsARightItem()
        {
            ItemFacts torch = T.Item("Torch", FitSlot.Torch, tag: "t", worn: WornSlot.RightHand);
            ItemFacts sword = T.Item("SwordIron", FitSlot.RightHand, tag: "s");
            GearSet set = T.Set();
            set.Slots[SlotKind.RightHand] = SlotEntry.Of(T.Ref(sword));
            set.Slots[SlotKind.LeftHand] = SlotEntry.Of(T.Ref(torch));
            SwapPlan p = SwapPlanner.Plan(set, T.Snap(torch, sword));
            Assert.Equal("Equip:SwordIron Equip:Torch", Describe(p));
            Assert.Equal(0, p.Count(Outcome.AlreadyWorn));
        }

        [Fact]
        public void SwordAndTorchWornIsNothingToDo()
        {
            ItemFacts sword = T.Item("SwordIron", FitSlot.RightHand, tag: "s", worn: WornSlot.RightHand);
            ItemFacts torch = T.Item("Torch", FitSlot.Torch, tag: "t", worn: WornSlot.LeftHand);
            GearSet set = T.Set();
            set.Slots[SlotKind.RightHand] = SlotEntry.Of(T.Ref(sword));
            set.Slots[SlotKind.LeftHand] = SlotEntry.Of(T.Ref(torch));
            Assert.True(SwapPlanner.Plan(set, T.Snap(sword, torch)).NothingToDo);
        }

        [Fact]
        public void AlreadyWornIsNothingToDo()
        {
            ItemFacts chest = T.Item("ArmorIronChest", FitSlot.Chest, tag: "c", worn: WornSlot.Chest);
            GearSet set = T.Set();
            set.Slots[SlotKind.Chest] = SlotEntry.Of(T.Ref(chest));
            SwapPlan p = SwapPlanner.Plan(set, T.Snap(chest));
            Assert.True(p.NothingToDo);
            Assert.Equal(1, p.Count(Outcome.AlreadyWorn));
            Assert.Equal(SetState.Equipped, SetStatus.Of(p, null, out int missing));
            Assert.Equal(0, missing);
        }

        [Fact]
        public void IgnoreLeavesSlotAlone()
        {
            ItemFacts chest = T.Item("ArmorIronChest", FitSlot.Chest, tag: "c", worn: WornSlot.Chest);
            Assert.True(SwapPlanner.Plan(T.Set(), T.Snap(chest)).NothingToDo);
        }

        [Fact]
        public void EmptyUnequipsFirst()
        {
            ItemFacts legs = T.Item("ArmorIronLegs", FitSlot.Legs, worn: WornSlot.Legs);
            ItemFacts helm = T.Item("HelmetIron", FitSlot.Helmet, tag: "h");
            GearSet set = T.Set();
            set.Slots[SlotKind.Helmet] = SlotEntry.Of(T.Ref(helm));
            set.Slots[SlotKind.Legs] = SlotEntry.Empty;
            SwapPlan p = SwapPlanner.Plan(set, T.Snap(helm, legs));
            Assert.Equal("Unequip:ArmorIronLegs Equip:HelmetIron", Describe(p));
            Assert.Equal(1, p.Count(Outcome.Unequip));
        }

        [Fact]
        public void TwoHandedRightSkipsLeftItem()
        {
            ItemFacts axe = T.Item("Battleaxe", FitSlot.RightHand, tag: "a", twoHanded: true);
            ItemFacts shield = T.Item("ShieldBanded", FitSlot.LeftHand, tag: "sh");
            GearSet set = T.Set();
            set.Slots[SlotKind.RightHand] = SlotEntry.Of(T.Ref(axe));
            set.Slots[SlotKind.LeftHand] = SlotEntry.Of(T.Ref(shield));
            SwapPlan p = SwapPlanner.Plan(set, T.Snap(axe, shield));
            Assert.Equal("Equip:Battleaxe", Describe(p));
            Assert.Equal(1, p.Count(Outcome.Skipped));
        }

        [Fact]
        public void TwoHandedRightWithIgnoredLeftReportsClearedHand()
        {
            ItemFacts axe = T.Item("Battleaxe", FitSlot.RightHand, tag: "a", twoHanded: true);
            ItemFacts shield = T.Item("ShieldBanded", FitSlot.LeftHand, worn: WornSlot.LeftHand);
            GearSet set = T.Set();
            set.Slots[SlotKind.RightHand] = SlotEntry.Of(T.Ref(axe));
            SwapPlan p = SwapPlanner.Plan(set, T.Snap(axe, shield));
            Assert.Equal(1, p.Count(Outcome.Cleared));
        }

        [Fact]
        public void EmptyHandNeverUnequipsTheItemTheSetUsesInTheOtherHand()
        {
            ItemFacts torch = T.Item("Torch", FitSlot.Torch, tag: "t", worn: WornSlot.RightHand);
            GearSet set = T.Set();
            set.Slots[SlotKind.LeftHand] = SlotEntry.Of(T.Ref(torch));
            set.Slots[SlotKind.RightHand] = SlotEntry.Empty;
            SwapPlan p = SwapPlanner.Plan(set, T.Snap(torch));
            Assert.True(p.NothingToDo);
        }

        [Fact]
        public void UtilitiesNotInTheSetComeOffFirst()
        {
            ItemFacts old = T.Item("Wishbone", FitSlot.Utility, worn: WornSlot.Utility);
            ItemFacts belt = T.Item("BeltStrength", FitSlot.Utility, tag: "b");
            GearSet set = T.Set();
            set.Utilities = UtilityEntry.Of(new[] { T.Ref(belt) });
            Assert.Equal("Unequip:Wishbone Equip:BeltStrength", Describe(SwapPlanner.Plan(set, T.Snap(old, belt))));
        }

        [Fact]
        public void MoreUtilitiesThanSlotsAreSkipped()
        {
            ItemFacts a = T.Item("BeltStrength", FitSlot.Utility, tag: "a");
            ItemFacts b = T.Item("Wishbone", FitSlot.Utility, tag: "b");
            GearSet set = T.Set();
            set.Utilities = UtilityEntry.Of(new[] { T.Ref(a), T.Ref(b) });
            SwapPlan p = SwapPlanner.Plan(set, T.Snap(a, b));
            Assert.Equal("Equip:BeltStrength", Describe(p));
            Assert.Equal(1, p.Count(Outcome.Skipped));
        }

        [Fact]
        public void ExtraSlotsUtilitiesKeepWhatIsWorn()
        {
            ItemFacts a = T.Item("Demister", FitSlot.Utility, worn: WornSlot.Utility);
            ItemFacts b = T.Item("BeltStrength", FitSlot.Utility, tag: "b", worn: WornSlot.Utility);
            ItemFacts c = T.Item("Wishbone", FitSlot.Utility, tag: "c");
            InventorySnapshot s = T.Snap(a, b, c);
            s.UtilitySlots = 5;
            GearSet set = T.Set();
            set.Utilities = UtilityEntry.Of(new[] { T.Ref(b), T.Ref(c) });
            SwapPlan p = SwapPlanner.Plan(set, s);
            Assert.Equal("Unequip:Demister Equip:Wishbone", Describe(p));
            Assert.Equal(1, p.Count(Outcome.AlreadyWorn));
        }

        [Fact]
        public void MissingAndSubstituteAreReported()
        {
            ItemFacts antler = T.Item("PickaxeAntler", FitSlot.RightHand);
            ItemFacts iron2 = T.Item("PickaxeIron", FitSlot.RightHand);
            GearSet set = T.Set();
            set.Slots[SlotKind.RightHand] = SlotEntry.Of(T.Ref("PickaxeIron", "gone"));
            set.Slots[SlotKind.Chest] = SlotEntry.Of(T.Ref("ArmorIronChest", "c"));
            SwapPlan p = SwapPlanner.Plan(set, T.Snap(antler, iron2));
            Assert.Equal("Equip:PickaxeIron", Describe(p));
            Assert.Equal(1, p.Count(Outcome.Substitute));
            Assert.Equal(1, p.Count(Outcome.Missing));
            Assert.Equal(SetState.Missing, SetStatus.Of(p, null, out int missing));
            Assert.Equal("1 item missing", SetStatus.Label(SetState.Missing, missing));
        }

        [Fact]
        public void QueuedItemsAreNotQueuedTwice()
        {
            ItemFacts helm = T.Item("HelmetIron", FitSlot.Helmet, tag: "h", queued: QueuedAs.Equip);
            ItemFacts chest = T.Item("ArmorIronChest", FitSlot.Chest, tag: "c", worn: WornSlot.Chest, queued: QueuedAs.Unequip);
            GearSet set = T.Set();
            set.Slots[SlotKind.Helmet] = SlotEntry.Of(T.Ref(helm));
            set.Slots[SlotKind.Chest] = SlotEntry.Of(T.Ref(chest));
            SwapPlan p = SwapPlanner.Plan(set, T.Snap(helm, chest));
            Assert.Equal("CancelQueued:ArmorIronChest", Describe(p));
        }

        [Fact]
        public void ReadyWhenStepsRemain()
        {
            ItemFacts helm = T.Item("HelmetIron", FitSlot.Helmet, tag: "h");
            GearSet set = T.Set();
            set.Slots[SlotKind.Helmet] = SlotEntry.Of(T.Ref(helm));
            Assert.Equal(SetState.Ready, SetStatus.Of(SwapPlanner.Plan(set, T.Snap(helm)), null, out _));
            Assert.Equal("Ready", SetStatus.Label(SetState.Ready, 0));
            Assert.Equal("Equipped", SetStatus.Label(SetState.Equipped, 0));
            Assert.Equal("3 items missing", SetStatus.Label(SetState.Missing, 3));
        }
    }
}
