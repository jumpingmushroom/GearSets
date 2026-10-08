using System;
using System.Collections.Generic;
using GearSets.Core.Model;

namespace GearSets.Core
{
    /// <summary>The local player's sets, in Player.m_customData (saved with the character, never synced).</summary>
    internal static class SetStore
    {
        private static readonly IList<string> NoSets = new string[0];
        private static Dictionary<string, List<string>> _membership = new Dictionary<string, List<string>>();
        private static string _unreadable;

        public static SetBook Book { get; private set; } = new SetBook();

        public static event Action Changed;

        public static void Load(Player p)
        {
            Book = new SetBook { MaxSets = PluginConfig.MaxSets.Value };
            _unreadable = null;
            string raw;
            if (p != null && p.m_customData.TryGetValue(SetSerializer.Key, out raw) && !string.IsNullOrEmpty(raw))
            {
                List<GearSet> sets;
                string error;
                if (SetSerializer.TryRead(raw, out sets, out error))
                    Book.Sets.AddRange(sets);
                else
                {
                    _unreadable = raw;
                    GearSetsPlugin.Log.LogWarning("GearSets: saved sets could not be read (" + error +
                        "); starting with none. The data is kept and backed up before anything is saved.");
                }
            }
            GearSetsPlugin.Verbose("GearSets: loaded " + Book.Sets.Count + " sets.");
            Rebuild();
        }

        public static void Save()
        {
            Player p = Player.m_localPlayer;
            if (p == null)
                return;
            if (_unreadable != null)
            {
                p.m_customData[SetSerializer.BackupKey] = _unreadable;
                _unreadable = null;
            }
            p.m_customData[SetSerializer.Key] = SetSerializer.Write(Book.Sets);
            Rebuild();
        }

        /// <summary>Names of the sets that actively use the item with this tag; empty, never null.</summary>
        public static IList<string> SetsFor(string tag)
        {
            List<string> names;
            if (tag != null && _membership.TryGetValue(tag, out names))
                return names;
            return NoSets;
        }

        private static void Rebuild()
        {
            _membership = Book.Membership();
            Action handler = Changed;
            if (handler != null)
                handler();
        }
    }
}
