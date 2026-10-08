using System.Collections.Generic;

namespace GearSets.Core.Model
{
    /// <summary>A neutral view of one carried item, built by the game layer (Snapshotter).</summary>
    public sealed class ItemFacts
    {
        /// <summary>Position in InventorySnapshot.Items; the game layer maps it back to the ItemData.</summary>
        public int Index;
        public string Tag;
        public string Prefab;
        public string Name;
        public int Quality = 1;
        public int Variant;
        public long CrafterId;
        public float Durability = 100f;
        public bool UsesDurability;
        public bool Stackable;
        public int Stack = 1;
        public FitSlot Fits;
        public bool TwoHanded;
        public int X;
        public int Y;
        public WornSlot Worn;
        public QueuedAs Queued;

        /// <summary>An item at 0 durability can't be equipped and counts as missing.</summary>
        public bool Broken { get { return UsesDurability && Durability <= 0f; } }

        public ItemRef ToRef(string tag)
        {
            return new ItemRef(Stackable ? null : tag, Prefab, Name, Quality, Variant, CrafterId);
        }
    }

    /// <summary>Everything the player carries plus the shape of their inventory.</summary>
    public sealed class InventorySnapshot
    {
        public readonly List<ItemFacts> Items = new List<ItemFacts>();
        /// <summary>1 in vanilla; 1 + ExtraSlots' active extra utility slots.</summary>
        public int UtilitySlots = 1;
        public int Width = 8;
        /// <summary>Rows of the normal grid. Rows at or below this belong to ExtraSlots' slot cells.</summary>
        public int PlayerRows = 4;

        public ItemFacts Add(ItemFacts f)
        {
            f.Index = Items.Count;
            Items.Add(f);
            return f;
        }

        public ItemFacts WornIn(WornSlot slot)
        {
            foreach (ItemFacts f in Items)
                if (f.Worn == slot)
                    return f;
            return null;
        }

        public List<ItemFacts> WornUtilities()
        {
            var list = new List<ItemFacts>();
            foreach (ItemFacts f in Items)
                if (f.Worn == WornSlot.Utility)
                    list.Add(f);
            return list;
        }

        public ItemFacts At(int x, int y)
        {
            foreach (ItemFacts f in Items)
                if (f.X == x && f.Y == y)
                    return f;
            return null;
        }
    }
}
