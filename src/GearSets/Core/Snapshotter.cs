using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GearSets.Core.Model;
using GearSets.Integrations;

namespace GearSets.Core
{
    /// <summary>A snapshot plus the ItemData behind each ItemFacts (same index).</summary>
    internal sealed class Snapshot
    {
        public readonly InventorySnapshot Facts;
        public readonly List<ItemDrop.ItemData> Items;

        public Snapshot(InventorySnapshot facts, List<ItemDrop.ItemData> items)
        {
            Facts = facts;
            Items = items;
        }

        public ItemDrop.ItemData Of(ItemFacts f)
        {
            return Items[f.Index];
        }
    }

    internal static class Snapshotter
    {
        private sealed class RefComparer : IEqualityComparer<ItemDrop.ItemData>
        {
            public static readonly RefComparer Instance = new RefComparer();
            public bool Equals(ItemDrop.ItemData x, ItemDrop.ItemData y) { return ReferenceEquals(x, y); }
            public int GetHashCode(ItemDrop.ItemData o) { return RuntimeHelpers.GetHashCode(o); }
        }

        public static Snapshot Take(Player p)
        {
            Inventory inv = p.GetInventory();
            var facts = new InventorySnapshot
            {
                Width = inv.GetWidth(),
                PlayerRows = EquipmentModel.PlayerRows(inv),
                UtilitySlots = EquipmentModel.UtilitySlotCount()
            };
            var items = new List<ItemDrop.ItemData>();
            Dictionary<ItemDrop.ItemData, WornSlot> worn = Worn(p);
            Dictionary<ItemDrop.ItemData, QueuedAs> queued = Queued(p);

            foreach (ItemDrop.ItemData it in inv.GetAllItems())
            {
                WornSlot w;
                QueuedAs q;
                facts.Add(new ItemFacts
                {
                    Tag = Tags.Get(it),
                    Prefab = it.m_dropPrefab != null ? it.m_dropPrefab.name : it.m_shared.m_name,
                    Name = it.m_shared.m_name,
                    Quality = it.m_quality,
                    Variant = it.m_variant,
                    CrafterId = it.m_crafterID,
                    Durability = it.m_durability,
                    UsesDurability = it.m_shared.m_useDurability,
                    Stackable = it.m_shared.m_maxStackSize > 1,
                    Stack = it.m_stack,
                    Fits = Classify(it),
                    TwoHanded = it.IsTwoHanded(),
                    X = it.m_gridPos.x,
                    Y = it.m_gridPos.y,
                    Worn = worn.TryGetValue(it, out w) ? w : WornSlot.None,
                    Queued = queued.TryGetValue(it, out q) ? q : QueuedAs.None
                });
                items.Add(it);
            }
            return new Snapshot(facts, items);
        }

        private static Dictionary<ItemDrop.ItemData, WornSlot> Worn(Player p)
        {
            var map = new Dictionary<ItemDrop.ItemData, WornSlot>(RefComparer.Instance);
            Put(map, p.m_helmetItem, WornSlot.Helmet);
            Put(map, p.m_chestItem, WornSlot.Chest);
            Put(map, p.m_legItem, WornSlot.Legs);
            Put(map, p.m_shoulderItem, WornSlot.Shoulder);
            Put(map, p.m_trinketItem, WornSlot.Trinket);
            Put(map, p.m_ammoItem, WornSlot.Ammo);
            Put(map, p.m_rightItem, WornSlot.RightHand);
            Put(map, p.m_leftItem, WornSlot.LeftHand);
            Put(map, p.m_hiddenRightItem, WornSlot.RightHand);
            Put(map, p.m_hiddenLeftItem, WornSlot.LeftHand);
            foreach (ItemDrop.ItemData u in EquipmentModel.WornUtilities(p))
                Put(map, u, WornSlot.Utility);
            return map;
        }

        private static void Put(Dictionary<ItemDrop.ItemData, WornSlot> map, ItemDrop.ItemData item, WornSlot slot)
        {
            if (item != null && !map.ContainsKey(item))
                map[item] = slot;
        }

        private static Dictionary<ItemDrop.ItemData, QueuedAs> Queued(Player p)
        {
            var map = new Dictionary<ItemDrop.ItemData, QueuedAs>(RefComparer.Instance);
            foreach (Player.MinorActionData a in p.m_actionQueue)
            {
                if (a.m_item == null || map.ContainsKey(a.m_item))
                    continue;
                if (a.m_type == Player.MinorActionData.ActionType.Equip)
                    map[a.m_item] = QueuedAs.Equip;
                else if (a.m_type == Player.MinorActionData.ActionType.Unequip)
                    map[a.m_item] = QueuedAs.Unequip;
            }
            return map;
        }

        private static FitSlot Classify(ItemDrop.ItemData it)
        {
            switch (it.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.Helmet: return FitSlot.Helmet;
                case ItemDrop.ItemData.ItemType.Chest: return FitSlot.Chest;
                case ItemDrop.ItemData.ItemType.Legs: return FitSlot.Legs;
                case ItemDrop.ItemData.ItemType.Shoulder: return FitSlot.Shoulder;
                case ItemDrop.ItemData.ItemType.Utility: return FitSlot.Utility;
                case ItemDrop.ItemData.ItemType.Trinket: return FitSlot.Trinket;
                case ItemDrop.ItemData.ItemType.Ammo: return FitSlot.Ammo;
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.Tool:
                    return FitSlot.RightHand;
                case ItemDrop.ItemData.ItemType.Shield:
                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                    return FitSlot.LeftHand;
                case ItemDrop.ItemData.ItemType.Torch: return FitSlot.Torch;
                default: return FitSlot.None;
            }
        }
    }
}
