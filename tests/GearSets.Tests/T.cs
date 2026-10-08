using GearSets.Core.Model;

namespace GearSets.Tests
{
    /// <summary>Short builders so each test reads as the inventory it describes.</summary>
    internal static class T
    {
        public static ItemFacts Item(string prefab, FitSlot fits, string tag = null, WornSlot worn = WornSlot.None,
            int x = 0, int y = 1, int quality = 1, float durability = 100f, bool stackable = false, int stack = 1,
            bool twoHanded = false, QueuedAs queued = QueuedAs.None, int variant = 0)
        {
            return new ItemFacts
            {
                Prefab = prefab, Name = "$item_" + prefab.ToLowerInvariant(), Fits = fits, Tag = tag, Worn = worn,
                X = x, Y = y, Quality = quality, Durability = durability, UsesDurability = !stackable,
                Stackable = stackable, Stack = stack, TwoHanded = twoHanded, Queued = queued, Variant = variant
            };
        }

        public static InventorySnapshot Snap(params ItemFacts[] items)
        {
            var s = new InventorySnapshot();
            foreach (ItemFacts i in items)
                s.Add(i);
            return s;
        }

        public static ItemRef Ref(ItemFacts f)
        {
            return f.ToRef(f.Tag);
        }

        public static ItemRef Ref(string prefab, string tag = null, int quality = 1)
        {
            return new ItemRef(tag, prefab, "$item_" + prefab.ToLowerInvariant(), quality, 0, 0L);
        }

        public static GearSet Set(string name = "Test")
        {
            return new GearSet("id-" + name, name, "SwordIron");
        }
    }
}
