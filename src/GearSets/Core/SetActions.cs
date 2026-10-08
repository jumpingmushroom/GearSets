using System.Collections.Generic;
using GearSets.Core.Model;

namespace GearSets.Core
{
    /// <summary>Everything that changes the set book. Captures write new item tags before saving.</summary>
    internal static class SetActions
    {
        public static GearSet SaveNew(string name, string icon, CaptureMask mask, out string error)
        {
            Player p = Player.m_localPlayer;
            if (p == null)
            {
                error = "Not in a game.";
                return null;
            }
            SetBook book = SetStore.Book;
            if (book.IsFull)
            {
                error = "You already have " + book.MaxSets + " gear sets.";
                return null;
            }
            error = book.NameError(name, null);
            if (error != null)
                return null;
            var set = new GearSet(Tags.New(), name.Trim(), icon);
            Capture(p, set, mask);
            if (string.IsNullOrEmpty(set.Icon))
                set.Icon = DefaultIcon(set);
            book.Add(set);
            SetStore.Save();
            return set;
        }

        /// <summary>Re-capture what is worn, keeping which slots are ignored.</summary>
        public static void Update(GearSet set)
        {
            Player p = Player.m_localPlayer;
            if (p == null || set == null)
                return;
            Capture(p, set, CaptureMask.From(set));
            SetStore.Save();
        }

        /// <summary>Null on success, else the reason.</summary>
        public static string Rename(GearSet set, string name)
        {
            string error = SetStore.Book.NameError(name, set.Id);
            if (error != null)
                return error;
            set.Name = name.Trim();
            SetStore.Save();
            return null;
        }

        public static void SetIcon(GearSet set, string prefab)
        {
            set.Icon = prefab;
            SetStore.Save();
        }

        public static void Delete(GearSet set)
        {
            SetStore.Book.Remove(set.Id);
            SetStore.Save();
        }

        public static string DefaultIcon(GearSet set)
        {
            SlotKind[] order = { SlotKind.RightHand, SlotKind.LeftHand, SlotKind.Chest, SlotKind.Helmet, SlotKind.Shoulder, SlotKind.Legs };
            foreach (SlotKind k in order)
                if (set.Slots[k].Mode == EntryMode.Item)
                    return set.Slots[k].Item.Prefab;
            List<string> any = IconChoices(set);
            return any.Count > 0 ? any[0] : "";
        }

        /// <summary>Distinct prefabs of the set's items, worn slots first, then utilities and hotbar.</summary>
        public static List<string> IconChoices(GearSet set)
        {
            var list = new List<string>();
            foreach (SlotKind k in GearSet.AllSlots)
                Add(list, set.Slots[k].Item);
            foreach (ItemRef r in set.Utilities.Items)
                Add(list, r);
            foreach (SlotEntry e in set.Hotbar)
                Add(list, e.Item);
            return list;
        }

        private static void Add(List<string> list, ItemRef r)
        {
            if (r != null && !string.IsNullOrEmpty(r.Prefab) && !list.Contains(r.Prefab))
                list.Add(r.Prefab);
        }

        private static void Capture(Player p, GearSet set, CaptureMask mask)
        {
            Snapshot s = Snapshotter.Take(p);
            foreach (TagAssignment a in SetCapture.Capture(s.Facts, set, mask, Tags.New))
                Tags.Set(s.Of(a.Item), a.Tag);
        }
    }
}
