using System;
using System.Collections.Generic;
using GearSets.Core.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GearSets.UI
{
    /// <summary>The left column: one row per set with icon, name and status.</summary>
    internal sealed class SetListView
    {
        private sealed class Row
        {
            public Button Button;
            public Image Bg;
            public Image Icon;
            public TextMeshProUGUI Name;
            public TextMeshProUGUI Status;
        }

        private readonly RectTransform _root;
        private readonly List<Row> _rows = new List<Row>();
        private IList<GearSet> _sets = new List<GearSet>();

        public Action<GearSet> Selected;

        public SetListView(Transform parent)
        {
            _root = UiKit.Rect("SetList", parent);
            UiKit.Column(_root.gameObject, 6f, new RectOffset(0, 0, 0, 0));
            LayoutElement le = _root.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = 170f;
            le.flexibleHeight = 1f;
        }

        public void Render(IList<GearSet> sets, string selectedId, Func<GearSet, string> status, Func<GearSet, Color> statusColor)
        {
            _sets = sets;
            while (_rows.Count < sets.Count)
                _rows.Add(MakeRow(_rows.Count));
            for (int i = 0; i < _rows.Count; i++)
            {
                Row r = _rows[i];
                bool on = i < sets.Count;
                r.Button.gameObject.SetActive(on);
                if (!on)
                    continue;
                GearSet s = sets[i];
                bool sel = s.Id == selectedId;
                r.Bg.color = sel ? UiKit.Selected : UiKit.Tile;
                r.Icon.sprite = Icons.For(s.Icon);
                r.Icon.enabled = r.Icon.sprite != null;
                r.Name.text = sel ? "<b>" + s.Name + "</b>" : s.Name;
                r.Status.text = status(s);
                r.Status.color = statusColor(s);
            }
        }

        private Row MakeRow(int index)
        {
            var r = new Row();
            RectTransform rt = UiKit.Rect("Set" + index, _root);
            r.Bg = rt.gameObject.AddComponent<Image>();
            r.Bg.sprite = UiKit.White;
            r.Button = rt.gameObject.AddComponent<Button>();
            r.Button.targetGraphic = r.Bg;
            r.Button.onClick.AddListener(() =>
            {
                if (index < _sets.Count && Selected != null)
                    Selected(_sets[index]);
            });
            UiKit.Row(rt.gameObject, 8f, new RectOffset(8, 8, 6, 6));
            UiKit.Size(rt.gameObject, -1f, 52f);

            r.Icon = UiKit.Img(rt, "Icon", null, Color.white);
            UiKit.Size(r.Icon.gameObject, 30f, 30f);

            RectTransform col = UiKit.Rect("Text", rt);
            UiKit.Column(col.gameObject, 1f, new RectOffset(0, 0, 0, 0));
            col.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            r.Name = UiKit.Text(col, "Name", 15f, TextAlignmentOptions.Left);
            r.Name.textWrappingMode = TextWrappingModes.NoWrap;
            r.Name.overflowMode = TextOverflowModes.Ellipsis;
            r.Status = UiKit.Text(col, "Status", 12f, TextAlignmentOptions.Left);
            return r;
        }
    }
}
