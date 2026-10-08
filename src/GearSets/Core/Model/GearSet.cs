using System.Collections.Generic;

namespace GearSets.Core.Model
{
    /// <summary>One named set: what to wear in each slot, which utility items, and the hotbar layout.</summary>
    public sealed class GearSet
    {
        public const int MaxNameLength = 24;
        public const int HotbarSize = 8;

        public static readonly SlotKind[] AllSlots =
        {
            SlotKind.Helmet, SlotKind.Chest, SlotKind.Legs, SlotKind.Shoulder,
            SlotKind.Trinket, SlotKind.LeftHand, SlotKind.RightHand, SlotKind.Ammo
        };

        public string Id;
        public string Name;
        /// <summary>Prefab name whose sprite is the set's icon.</summary>
        public string Icon;
        public readonly Dictionary<SlotKind, SlotEntry> Slots = new Dictionary<SlotKind, SlotEntry>();
        public UtilityEntry Utilities = UtilityEntry.Ignore;
        /// <summary>Row 0 of the inventory, columns 0-7. Entries are Ignore or Item only.</summary>
        public readonly SlotEntry[] Hotbar = new SlotEntry[HotbarSize];

        public GearSet(string id, string name, string icon)
        {
            Id = id;
            Name = name;
            Icon = icon;
            foreach (SlotKind k in AllSlots)
                Slots[k] = SlotEntry.Ignore;
            for (int i = 0; i < HotbarSize; i++)
                Hotbar[i] = SlotEntry.Ignore;
        }

        public bool HasHotbar
        {
            get
            {
                foreach (SlotEntry e in Hotbar)
                    if (e.Mode == EntryMode.Item)
                        return true;
                return false;
            }
        }

        /// <summary>Tags of every item this set actively uses (ignored entries don't count).</summary>
        public IEnumerable<string> Tags()
        {
            foreach (SlotEntry e in Slots.Values)
                if (e.Mode == EntryMode.Item && e.Item.Tag != null)
                    yield return e.Item.Tag;
            if (Utilities.Mode == EntryMode.Item)
                foreach (ItemRef r in Utilities.Items)
                    if (r.Tag != null)
                        yield return r.Tag;
            foreach (SlotEntry e in Hotbar)
                if (e.Mode == EntryMode.Item && e.Item.Tag != null)
                    yield return e.Item.Tag;
        }
    }
}
