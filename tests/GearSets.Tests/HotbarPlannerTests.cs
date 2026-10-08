using System.Linq;
using GearSets.Core.Model;
using Xunit;

namespace GearSets.Tests
{
    public class HotbarPlannerTests
    {
        private static string Describe(HotbarPlan p)
        {
            return string.Join(" ", p.Moves.Select(m => m.Item.Prefab + "->" + m.X + "," + m.Y));
        }

        private static GearSet SetWith(int position, ItemFacts item)
        {
            GearSet s = T.Set();
            s.Hotbar[position] = SlotEntry.Of(T.Ref(item));
            return s;
        }

        [Fact]
        public void SwapsWithTheOccupant()
        {
            ItemFacts pick = T.Item("PickaxeIron", FitSlot.RightHand, tag: "p", x: 3, y: 1);
            ItemFacts club = T.Item("Club", FitSlot.RightHand, x: 0, y: 0);
            HotbarPlan p = HotbarPlanner.Plan(SetWith(0, pick), T.Snap(pick, club));
            Assert.Equal("Club->3,1 PickaxeIron->0,0", Describe(p));
            Assert.Equal(1, p.Count(Outcome.Equip));
        }

        [Fact]
        public void InPlaceIsNoMove()
        {
            ItemFacts pick = T.Item("PickaxeIron", FitSlot.RightHand, tag: "p", x: 2, y: 0);
            HotbarPlan p = HotbarPlanner.Plan(SetWith(2, pick), T.Snap(pick));
            Assert.Empty(p.Moves);
            Assert.Equal(1, p.Count(Outcome.AlreadyWorn));
        }

        [Fact]
        public void EmptyTargetJustMoves()
        {
            ItemFacts mead = T.Item("MeadHealthMinor", FitSlot.None, stackable: true, stack: 5, x: 4, y: 3);
            HotbarPlan p = HotbarPlanner.Plan(SetWith(5, mead), T.Snap(mead));
            Assert.Equal("MeadHealthMinor->5,0", Describe(p));
        }

        [Fact]
        public void NeverMovesGearWornInAnExtraSlotsCell()
        {
            ItemFacts cape = T.Item("CapeWolf", FitSlot.Shoulder, tag: "c", worn: WornSlot.Shoulder, x: 3, y: 6);
            HotbarPlan p = HotbarPlanner.Plan(SetWith(0, cape), T.Snap(cape));
            Assert.Empty(p.Moves);
            Assert.Equal(1, p.Count(Outcome.Skipped));
        }

        [Fact]
        public void QuickSlotItemDisplacesOccupantIntoTheGrid()
        {
            ItemFacts mead = T.Item("MeadHealthMinor", FitSlot.None, stackable: true, x: 0, y: 4);
            ItemFacts club = T.Item("Club", FitSlot.RightHand, x: 0, y: 0);
            ItemFacts stone = T.Item("Stone", FitSlot.None, stackable: true, x: 0, y: 1);
            HotbarPlan p = HotbarPlanner.Plan(SetWith(0, mead), T.Snap(mead, club, stone));
            Assert.Equal("Club->1,1 MeadHealthMinor->0,0", Describe(p));
        }

        [Fact]
        public void NoRoomForTheOccupantSkipsTheEntry()
        {
            var items = new System.Collections.Generic.List<ItemFacts>();
            ItemFacts mead = T.Item("MeadHealthMinor", FitSlot.None, stackable: true, x: 0, y: 4);
            items.Add(mead);
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 8; x++)
                    items.Add(T.Item("Stone" + x + y, FitSlot.None, stackable: true, x: x, y: y));
            HotbarPlan p = HotbarPlanner.Plan(SetWith(0, mead), T.Snap(items.ToArray()));
            Assert.Empty(p.Moves);
            Assert.Equal(1, p.Count(Outcome.Skipped));
        }

        [Fact]
        public void MissingIsReported()
        {
            GearSet s = T.Set();
            s.Hotbar[1] = SlotEntry.Of(T.Ref("SwordIron", "gone"));
            HotbarPlan p = HotbarPlanner.Plan(s, T.Snap());
            Assert.Equal(1, p.Count(Outcome.Missing));
        }

        [Fact]
        public void ChainedSwapsSettle()
        {
            ItemFacts a = T.Item("SwordIron", FitSlot.RightHand, tag: "a", x: 1, y: 0);
            ItemFacts b = T.Item("ShieldBanded", FitSlot.LeftHand, tag: "b", x: 0, y: 0);
            GearSet s = T.Set();
            s.Hotbar[0] = SlotEntry.Of(T.Ref(a));
            s.Hotbar[1] = SlotEntry.Of(T.Ref(b));
            HotbarPlan p = HotbarPlanner.Plan(s, T.Snap(a, b));
            Assert.Equal("ShieldBanded->1,0 SwordIron->0,0", Describe(p));
            Assert.Equal(1, p.Count(Outcome.AlreadyWorn));
        }
    }
}
