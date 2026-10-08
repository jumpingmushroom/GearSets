using System;
using System.Collections.Generic;
using System.Reflection;
using GearSets.Core;
using GearSets.Core.Model;
using GearSets.UI;
using HarmonyLib;

namespace GearSets.Integrations
{
    /// <summary>
    /// Quick Stack Store Sort Trash Restock deletes items without the game's drop path: its trash
    /// asks first for set items, quick-trash keeps them (their slots count as favourited while it
    /// runs), and Store All leaves them in the inventory.
    /// </summary>
    internal static class QuickStackAdapter
    {
        private static MethodInfo _trashItem;
        private static MethodInfo _getPlayerConfig;
        private static MethodInfo _isTrashFlagged;
        private static MethodInfo _isSlotFavorited;
        private static FieldInfo _overrideHotkeyBar;
        private static FieldInfo _trashCanAffectHotkeyBar;
        private static bool _bypass;
        private static bool _inQuickTrash;
        private static readonly HashSet<int> SetCells = new HashSet<int>();
        private static int _kept;
        private static List<ItemDrop.ItemData> _stash;
        private static List<ItemDrop.ItemData> _before;
        private static int _flaggedInStash;

        public static void Init(Harmony harmony)
        {
            if (!PluginConfig.IntegrateQuickStack.Value || !ModIds.Loaded(ModIds.QuickStack))
                return;
            try
            {
                Type trash = AccessTools.TypeByName("QuickStackStore.TrashModule");
                Type store = AccessTools.TypeByName("QuickStackStore.StoreTakeAllModule");
                Type userConfig = AccessTools.TypeByName("QuickStackStore.UserConfig");
                _trashItem = trash != null ? AccessTools.Method(trash, "TrashItem") : null;
                MethodInfo quickTrash = trash != null ? AccessTools.Method(trash, "QuickTrash") : null;
                MethodInfo shouldStore = store != null ? AccessTools.Method(store, "ShouldStoreItem") : null;
                _getPlayerConfig = userConfig != null ? AccessTools.Method(userConfig, "GetPlayerConfig", new[] { typeof(long) }) : null;
                _isTrashFlagged = userConfig != null ? AccessTools.Method(userConfig, "IsItemNameConsideredTrashFlagged") : null;
                if (_trashItem == null || quickTrash == null || shouldStore == null)
                    throw new MissingMemberException("Quick Stack API not found (TrashItem/QuickTrash/ShouldStoreItem)");

                harmony.Patch(_trashItem, prefix: new HarmonyMethod(typeof(QuickStackAdapter), nameof(TrashPrefix)));
                // Before QuickTrash: patching marks IsSlotFavorited no-inline, and QuickTrash's
                // replacement is compiled when it is patched.
                PatchSlotFavorited(harmony, userConfig);
                harmony.Patch(quickTrash,
                    prefix: new HarmonyMethod(typeof(QuickStackAdapter), nameof(QuickTrashPrefix)),
                    finalizer: new HarmonyMethod(typeof(QuickStackAdapter), nameof(QuickTrashFinalizer)));
                harmony.Patch(shouldStore, prefix: new HarmonyMethod(typeof(QuickStackAdapter), nameof(ShouldStorePrefix)));
                GearSetsPlugin.Log.LogInfo("GearSets: Quick Stack integration on.");
            }
            catch (Exception e)
            {
                GearSetsPlugin.WarnOnce("GearSets: Quick Stack integration disabled", e);
            }
        }

        /// <summary>
        /// Quick-trash already skips favourited slots, so while it runs, slots holding set items
        /// report as favourited. Without that hook, quick-trash falls back to hiding set items
        /// from the item list (the stash path).
        /// </summary>
        private static void PatchSlotFavorited(Harmony harmony, Type userConfig)
        {
            try
            {
                _isSlotFavorited = userConfig != null ? AccessTools.Method(userConfig, "IsSlotFavorited", new[] { typeof(Vector2i) }) : null;
                if (_isSlotFavorited == null)
                    throw new MissingMemberException("QuickStackStore.UserConfig.IsSlotFavorited not found");
                harmony.Patch(_isSlotFavorited, prefix: new HarmonyMethod(typeof(QuickStackAdapter), nameof(SlotFavoritedPrefix)));
            }
            catch (Exception e)
            {
                _isSlotFavorited = null;
                GearSetsPlugin.WarnOnce("GearSets: Quick Stack slot hook unavailable, quick-trash hides set items instead", e);
            }
            try
            {
                Type config = AccessTools.TypeByName("QuickStackStore.QSSConfig");
                Type general = config != null ? AccessTools.Inner(config, "GeneralConfig") : null;
                Type trash = config != null ? AccessTools.Inner(config, "TrashConfig") : null;
                _overrideHotkeyBar = general != null ? AccessTools.Field(general, "OverrideHotkeyBarBehavior") : null;
                _trashCanAffectHotkeyBar = trash != null ? AccessTools.Field(trash, "TrashingCanAffectHotkeyBar") : null;
            }
            catch (Exception e)
            {
                _overrideHotkeyBar = _trashCanAffectHotkeyBar = null;
                GearSetsPlugin.WarnOnce("GearSets: Quick Stack hotbar settings not found", e);
            }
        }

        private static int Cell(Vector2i pos)
        {
            return pos.y * 1000 + pos.x;
        }

        private static bool SlotFavoritedPrefix(Vector2i position, ref bool __result)
        {
            try
            {
                if (!_inQuickTrash || !SetCells.Contains(Cell(position)))
                    return true;
                __result = true;
                return false;
            }
            catch (Exception e)
            {
                GearSetsPlugin.WarnOnce("GearSets: Quick Stack slot hook failed", e);
                return true;
            }
        }

        /// <summary>
        /// How many set items quick-trash would have removed, by its own test: hotbar rule, slot not
        /// favourited, trash-flagged. If its hotbar settings or the slot test can't be read, only
        /// the trash flag counts; without the trash flag, -1 (no message).
        /// </summary>
        private static int WouldTrash(Player p, List<ItemDrop.ItemData> setItems)
        {
            if (_getPlayerConfig == null || _isTrashFlagged == null)
                return -1;
            try
            {
                object cfg = _getPlayerConfig.Invoke(null, new object[] { p.GetPlayerID() });
                bool full = _isSlotFavorited != null && _overrideHotkeyBar != null && _trashCanAffectHotkeyBar != null;
                bool hotbarOk = false;
                if (full)
                {
                    var over = (BepInEx.Configuration.ConfigEntryBase)_overrideHotkeyBar.GetValue(null);
                    var can = (BepInEx.Configuration.ConfigEntryBase)_trashCanAffectHotkeyBar.GetValue(null);
                    hotbarOk = over.BoxedValue.ToString() != "NeverAffectHotkeyBar" && (bool)can.BoxedValue;
                }
                int count = 0;
                foreach (ItemDrop.ItemData it in setItems)
                {
                    if (!(bool)_isTrashFlagged.Invoke(cfg, new object[] { it.m_shared }))
                        continue;
                    if (full && ((it.m_gridPos.y == 0 && !hotbarOk) || (bool)_isSlotFavorited.Invoke(cfg, new object[] { it.m_gridPos })))
                        continue;
                    count++;
                }
                return count;
            }
            catch (Exception e)
            {
                GearSetsPlugin.WarnOnce("GearSets: quick-trash flag detection failed", e);
                return -1;
            }
        }

        private static bool InSet(ItemDrop.ItemData item)
        {
            return item != null && SetStore.SetsFor(Tags.Get(item)).Count > 0;
        }

        private static bool TrashPrefix(object[] __args)
        {
            try
            {
                if (_bypass || !PluginConfig.Enabled.Value || !PluginConfig.ConfirmTrash.Value)
                    return true;
                var item = __args[2] as ItemDrop.ItemData;
                if (!InSet(item))
                    return true;
                object[] saved = (object[])__args.Clone();
                if (InventoryGui.instance != null)
                    InventoryGui.instance.SetupDragItem(null, null, 1);
                Confirm.Ask("Trash " + Loc.T(item.m_shared.m_name) + "?",
                    "It's in your gear sets " + ReportText.JoinAnd(new List<string>(SetStore.SetsFor(Tags.Get(item)))) + " and will be destroyed.",
                    () =>
                    {
                        _bypass = true;
                        try
                        {
                            _trashItem.Invoke(null, saved);
                        }
                        finally
                        {
                            _bypass = false;
                        }
                    });
                return false;
            }
            catch (Exception e)
            {
                GearSetsPlugin.WarnOnce("GearSets: Quick Stack trash guard failed", e);
                return true;
            }
        }

        /// <summary>
        /// Mark the slots holding set items so quick-trash skips them (SlotFavoritedPrefix). Without
        /// that hook, hide set items from the player's item list; the finalizer puts them back.
        /// </summary>
        private static void QuickTrashPrefix()
        {
            _stash = null;
            _before = null;
            _flaggedInStash = -1;
            _inQuickTrash = false;
            SetCells.Clear();
            _kept = -1;
            try
            {
                Player p = Player.m_localPlayer;
                if (p == null || !PluginConfig.Enabled.Value)
                    return;
                List<ItemDrop.ItemData> all = p.GetInventory().m_inventory;
                List<ItemDrop.ItemData> stash = all.FindAll(InSet);
                if (stash.Count == 0)
                    return;
                if (_isSlotFavorited != null)
                {
                    _kept = WouldTrash(p, stash); // before the hook is live, so it sees the real favourites
                    foreach (ItemDrop.ItemData it in stash)
                        SetCells.Add(Cell(it.m_gridPos));
                    _inQuickTrash = true;
                    return;
                }
                _before = new List<ItemDrop.ItemData>(all);
                _stash = stash;
                foreach (ItemDrop.ItemData it in stash)
                    all.Remove(it);
                if (_getPlayerConfig != null && _isTrashFlagged != null)
                {
                    try
                    {
                        object cfg = _getPlayerConfig.Invoke(null, new object[] { p.GetPlayerID() });
                        int count = 0;
                        foreach (ItemDrop.ItemData it in stash)
                            if ((bool)_isTrashFlagged.Invoke(cfg, new object[] { it.m_shared }))
                                count++;
                        _flaggedInStash = count;
                    }
                    catch (Exception e)
                    {
                        _flaggedInStash = -1;
                        GearSetsPlugin.WarnOnce("GearSets: quick-trash flag detection failed", e);
                    }
                }
            }
            catch (Exception e)
            {
                _stash = null;
                _inQuickTrash = false;
                SetCells.Clear();
                GearSetsPlugin.WarnOnce("GearSets: quick-trash guard failed", e);
            }
        }

        private static void QuickTrashFinalizer()
        {
            if (_inQuickTrash)
            {
                _inQuickTrash = false;
                SetCells.Clear();
                try
                {
                    Player p = Player.m_localPlayer;
                    if (p != null && _kept > 0)
                        p.Message(MessageHud.MessageType.TopLeft, "Quick trash kept " + _kept + " gear set item" + (_kept == 1 ? "" : "s") + ".");
                }
                catch (Exception e)
                {
                    GearSetsPlugin.WarnOnce("GearSets: quick-trash message failed", e);
                }
                _kept = -1;
                return;
            }
            if (_stash == null)
                return;
            try
            {
                Player p = Player.m_localPlayer;
                if (p == null)
                    return;
                Inventory inv = p.GetInventory();
                List<ItemDrop.ItemData> all = inv.m_inventory;
                all.AddRange(_stash);
                int kept;
                if (_flaggedInStash >= 0)
                {
                    kept = _flaggedInStash;
                }
                else
                {
                    var trashedNames = new HashSet<string>();
                    foreach (ItemDrop.ItemData it in _before)
                        if (!_stash.Contains(it) && !all.Contains(it))
                            trashedNames.Add(it.m_shared.m_name);
                    kept = 0;
                    foreach (ItemDrop.ItemData it in _stash)
                        if (trashedNames.Contains(it.m_shared.m_name))
                            kept++;
                }
                inv.Changed();
                if (kept > 0)
                    p.Message(MessageHud.MessageType.TopLeft, "Quick trash kept " + kept + " gear set item" + (kept == 1 ? "" : "s") + ".");
            }
            catch (Exception e)
            {
                GearSetsPlugin.WarnOnce("GearSets: restoring items after quick-trash failed", e);
            }
            finally
            {
                _stash = null;
                _before = null;
                _flaggedInStash = -1;
            }
        }

        private static bool ShouldStorePrefix(object __0, ref bool __result)
        {
            try
            {
                if (!PluginConfig.Enabled.Value || !PluginConfig.StoreAllSkipsSetItems.Value || !InSet(__0 as ItemDrop.ItemData))
                    return true;
                __result = false;
                return false;
            }
            catch (Exception e)
            {
                GearSetsPlugin.WarnOnce("GearSets: Store All guard failed", e);
                return true;
            }
        }
    }
}
