using System;
using System.Collections.Generic;

namespace GearSets.Core.Model
{
    /// <summary>A character's sets: name rules, the limit, and which sets use which item.</summary>
    public sealed class SetBook
    {
        public readonly List<GearSet> Sets = new List<GearSet>();
        public int MaxSets = 10;

        public bool IsFull { get { return Sets.Count >= MaxSets; } }

        public GearSet Find(string id)
        {
            foreach (GearSet s in Sets)
                if (s.Id == id)
                    return s;
            return null;
        }

        public GearSet FindByName(string name)
        {
            string n = (name ?? "").Trim();
            foreach (GearSet s in Sets)
                if (string.Equals(s.Name, n, StringComparison.OrdinalIgnoreCase))
                    return s;
            return null;
        }

        /// <summary>Null when the name is fine for a set with id <paramref name="exceptId"/> (null for a new set).</summary>
        public string NameError(string name, string exceptId)
        {
            string n = (name ?? "").Trim();
            if (n.Length == 0)
                return "Name can't be empty.";
            if (n.Length > GearSet.MaxNameLength)
                return "Name is longer than " + GearSet.MaxNameLength + " characters.";
            if (n.IndexOf('<') >= 0 || n.IndexOf('>') >= 0)
                return "Name can't contain < or >."; // names are shown as rich text
            GearSet other = FindByName(n);
            if (other != null && other.Id != exceptId)
                return "A set named " + other.Name + " already exists.";
            return null;
        }

        public void Add(GearSet s)
        {
            if (IsFull)
                throw new InvalidOperationException("The set book is full.");
            Sets.Add(s);
        }

        public bool Remove(string id)
        {
            GearSet s = Find(id);
            return s != null && Sets.Remove(s);
        }

        /// <summary>Item tag → names of the sets that actively use it, in set order.</summary>
        public Dictionary<string, List<string>> Membership()
        {
            var map = new Dictionary<string, List<string>>();
            foreach (GearSet s in Sets)
                foreach (string tag in s.Tags())
                {
                    List<string> names;
                    if (!map.TryGetValue(tag, out names))
                        map[tag] = names = new List<string>();
                    if (!names.Contains(s.Name))
                        names.Add(s.Name);
                }
            return map;
        }
    }
}
