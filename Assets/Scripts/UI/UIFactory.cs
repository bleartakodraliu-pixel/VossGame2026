using UnityEngine;
using UnityEngine.UI;

namespace Voss.UI
{
    /// <summary>
    /// Builds uGUI elements from code so the interface needs no prefabs or scene setup.
    /// Visual style: minimal, dark, thin type, brass accent.
    /// </summary>
    public static class UIFactory
    {
        public static readonly Color Ink = new Color(0.88f, 0.86f, 0.80f, 1f);
        public static readonly Color Dim = new Color(0.55f, 0.53f, 0.48f, 1f);
        public static readonly Color Brass = new Color(0.78f, 0.62f, 0.32f, 1f);
        public static readonly Color PanelBg = new Color(0.03f, 0.03f, 0.035f, 0.94f);
        public static readonly Color ButtonBg = new Color(0.10f, 0.10f, 0.11f, 1f);
        public static readonly Color ButtonHover = new Color(0.20f, 0.17f, 0.12f, 1f);
        public static readonly Color Danger = new Color(0.75f, 0.15f, 0.12f, 1f);
        public static readonly Color Accept = new Color(0.25f, 0.65f, 0.35f, 1f);

        private static Font cachedFont;

        public static Font DefaultFont
        {
            get
            {
                if (cachedFont != null) return cachedFont;
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (cachedFont == null) cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return cachedFont;
            }
        }

        public static RectTransform Panel(Transform parent, string name, Color color, bool fullscreen = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            if (fullscreen) Stretch(rt);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            return rt;
        }

        public static RectTransform Empty(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        public static Text Label(Transform parent, string name, string text, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = DefaultFont;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = style;
            t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            return t;
        }

        public static Button Button(Transform parent, string name, string label, int fontSize,
            UnityEngine.Events.UnityAction onClick, Color? bg = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = bg ?? ButtonBg;
            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.4f, 1.3f, 1.1f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.selectedColor = Color.white;
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(onClick);

            var txt = Label(go.transform, "Label", label, fontSize, Ink);
            Stretch(txt.rectTransform);

            // Thin brass outline for the cinematic look.
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(Brass.r, Brass.g, Brass.b, 0.45f);
            outline.effectDistance = new Vector2(1f, -1f);
            return btn;
        }

        public static void Stretch(RectTransform rt, float padding = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        public static void Center(RectTransform rt, Vector2 size, Vector2 offset = default)
        {
            Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), offset, size);
        }

        public static VerticalLayoutGroup VerticalGroup(RectTransform rt, float spacing, int padding)
        {
            var g = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing;
            g.padding = new RectOffset(padding, padding, padding, padding);
            g.childAlignment = TextAnchor.MiddleCenter;
            g.childControlHeight = true;
            g.childControlWidth = true;
            g.childForceExpandHeight = false;
            g.childForceExpandWidth = true;
            return g;
        }

        public static GridLayoutGroup Grid(RectTransform rt, Vector2 cell, Vector2 spacing, int columns)
        {
            var g = rt.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = cell;
            g.spacing = spacing;
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = columns;
            g.childAlignment = TextAnchor.MiddleCenter;
            return g;
        }

        public static LayoutElement Height(Component c, float height)
        {
            var le = c.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            return le;
        }
    }
}
