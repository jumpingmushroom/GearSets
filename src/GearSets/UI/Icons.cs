using GearSets.Core.Model;
using UnityEngine;

namespace GearSets.UI
{
    internal static class Icons
    {
        public static Sprite For(string prefab, int variant = 0)
        {
            if (string.IsNullOrEmpty(prefab) || ObjectDB.instance == null)
                return null;
            GameObject go = ObjectDB.instance.GetItemPrefab(prefab);
            if (go == null)
                return null;
            ItemDrop drop = go.GetComponent<ItemDrop>();
            if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null)
                return null;
            Sprite[] icons = drop.m_itemData.m_shared.m_icons;
            if (icons == null || icons.Length == 0)
                return null;
            return icons[Mathf.Clamp(variant, 0, icons.Length - 1)];
        }

        public static Sprite For(ItemRef r)
        {
            return r == null ? null : For(r.Prefab, r.Variant);
        }
    }
}
