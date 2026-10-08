using System.Collections.Generic;
using System.Linq;
using GearSets.Core.Model;
using Xunit;

namespace GearSets.Tests
{
    public class SerializerTests
    {
        [Fact]
        public void JsonRoundTripsStringsAndNumbers()
        {
            var o = new Dictionary<string, object>
            {
                ["s"] = "a \"quoted\" \\ line\nnext\u0001",
                ["n"] = -9223372036854775807L,
                ["b"] = true,
                ["z"] = null,
                ["l"] = new List<object> { 1L, "x" }
            };
            var back = (Dictionary<string, object>)MiniJson.Parse(MiniJson.Write(o));
            Assert.Equal(o["s"], back["s"]);
            Assert.Equal(-9223372036854775807L, back["n"]);
            Assert.Equal(true, back["b"]);
            Assert.Null(back["z"]);
            Assert.Equal(new object[] { 1L, "x" }, (List<object>)back["l"]);
        }

        [Fact]
        public void JsonRejectsGarbage()
        {
            Assert.ThrowsAny<System.FormatException>(() => MiniJson.Parse("{\"a\":"));
            Assert.ThrowsAny<System.FormatException>(() => MiniJson.Parse("[1,2] x"));
        }

        [Fact]
        public void SetsRoundTrip()
        {
            GearSet s = new GearSet("abc", "Mining", "PickaxeIron");
            s.Slots[SlotKind.Chest] = SlotEntry.Of(new ItemRef("c1", "ArmorPaddedCuirass", "$item_chest_pcuirass", 3, 1, -42L));
            s.Slots[SlotKind.LeftHand] = SlotEntry.Empty;
            s.Slots[SlotKind.Helmet] = SlotEntry.Of(T.Ref("HelmetPadded", "h")).Toggle();
            s.Slots[SlotKind.Legs] = SlotEntry.Empty.Toggle();
            s.Utilities = UtilityEntry.Of(new[] { T.Ref("BeltStrength", "b"), T.Ref("Wishbone", "w") });
            s.Hotbar[0] = SlotEntry.Of(T.Ref("PickaxeIron", "p"));
            s.Hotbar[3] = SlotEntry.Of(T.Ref("MeadHealthMinor"));

            string text = SetSerializer.Write(new[] { s });
            Assert.True(SetSerializer.TryRead(text, out List<GearSet> sets, out string error), error);
            GearSet r = Assert.Single(sets);

            Assert.Equal("abc", r.Id);
            Assert.Equal("Mining", r.Name);
            Assert.Equal("PickaxeIron", r.Icon);
            ItemRef chest = r.Slots[SlotKind.Chest].Item;
            Assert.Equal(("c1", "ArmorPaddedCuirass", "$item_chest_pcuirass", 3, 1, -42L),
                (chest.Tag, chest.Prefab, chest.Name, chest.Quality, chest.Variant, chest.CrafterId));
            Assert.Equal(EntryMode.Empty, r.Slots[SlotKind.LeftHand].Mode);
            Assert.Equal(EntryMode.Ignore, r.Slots[SlotKind.Helmet].Mode);
            Assert.Equal("h", r.Slots[SlotKind.Helmet].Item.Tag);
            Assert.True(r.Slots[SlotKind.Legs].WasEmpty);
            Assert.Equal(EntryMode.Ignore, r.Slots[SlotKind.Trinket].Mode);
            Assert.Equal(new[] { "b", "w" }, r.Utilities.Items.Select(i => i.Tag));
            Assert.Equal("p", r.Hotbar[0].Item.Tag);
            Assert.Null(r.Hotbar[3].Item.Tag);
            Assert.Equal(EntryMode.Ignore, r.Hotbar[7].Mode);
        }

        [Fact]
        public void UnknownSlotsAndShortHotbarAreTolerated()
        {
            string text = "{\"v\":1,\"sets\":[{\"id\":\"x\",\"name\":\"A\",\"icon\":\"\",\"slots\":{\"Gloves\":{\"m\":\"empty\"}},\"hotbar\":[{\"m\":\"ignore\"}]}]}";
            Assert.True(SetSerializer.TryRead(text, out List<GearSet> sets, out _));
            Assert.Equal(EntryMode.Ignore, sets[0].Slots[SlotKind.Chest].Mode);
            Assert.Equal(EntryMode.Ignore, sets[0].Utilities.Mode);
            Assert.Equal(8, sets[0].Hotbar.Length);
        }

        [Theory]
        [InlineData("")]
        [InlineData("not json")]
        [InlineData("{\"v\":99,\"sets\":[]}")]
        [InlineData("{\"v\":1}")]
        public void BadDataIsReportedNotThrown(string text)
        {
            Assert.False(SetSerializer.TryRead(text, out List<GearSet> sets, out string error));
            Assert.Null(sets);
            Assert.False(string.IsNullOrEmpty(error));
        }
    }
}
