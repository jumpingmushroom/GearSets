using System;
using System.Collections.Generic;

namespace GearSets.Core.Model
{
    /// <summary>
    /// One slot of a set. An ignored entry remembers what it was (Item, or WasEmpty) so clicking
    /// the tile again restores it.
    /// </summary>
    public sealed class SlotEntry
    {
        public static readonly SlotEntry Ignore = new SlotEntry(EntryMode.Ignore, null, false);
        public static readonly SlotEntry Empty = new SlotEntry(EntryMode.Empty, null, false);

        public readonly EntryMode Mode;
        /// <summary>For Item: the item. For Ignore: the item it held before it was ignored, or null.</summary>
        public readonly ItemRef Item;
        /// <summary>For Ignore: it was Empty before it was ignored.</summary>
        public readonly bool WasEmpty;

        private SlotEntry(EntryMode mode, ItemRef item, bool wasEmpty)
        {
            Mode = mode;
            Item = item;
            WasEmpty = wasEmpty;
        }

        public static SlotEntry Of(ItemRef item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            return new SlotEntry(EntryMode.Item, item, false);
        }

        /// <summary>For the serializer: any combination it may have written.</summary>
        public static SlotEntry Create(EntryMode mode, ItemRef item, bool wasEmpty)
        {
            if (mode == EntryMode.Item)
                return Of(item);
            if (mode == EntryMode.Empty)
                return Empty;
            return item == null && !wasEmpty ? Ignore : new SlotEntry(EntryMode.Ignore, item, wasEmpty && item == null);
        }

        /// <summary>A click on the tile: Item and Empty become Ignore; Ignore goes back to what it was.</summary>
        public SlotEntry Toggle()
        {
            switch (Mode)
            {
                case EntryMode.Item: return new SlotEntry(EntryMode.Ignore, Item, false);
                case EntryMode.Empty: return new SlotEntry(EntryMode.Ignore, null, true);
                default:
                    if (Item != null)
                        return Of(Item);
                    return WasEmpty ? Empty : this;
            }
        }
    }

    /// <summary>The utility items of a set: a list, because ExtraSlots allows up to five worn at once.</summary>
    public sealed class UtilityEntry
    {
        private static readonly ItemRef[] None = new ItemRef[0];

        public static readonly UtilityEntry Ignore = new UtilityEntry(EntryMode.Ignore, None, false);
        public static readonly UtilityEntry Empty = new UtilityEntry(EntryMode.Empty, None, false);

        public readonly EntryMode Mode;
        /// <summary>For Item: the items to wear. For Ignore: what it held before, possibly empty.</summary>
        public readonly IReadOnlyList<ItemRef> Items;
        public readonly bool WasEmpty;

        private UtilityEntry(EntryMode mode, IReadOnlyList<ItemRef> items, bool wasEmpty)
        {
            Mode = mode;
            Items = items;
            WasEmpty = wasEmpty;
        }

        /// <summary>No items means "wear no utility items" (Empty).</summary>
        public static UtilityEntry Of(IList<ItemRef> items)
        {
            if (items == null || items.Count == 0)
                return Empty;
            return new UtilityEntry(EntryMode.Item, new List<ItemRef>(items), false);
        }

        public static UtilityEntry Create(EntryMode mode, IList<ItemRef> items, bool wasEmpty)
        {
            if (mode == EntryMode.Item)
                return Of(items);
            if (mode == EntryMode.Empty)
                return Empty;
            bool hasItems = items != null && items.Count > 0;
            if (!hasItems && !wasEmpty)
                return Ignore;
            return new UtilityEntry(EntryMode.Ignore, hasItems ? new List<ItemRef>(items) : (IReadOnlyList<ItemRef>)None, wasEmpty && !hasItems);
        }

        public UtilityEntry Toggle()
        {
            switch (Mode)
            {
                case EntryMode.Item: return new UtilityEntry(EntryMode.Ignore, Items, false);
                case EntryMode.Empty: return new UtilityEntry(EntryMode.Ignore, None, true);
                default:
                    if (Items.Count > 0)
                        return new UtilityEntry(EntryMode.Item, Items, false);
                    return WasEmpty ? Empty : this;
            }
        }
    }
}
