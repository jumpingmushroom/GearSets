using System;
using System.Collections.Generic;

namespace GearSets.Core
{
    /// <summary>The per-copy identity a set follows: m_customData["gearsets.id"], saved with the item everywhere.</summary>
    internal static class Tags
    {
        public const string Key = "gearsets.id";

        public static string Get(ItemDrop.ItemData item)
        {
            string tag;
            if (item == null || item.m_customData == null || !item.m_customData.TryGetValue(Key, out tag) || string.IsNullOrEmpty(tag))
                return null;
            return tag;
        }

        public static void Set(ItemDrop.ItemData item, string tag)
        {
            if (item.m_customData == null)
                item.m_customData = new Dictionary<string, string>();
            item.m_customData[Key] = tag;
        }

        public static string New()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
