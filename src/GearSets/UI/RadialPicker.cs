using System;
using System.Collections.Generic;
using GearSets.Core;
using GearSets.Core.Model;
using GearSets.Integrations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GearSets.UI
{
    /// <summary>
    /// Hold the radial key in the world: sets on a ring, point with the mouse, release to equip.
    /// The cursor stays locked; direction comes from accumulated mouse movement, while camera look
    /// and attacks are frozen (PlayerPatches). Release near the centre or right-click to cancel.
    /// </summary>
    internal static class RadialPicker
    {
        private const float Radius = 210f;
        private const float DeadZone = 40f;

        private sealed class Node
        {
            public RectTransform Rt;
            public Image Bg;
            public Image Icon;
            public TextMeshProUGUI Name;
        }

        private static RectTransform _root;
        private static RectTransform _ring;
        private static TextMeshProUGUI _title;
        private static TextMeshProUGUI _status;
        private static TextMeshProUGUI _detail;
        private static TextMeshProUGUI _hint;
        private static readonly List<Node> Nodes = new List<Node>();
        private static readonly List<GearSet> Sets = new List<GearSet>();
        private static readonly List<string> States = new List<string>();
        private static readonly List<string> Details = new List<string>();
        private static readonly List<Color> Colors = new List<Color>();
        private static Vector2 _aim;
        private static int _hover = -1;

        public static bool IsOpen { get; private set; }

        public static void Tick()
        {
            if (!IsOpen)
            {
                if (PluginConfig.RadialEnabled.Value && Keys.Down(PluginConfig.RadialKey.Value) && CanOpen())
                    Open();
                return;
            }
            Player p = Player.m_localPlayer;
            if (p == null || p.IsDead() || InventoryGui.IsVisible() || Menu.IsVisible() || ZInput.GetButtonDown("Block"))
            {
                Close();
                return;
            }
            _aim += ZInput.GetMouseDelta();
            if (_aim.magnitude > Radius)
                _aim = _aim.normalized * Radius;
            int hover = _aim.magnitude < DeadZone ? -1 : Sector(_aim, Sets.Count);
            if (hover != _hover)
            {
                _hover = hover;
                Render();
            }
            if (!Keys.Held(PluginConfig.RadialKey.Value))
            {
                GearSet pick = _hover >= 0 ? Sets[_hover] : null;
                Close();
                if (pick != null)
                    SwapExecutor.Equip(pick);
            }
        }

        private static bool CanOpen()
        {
            Player p = Player.m_localPlayer;
            if (p == null || p.IsDead() || !p.TakeInput() || Hud.instance == null || Hud.InRadial() || ConfigManagerAdapter.WindowOpen)
                return false;
            if (SetStore.Book.Sets.Count == 0)
            {
                p.Message(MessageHud.MessageType.TopLeft, "No gear sets yet. Save one from the Gear Sets tab in the inventory.");
                return false;
            }
            return true;
        }

        /// <summary>Node i sits at angle i·360/n, clockwise from straight up.</summary>
        private static int Sector(Vector2 aim, int n)
        {
            float deg = Mathf.Atan2(aim.x, aim.y) * Mathf.Rad2Deg;
            if (deg < 0f)
                deg += 360f;
            float step = 360f / n;
            return Mathf.RoundToInt(deg / step) % n;
        }

        private static void Open()
        {
            Build();
            Sets.Clear();
            Sets.AddRange(SetStore.Book.Sets);
            Statuses();
            float size = Sets.Count > 8 ? 88f : 112f;
            while (Nodes.Count < Sets.Count)
                Nodes.Add(MakeNode(Nodes.Count));
            for (int i = 0; i < Nodes.Count; i++)
            {
                Node node = Nodes[i];
                bool on = i < Sets.Count;
                node.Rt.gameObject.SetActive(on);
                if (!on)
                    continue;
                float a = i * 2f * Mathf.PI / Sets.Count;
                node.Rt.sizeDelta = new Vector2(size, size);
                node.Rt.anchoredPosition = new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * Radius;
                node.Icon.sprite = Icons.For(Sets[i].Icon);
                node.Icon.enabled = node.Icon.sprite != null;
                node.Name.text = Sets[i].Name;
            }
            _hint.text = "Hold <color=#f2a64a>" + PluginConfig.RadialKey.Value + "</color> · point at a set · release to equip · right-click to cancel";
            _aim = Vector2.zero;
            _hover = -1;
            IsOpen = true;
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            Render();
        }

        /// <summary>Safe to call any time: also from Runtime when a tick throws or the mod is disabled.</summary>
        internal static void Close()
        {
            IsOpen = false;
            _hover = -1;
            try
            {
                if (_root != null)
                    _root.gameObject.SetActive(false);
            }
            catch (Exception e)
            {
                GearSetsPlugin.WarnOnce("GearSets: closing the radial failed", e);
            }
        }

        private static void Statuses()
        {
            States.Clear();
            Details.Clear();
            Colors.Clear();
            Player p = Player.m_localPlayer;
            Snapshot snap = Snapshotter.Take(p);
            foreach (GearSet s in Sets)
            {
                SwapPlan plan = SwapPlanner.Plan(s, snap.Facts);
                HotbarPlan hot = s.HasHotbar ? HotbarPlanner.Plan(s, snap.Facts) : null;
                int missing;
                SetState st = SetStatus.Of(plan, hot, out missing);
                States.Add(st == SetState.Equipped ? "Already wearing" : SetStatus.Label(st, missing));
                Colors.Add(st == SetState.Equipped ? UiKit.Equipped : st == SetState.Missing ? UiKit.Warn : UiKit.Muted);
                var names = new List<string>();
                foreach (ReportLine l in plan.Lines)
                    if (l.Outcome == Outcome.Missing)
                        names.Add(Loc.T(l.Wanted.Name));
                int changes = plan.Steps.Count + (hot != null ? hot.Moves.Count : 0);
                Details.Add(names.Count > 0 ? "Missing: " + ReportText.JoinAnd(names)
                    : changes == 0 ? "Nothing to change." : changes == 1 ? "1 change." : changes + " changes.");
            }
        }

        private static void Render()
        {
            for (int i = 0; i < Sets.Count; i++)
            {
                bool on = i == _hover;
                Nodes[i].Bg.color = on ? new Color(0.29f, 0.20f, 0.13f, 0.97f) : new Color(0.11f, 0.086f, 0.067f, 0.92f);
                Nodes[i].Rt.localScale = Vector3.one * (on ? 1.12f : 1f);
            }
            if (_hover < 0)
            {
                _title.text = "<b>Gear Sets</b>";
                _status.text = "Point at a set";
                _status.color = UiKit.Muted;
                _detail.text = "Release to equip · release here or right-click to cancel";
                return;
            }
            _title.text = "<b>" + Sets[_hover].Name + "</b>";
            _status.text = States[_hover];
            _status.color = Colors[_hover];
            _detail.text = Details[_hover];
        }

        private static void Build()
        {
            if (_root != null)
                return;
            Nodes.Clear(); // a rebuild after a logout: the old nodes went with the old Hud
            Transform parent = Hud.instance.m_rootObject.transform;
            _root = UiKit.Rect("GearSetsRadial", parent);
            UiKit.Stretch(_root);
            Image dim = _root.gameObject.AddComponent<Image>();
            dim.sprite = UiKit.White;
            dim.color = new Color(0.03f, 0.04f, 0.03f, 0.45f);
            dim.raycastTarget = false;

            _ring = UiKit.Rect("Ring", _root);
            _ring.anchorMin = _ring.anchorMax = new Vector2(0.5f, 0.5f);
            _ring.sizeDelta = Vector2.zero;

            Image centre = UiKit.Img(_ring, "Centre", UiKit.Circle, new Color(0.11f, 0.086f, 0.067f, 0.94f));
            ((RectTransform)centre.transform).sizeDelta = new Vector2(230f, 230f);
            RectTransform text = UiKit.Rect("Text", _ring);
            text.sizeDelta = new Vector2(190f, 190f);
            UiKit.Column(text.gameObject, 4f, new RectOffset(0, 0, 0, 0)).childAlignment = TextAnchor.MiddleCenter;
            _title = UiKit.Text(text, "Title", 24f, TextAlignmentOptions.Center);
            _title.color = UiKit.Accent;
            _status = UiKit.Text(text, "Status", 15f, TextAlignmentOptions.Center);
            _detail = UiKit.Text(text, "Detail", 12f, TextAlignmentOptions.Center);
            _detail.color = UiKit.Muted;

            _hint = UiKit.Text(_root, "Hint", 14f, TextAlignmentOptions.Center);
            var hr = (RectTransform)_hint.transform;
            hr.anchorMin = new Vector2(0f, 0f);
            hr.anchorMax = new Vector2(1f, 0f);
            hr.pivot = new Vector2(0.5f, 0f);
            hr.anchoredPosition = new Vector2(0f, 60f);
            hr.sizeDelta = new Vector2(0f, 24f);

            _root.gameObject.SetActive(false);
        }

        private static Node MakeNode(int index)
        {
            var n = new Node();
            n.Bg = UiKit.Img(_ring, "Node" + index, UiKit.Circle, Color.white);
            n.Rt = (RectTransform)n.Bg.transform;
            UiKit.Column(n.Rt.gameObject, 2f, new RectOffset(6, 6, 14, 10)).childAlignment = TextAnchor.MiddleCenter;
            n.Icon = UiKit.Img(n.Rt, "Icon", null, Color.white);
            UiKit.Size(n.Icon.gameObject, 40f, 40f);
            n.Name = UiKit.Text(n.Rt, "Name", 13f, TextAlignmentOptions.Center);
            n.Name.textWrappingMode = TextWrappingModes.NoWrap;
            n.Name.overflowMode = TextOverflowModes.Ellipsis;
            return n;
        }
    }
}
