using System.Collections.Generic;
using GearSets.Core;
using GearSets.Core.Model;
using UnityEngine;

namespace GearSets.UI
{
    /// <summary>Builds the list and detail into the window body and renders them on each refresh.</summary>
    internal static class WindowContent
    {
        private static SetListView _list;
        private static SetDetailView _detail;
        private static readonly Dictionary<string, SetState> States = new Dictionary<string, SetState>();
        private static readonly Dictionary<string, int> Missing = new Dictionary<string, int>();

        public static string SelectedId { get; private set; }

        public static void EnsureBuilt()
        {
            if (_list != null || GearSetsWindow.Body == null)
                return;
            _list = new SetListView(GearSetsWindow.Body);
            _list.Selected = s => Select(s.Id);
            _detail = new SetDetailView(GearSetsWindow.Body, GearSetsWindow.Template);
            SetDetailView.RenameClicked = SaveDialog.OpenEdit;
            GearSetsWindow.Refreshing += Render;
        }

        public static void Select(string id)
        {
            SelectedId = id;
            GearSetsWindow.Refresh();
        }

        private static void Render()
        {
            Player p = Player.m_localPlayer;
            List<GearSet> sets = SetStore.Book.Sets;
            if (SetStore.Book.Find(SelectedId) == null)
                SelectedId = sets.Count > 0 ? sets[0].Id : null;

            Snapshot snap = p != null ? Snapshotter.Take(p) : null;
            SwapPlan selPlan = null;
            HotbarPlan selHot = null;
            States.Clear();
            Missing.Clear();
            foreach (GearSet s in sets)
            {
                if (snap == null)
                    continue;
                SwapPlan plan = SwapPlanner.Plan(s, snap.Facts);
                HotbarPlan hot = s.HasHotbar ? HotbarPlanner.Plan(s, snap.Facts) : null;
                int missing;
                States[s.Id] = SetStatus.Of(plan, hot, out missing);
                Missing[s.Id] = missing;
                if (s.Id == SelectedId)
                {
                    selPlan = plan;
                    selHot = hot;
                }
            }

            _list.Render(sets, SelectedId, StatusText, StatusColor);
            GearSet sel = SetStore.Book.Find(SelectedId);
            _detail.Render(sel, selPlan ?? new SwapPlan(), selHot, sel != null ? StatusText(sel) : "",
                sel != null ? StatusColor(sel) : UiKit.Muted);
        }

        private static string StatusText(GearSet s)
        {
            SetState st;
            int m;
            return States.TryGetValue(s.Id, out st) ? SetStatus.Label(st, Missing.TryGetValue(s.Id, out m) ? m : 0) : "";
        }

        private static Color StatusColor(GearSet s)
        {
            SetState st;
            if (!States.TryGetValue(s.Id, out st))
                return UiKit.Muted;
            return st == SetState.Equipped ? UiKit.Equipped : st == SetState.Missing ? UiKit.Warn : UiKit.Muted;
        }
    }
}
