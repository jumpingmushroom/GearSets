using System.Collections.Generic;
using GearSets.Core.Model;
using Xunit;

namespace GearSets.Tests
{
    public class MatcherTests
    {
        private static readonly HashSet<int> None = new HashSet<int>();

        [Fact]
        public void ExactTagWinsOverBetterCopy()
        {
            ItemFacts mine = T.Item("PickaxeIron", FitSlot.RightHand, tag: "p", quality: 1);
            ItemFacts better = T.Item("PickaxeIron", FitSlot.RightHand, quality: 4);
            InventorySnapshot s = T.Snap(better, mine);
            Assert.Same(mine, ItemMatcher.Find(T.Ref(mine), s, None, out bool sub));
            Assert.False(sub);
        }

        [Fact]
        public void MissingTagFallsBackToBestSubstitute()
        {
            ItemFacts q2 = T.Item("PickaxeIron", FitSlot.RightHand, quality: 2, durability: 50f);
            ItemFacts q3 = T.Item("PickaxeIron", FitSlot.RightHand, quality: 3, durability: 10f);
            ItemFacts other = T.Item("PickaxeAntler", FitSlot.RightHand, quality: 4);
            InventorySnapshot s = T.Snap(q2, other, q3);
            Assert.Same(q3, ItemMatcher.Find(T.Ref("PickaxeIron", "gone"), s, None, out bool sub));
            Assert.True(sub);
        }

        [Fact]
        public void BrokenExactCopyCountsAsMissing()
        {
            ItemFacts broken = T.Item("SwordIron", FitSlot.RightHand, tag: "s", durability: 0f);
            InventorySnapshot s = T.Snap(broken);
            Assert.Null(ItemMatcher.Find(T.Ref(broken), s, None, out _));
        }

        [Fact]
        public void TakenItemsAreSkipped()
        {
            ItemFacts a = T.Item("SwordIron", FitSlot.RightHand, tag: "s");
            InventorySnapshot s = T.Snap(a);
            Assert.Null(ItemMatcher.Find(T.Ref(a), s, new HashSet<int> { a.Index }, out _));
        }

        [Fact]
        public void StackablesMatchByPrefabSameQualityThenBiggestStack()
        {
            ItemFacts small = T.Item("MeadHealthMinor", FitSlot.None, stackable: true, stack: 2);
            ItemFacts big = T.Item("MeadHealthMinor", FitSlot.None, stackable: true, stack: 9);
            ItemFacts otherQ = T.Item("MeadHealthMinor", FitSlot.None, stackable: true, stack: 50, quality: 2);
            InventorySnapshot s = T.Snap(small, otherQ, big);
            Assert.Same(big, ItemMatcher.Find(T.Ref("MeadHealthMinor"), s, None, out bool sub));
            Assert.False(sub);
        }

        [Fact]
        public void SubstituteAlreadyWornIsPreferred()
        {
            ItemFacts loose = T.Item("PickaxeIron", FitSlot.RightHand, quality: 3);
            ItemFacts held = T.Item("PickaxeIron", FitSlot.RightHand, quality: 3, worn: WornSlot.RightHand);
            InventorySnapshot s = T.Snap(loose, held);
            Assert.Same(held, ItemMatcher.Find(T.Ref("PickaxeIron", "gone"), s, None, out _, WornSlot.RightHand));
        }

        [Fact]
        public void DuplicateTagPicksHigherDurability()
        {
            ItemFacts worn = T.Item("SwordIron", FitSlot.RightHand, tag: "s", durability: 20f);
            ItemFacts fresh = T.Item("SwordIron", FitSlot.RightHand, tag: "s", durability: 90f);
            InventorySnapshot s = T.Snap(worn, fresh);
            Assert.Same(fresh, ItemMatcher.Find(T.Ref("SwordIron", "s"), s, None, out bool sub));
            Assert.False(sub);
        }

        [Fact]
        public void NothingMatches()
        {
            Assert.Null(ItemMatcher.Find(T.Ref("Nope", "n"), T.Snap(), None, out bool sub));
            Assert.False(sub);
        }
    }
}
