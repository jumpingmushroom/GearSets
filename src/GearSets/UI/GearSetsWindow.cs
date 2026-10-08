using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GearSets.UI
{
    /// <summary>
    /// The floating Gear Sets window: closed by default, a child of the inventory root so it hides
    /// with the inventory, dragged by its title bar, position saved in config.
    /// </summary>
    internal static class GearSetsWindow
    {
        public const float Width = 700f;

        private static TextMeshProUGUI _count;
        private static bool _open;
        private static float _nextRefresh;
        private static bool _hooked;

        public static RectTransform Root { get; private set; }
        public static RectTransform TitleBar { get; private set; }
        public static RectTransform Body { get; private set; }
        public static Button Template { get; private set; }

        /// <summary>Raised by Refresh while visible; the list and detail views render on it.</summary>
        public static event Action Refreshing;

        public static bool Visible { get { return Root != null && Root.gameObject.activeSelf; } }

        public static void OnInventoryShow(InventoryGui gui)
        {
            if (Root == null)
                Build(gui);
            Apply();
            if (Visible)
                Refresh();
        }

        public static void Toggle()
        {
            _open = !_open;
            Apply();
            if (Visible)
                Refresh();
        }

        public static void Close()
        {
            _open = false;
            Apply();
        }

        public static void Tick()
        {
            if (Visible && Time.unscaledTime >= _nextRefresh)
                Refresh();
        }

        public static void Refresh()
        {
            _nextRefresh = Time.unscaledTime + 1f;
            if (!Visible)
                return;
            _count.text = Core.SetStore.Book.Sets.Count + " / " + Core.SetStore.Book.MaxSets;
            Action handler = Refreshing;
            if (handler != null)
                handler();
        }

        private static void Apply()
        {
            if (Root == null)
                return;
            bool show = _open && PluginConfig.Enabled.Value;
            Root.gameObject.SetActive(show);
            if (!show)
                return;
            Root.localScale = Vector3.one * PluginConfig.WindowScale.Value;
            LayoutRebuilder.ForceRebuildLayoutImmediate(Root);
            RectTransform canvas = CanvasRect();
            if (canvas == null)
                return;
            // Config X/Y are canvas units from the screen centre, whatever size the inventory root is.
            Vector2 c = canvas.rect.center + new Vector2(PluginConfig.WindowX.Value, PluginConfig.WindowY.Value);
            Root.position = canvas.TransformPoint(new Vector3(c.x, c.y, 0f));
            Clamp(canvas);
        }

        private static RectTransform CanvasRect()
        {
            Canvas canvas = Root.GetComponentInParent<Canvas>();
            return canvas != null ? (RectTransform)canvas.rootCanvas.transform : null;
        }

        /// <summary>Keep the whole window on screen, working in root-canvas space.</summary>
        private static void Clamp(RectTransform canvas)
        {
            var corners = new Vector3[4];
            Root.GetWorldCorners(corners);
            Vector3 min = canvas.InverseTransformPoint(corners[0]);
            Vector3 max = canvas.InverseTransformPoint(corners[2]);
            Rect r = canvas.rect;
            Vector2 shift = Vector2.zero;
            if (min.x < r.xMin) shift.x = r.xMin - min.x;
            else if (max.x > r.xMax) shift.x = r.xMax - max.x;
            if (min.y < r.yMin) shift.y = r.yMin - min.y;
            else if (max.y > r.yMax) shift.y = r.yMax - max.y;
            if (shift != Vector2.zero)
                Root.position += canvas.TransformVector(new Vector3(shift.x, shift.y, 0f));
        }

        private static void SavePosition()
        {
            RectTransform canvas = CanvasRect();
            if (canvas == null)
                return;
            Clamp(canvas);
            Vector2 local = (Vector2)canvas.InverseTransformPoint(Root.position) - canvas.rect.center;
            PluginConfig.WindowX.Value = Mathf.Round(local.x);
            PluginConfig.WindowY.Value = Mathf.Round(local.y);
        }

        private static void Build(InventoryGui gui)
        {
            Transform parent = gui.m_inventoryRoot != null ? gui.m_inventoryRoot : (Transform)gui.m_player;
            Template = gui.m_craftButton;

            Root = UiKit.Rect("GearSetsWindow", parent);
            Root.SetAsLastSibling();
            Root.anchorMin = Root.anchorMax = new Vector2(0.5f, 0.5f);
            Root.pivot = new Vector2(0.5f, 0.5f);
            Root.sizeDelta = new Vector2(Width, 0f);
            UiKit.Background(Root.gameObject, gui);
            UiKit.Column(Root.gameObject, 0f, new RectOffset(0, 0, 0, 0));
            Root.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TitleBar = UiKit.Rect("TitleBar", Root);
            var barBg = TitleBar.gameObject.AddComponent<Image>();
            barBg.sprite = UiKit.White;
            barBg.color = new Color(0f, 0f, 0f, 0.25f);
            UiKit.Row(TitleBar.gameObject, 10f, new RectOffset(14, 8, 6, 6));
            UiKit.Size(TitleBar.gameObject, -1f, 46f);
            var drag = TitleBar.gameObject.AddComponent<DragHandle>();
            drag.Target = Root;
            drag.Dropped = SavePosition;

            TextMeshProUGUI title = UiKit.Text(TitleBar, "Title", 20f, TextAlignmentOptions.Left);
            title.text = "<b>Gear Sets</b>";
            title.color = UiKit.Accent;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            _count = UiKit.Text(TitleBar, "Count", 14f, TextAlignmentOptions.Left);
            _count.color = UiKit.Muted;
            _count.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            Button saveNew = UiKit.Button(TitleBar, "SaveNew", Template, "+ Save current as new set", SaveDialog.OpenNew);
            UiKit.Size(saveNew.gameObject, 210f, 32f);

            Button close = UiKit.Button(TitleBar, "Close", Template, "X", Close);
            UiKit.Size(close.gameObject, 36f, 32f);

            Body = UiKit.Rect("Body", Root);
            UiKit.Row(Body.gameObject, 14f, new RectOffset(14, 14, 14, 14)).childAlignment = TextAnchor.UpperLeft;
            UiKit.Size(Body.gameObject, -1f, 360f);

            var hint = UiKit.Text(Root, "Hint", 12f, TextAlignmentOptions.Left);
            hint.text = "Drag the title bar to move. The position is remembered.";
            hint.color = new Color(UiKit.Muted.r, UiKit.Muted.g, UiKit.Muted.b, 0.7f);
            hint.margin = new Vector4(14f, 0f, 14f, 8f);

            if (!_hooked)
            {
                _hooked = true;
                PluginConfig.Enabled.SettingChanged += (s, e) => Apply();
                PluginConfig.WindowScale.SettingChanged += (s, e) => Apply();
                Core.SetStore.Changed += Refresh;
            }
            Root.gameObject.SetActive(false);
        }
    }
}
