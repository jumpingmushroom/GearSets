using System;
using GearSets.Core.Model;
using Xunit;

namespace GearSets.Tests
{
    public class SetBookTests
    {
        [Fact]
        public void NameRules()
        {
            var book = new SetBook();
            book.Add(new GearSet("1", "Mining", "x"));
            Assert.NotNull(book.NameError("", null));
            Assert.NotNull(book.NameError("   ", null));
            Assert.NotNull(book.NameError(new string('a', 25), null));
            Assert.Null(book.NameError(new string('a', 24), null));
            Assert.NotNull(book.NameError(" mining ", null));
            Assert.Null(book.NameError("Mining", "1"));
            Assert.Same(book.Sets[0], book.FindByName("MINING"));
        }

        [Fact]
        public void FullBookRefusesMore()
        {
            var book = new SetBook { MaxSets = 1 };
            book.Add(new GearSet("1", "A", "x"));
            Assert.True(book.IsFull);
            Assert.Throws<InvalidOperationException>(() => book.Add(new GearSet("2", "B", "x")));
        }

        [Fact]
        public void RemoveAndFind()
        {
            var book = new SetBook();
            book.Add(new GearSet("1", "A", "x"));
            Assert.NotNull(book.Find("1"));
            Assert.True(book.Remove("1"));
            Assert.False(book.Remove("1"));
            Assert.Null(book.Find("1"));
        }

        [Fact]
        public void MembershipMapsTagsToSetNames()
        {
            var book = new SetBook();
            GearSet a = new GearSet("1", "Combat", "x");
            a.Slots[SlotKind.Shoulder] = SlotEntry.Of(T.Ref("CapeWolf", "cape"));
            a.Hotbar[0] = SlotEntry.Of(T.Ref("CapeWolf", "cape"));
            GearSet b = new GearSet("2", "Mining", "x");
            b.Slots[SlotKind.Shoulder] = SlotEntry.Of(T.Ref("CapeWolf", "cape"));
            book.Add(a);
            book.Add(b);
            Assert.Equal(new[] { "Combat", "Mining" }, book.Membership()["cape"]);
        }
    }
}
