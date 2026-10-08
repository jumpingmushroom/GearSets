using System;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace GearSets.Integrations
{
    /// <summary>Whether a configuration manager window (shudnal's or BepInEx's) is open.</summary>
    internal static class ConfigManagerAdapter
    {
        private static Func<bool> _open;

        public static bool WindowOpen
        {
            get
            {
                try
                {
                    return _open != null && _open();
                }
                catch (Exception e)
                {
                    _open = null;
                    GearSetsPlugin.WarnOnce("GearSets: configuration manager check failed; turned off", e);
                    return false;
                }
            }
        }

        public static void Init()
        {
            _open = null;
            if (!PluginConfig.IntegrateConfigurationManager.Value)
                return;
            foreach (string id in new[] { ModIds.ConfigurationManager, ModIds.ConfigurationManagerBepis })
            {
                PluginInfo info;
                if (!Chainloader.PluginInfos.TryGetValue(id, out info) || info.Instance == null)
                    continue;
                PropertyInfo prop = AccessTools.Property(info.Instance.GetType(), "DisplayingWindow");
                if (prop == null)
                    continue;
                object instance = info.Instance;
                _open = () => (bool)prop.GetValue(instance, null);
                return;
            }
        }
    }
}
