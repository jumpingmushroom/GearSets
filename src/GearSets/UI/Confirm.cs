using System;

namespace GearSets.UI
{
    /// <summary>The game's own yes/no popup.</summary>
    internal static class Confirm
    {
        public static void Ask(string header, string text, Action yes)
        {
            UnifiedPopup.Push(new YesNoPopup(header, text,
                delegate
                {
                    UnifiedPopup.Pop();
                    try
                    {
                        yes();
                    }
                    catch (Exception e)
                    {
                        GearSetsPlugin.WarnOnce("GearSets: confirmed action failed", e);
                    }
                },
                delegate { UnifiedPopup.Pop(); },
                false));
        }
    }
}
