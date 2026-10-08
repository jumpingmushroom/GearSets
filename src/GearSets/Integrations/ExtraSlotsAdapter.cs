using System;
using System.Reflection;
using HarmonyLib;

namespace GearSets.Integrations
{
    /// <summary>
    /// ExtraSlots 1.2.17: up to four extra utility slots (HumanoidExtension.GetExtraUtility) and a
    /// taller inventory whose rows from API.GetInventoryHeightPlayer() down are slot cells.
    /// Reflection only; any failure turns the adapter off and GearSets behaves as in vanilla.
    /// </summary>
    internal static class ExtraSlotsAdapter
    {
        private static MethodInfo _getExtraUtility;
        private static MemberInfo _activeSlots;
        private static MethodInfo _heightPlayer;

        public static bool Active { get; private set; }

        public static void Init()
        {
            Active = false;
            if (!PluginConfig.IntegrateExtraSlots.Value || !ModIds.Loaded(ModIds.ExtraSlots))
                return;
            try
            {
                Type ext = AccessTools.TypeByName("ExtraSlots.HumanoidExtension");
                Type util = AccessTools.TypeByName("ExtraSlots.ExtraUtilitySlots");
                Type api = AccessTools.TypeByName("ExtraSlots.API");
                _getExtraUtility = ext != null ? AccessTools.Method(ext, "GetExtraUtility", new[] { typeof(Humanoid), typeof(int) }) : null;
                _activeSlots = util != null ? (MemberInfo)AccessTools.Property(util, "ActiveSlots") ?? AccessTools.Field(util, "ActiveSlots") : null;
                _heightPlayer = api != null ? AccessTools.Method(api, "GetInventoryHeightPlayer") : null;
                if (_getExtraUtility == null || _activeSlots == null || _heightPlayer == null)
                    throw new MissingMemberException("ExtraSlots API not found (GetExtraUtility/ActiveSlots/GetInventoryHeightPlayer)");
                Active = true;
                GearSetsPlugin.Log.LogInfo("GearSets: ExtraSlots integration on.");
            }
            catch (Exception e)
            {
                GearSetsPlugin.WarnOnce("GearSets: ExtraSlots integration disabled", e);
            }
        }

        public static int ExtraUtilitySlots()
        {
            try
            {
                object v = _activeSlots is PropertyInfo p ? p.GetValue(null, null) : ((FieldInfo)_activeSlots).GetValue(null);
                return Math.Max(0, Math.Min(4, (int)v));
            }
            catch (Exception e)
            {
                Fail(e);
                return 0;
            }
        }

        public static ItemDrop.ItemData ExtraUtility(Player p, int index)
        {
            try
            {
                return (ItemDrop.ItemData)_getExtraUtility.Invoke(null, new object[] { p, index });
            }
            catch (Exception e)
            {
                Fail(e);
                return null;
            }
        }

        public static int PlayerRows()
        {
            try
            {
                return (int)_heightPlayer.Invoke(null, null);
            }
            catch (Exception e)
            {
                Fail(e);
                return -1;
            }
        }

        private static void Fail(Exception e)
        {
            Active = false;
            GearSetsPlugin.WarnOnce("GearSets: ExtraSlots call failed; integration turned off", e);
        }
    }
}
