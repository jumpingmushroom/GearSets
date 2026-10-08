using System.Collections.Generic;
using GearSets.Core;
using GearSets.Core.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GearSets.UI
{
    /// <summary>
    /// Overlay over the top of the window, sized to its content. New: name, icon (from the worn items), which slots to include,
    /// and which hotbar positions to remember. Edit: name and icon only.
    /// </summary>
    internal static class SaveDialog
    {
        private static readonly SlotKind[] TileSlots =
        {
            SlotKind.Helmet, SlotKind.Chest, SlotKind.Legs, SlotKind.Shoulder, SlotKind.Trinket,
            SlotKind.LeftHand, SlotKind.RightHand, SlotKind.Ammo
        };

        private static RectTransform _root;
        private static TextMeshProUGUI _title;
        private static Button _nameButton;
        private static TextMeshProUGUI _error;
        private static RectTransform _iconRow;
        private static readonly List<Button> IconButtons = new List<Button>();
        private static RectTransform _captureSection;
        private static readonly List<Button> SlotToggles = new List<Button>();
        private static Button _hotbarToggle;
        private static readonly Button[] HotToggles = new Button[GearSet.HotbarSize];
        private static TextMeshProUGUI _summary;

        private static GearSet _editing;
        private static string _name;
        private static string _icon;
        private static List<string> _iconChoices = new List<string>();
        private static CaptureMask _mask;
        private static bool _rememberHotbar;
        private static Snapshot _snap;

        public static bool IsOpen { get { return _root != null && _root.gameObject.activeSelf; } }

        public static void OpenNew()
        {
            Player p = Player.m_localPlayer;
            if (p == null)
                return;
            if (SetStore.Book.IsFull)
            {
                p.Message(MessageHud.MessageType.Center, "You already have " + SetStore.Book.MaxSets + " gear sets.");
                return;
            }
            Build();
            _editing = null;
            _snap = Snapshotter.Take(p);
            _mask = CaptureMask.All();
            _rememberHotbar = true;
            for (int i = 0; i < GearSet.HotbarSize; i++)
                _mask.Hotbar[i] = _snap.Facts.At(i, 0) != null;
            _name = FreeName();
            _iconChoices = WornPrefabs(_snap.Facts);
            _icon = _iconChoices.Count > 0 ? _iconChoices[0] : "";
            Show("Save current gear as a set", true);
        }

        public static void OpenEdit(GearSet set)
        {
            Build();
            _editing = set;
            _name = set.Name;
            _icon = set.Icon;
            _iconChoices = SetActions.IconChoices(set);
            Show("Rename / icon", false);
        }

        private static void Show(string title, bool capture)
        {
            _title.text = "<b>" + title + "</b>";
            _error.text = "";
            _captureSection.gameObject.SetActive(capture);
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            Render();
        }

        private static void Close()
        {
            if (_root != null)
                _root.gameObject.SetActive(false);
            _snap = null;
        }

        private static void Save()
        {
            if (_editing != null)
            {
                string err = SetActions.Rename(_editing, _name);
                if (err != null)
                {
                    _error.text = err;
                    return;
                }
                SetActions.SetIcon(_editing, _icon);
                Close();
                return;
            }
            if (!_rememberHotbar)
                for (int i = 0; i < GearSet.HotbarSize; i++)
                    _mask.Hotbar[i] = false;
            string error;
            GearSet s = SetActions.SaveNew(_name, _icon, _mask, out error);
            if (s == null)
            {
                _error.text = error;
                return;
            }
            Close();
            WindowContent.Select(s.Id);
        }

        private static void Render()
        {
            UiKit.SetLabel(_nameButton, _name, UiKit.TextColor);

            while (IconButtons.Count < Mathf.Min(_iconChoices.Count, 9))
            {
                int index = IconButtons.Count;
                Button b = UiKit.Button(_iconRow, "Icon" + index, GearSetsWindow.Template, "", () =>
                {
                    if (index < _iconChoices.Count)
                        _icon = _iconChoices[index];
                    Render();
                });
                UiKit.Size(b.gameObject, 44f, 44f);
                Image icon = UiKit.Img(b.transform, "Sprite", null, Color.white);
                ((RectTransform)icon.transform).sizeDelta = new Vector2(30f, 30f);
                IconButtons.Add(b);
            }
            for (int i = 0; i < IconButtons.Count; i++)
            {
                bool on = i < _iconChoices.Count;
                IconButtons[i].gameObject.SetActive(on);
                if (!on)
                    continue;
                Image sprite = IconButtons[i].transform.Find("Sprite").GetComponent<Image>();
                sprite.sprite = Icons.For(_iconChoices[i]);
                IconButtons[i].GetComponent<Image>().color = _iconChoices[i] == _icon ? UiKit.Accent : Color.white;
            }

            if (_editing != null || _snap == null)
                return;

            for (int i = 0; i < SlotToggles.Count; i++)
            {
                bool include;
                string label, item;
                if (i == TileSlots.Length)
                {
                    include = _mask.Utilities;
                    label = "Utility";
                    var names = new List<string>();
                    foreach (ItemFacts f in _snap.Facts.WornUtilities())
                        names.Add(Loc.T(f.Name));
                    item = names.Count > 0 ? string.Join(" + ", names.ToArray()) : "Leave empty";
                }
                else
                {
                    SlotKind k = TileSlots[i];
                    include = _mask.Slots.Contains(k);
                    label = Slots.Label(k);
                    ItemFacts worn = _snap.Facts.WornIn(Slots.Worn(k));
                    item = worn != null ? Loc.T(worn.Name) : "Leave empty";
                }
                UiKit.SetLabel(SlotToggles[i], (include ? "<color=#7ee0c3>[x]</color> " : "[ ] ") + "<size=10>" + label.ToUpperInvariant() +
                    "</size>\n" + (include ? item : "<i>Ignored</i>"), include ? UiKit.TextColor : UiKit.Muted);
            }

            UiKit.SetLabel(_hotbarToggle, (_rememberHotbar ? "<color=#7ee0c3>[x]</color>" : "[ ]") + " Remember hotbar layout", UiKit.TextColor);
            int hotCount = 0;
            for (int i = 0; i < GearSet.HotbarSize; i++)
            {
                HotToggles[i].gameObject.SetActive(_rememberHotbar);
                ItemFacts f = _snap.Facts.At(i, 0);
                Image sprite = HotToggles[i].transform.Find("Sprite").GetComponent<Image>();
                sprite.sprite = f != null ? Icons.For(f.Prefab, f.Variant) : null;
                sprite.enabled = sprite.sprite != null;
                sprite.color = new Color(1f, 1f, 1f, _mask.Hotbar[i] ? 1f : 0.3f);
                if (_rememberHotbar && _mask.Hotbar[i] && f != null)
                    hotCount++;
            }
            int slotCount = _mask.Slots.Count + (_mask.Utilities ? 1 : 0);
            _summary.text = slotCount + " of 9 slots" + (hotCount > 0 ? " + " + hotCount + " hotbar positions" : "") + " · saves to this character";
        }

        private static void Build()
        {
            if (_root != null)
                return;
            RectTransform window = GearSetsWindow.Root;
            Button template = GearSetsWindow.Template;
            _root = UiKit.Rect("SaveDialog", window);
            _root.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            _root.anchorMin = new Vector2(0f, 1f);
            _root.anchorMax = new Vector2(1f, 1f);
            _root.pivot = new Vector2(0.5f, 1f);
            _root.anchoredPosition = Vector2.zero;
            _root.sizeDelta = Vector2.zero;
            _root.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var bg = _root.gameObject.AddComponent<Image>();
            bg.sprite = UiKit.White;
            bg.color = new Color(0.10f, 0.08f, 0.06f, 0.98f);
            UiKit.Column(_root.gameObject, 12f, new RectOffset(20, 20, 16, 16));

            _title = UiKit.Text(_root, "Title", 20f, TextAlignmentOptions.Left);
            _title.color = UiKit.Accent;

            RectTransform nameRow = UiKit.Rect("NameRow", _root);
            UiKit.Row(nameRow.gameObject, 10f, new RectOffset(0, 0, 0, 0));
            TextMeshProUGUI nameLabel = UiKit.Text(nameRow, "Label", 13f, TextAlignmentOptions.Left);
            nameLabel.text = "Name";
            nameLabel.color = UiKit.Muted;
            UiKit.Size(nameLabel.gameObject, 50f, 36f);
            _nameButton = UiKit.Button(nameRow, "Name", template, "", () => NameInput.Ask(_name, v =>
            {
                _name = v.Trim();
                _error.text = "";
                Render();
            }));
            UiKit.Size(_nameButton.gameObject, 260f, 36f);
            TextMeshProUGUI nameHint = UiKit.Text(nameRow, "Hint", 12f, TextAlignmentOptions.Left);
            nameHint.text = "click to type";
            nameHint.color = UiKit.Muted;

            TextMeshProUGUI iconLabel = UiKit.Text(_root, "IconLabel", 13f, TextAlignmentOptions.Left);
            iconLabel.text = "Icon";
            iconLabel.color = UiKit.Muted;
            _iconRow = UiKit.Rect("Icons", _root);
            UiKit.Row(_iconRow.gameObject, 6f, new RectOffset(0, 0, 0, 0));
            UiKit.Size(_iconRow.gameObject, -1f, 44f);

            _captureSection = UiKit.Rect("Capture", _root);
            UiKit.Column(_captureSection.gameObject, 8f, new RectOffset(0, 0, 0, 0));
            TextMeshProUGUI incl = UiKit.Text(_captureSection, "Caption", 12f, TextAlignmentOptions.Left);
            incl.text = "EQUIPMENT TO INCLUDE  <size=11><color=#9c8e72>unchecked slots are left as they are when you equip the set</color></size>";
            incl.color = UiKit.Muted;
            RectTransform grid = UiKit.Rect("Slots", _captureSection);
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(200f, 46f);
            g.spacing = new Vector2(6f, 6f);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 3;
            for (int i = 0; i <= TileSlots.Length; i++)
            {
                int index = i;
                Button b = UiKit.Button(grid, "Slot" + i, template, "", () =>
                {
                    if (index == TileSlots.Length)
                        _mask.Utilities = !_mask.Utilities;
                    else if (!_mask.Slots.Remove(TileSlots[index]))
                        _mask.Slots.Add(TileSlots[index]);
                    Render();
                });
                TMP_Text t = b.GetComponentInChildren<TMP_Text>(true);
                t.alignment = TextAlignmentOptions.Left;
                t.textWrappingMode = TextWrappingModes.Normal;
                t.fontSize = 13f;
                t.margin = new Vector4(8f, 0f, 4f, 0f);
                SlotToggles.Add(b);
            }

            _hotbarToggle = UiKit.Button(_captureSection, "RememberHotbar", template, "", () =>
            {
                _rememberHotbar = !_rememberHotbar;
                Render();
            });
            UiKit.Size(_hotbarToggle.gameObject, 260f, 32f);
            RectTransform hot = UiKit.Rect("Hotbar", _captureSection);
            var hg = hot.gameObject.AddComponent<GridLayoutGroup>();
            hg.cellSize = new Vector2(44f, 44f);
            hg.spacing = new Vector2(5f, 5f);
            hg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            hg.constraintCount = GearSet.HotbarSize;
            for (int i = 0; i < GearSet.HotbarSize; i++)
            {
                int index = i;
                Button b = UiKit.Button(hot, "Hot" + i, template, (i + 1).ToString(), () =>
                {
                    _mask.Hotbar[index] = !_mask.Hotbar[index];
                    Render();
                });
                TMP_Text t = b.GetComponentInChildren<TMP_Text>(true);
                t.alignment = TextAlignmentOptions.TopLeft;
                t.fontSize = 10f;
                t.margin = new Vector4(4f, 2f, 0f, 0f);
                Image sprite = UiKit.Img(b.transform, "Sprite", null, Color.white);
                ((RectTransform)sprite.transform).sizeDelta = new Vector2(28f, 28f);
                HotToggles[i] = b;
            }

            _error = UiKit.Text(_root, "Error", 13f, TextAlignmentOptions.Left);
            _error.color = UiKit.Warn;

            RectTransform footer = UiKit.Rect("Footer", _root);
            UiKit.Row(footer.gameObject, 10f, new RectOffset(0, 0, 4, 0));
            _summary = UiKit.Text(footer, "Summary", 12f, TextAlignmentOptions.Left);
            _summary.color = UiKit.Muted;
            _summary.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            Button cancel = UiKit.Button(footer, "Cancel", template, "Cancel", Close);
            UiKit.Size(cancel.gameObject, 90f, 38f);
            Button save = UiKit.Button(footer, "Save", template, "<b>Save set</b>", Save);
            UiKit.Size(save.gameObject, 110f, 38f);

            _root.gameObject.SetActive(false);
        }

        private static string FreeName()
        {
            for (int n = SetStore.Book.Sets.Count + 1; ; n++)
                if (SetStore.Book.FindByName("Set " + n) == null)
                    return "Set " + n;
        }

        private static List<string> WornPrefabs(InventorySnapshot snap)
        {
            var list = new List<string>();
            WornSlot[] order = { WornSlot.RightHand, WornSlot.LeftHand, WornSlot.Chest, WornSlot.Helmet, WornSlot.Shoulder, WornSlot.Legs, WornSlot.Trinket, WornSlot.Utility };
            foreach (WornSlot w in order)
                foreach (ItemFacts f in snap.Items)
                    if (f.Worn == w && !f.Stackable && !list.Contains(f.Prefab))
                        list.Add(f.Prefab);
            return list;
        }
    }
}
