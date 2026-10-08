using System.Linq;
using GearSets.Core.Model;
using Xunit;

namespace GearSets.Tests
{
    public class ModelTests
    {
        [Fact]
        public void NewSetIgnoresEverything()
        {
            GearSet s = T.Set();
            Assert.All(GearSet.AllSlots, k => Assert.Equal(EntryMode.Ignore, s.Slots[k].Mode));
            Assert.Equal(EntryMode.Ignore, s.Utilities.Mode);
            Assert.All(s.Hotbar, e => Assert.Equal(EntryMode.Ignore, e.Mode));
            Assert.False(s.HasHotbar);
        }

        [Fact]
        public void ToggleRemembersItem()
        {
            SlotEntry item = SlotEntry.Of(T.Ref("SwordIron", "t1"));
            SlotEntry ignored = item.Toggle();
            Assert.Equal(EntryMode.Ignore, ignored.Mode);
            Assert.Equal("t1", ignored.Item.Tag);
            SlotEntry back = ignored.Toggle();
            Assert.Equal(EntryMode.Item, back.Mode);
            Assert.Equal("t1", back.Item.Tag);
        }

        [Fact]
        public void ToggleRemembersEmpty()
        {
            SlotEntry ignored = SlotEntry.Empty.Toggle();
            Assert.Equal(EntryMode.Ignore, ignored.Mode);
            Assert.True(ignored.WasEmpty);
            Assert.Equal(EntryMode.Empty, ignored.Toggle().Mode);
        }

        [Fact]
        public void ToggleOfPlainIgnoreStaysIgnore()
        {
            Assert.Equal(EntryMode.Ignore, SlotEntry.Ignore.Toggle().Mode);
        }

        [Fact]
        public void UtilityToggleRemembersItems()
        {
            UtilityEntry u = UtilityEntry.Of(new[] { T.Ref("BeltStrength", "b"), T.Ref("Wishbone", "w") });
            UtilityEntry back = u.Toggle().Toggle();
            Assert.Equal(EntryMode.Item, back.Mode);
            Assert.Equal(new[] { "b", "w" }, back.Items.Select(r => r.Tag));
            Assert.Equal(EntryMode.Empty, UtilityEntry.Of(new ItemRef[0]).Mode);
        }

        [Fact]
        public void TagsCoversActiveEntriesOnly()
        {
            GearSet s = T.Set();
            s.Slots[SlotKind.Chest] = SlotEntry.Of(T.Ref("ArmorIronChest", "c"));
            s.Slots[SlotKind.Legs] = SlotEntry.Of(T.Ref("ArmorIronLegs", "l")).Toggle();
            s.Utilities = UtilityEntry.Of(new[] { T.Ref("BeltStrength", "b") });
            s.Hotbar[0] = SlotEntry.Of(T.Ref("SwordIron", "s"));
            s.Hotbar[1] = SlotEntry.Of(T.Ref("MeadHealthMinor"));
            Assert.Equal(new[] { "b", "c", "s" }, s.Tags().OrderBy(t => t));
            Assert.True(s.HasHotbar);
        }

        [Fact]
        public void SnapshotLookups()
        {
            ItemFacts helm = T.Item("HelmetIron", FitSlot.Helmet, worn: WornSlot.Helmet, x: 0, y: 2);
            ItemFacts belt = T.Item("BeltStrength", FitSlot.Utility, worn: WornSlot.Utility, x: 1, y: 2);
            ItemFacts bone = T.Item("Wishbone", FitSlot.Utility, worn: WornSlot.Utility, x: 2, y: 2);
            InventorySnapshot s = T.Snap(helm, belt, bone);
            Assert.Same(helm, s.WornIn(WornSlot.Helmet));
            Assert.Null(s.WornIn(WornSlot.Chest));
            Assert.Equal(2, s.WornUtilities().Count);
            Assert.Same(bone, s.At(2, 2));
            Assert.Equal(new[] { 0, 1, 2 }, s.Items.Select(i => i.Index));
        }

        [Fact]
        public void StackablesNeverCarryATag()
        {
            ItemFacts mead = T.Item("MeadHealthMinor", FitSlot.None, tag: "x", stackable: true);
            Assert.Null(mead.ToRef("x").Tag);
            Assert.True(T.Item("Sword", FitSlot.RightHand, durability: 0f).Broken);
        }
    }
}
