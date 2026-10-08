using System.Collections.Generic;
using GearSets.Core.Model;
using UnityEngine;

namespace GearSets.Core
{
    /// <summary>
    /// Runs a swap: plan → steps through the vanilla equip queue (or instantly) → wait for the queue
    /// → one frame for ExtraSlots to re-seat gear → verify (one hand correction if another mod such
    /// as AutoShield changed the hands) → hotbar moves → message.
    /// </summary>
    internal static class SwapExecutor
    {
        private const float Timeout = 30f;

        private static readonly List<ItemDrop.ItemData> Watch = new List<ItemDrop.ItemData>();
        private static GearSet _set;
        private static SwapPlan _plan;
        private static float _deadline;
        private static bool _corrected;
        private static int _phase; // 0 idle, 1 waiting for the queue, 2 finish next frame

        public static bool Busy { get { return _phase != 0; } }

        public static void Equip(GearSet set)
        {
            Player p = Player.m_localPlayer;
            if (p == null || set == null)
                return;
            string refusal = SwapGate.Refusal(p);
            if (refusal != null)
            {
                p.Message(MessageHud.MessageType.Center, refusal);
                return;
            }
            _set = set;
            _corrected = false;
            Snapshot snap = Snapshotter.Take(p);
            _plan = SwapPlanner.Plan(set, snap.Facts);
            Log("plan for " + set.Name, _plan.Steps);
            Run(p, snap, _plan.Steps);
            WatchQueued(p, snap, _plan);
            if (Watch.Count > 0)
                p.Message(MessageHud.MessageType.TopLeft, "Equipping " + set.Name + "…");
            _phase = 1;
            _deadline = Time.time + Timeout;
        }

        public static void Tick()
        {
            if (_phase == 0)
                return;
            Player p = Player.m_localPlayer;
            if (p == null || p.IsDead())
            {
                Reset();
                return;
            }
            if (_phase == 1)
            {
                if (Time.time < _deadline)
                    foreach (ItemDrop.ItemData it in Watch)
                        if (p.IsEquipActionQueued(it))
                            return;
                _phase = 2;
                return;
            }
            Finish(p);
        }

        private static void Finish(Player p)
        {
            Snapshot now = Snapshotter.Take(p);
            SwapPlan remaining = SwapPlanner.Plan(_set, now.Facts);
            if (!_corrected)
            {
                List<Step> handFix = HandSteps(remaining);
                if (handFix.Count > 0)
                {
                    _corrected = true;
                    Log("hand correction", handFix);
                    Run(p, now, handFix);
                    _phase = 1;
                    _deadline = Time.time + Timeout;
                    return;
                }
            }
            HotbarPlan hotbar = null;
            if (_set.HasHotbar)
            {
                hotbar = HotbarPlanner.Plan(_set, now.Facts);
                Apply(p, now, hotbar);
            }
            p.Message(MessageHud.MessageType.TopLeft, ReportText.Compose(_set.Name, _plan, remaining, hotbar, Loc.T));
            Reset();
        }

        private static void Run(Player p, Snapshot s, List<Step> steps)
        {
            Watch.Clear();
            bool instant = PluginConfig.InstantSwap.Value;
            foreach (Step step in steps)
            {
                ItemDrop.ItemData it = s.Of(step.Item);
                bool queue = !instant && it.m_shared.m_equipDuration > 0f;
                switch (step.Kind)
                {
                    case StepKind.CancelQueued:
                        p.RemoveEquipAction(it);
                        break;
                    case StepKind.Unequip:
                        if (queue)
                        {
                            p.QueueUnequipAction(it);
                            Watch.Add(it);
                        }
                        else
                            p.UnequipItem(it, true);
                        break;
                    case StepKind.Equip:
                        if (queue)
                        {
                            p.QueueEquipAction(it);
                            Watch.Add(it);
                        }
                        else
                            p.EquipItem(it, true);
                        break;
                }
            }
        }

        /// <summary>
        /// Items of this plan already in the vanilla queue (from an earlier press) produce no step,
        /// but the swap isn't done until they are: wait for them too.
        /// </summary>
        private static void WatchQueued(Player p, Snapshot s, SwapPlan plan)
        {
            var used = new HashSet<ItemDrop.ItemData>();
            foreach (Step step in plan.Steps)
                used.Add(s.Of(step.Item));
            foreach (ReportLine l in plan.Lines)
                if (l.Used != null)
                    used.Add(s.Of(l.Used));
            foreach (Player.MinorActionData a in p.m_actionQueue)
            {
                if (a.m_item == null || !used.Contains(a.m_item) || Watch.Contains(a.m_item))
                    continue;
                if (a.m_type == Player.MinorActionData.ActionType.Equip || a.m_type == Player.MinorActionData.ActionType.Unequip)
                    Watch.Add(a.m_item);
            }
        }

        private static List<Step> HandSteps(SwapPlan plan)
        {
            var list = new List<Step>();
            foreach (Step s in plan.Steps)
            {
                FitSlot f = s.Item.Fits;
                if (f == FitSlot.LeftHand || f == FitSlot.RightHand || f == FitSlot.Torch || Slots.IsHand(s.Item.Worn))
                    list.Add(s);
            }
            return list;
        }

        private static void Apply(Player p, Snapshot s, HotbarPlan plan)
        {
            if (plan.Moves.Count == 0)
                return;
            foreach (Move m in plan.Moves)
                s.Of(m.Item).m_gridPos = new Vector2i(m.X, m.Y);
            p.GetInventory().Changed();
        }

        internal static void Reset()
        {
            _phase = 0;
            _set = null;
            _plan = null;
            Watch.Clear();
        }

        private static void Log(string what, List<Step> steps)
        {
            if (!PluginConfig.Verbose.Value)
                return;
            var parts = new List<string>();
            foreach (Step s in steps)
                parts.Add(s.Kind + " " + s.Item.Prefab);
            GearSetsPlugin.Log.LogInfo("GearSets: " + what + ": " + (parts.Count == 0 ? "nothing" : string.Join(", ", parts.ToArray())));
        }
    }
}
