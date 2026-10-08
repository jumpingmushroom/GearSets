using System.Collections.Generic;
using GearSets.Core.Model;
using Xunit;

namespace GearSets.Tests
{
    public class RadialOrderTests
    {
        private static readonly int[] Range = { 8, 12 };

        /// <summary>
        /// What Valheim's RadialBase.ConstructRadial does with a top-level page: pick the slots per
        /// ring, and when the page is not full move element 0 to the middle and rotate so that it
        /// sits at the top. Returns the set index at each clockwise slot from the top (-1 = gap).
        /// </summary>
        private static int[] Layout(int[] order, int[] range)
        {
            int count = order.Length;
            int max = range[0];
            for (int i = 0; i < range.Length - 1; i++)
                if (count > range[i])
                    max = range[i + 1];
            var list = new List<int>(order);
            int offset = 0;
            if (count < max)
            {
                int num = count / 2;
                int first = list[0];
                list.RemoveAt(0);
                list.Insert(num, first);
                offset = -num;
            }
            var slots = new int[System.Math.Max(max, count)];
            for (int i = 0; i < slots.Length; i++)
                slots[i] = -1;
            for (int i = 0; i < list.Count; i++)
                slots[(((i + offset) % slots.Length) + slots.Length) % slots.Length] = list[i];
            return slots;
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        [InlineData(11)]
        [InlineData(12)]
        public void FirstSetAtTopThenClockwise(int count)
        {
            int[] slots = Layout(RadialOrder.TopLevel(count, Range), Range);
            var seen = new List<int>();
            foreach (int s in slots)
                if (s >= 0)
                    seen.Add(s);
            for (int i = 0; i < count; i++)
                Assert.Equal(i, seen[i]);
            Assert.Equal(0, slots[0]);
        }

        [Fact]
        public void MoreThanOneRingKeepsTheOrder()
        {
            Assert.Equal(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 }, RadialOrder.TopLevel(14, Range));
        }

        [Fact]
        public void FullRingKeepsTheOrder()
        {
            Assert.Equal(new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, RadialOrder.TopLevel(8, Range));
        }

        [Fact]
        public void MissingRangeKeepsTheOrder()
        {
            Assert.Equal(new[] { 0, 1, 2 }, RadialOrder.TopLevel(3, null));
        }
    }
}
