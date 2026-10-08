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
    /// asks first for set items, quick-trash keeps them, and Store All leaves them in the inventory.
    /// </summary>
    internal static class QuickStackAdapter
    {
        private static MethodInfo _trashItem;
        private static bool _bypass;
        private static List<ItemDrop.ItemData> _stash;
        private static List<ItemDrop.ItemData> _before;

        public static void Init(Harmony harmony)
        {
            if (!PluginConfig.IntegrateQuickStack.Value || !ModIds.Loaded(ModIds.QuickStack))
                return;
            try
            {
                Type trash = AccessTools.TypeByName("QuickStackStore.TrashModule");
                Type store = AccessTools.TypeByName("QuickStackStore.StoreTakeAllModule");
                _trashItem = trash != null ? AccessTools.Method(trash, "TrashItem") : null;
                MethodInfo quickTrash = trash != null ? AccessTools.Method(trash, "QuickTrash") : null;
                MethodInfo shouldStore = store != null ? AccessTools.Method(store, "ShouldStoreItem") : null;
                if (_trashItem == null || quickTrash == null || shouldStore == null)
                    throw new MissingMemberException("Quick Stack API not found (TrashItem/QuickTrash/ShouldStoreItem)");

                harmony.Patch(_trashItem, prefix: new HarmonyMethod(typeof(QuickStackAdapter), nameof(TrashPrefix)));
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

        /// <summary>Hide set items from the player's item list while quick-trash runs; the finalizer puts them back.</summary>
        private static void QuickTrashPrefix()
        {
            _stash = null;
            _before = null;
            try
            {
                Player p = Player.m_localPlayer;
                if (p == null || !PluginConfig.Enabled.Value)
                    return;
                List<ItemDrop.ItemData> all = p.GetInventory().m_inventory;
                List<ItemDrop.ItemData> stash = all.FindAll(InSet);
                if (stash.Count == 0)
                    return;
                _before = new List<ItemDrop.ItemData>(all);
                _stash = stash;
                foreach (ItemDrop.ItemData it in stash)
                    all.Remove(it);
            }
            catch (Exception e)
            {
                _stash = null;
                GearSetsPlugin.WarnOnce("GearSets: quick-trash guard failed", e);
            }
        }

        private static void QuickTrashFinalizer()
        {
            if (_stash == null)
                return;
            try
            {
                Player p = Player.m_localPlayer;
                if (p == null)
                    return;
                Inventory inv = p.GetInventory();
                List<ItemDrop.ItemData> all = inv.m_inventory;
                var trashedNames = new HashSet<string>();
                foreach (ItemDrop.ItemData it in _before)
                    if (!_stash.Contains(it) && !all.Contains(it))
                        trashedNames.Add(it.m_shared.m_name);
                all.AddRange(_stash);
                int kept = 0;
                foreach (ItemDrop.ItemData it in _stash)
                    if (trashedNames.Contains(it.m_shared.m_name))
                        kept++;
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
