using System;
using GearSets.Core.Model;
using UnityEngine;

namespace GearSets.UI
{
    /// <summary>
    /// Names go through the game's own text popup (as when naming a portal), so game hotkeys are
    /// blocked while typing. Pending lets InventoryPatches stop the inventory from closing on E/Tab.
    /// </summary>
    internal sealed class NameInput : TextReceiver
    {
        private static int _askedFrame;

        private readonly string _initial;
        private readonly Action<string> _done;

        private NameInput(string initial, Action<string> done)
        {
            _initial = initial ?? "";
            _done = done;
        }

        public static bool Pending { get; private set; }

        public static void Ask(string initial, Action<string> done)
        {
            if (TextInput.instance == null)
                return;
            Pending = true;
            _askedFrame = Time.frameCount;
            TextInput.instance.RequestText(new NameInput(initial, done), "Gear set name", GearSet.MaxNameLength);
        }

        /// <summary>Called by the Update guard; clears Pending once the popup has closed (Esc cancels without SetText).</summary>
        public static void Poll()
        {
            if (Pending && Time.frameCount > _askedFrame + 2 && !TextInput.IsVisible())
                Pending = false;
        }

        public string GetText()
        {
            return _initial;
        }

        public void SetText(string text)
        {
            Pending = false;
            _done(text ?? "");
        }
    }
}
