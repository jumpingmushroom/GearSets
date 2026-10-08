using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace GearSets
{
    /// <summary>
    /// WoW-style gear sets: save what you wear as a named set and re-equip it later. Client-side
    /// only; equipping goes through the vanilla equip queue.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency("shudnal.ExtraSlots", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("goldenrevolver.quick_stack_store", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("shudnal.MyLittleUI", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("_shudnal.ConfigurationManager", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.bepis.bepinex.configurationmanager", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim.x86_64")]
    public sealed class GearSetsPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jumpingmushroom.gearsets";
        public const string PluginName = "GearSets";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;
        internal static Harmony Harmony;

        private static readonly HashSet<string> Warned = new HashSet<string>();

        private void Awake()
        {
            Log = Logger;
            PluginConfig.Bind(Config);

            Harmony = new Harmony(PluginGuid);
            Harmony.PatchAll(typeof(GearSetsPlugin).Assembly);
            Integrations.Adapters.Init(Harmony);
            Core.ConsoleCommands.Register();
            PluginConfig.MaxSets.SettingChanged += (s, e) => Core.SetStore.Book.MaxSets = PluginConfig.MaxSets.Value;

            Logger.LogInfo(PluginName + " " + PluginVersion + " loaded.");
        }

        private void OnDestroy()
        {
            if (Harmony != null)
                Harmony.UnpatchSelf();
        }

        /// <summary>Log an exception once per key, so a broken hook can't flood the log every frame.</summary>
        internal static void WarnOnce(string key, Exception e)
        {
            if (Warned.Add(key))
                Log.LogWarning(key + ": " + e);
        }

        internal static void Verbose(string message)
        {
            if (PluginConfig.Verbose != null && PluginConfig.Verbose.Value)
                Log.LogInfo(message);
        }
    }
}
