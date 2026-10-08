namespace GearSets.Core.Model
{
    /// <summary>What a set remembers about one item: the exact copy (Tag) and enough to find a substitute.</summary>
    public sealed class ItemRef
    {
        /// <summary>gearsets.id of the exact copy; null for stackable items, which match by prefab.</summary>
        public readonly string Tag;
        /// <summary>Prefab name (ItemData.m_dropPrefab.name), e.g. "PickaxeIron".</summary>
        public readonly string Prefab;
        /// <summary>Localization token (m_shared.m_name), e.g. "$item_pickaxe_iron".</summary>
        public readonly string Name;
        public readonly int Quality;
        public readonly int Variant;
        public readonly long CrafterId;

        public ItemRef(string tag, string prefab, string name, int quality, int variant, long crafterId)
        {
            Tag = tag;
            Prefab = prefab;
            Name = name;
            Quality = quality;
            Variant = variant;
            CrafterId = crafterId;
        }
    }
}
