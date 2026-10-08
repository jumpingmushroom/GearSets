using BepInEx.Configuration;
using UnityEngine;

namespace GearSets
{
    public enum MarkerCorner
    {
        BottomLeft,
        BottomRight,
        TopLeft,
        TopRight
    }

    public enum RadialStyle
    {
        /// <summary>Valheim's own radial menu.</summary>
        BuiltIn,
        /// <summary>GearSets' own hold-and-point ring.</summary>
        Classic
    }

    public static class PluginConfig
    {
        // General
        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<int> MaxSets;
        public static ConfigEntry<bool> InstantSwap;

        // Radial
        public static ConfigEntry<bool> RadialEnabled;
        public static ConfigEntry<KeyboardShortcut> RadialKey;
        public static ConfigEntry<RadialStyle> RadialStyle;
        public static ConfigEntry<bool> ShowInMainRadial;

        // UI
        public static ConfigEntry<float> WindowScale;
        public static ConfigEntry<float> WindowX;
        public static ConfigEntry<float> WindowY;
        public static ConfigEntry<bool> ShowTooltipLine;
        public static ConfigEntry<bool> ShowCellMarker;
        public static ConfigEntry<MarkerCorner> CellMarkerCorner;

        // Protection
        public static ConfigEntry<bool> ConfirmDrop;
        public static ConfigEntry<bool> ConfirmObliterate;
        public static ConfigEntry<bool> ConfirmTrash;
        public static ConfigEntry<bool> StoreAllSkipsSetItems;

        // Integrations
        public static ConfigEntry<bool> IntegrateExtraSlots;
        public static ConfigEntry<bool> IntegrateQuickStack;
        public static ConfigEntry<bool> IntegrateConfigurationManager;

        // Logging
        public static ConfigEntry<bool> Verbose;

        private static ConfigurationManagerAttributes Attr(int order, bool advanced = false)
        {
            return new ConfigurationManagerAttributes { Order = order, IsAdvanced = advanced };
        }

        public static void Bind(ConfigFile cfg)
        {
            Enabled = cfg.Bind("General", "Enabled", true,
                new ConfigDescription("Master switch. Off hides the tab, window, radial, markers and tooltip line.", null, Attr(100)));
            MaxSets = cfg.Bind("General", "MaxSets", 10,
                new ConfigDescription("Most gear sets per character.", new AcceptableValueRange<int>(1, 20), Attr(95)));
            InstantSwap = cfg.Bind("General", "InstantSwap", false,
                new ConfigDescription("Equip a whole set instantly instead of through the vanilla equip queue " +
                    "(about one second per armour piece, interrupted by jumping, dodging, attacking or running).", null, Attr(90)));

            RadialEnabled = cfg.Bind("Radial", "RadialEnabled", true,
                new ConfigDescription("The radial key opens a radial of your gear sets in the world.", null, Attr(80)));
            RadialKey = cfg.Bind("Radial", "RadialKey", new KeyboardShortcut(KeyCode.H),
                new ConfigDescription("Opens the gear set radial. Tap or hold it like the vanilla radial key (G); " +
                    "T is the emote wheel.", null, Attr(75)));
            RadialStyle = cfg.Bind("Radial", "RadialStyle", GearSets.RadialStyle.BuiltIn,
                new ConfigDescription("BuiltIn uses Valheim's own radial menu. Classic is GearSets' own ring: " +
                    "hold the key, point at a set, release to equip.", null, Attr(74)));
            ShowInMainRadial = cfg.Bind("Radial", "ShowInMainRadial", true,
                new ConfigDescription("Add a Gear Sets group to Valheim's radial menu (G, or the gamepad radial).", null, Attr(73)));

            WindowScale = cfg.Bind("UI", "WindowScale", 1f,
                new ConfigDescription("Size of the Gear Sets window.", new AcceptableValueRange<float>(0.6f, 1.6f), Attr(70)));
            WindowX = cfg.Bind("UI", "WindowX", 0f,
                new ConfigDescription("Window position from the screen centre (set by dragging its title bar).", null, Attr(65, true)));
            WindowY = cfg.Bind("UI", "WindowY", 0f,
                new ConfigDescription("Window position from the screen centre (set by dragging its title bar).", null, Attr(64, true)));
            ShowTooltipLine = cfg.Bind("UI", "ShowTooltipLine", true,
                new ConfigDescription("Add \"Gear sets: …\" to the tooltip of items that belong to a set.", null, Attr(60)));
            ShowCellMarker = cfg.Bind("UI", "ShowCellMarker", true,
                new ConfigDescription("Mark inventory cells holding set items with a small diamond.", null, Attr(55)));
            CellMarkerCorner = cfg.Bind("UI", "CellMarkerCorner", MarkerCorner.BottomLeft,
                new ConfigDescription("Corner of the cell marker. Top-right is where MyLittleUI draws quality stars.", null, Attr(50)));

            ConfirmDrop = cfg.Bind("Protection", "ConfirmDrop", true,
                new ConfigDescription("Ask before dropping an item that belongs to a set.", null, Attr(45)));
            ConfirmObliterate = cfg.Bind("Protection", "ConfirmObliterate", true,
                new ConfigDescription("Ask before the obliterator destroys items that belong to a set.", null, Attr(44)));
            ConfirmTrash = cfg.Bind("Protection", "ConfirmTrash", true,
                new ConfigDescription("Ask before Quick Stack's trash destroys a set item; quick-trash keeps set items.", null, Attr(43)));
            StoreAllSkipsSetItems = cfg.Bind("Protection", "StoreAllSkipsSetItems", true,
                new ConfigDescription("Quick Stack's Store All leaves set items in your inventory, like favourites.", null, Attr(42)));

            IntegrateExtraSlots = cfg.Bind("Integrations", "ExtraSlots", true,
                new ConfigDescription("Use ExtraSlots' extra utility slots and equipment cells when it is installed.", null, Attr(30, true)));
            IntegrateQuickStack = cfg.Bind("Integrations", "QuickStackStore", true,
                new ConfigDescription("Protect set items from Quick Stack's trash and Store All when it is installed.", null, Attr(29, true)));
            IntegrateConfigurationManager = cfg.Bind("Integrations", "ConfigurationManager", true,
                new ConfigDescription("Don't open the radial while the configuration manager window is open.", null, Attr(28, true)));

            Verbose = cfg.Bind("Logging", "Verbose", false,
                new ConfigDescription("Log each plan and swap step.", null, Attr(10, true)));
        }
    }
}
