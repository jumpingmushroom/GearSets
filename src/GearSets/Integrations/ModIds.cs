using BepInEx.Bootstrap;

namespace GearSets.Integrations
{
    internal static class ModIds
    {
        public const string ExtraSlots = "shudnal.ExtraSlots";
        public const string QuickStack = "goldenrevolver.quick_stack_store";
        public const string MyLittleUI = "shudnal.MyLittleUI";
        public const string ConfigurationManager = "_shudnal.ConfigurationManager";
        public const string ConfigurationManagerBepis = "com.bepis.bepinex.configurationmanager";

        public static bool Loaded(string guid)
        {
            return Chainloader.PluginInfos.ContainsKey(guid);
        }
    }
}
