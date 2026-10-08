using System.Linq;
using GearSets.Core.Model;
using Xunit;

namespace GearSets.Tests
{
    public class CaptureTests
    {
        private static int _n;

        private static string NewTag()
        {
            return "new" + (++_n);
        }

        [Fact]
        public void CapturesWornSlotsAndTagsUntaggedItems()
        {
            ItemFacts chest = T.Item("ArmorIronChest", FitSlot.Chest, tag: "c", worn: WornSlot.Chest);
            ItemFacts helm = T.Item("HelmetIron", FitSlot.Helmet, worn: WornSlot.Helmet);
            ItemFacts legs = T.Item("ArmorIronLegs", FitSlot.Legs, worn: WornSlot.Legs);
            CaptureMask mask = CaptureMask.All();
            mask.Slots.Remove(SlotKind.Legs);
            GearSet set = T.Set();

            var tags = SetCapture.Capture(T.Snap(chest, helm, legs), set, mask, NewTag);

            Assert.Equal("c", set.Slots[SlotKind.Chest].Item.Tag);
            Assert.Equal(2, tags.Count);
            TagAssignment a = tags.Single(t => t.Item == helm);
            Assert.Equal(a.Tag, set.Slots[SlotKind.Helmet].Item.Tag);
            Assert.Equal(EntryMode.Ignore, set.Slots[SlotKind.Legs].Mode);
            Assert.Equal(tags.Single(t => t.Item == legs).Tag, set.Slots[SlotKind.Legs].Item.Tag);
            Assert.Equal(EntryMode.Empty, set.Slots[SlotKind.LeftHand].Mode);
            Assert.Equal(EntryMode.Empty, set.Utilities.Mode);
        }

        [Fact]
        public void CapturesAllWornUtilities()
        {
            ItemFacts a = T.Item("BeltStrength", FitSlot.Utility, tag: "a", worn: WornSlot.Utility);
            ItemFacts b = T.Item("Wishbone", FitSlot.Utility, tag: "b", worn: WornSlot.Utility);
            GearSet set = T.Set();
            SetCapture.Capture(T.Snap(a, b), set, CaptureMask.All(), NewTag);
            Assert.Equal(new[] { "a", "b" }, set.Utilities.Items.Select(r => r.Tag));
        }

        [Fact]
        public void HotbarCapturesRowZeroAndLeavesGapsIgnored()
        {
            ItemFacts sword = T.Item("SwordIron", FitSlot.RightHand, tag: "s", worn: WornSlot.RightHand, x: 0, y: 0);
            ItemFacts mead = T.Item("MeadHealthMinor", FitSlot.None, stackable: true, x: 2, y: 0);
            GearSet set = T.Set();
            var tags = SetCapture.Capture(T.Snap(sword, mead), set, CaptureMask.All(), NewTag);
            Assert.Empty(tags);
            Assert.Equal("s", set.Hotbar[0].Item.Tag);
            Assert.Equal(EntryMode.Ignore, set.Hotbar[1].Mode);
            Assert.Null(set.Hotbar[2].Item.Tag);
            Assert.Equal("MeadHealthMinor", set.Hotbar[2].Item.Prefab);
            Assert.Equal("s", set.Slots[SlotKind.RightHand].Item.Tag);
        }

        [Fact]
        public void SameItemInHandAndHotbarGetsOneTag()
        {
            ItemFacts sword = T.Item("SwordIron", FitSlot.RightHand, worn: WornSlot.RightHand, x: 0, y: 0);
            GearSet set = T.Set();
            var tags = SetCapture.Capture(T.Snap(sword), set, CaptureMask.All(), NewTag);
            Assert.Single(tags);
            Assert.Equal(set.Slots[SlotKind.RightHand].Item.Tag, set.Hotbar[0].Item.Tag);
        }

        [Fact]
        public void ClonedTagIsMovedToTheOtherCopy()
        {
            ItemFacts mine = T.Item("SwordIron", FitSlot.RightHand, tag: "s", worn: WornSlot.RightHand, x: 1, y: 1);
            ItemFacts clone = T.Item("SwordIron", FitSlot.RightHand, tag: "s", x: 2, y: 2);
            GearSet set = T.Set();
            CaptureMask mask = CaptureMask.All();
            for (int i = 0; i < 8; i++)
                mask.Hotbar[i] = false;
            var tags = SetCapture.Capture(T.Snap(mine, clone), set, mask, NewTag);
            TagAssignment a = Assert.Single(tags);
            Assert.Same(clone, a.Item);
            Assert.Equal("s", set.Slots[SlotKind.RightHand].Item.Tag);
            Assert.NotEqual("s", a.Tag);
        }

        [Fact]
        public void ExcludedWornSlotRemembersItsItemAndTogglesBack()
        {
            ItemFacts chest = T.Item("ArmorIronChest", FitSlot.Chest, tag: "c", worn: WornSlot.Chest);
            CaptureMask mask = CaptureMask.All();
            mask.Slots.Remove(SlotKind.Chest);
            GearSet set = T.Set();
            SetCapture.Capture(T.Snap(chest), set, mask, NewTag);
            SlotEntry e = set.Slots[SlotKind.Chest];
            Assert.Equal(EntryMode.Ignore, e.Mode);
            Assert.Equal("c", e.Item.Tag);
            SlotEntry back = e.Toggle();
            Assert.Equal(EntryMode.Item, back.Mode);
            Assert.Equal("c", back.Item.Tag);
            Assert.DoesNotContain("c", set.Tags());
        }

        [Fact]
        public void ExcludedEmptySlotRemembersEmptyAndTogglesBack()
        {
            CaptureMask mask = CaptureMask.All();
            mask.Slots.Remove(SlotKind.Legs);
            GearSet set = T.Set();
            SetCapture.Capture(T.Snap(), set, mask, NewTag);
            SlotEntry e = set.Slots[SlotKind.Legs];
            Assert.Equal(EntryMode.Ignore, e.Mode);
            Assert.Null(e.Item);
            Assert.True(e.WasEmpty);
            Assert.Equal(EntryMode.Empty, e.Toggle().Mode);
        }

        [Fact]
        public void ExcludedUtilitiesRememberWhatWasWorn()
        {
            ItemFacts belt = T.Item("BeltStrength", FitSlot.Utility, tag: "b", worn: WornSlot.Utility);
            CaptureMask mask = CaptureMask.All();
            mask.Utilities = false;
            GearSet set = T.Set();
            SetCapture.Capture(T.Snap(belt), set, mask, NewTag);
            Assert.Equal(EntryMode.Ignore, set.Utilities.Mode);
            Assert.Equal(new[] { "b" }, set.Utilities.Items.Select(r => r.Tag));
            Assert.Equal(EntryMode.Item, set.Utilities.Toggle().Mode);

            GearSet none = T.Set("None");
            SetCapture.Capture(T.Snap(), none, mask, NewTag);
            Assert.Equal(EntryMode.Ignore, none.Utilities.Mode);
            Assert.True(none.Utilities.WasEmpty);
            Assert.Equal(EntryMode.Empty, none.Utilities.Toggle().Mode);
        }

        [Fact]
        public void ExcludedHotbarPositionRemembersItsItem()
        {
            ItemFacts axe = T.Item("AxeIron", FitSlot.RightHand, x: 3, y: 0);
            CaptureMask mask = CaptureMask.All();
            mask.Hotbar[3] = false;
            GearSet set = T.Set();
            var tags = SetCapture.Capture(T.Snap(axe), set, mask, NewTag);
            TagAssignment a = Assert.Single(tags);
            Assert.Equal(EntryMode.Ignore, set.Hotbar[3].Mode);
            Assert.Equal(a.Tag, set.Hotbar[3].Item.Tag);
            Assert.Equal(EntryMode.Item, set.Hotbar[3].Toggle().Mode);
            Assert.False(set.HasHotbar);
        }

        [Fact]
        public void UpdateFromCurrentRefreshesWhatAnIgnoreRemembers()
        {
            GearSet set = T.Set();
            set.Slots[SlotKind.Chest] = SlotEntry.Of(T.Ref("ArmorLeatherChest", "old")).Toggle();
            ItemFacts chest = T.Item("ArmorIronChest", FitSlot.Chest, tag: "c", worn: WornSlot.Chest);
            SetCapture.Capture(T.Snap(chest), set, CaptureMask.From(set), NewTag);
            Assert.Equal(EntryMode.Ignore, set.Slots[SlotKind.Chest].Mode);
            Assert.Equal("c", set.Slots[SlotKind.Chest].Item.Tag);
        }

        [Fact]
        public void MaskFromSetKeepsIgnores()
        {
            GearSet set = T.Set();
            set.Slots[SlotKind.Chest] = SlotEntry.Empty;
            set.Utilities = UtilityEntry.Empty;
            set.Hotbar[3] = SlotEntry.Of(T.Ref("Torch", "t"));
            CaptureMask m = CaptureMask.From(set);
            Assert.Equal(new[] { SlotKind.Chest }, m.Slots.ToArray());
            Assert.True(m.Utilities);
            Assert.Equal(new[] { false, false, false, true, false, false, false, false }, m.Hotbar);
        }
    }
}
