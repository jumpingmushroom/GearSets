using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace GearSets.Core
{
    /// <summary>
    /// KeyboardShortcut.IsDown() fails while any other key is held (e.g. W while running), which
    /// would make a hold-key radial unusable on the move. These only check the shortcut's own keys.
    /// </summary>
    internal static class Keys
    {
        public static bool Down(KeyboardShortcut s)
        {
            return s.MainKey != KeyCode.None && UnityInput.Current.GetKeyDown(s.MainKey) && Modifiers(s);
        }

        public static bool Held(KeyboardShortcut s)
        {
            return s.MainKey != KeyCode.None && UnityInput.Current.GetKey(s.MainKey);
        }

        public static bool Up(KeyboardShortcut s)
        {
            return s.MainKey != KeyCode.None && UnityInput.Current.GetKeyUp(s.MainKey);
        }

        private static bool Modifiers(KeyboardShortcut s)
        {
            foreach (KeyCode m in s.Modifiers)
                if (!UnityInput.Current.GetKey(m))
                    return false;
            return true;
        }
    }
}
