using System.Collections.Generic;

namespace GearSets.Integrations
{
    /// <summary>The equipment shape GearSets plans against: vanilla, or ExtraSlots when it is active.</summary>
    internal static class EquipmentModel
    {
        public static int UtilitySlotCount()
        {
            return ExtraSlotsAdapter.Active ? 1 + ExtraSlotsAdapter.ExtraUtilitySlots() : 1;
        }

        public static int PlayerRows(Inventory inv)
        {
            if (ExtraSlotsAdapter.Active)
            {
                int rows = ExtraSlotsAdapter.PlayerRows();
                if (rows > 0)
                    return rows;
            }
            return inv.GetHeight();
        }

        public static List<ItemDrop.ItemData> WornUtilities(Player p)
        {
            var list = new List<ItemDrop.ItemData>();
            if (p.m_utilityItem != null)
                list.Add(p.m_utilityItem);
            if (ExtraSlotsAdapter.Active)
                for (int i = 0; i < 4; i++)
                {
                    ItemDrop.ItemData x = ExtraSlotsAdapter.ExtraUtility(p, i);
                    if (x != null && !list.Contains(x))
                        list.Add(x);
                }
            return list;
        }
    }
}
