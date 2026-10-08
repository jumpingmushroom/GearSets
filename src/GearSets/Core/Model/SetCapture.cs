using System;
using System.Collections.Generic;

namespace GearSets.Core.Model
{
    /// <summary>Which parts of the worn gear a capture records. Excluded parts become Ignore.</summary>
    public sealed class CaptureMask
    {
        public readonly HashSet<SlotKind> Slots = new HashSet<SlotKind>();
        public bool Utilities;
        public readonly bool[] Hotbar = new bool[GearSet.HotbarSize];

        public static CaptureMask All()
        {
            var m = new CaptureMask { Utilities = true };
            foreach (SlotKind k in GearSet.AllSlots)
                m.Slots.Add(k);
            for (int i = 0; i < m.Hotbar.Length; i++)
                m.Hotbar[i] = true;
            return m;
        }

        /// <summary>"Update from current": keep the set's ignored slots ignored.</summary>
        public static CaptureMask From(GearSet set)
        {
            var m = new CaptureMask { Utilities = set.Utilities.Mode != EntryMode.Ignore };
            foreach (SlotKind k in GearSet.AllSlots)
                if (set.Slots[k].Mode != EntryMode.Ignore)
                    m.Slots.Add(k);
            for (int i = 0; i < m.Hotbar.Length; i++)
                m.Hotbar[i] = set.Hotbar[i].Mode == EntryMode.Item;
            return m;
        }
    }

    /// <summary>The game layer writes Tag into Item's m_customData.</summary>
    public sealed class TagAssignment
    {
        public readonly ItemFacts Item;
        public readonly string Tag;

        public TagAssignment(ItemFacts item, string tag)
        {
            Item = item;
            Tag = tag;
        }
    }

    public static class SetCapture
    {
        /// <summary>
        /// Records what is worn (and hotbar row 0) into <paramref name="into"/>. Untagged equippables
        /// get a new tag; when another item carries the same tag as a captured one (a clone), the
        /// other copy gets a new tag so the set keeps following this one.
        /// </summary>
        public static List<TagAssignment> Capture(InventorySnapshot snap, GearSet into, CaptureMask mask, Func<string> newTag)
        {
            var assigned = new List<TagAssignment>();
            var tags = new Dictionary<int, string>();

            foreach (SlotKind k in GearSet.AllSlots)
            {
                if (!mask.Slots.Contains(k))
                {
                    into.Slots[k] = SlotEntry.Ignore;
                    continue;
                }
                ItemFacts worn = snap.WornIn(Slots.Worn(k));
                into.Slots[k] = worn == null ? SlotEntry.Empty : SlotEntry.Of(Ref(worn, snap, tags, assigned, newTag));
            }

            if (!mask.Utilities)
                into.Utilities = UtilityEntry.Ignore;
            else
            {
                var refs = new List<ItemRef>();
                foreach (ItemFacts u in snap.WornUtilities())
                    refs.Add(Ref(u, snap, tags, assigned, newTag));
                into.Utilities = UtilityEntry.Of(refs);
            }

            for (int i = 0; i < GearSet.HotbarSize; i++)
            {
                ItemFacts f = mask.Hotbar[i] ? snap.At(i, 0) : null;
                into.Hotbar[i] = f == null ? SlotEntry.Ignore : SlotEntry.Of(Ref(f, snap, tags, assigned, newTag));
            }
            return assigned;
        }

        private static ItemRef Ref(ItemFacts f, InventorySnapshot snap, Dictionary<int, string> tags,
            List<TagAssignment> assigned, Func<string> newTag)
        {
            if (f.Stackable)
                return f.ToRef(null);
            string tag;
            if (!tags.TryGetValue(f.Index, out tag))
            {
                tag = f.Tag;
                if (tag == null)
                {
                    tag = newTag();
                    assigned.Add(new TagAssignment(f, tag));
                    f.Tag = tag;
                }
                else
                {
                    foreach (ItemFacts other in snap.Items)
                    {
                        if (other == f || other.Stackable || other.Tag != tag || tags.ContainsKey(other.Index))
                            continue;
                        string fresh = newTag();
                        assigned.Add(new TagAssignment(other, fresh));
                        other.Tag = fresh;
                    }
                }
                tags[f.Index] = tag;
            }
            return f.ToRef(tag);
        }
    }
}
