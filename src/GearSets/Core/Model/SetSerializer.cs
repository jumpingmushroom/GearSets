using System;
using System.Collections.Generic;

namespace GearSets.Core.Model
{
    /// <summary>
    /// All of a character's sets as one JSON string in Player.m_customData[Key]:
    /// {"v":1,"sets":[{"id","name","icon","slots":{"Chest":entry,…},"util":util,"hotbar":[entry×8]}]}
    /// entry = {"m":"ignore"|"empty"|"item","ref":ref?,"e":true?}; util = {"m":…,"refs":[ref…],"e":true?};
    /// ref = {"t":tag|null,"p":prefab,"n":name,"q":quality,"v":variant,"c":crafterId}.
    /// </summary>
    public static class SetSerializer
    {
        public const string Key = "gearsets.v1";
        public const string BackupKey = "gearsets.v1.bak";
        public const int Version = 1;

        public static string Write(IEnumerable<GearSet> sets)
        {
            var list = new List<object>();
            foreach (GearSet s in sets)
            {
                var slots = new Dictionary<string, object>();
                foreach (SlotKind k in GearSet.AllSlots)
                    slots[k.ToString()] = Entry(s.Slots[k]);
                var hotbar = new List<object>();
                foreach (SlotEntry e in s.Hotbar)
                    hotbar.Add(Entry(e));
                var refs = new List<object>();
                foreach (ItemRef r in s.Utilities.Items)
                    refs.Add(Ref(r));
                var util = new Dictionary<string, object> { ["m"] = Mode(s.Utilities.Mode), ["refs"] = refs };
                if (s.Utilities.WasEmpty)
                    util["e"] = true;
                list.Add(new Dictionary<string, object>
                {
                    ["id"] = s.Id,
                    ["name"] = s.Name,
                    ["icon"] = s.Icon ?? "",
                    ["slots"] = slots,
                    ["util"] = util,
                    ["hotbar"] = hotbar
                });
            }
            return MiniJson.Write(new Dictionary<string, object> { ["v"] = (long)Version, ["sets"] = list });
        }

        public static bool TryRead(string text, out List<GearSet> sets, out string error)
        {
            sets = null;
            error = null;
            try
            {
                var root = MiniJson.Parse(text) as Dictionary<string, object>;
                if (root == null)
                    throw new FormatException("not an object");
                if (!(root.TryGetValue("v", out object v) && v is long ver && ver == Version))
                    throw new FormatException("unsupported version");
                if (!(root.TryGetValue("sets", out object raw) && raw is List<object> items))
                    throw new FormatException("no sets");
                var result = new List<GearSet>();
                foreach (object o in items)
                    result.Add(ReadSet((Dictionary<string, object>)o));
                sets = result;
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        private static object Entry(SlotEntry e)
        {
            var d = new Dictionary<string, object> { ["m"] = Mode(e.Mode) };
            if (e.Item != null)
                d["ref"] = Ref(e.Item);
            if (e.WasEmpty)
                d["e"] = true;
            return d;
        }

        private static object Ref(ItemRef r)
        {
            return new Dictionary<string, object>
            {
                ["t"] = r.Tag,
                ["p"] = r.Prefab,
                ["n"] = r.Name,
                ["q"] = (long)r.Quality,
                ["v"] = (long)r.Variant,
                ["c"] = r.CrafterId
            };
        }

        private static string Mode(EntryMode m)
        {
            return m == EntryMode.Item ? "item" : m == EntryMode.Empty ? "empty" : "ignore";
        }

        private static EntryMode ParseMode(object o)
        {
            string s = o as string;
            return s == "item" ? EntryMode.Item : s == "empty" ? EntryMode.Empty : EntryMode.Ignore;
        }

        private static GearSet ReadSet(Dictionary<string, object> d)
        {
            var s = new GearSet(Str(d, "id"), Str(d, "name"), Str(d, "icon"));
            if (string.IsNullOrEmpty(s.Id) || string.IsNullOrEmpty(s.Name))
                throw new FormatException("set without id or name");
            if (d.TryGetValue("slots", out object so) && so is Dictionary<string, object> slots)
                foreach (KeyValuePair<string, object> kv in slots)
                    if (Enum.TryParse(kv.Key, out SlotKind k) && Enum.IsDefined(typeof(SlotKind), k))
                        s.Slots[k] = ReadEntry((Dictionary<string, object>)kv.Value);
            if (d.TryGetValue("util", out object uo) && uo is Dictionary<string, object> util)
            {
                var refs = new List<ItemRef>();
                if (util.TryGetValue("refs", out object ro) && ro is List<object> rl)
                    foreach (object r in rl)
                        refs.Add(ReadRef((Dictionary<string, object>)r));
                s.Utilities = UtilityEntry.Create(ParseMode(Get(util, "m")), refs, Get(util, "e") is bool b && b);
            }
            if (d.TryGetValue("hotbar", out object ho) && ho is List<object> hotbar)
                for (int i = 0; i < hotbar.Count && i < GearSet.HotbarSize; i++)
                    s.Hotbar[i] = ReadEntry((Dictionary<string, object>)hotbar[i]);
            return s;
        }

        private static SlotEntry ReadEntry(Dictionary<string, object> d)
        {
            ItemRef r = Get(d, "ref") is Dictionary<string, object> rd ? ReadRef(rd) : null;
            EntryMode mode = ParseMode(Get(d, "m"));
            if (mode == EntryMode.Item && r == null)
                mode = EntryMode.Ignore;
            return SlotEntry.Create(mode, r, Get(d, "e") is bool b && b);
        }

        private static ItemRef ReadRef(Dictionary<string, object> d)
        {
            return new ItemRef(Get(d, "t") as string, Str(d, "p"), Str(d, "n"),
                (int)Num(d, "q", 1), (int)Num(d, "v", 0), Num(d, "c", 0));
        }

        private static object Get(Dictionary<string, object> d, string key)
        {
            return d.TryGetValue(key, out object v) ? v : null;
        }

        private static string Str(Dictionary<string, object> d, string key)
        {
            return Get(d, key) as string ?? "";
        }

        private static long Num(Dictionary<string, object> d, string key, long fallback)
        {
            object v = Get(d, key);
            if (v is long l)
                return l;
            if (v is double x)
                return (long)x;
            return fallback;
        }
    }
}
