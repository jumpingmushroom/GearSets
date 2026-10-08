using System;
using System.Collections.Generic;
using GearSets.Core;
using GearSets.Core.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GearSets.UI
{
    /// <summary>
    /// The right column: the selected set's 9 slot tiles and 8 hotbar tiles (click = ignore/include),
    /// and Equip / Update from current / Rename / Delete.
    /// </summary>
    internal sealed class SetDetailView
    {
        private enum TileKind { Slot, Utility }

        private sealed class Tile
        {
            public Button Button;
            public Image Bg;
            public Image Icon;
            public TextMeshProUGUI Label;
            public TextMeshProUGUI Name;
            public TextMeshProUGUI Note;
        }

        private static readonly SlotKind[] Order =
        {
            SlotKind.Helmet, SlotKind.Chest, SlotKind.Legs, SlotKind.Shoulder, SlotKind.Trinket,
            SlotKind.LeftHand, SlotKind.RightHand, SlotKind.Ammo
        };

        /// <summary>Set by the save dialog (Task 13).</summary>
        public static Action<GearSet> RenameClicked;

        private readonly RectTransform _root;
        private readonly RectTransform _content;
        private readonly TextMeshProUGUI _empty;
        private readonly Image _icon;
        private readonly TextMeshProUGUI _name;
        private readonly TextMeshProUGUI _status;
        private readonly Tile[] _slotTiles = new Tile[9];
        private readonly Tile[] _hotbarTiles = new Tile[GearSet.HotbarSize];
        private GearSet _set;

        public Action Changed;

        public SetDetailView(Transform parent, Button template)
        {
            _root = UiKit.Rect("Detail", parent);
            var bg = _root.gameObject.AddComponent<Image>();
            bg.sprite = UiKit.White;
            bg.color = new Color(0f, 0f, 0f, 0.18f);
            UiKit.Column(_root.gameObject, 0f, new RectOffset(12, 12, 12, 12));
            LayoutElement le = _root.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.flexibleHeight = 1f;

            _empty = UiKit.Text(_root, "Empty", 15f, TextAlignmentOptions.TopLeft);
            _empty.text = "No gear sets yet.\n\nPut on the gear you want, then press <b>+ Save current as new set</b>.";
            _empty.color = UiKit.Muted;

            _content = UiKit.Rect("Content", _root);
            UiKit.Column(_content.gameObject, 10f, new RectOffset(0, 0, 0, 0));

            RectTransform head = UiKit.Rect("Head", _content);
            UiKit.Row(head.gameObject, 10f, new RectOffset(0, 0, 0, 0));
            _icon = UiKit.Img(head, "Icon", null, Color.white);
            UiKit.Size(_icon.gameObject, 30f, 30f);
            _name = UiKit.Text(head, "Name", 20f, TextAlignmentOptions.Left);
            _name.textWrappingMode = TextWrappingModes.NoWrap;
            _name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            _status = UiKit.Text(head, "Status", 13f, TextAlignmentOptions.Right);

            Caption(_content, "EQUIPMENT");
            RectTransform grid = UiKit.Rect("Slots", _content);
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(148f, 74f);
            g.spacing = new Vector2(6f, 6f);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 3;
            for (int i = 0; i < 9; i++)
            {
                int index = i;
                _slotTiles[i] = MakeTile(grid, "Slot" + i, () => ToggleSlot(index), true);
            }

            Caption(_content, "HOTBAR LAYOUT  <size=11><color=#9c8e72>click a slot to ignore / include it</color></size>");
            RectTransform hot = UiKit.Rect("Hotbar", _content);
            var hg = hot.gameObject.AddComponent<GridLayoutGroup>();
            hg.cellSize = new Vector2(50f, 46f);
            hg.spacing = new Vector2(5f, 5f);
            hg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            hg.constraintCount = GearSet.HotbarSize;
            for (int i = 0; i < GearSet.HotbarSize; i++)
            {
                int index = i;
                _hotbarTiles[i] = MakeTile(hot, "Hot" + i, () => ToggleHotbar(index), false);
            }

            RectTransform actions = UiKit.Rect("Actions", _content);
            UiKit.Row(actions.gameObject, 8f, new RectOffset(0, 0, 4, 0));
            Button equip = UiKit.Button(actions, "Equip", template, "<b>Equip</b>", () => { if (_set != null) SwapExecutor.Equip(_set); });
            UiKit.Size(equip.gameObject, 80f, 36f);
            Button update = UiKit.Button(actions, "Update", template, "Update from current", () =>
            {
                if (_set == null)
                    return;
                SetActions.Update(_set);
                Player p = Player.m_localPlayer;
                if (p != null)
                    p.Message(MessageHud.MessageType.TopLeft, _set.Name + " updated from what you are wearing.");
            });
            UiKit.Size(update.gameObject, 150f, 36f);
            Button rename = UiKit.Button(actions, "Rename", template, "Rename / icon", () =>
            {
                if (_set != null && RenameClicked != null)
                    RenameClicked(_set);
            });
            UiKit.Size(rename.gameObject, 110f, 36f);
            RectTransform spacer = UiKit.Rect("Spacer", actions);
            spacer.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            Button delete = UiKit.Button(actions, "Delete", template, "<color=#f0a08a>Delete</color>", () =>
            {
                GearSet s = _set;
                if (s == null)
                    return;
                Confirm.Ask("Delete " + s.Name + "?", "This removes the set. Your items are not affected.", () => SetActions.Delete(s));
            });
            UiKit.Size(delete.gameObject, 80f, 36f);
        }

        public void Render(GearSet set, SwapPlan plan, HotbarPlan hotbar, string status, Color statusColor)
        {
            _set = set;
            _empty.gameObject.SetActive(set == null);
            _content.gameObject.SetActive(set != null);
            if (set == null)
                return;
            _icon.sprite = Icons.For(set.Icon);
            _icon.enabled = _icon.sprite != null;
            _name.text = "<b>" + set.Name + "</b>";
            _status.text = status;
            _status.color = statusColor;

            var lines = new Dictionary<string, ReportLine>();
            foreach (ReportLine l in plan.Lines)
                if (!lines.ContainsKey(l.Slot) || l.Outcome == Outcome.Missing || l.Outcome == Outcome.Substitute)
                    lines[l.Slot] = l;
            if (hotbar != null)
                foreach (ReportLine l in hotbar.Lines)
                    lines[l.Slot] = l;

            // tiles 0-3 helmet/chest/legs/cape, 4 utility, 5 trinket, 6 left, 7 right, 8 ammo
            SlotKind[] tileSlots = { SlotKind.Helmet, SlotKind.Chest, SlotKind.Legs, SlotKind.Shoulder };
            for (int i = 0; i < 4; i++)
                FillSlot(_slotTiles[i], tileSlots[i], set.Slots[tileSlots[i]], lines);
            FillUtility(_slotTiles[4], set.Utilities, lines);
            FillSlot(_slotTiles[5], SlotKind.Trinket, set.Slots[SlotKind.Trinket], lines);
            FillSlot(_slotTiles[6], SlotKind.LeftHand, set.Slots[SlotKind.LeftHand], lines);
            FillSlot(_slotTiles[7], SlotKind.RightHand, set.Slots[SlotKind.RightHand], lines);
            FillSlot(_slotTiles[8], SlotKind.Ammo, set.Slots[SlotKind.Ammo], lines);

            for (int i = 0; i < GearSet.HotbarSize; i++)
            {
                SlotEntry e = set.Hotbar[i];
                Tile t = _hotbarTiles[i];
                ReportLine line;
                bool missing = lines.TryGetValue("Hotbar " + (i + 1), out line) && line.Outcome == Outcome.Missing;
                t.Label.text = (i + 1).ToString();
                t.Name.text = "";
                t.Note.text = "";
                SetIcon(t, e.Item, e.Mode == EntryMode.Item ? (missing ? 0.45f : 1f) : 0.3f);
                t.Bg.color = e.Mode == EntryMode.Item ? UiKit.Tile : UiKit.TileOff;
                SetOutline(t, missing);
            }
        }

        private static void FillSlot(Tile t, SlotKind k, SlotEntry e, Dictionary<string, ReportLine> lines)
        {
            t.Label.text = Slots.Label(k).ToUpperInvariant();
            ReportLine line;
            lines.TryGetValue(Slots.Label(k), out line);
            Fill(t, e.Mode, e.Item, e.Item != null ? Loc.T(e.Item.Name) : "", line);
        }

        private static void FillUtility(Tile t, UtilityEntry u, Dictionary<string, ReportLine> lines)
        {
            t.Label.text = "UTILITY";
            var names = new List<string>();
            foreach (ItemRef r in u.Items)
                names.Add(Loc.T(r.Name));
            ReportLine line;
            lines.TryGetValue("Utility", out line);
            Fill(t, u.Mode, u.Items.Count > 0 ? u.Items[0] : null, string.Join(" + ", names.ToArray()), line);
        }

        private static void Fill(Tile t, EntryMode mode, ItemRef item, string name, ReportLine line)
        {
            bool missing = false;
            t.Note.text = "";
            if (mode == EntryMode.Ignore)
            {
                t.Name.text = "<i>Ignored</i>";
                t.Name.color = UiKit.Muted;
                SetIcon(t, item, 0.3f);
                t.Bg.color = UiKit.TileOff;
            }
            else if (mode == EntryMode.Empty)
            {
                t.Name.text = "Leave empty";
                t.Name.color = UiKit.Muted;
                SetIcon(t, null, 1f);
                t.Bg.color = UiKit.Tile;
            }
            else
            {
                t.Name.text = name;
                t.Name.color = UiKit.TextColor;
                t.Bg.color = UiKit.Tile;
                if (line != null && line.Outcome == Outcome.Missing)
                {
                    missing = true;
                    t.Note.text = "Missing";
                }
                else if (line != null && line.Outcome == Outcome.Substitute && line.Used != null)
                {
                    missing = true;
                    t.Note.text = "Missing – will use " + Loc.T(line.Used.Name);
                }
                SetIcon(t, item, missing ? 0.45f : 1f);
            }
            SetOutline(t, missing);
        }

        private static void SetIcon(Tile t, ItemRef item, float alpha)
        {
            t.Icon.sprite = Icons.For(item);
            t.Icon.enabled = t.Icon.sprite != null;
            t.Icon.color = new Color(1f, 1f, 1f, alpha);
        }

        private static void SetOutline(Tile t, bool on)
        {
            Outline o = t.Bg.GetComponent<Outline>();
            if (o == null)
            {
                o = t.Bg.gameObject.AddComponent<Outline>();
                o.effectDistance = new Vector2(1.5f, -1.5f);
            }
            o.effectColor = UiKit.Warn;
            o.enabled = on;
        }

        private void ToggleSlot(int tile)
        {
            if (_set == null)
                return;
            if (tile == 4)
                _set.Utilities = _set.Utilities.Toggle();
            else
            {
                SlotKind k = tile < 4 ? Order[tile] : tile == 5 ? SlotKind.Trinket : tile == 6 ? SlotKind.LeftHand
                    : tile == 7 ? SlotKind.RightHand : SlotKind.Ammo;
                _set.Slots[k] = _set.Slots[k].Toggle();
            }
            SetStore.Save();
        }

        private void ToggleHotbar(int i)
        {
            if (_set == null)
                return;
            _set.Hotbar[i] = _set.Hotbar[i].Toggle();
            SetStore.Save();
        }

        private static void Caption(Transform parent, string text)
        {
            TextMeshProUGUI t = UiKit.Text(parent, "Caption", 12f, TextAlignmentOptions.Left);
            t.text = text;
            t.color = UiKit.Muted;
            t.characterSpacing = 4f;
        }

        private static Tile MakeTile(Transform parent, string name, UnityEngine.Events.UnityAction onClick, bool big)
        {
            var t = new Tile();
            RectTransform rt = UiKit.Rect(name, parent);
            t.Bg = rt.gameObject.AddComponent<Image>();
            t.Bg.sprite = UiKit.White;
            t.Bg.color = UiKit.Tile;
            t.Button = rt.gameObject.AddComponent<Button>();
            t.Button.targetGraphic = t.Bg;
            t.Button.onClick.AddListener(onClick);

            t.Label = UiKit.Text(rt, "Label", big ? 10f : 10f, TextAlignmentOptions.TopLeft);
            var lr = (RectTransform)t.Label.transform;
            lr.anchorMin = new Vector2(0f, 1f);
            lr.anchorMax = new Vector2(1f, 1f);
            lr.pivot = new Vector2(0f, 1f);
            lr.anchoredPosition = new Vector2(6f, -3f);
            lr.sizeDelta = new Vector2(-8f, 14f);
            t.Label.color = UiKit.Muted;

            t.Icon = UiKit.Img(rt, "Icon", null, Color.white);
            var ir = (RectTransform)t.Icon.transform;
            float s = big ? 28f : 26f;
            ir.sizeDelta = new Vector2(s, s);
            if (big)
            {
                ir.anchorMin = ir.anchorMax = new Vector2(0f, 0.5f);
                ir.anchoredPosition = new Vector2(22f, -4f);
            }
            else
            {
                ir.anchorMin = ir.anchorMax = new Vector2(0.5f, 0.5f);
                ir.anchoredPosition = new Vector2(0f, -3f);
            }

            t.Name = UiKit.Text(rt, "Name", 12f, TextAlignmentOptions.Left);
            var nr = (RectTransform)t.Name.transform;
            nr.anchorMin = new Vector2(0f, 0f);
            nr.anchorMax = new Vector2(1f, 1f);
            nr.offsetMin = new Vector2(big ? 40f : 2f, 14f);
            nr.offsetMax = new Vector2(-4f, -16f);
            t.Name.gameObject.SetActive(big);

            t.Note = UiKit.Text(rt, "Note", 10f, TextAlignmentOptions.BottomLeft);
            var no = (RectTransform)t.Note.transform;
            no.anchorMin = new Vector2(0f, 0f);
            no.anchorMax = new Vector2(1f, 0f);
            no.pivot = new Vector2(0f, 0f);
            no.anchoredPosition = new Vector2(6f, 3f);
            no.sizeDelta = new Vector2(-8f, 14f);
            t.Note.color = UiKit.Warn;
            t.Note.gameObject.SetActive(big);
            return t;
        }
    }
}
