namespace GearSets.Core.Model
{
    /// <summary>The single-item equipment slots a set records. Utility items are a list (GearSet.Utilities).</summary>
    public enum SlotKind { Helmet, Chest, Legs, Shoulder, Trinket, LeftHand, RightHand, Ammo }

    /// <summary>Where an item can be equipped, from its item type. Torch goes to either hand.</summary>
    public enum FitSlot { None, Helmet, Chest, Legs, Shoulder, Utility, Trinket, Ammo, RightHand, LeftHand, Torch }

    /// <summary>Where an item is worn right now. Hand items hidden with R count as worn in that hand.</summary>
    public enum WornSlot { None, Helmet, Chest, Legs, Shoulder, Utility, Trinket, Ammo, RightHand, LeftHand }

    /// <summary>Ignore: leave the slot alone. Empty: wear nothing there. Item: wear this item.</summary>
    public enum EntryMode { Ignore, Empty, Item }

    /// <summary>Whether an item already sits in the vanilla equip queue, and for which action.</summary>
    public enum QueuedAs { None, Equip, Unequip }

    public static class Slots
    {
        public static WornSlot Worn(SlotKind k)
        {
            switch (k)
            {
                case SlotKind.Helmet: return WornSlot.Helmet;
                case SlotKind.Chest: return WornSlot.Chest;
                case SlotKind.Legs: return WornSlot.Legs;
                case SlotKind.Shoulder: return WornSlot.Shoulder;
                case SlotKind.Trinket: return WornSlot.Trinket;
                case SlotKind.LeftHand: return WornSlot.LeftHand;
                case SlotKind.RightHand: return WornSlot.RightHand;
                default: return WornSlot.Ammo;
            }
        }

        public static string Label(SlotKind k)
        {
            switch (k)
            {
                case SlotKind.Shoulder: return "Cape";
                case SlotKind.LeftHand: return "Left hand";
                case SlotKind.RightHand: return "Right hand";
                default: return k.ToString();
            }
        }

        public static bool IsHand(WornSlot w)
        {
            return w == WornSlot.LeftHand || w == WornSlot.RightHand;
        }
    }
}
