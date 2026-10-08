using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GearSets.UI
{
    /// <summary>uGUI helpers in the game's look: its font, its button sprites, its panel background.</summary>
    internal static class UiKit
    {
        public static readonly Color Accent = new Color(0.949f, 0.651f, 0.290f);   // #f2a64a
        public static readonly Color Mint = new Color(0.494f, 0.878f, 0.765f);     // #7ee0c3
        public static readonly Color TextColor = new Color(0.925f, 0.886f, 0.800f); // #ece2cc
        public static readonly Color Muted = new Color(0.702f, 0.647f, 0.533f);    // #b3a588
        public static readonly Color Warn = new Color(0.941f, 0.569f, 0.373f);     // #f0915f
        public static readonly Color Equipped = new Color(0.549f, 0.753f, 0.918f); // #8cc0ea
        public static readonly Color Panel = new Color(0.141f, 0.110f, 0.082f, 0.96f);
        public static readonly Color Tile = new Color(0.165f, 0.129f, 0.098f, 1f);
        public static readonly Color TileOff = new Color(0.122f, 0.094f, 0.071f, 1f);
        public static readonly Color Selected = new Color(0.227f, 0.173f, 0.118f, 1f);

        private static Sprite _white;
        private static Sprite _circle;

        public static Sprite White
        {
            get
            {
                if (_white != null)
                    return _white;
                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var px = new Color32[16];
                for (int i = 0; i < px.Length; i++)
                    px[i] = new Color32(255, 255, 255, 255);
                tex.SetPixels32(px);
                tex.Apply();
                tex.hideFlags = HideFlags.HideAndDontSave;
                _white = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
                _white.hideFlags = HideFlags.HideAndDontSave;
                return _white;
            }
        }

        /// <summary>A soft-edged white disc for the radial.</summary>
        public static Sprite Circle
        {
            get
            {
                if (_circle != null)
                    return _circle;
                const int n = 128;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                var px = new Color32[n * n];
                float r = n / 2f;
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Mathf.Sqrt((x + 0.5f - r) * (x + 0.5f - r) + (y + 0.5f - r) * (y + 0.5f - r));
                        byte a = (byte)(Mathf.Clamp01(r - d) * 255f);
                        px[y * n + x] = new Color32(255, 255, 255, a);
                    }
                tex.SetPixels32(px);
                tex.Apply();
                tex.hideFlags = HideFlags.HideAndDontSave;
                _circle = Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), n);
                _circle.hideFlags = HideFlags.HideAndDontSave;
                return _circle;
            }
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void Size(GameObject go, float w, float h)
        {
            LayoutElement le = go.GetComponent<LayoutElement>();
            if (le == null)
                le = go.AddComponent<LayoutElement>();
            if (w >= 0f)
                le.minWidth = le.preferredWidth = w;
            if (h >= 0f)
                le.minHeight = le.preferredHeight = h;
        }

        public static TMP_FontAsset Font
        {
            get
            {
                InventoryGui gui = InventoryGui.instance;
                TMP_Text t = gui != null && gui.m_craftButton != null ? gui.m_craftButton.GetComponentInChildren<TMP_Text>(true) : null;
                if (t != null)
                    return t.font;
                return Hud.instance != null && Hud.instance.m_healthText != null ? Hud.instance.m_healthText.font : null;
            }
        }

        public static TextMeshProUGUI Text(Transform parent, string name, float size, TextAlignmentOptions align)
        {
            RectTransform rt = Rect(name, parent);
            rt.gameObject.SetActive(false); // TMP's Awake must see the font, or it logs a missing-font warning
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset font = Font;
            if (font != null)
                t.font = font;
            rt.gameObject.SetActive(true);
            t.fontSize = size;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.richText = true;
            t.raycastTarget = false;
            t.color = TextColor;
            return t;
        }

        public static Button Button(Transform parent, string name, Button template, string label, UnityAction onClick)
        {
            RectTransform rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            Image timg = template != null ? template.GetComponent<Image>() : null;
            if (timg != null)
            {
                img.sprite = timg.sprite;
                img.type = timg.type;
                img.color = timg.color;
            }
            else
            {
                img.sprite = White;
                img.color = new Color(0.3f, 0.25f, 0.2f, 0.9f);
            }
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            if (template != null)
            {
                b.transition = template.transition;
                b.colors = template.colors;
                b.spriteState = template.spriteState;
            }
            if (onClick != null)
                b.onClick.AddListener(onClick);
            TextMeshProUGUI t = Text(rt, "Label", 15f, TextAlignmentOptions.Center);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            Stretch((RectTransform)t.transform);
            t.text = label;
            return b;
        }

        public static void SetLabel(Button b, string text, Color color)
        {
            TMP_Text t = b.GetComponentInChildren<TMP_Text>(true);
            if (t == null)
                return;
            t.text = text;
            t.color = color;
        }

        public static Image Img(Transform parent, string name, Sprite sprite, Color color)
        {
            RectTransform rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>The player inventory block's own background, made opaque.</summary>
        public static void Background(GameObject go, InventoryGui gui)
        {
            var bg = go.AddComponent<UnityEngine.UI.Image>();
            Image host = gui != null && gui.m_player != null ? gui.m_player.GetComponent<Image>() : null;
            if (host != null && host.sprite != null)
            {
                bg.sprite = host.sprite;
                bg.type = host.type;
                Color c = host.color;
                c.a = Mathf.Max(c.a, 0.96f);
                bg.color = c;
            }
            else
            {
                bg.sprite = White;
                bg.color = Panel;
            }
        }

        public static VerticalLayoutGroup Column(GameObject go, float spacing, RectOffset padding)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = padding;
            v.childAlignment = TextAnchor.UpperLeft;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup Row(GameObject go, float spacing, RectOffset padding)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = padding;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            return h;
        }

        /// <summary>A small diamond: a square rotated 45°.</summary>
        public static Image Diamond(Transform parent, string name, float size, Color color)
        {
            Image img = Img(parent, name, White, color);
            img.preserveAspect = false;
            var rt = (RectTransform)img.transform;
            rt.sizeDelta = new Vector2(size, size);
            rt.localRotation = Quaternion.Euler(0f, 0f, 45f);
            return img;
        }
    }
}
