using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Voss.Audio;
using Voss.Core;

namespace Voss.UI
{
    /// <summary>
    /// Title screen: top-hat logo, THE VOSS EXPERIMENT with a red O, ESCAPE ROOM, and four framed buttons
    /// (Start Game, How to Play, Settings, Quit) down the left side over the theatre set, with How to Play
    /// and Settings cards.
    /// </summary>
    public class MainMenuPanel
    {
        private static readonly Color Silver = new Color(0.86f, 0.85f, 0.82f);
        private static readonly Color Blood = new Color(0.82f, 0.13f, 0.1f);

        private readonly GameObject root;
        private readonly GameObject menuColumn;
        private readonly GameObject howTo;
        private readonly GameObject settings;
        private readonly Text volumeValue;
        private readonly Text sensitivityValue;
        private readonly Text fullscreenValue;
        private Action onStart;
        private Action onQuit;

        public bool IsOpen => root.activeSelf;

        public MainMenuPanel(Transform parent)
        {
            var panel = UIFactory.Panel(parent, "MainMenu", Color.white);
            root = panel.gameObject;
            var shade = panel.GetComponent<Image>();
            shade.sprite = UIIcons.LeftShade;
            shade.raycastTarget = true;

            menuColumn = UIFactory.Empty(panel, "Column").gameObject;
            var col = menuColumn.GetComponent<RectTransform>();
            UIFactory.Place(col, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(110f, 0f), new Vector2(620f, 1000f));

            // Logo: top hat, title, ESCAPE ROOM between thin red rules.
            var hat = Icon(col, "HatCrown", UIIcons.HatCrown, new Color(0.12f, 0.02f, 0.02f), new Vector2(310f, -60f), 110f);
            hat.gameObject.AddComponent<Outline>().effectColor = new Color(Blood.r, Blood.g, Blood.b, 0.9f);
            Icon(col, "HatBand", UIIcons.HatBand, Blood, new Vector2(310f, -60f), 110f);
            Rule(col, new Vector2(70f, -112f), 150f);
            Rule(col, new Vector2(400f, -112f), 150f);

            var title = UIFactory.Label(col, "Title", "THE V<color=#d0261c>O</color>SS\nEXPERIMENT", 92, Silver, TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(310f, -125f), new Vector2(620f, 230f));
            title.lineSpacing = 0.9f;
            title.fontSize = 84;
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            var titleShadow = title.gameObject.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            titleShadow.effectDistance = new Vector2(3f, -3f);
            title.gameObject.AddComponent<Outline>().effectColor = new Color(0.4f, 0.03f, 0.02f, 0.5f);

            Rule(col, new Vector2(120f, -372f), 120f);
            Rule(col, new Vector2(500f, -372f), 120f);
            var sub = UIFactory.Label(col, "Subtitle", "E S C A P E    R O O M", 30, Blood, TextAnchor.MiddleCenter, FontStyle.Normal);
            UIFactory.Place(sub.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(310f, -372f), new Vector2(380f, 44f));
            Icon(col, "Ornament", UIIcons.Diamond, Blood, new Vector2(310f, -346f), 16f);

            // Buttons
            MenuButton(col, "Start Game", UIIcons.Play, new Vector2(30f, -450f), () => { Close(); onStart?.Invoke(); }, true);
            MenuButton(col, "How to Play", UIIcons.Book, new Vector2(30f, -540f), () => Show(howTo), false);
            MenuButton(col, "Settings", UIIcons.Gear, new Vector2(30f, -630f), () => Show(settings), false);
            MenuButton(col, "Quit", UIIcons.Power, new Vector2(30f, -720f), () => onQuit?.Invoke(), false);

            // How to Play card
            howTo = Card(panel, "HowToPlay", "HOW TO PLAY");
            var body = UIFactory.Label(howTo.transform, "Body",
                "<color=#c9a258>THE GOAL</color>\n" +
                "Twenty years ago, E. Voss vanished in front of four hundred people.\n" +
                "Now you are inside his laboratory. Pass his three tests -\n" +
                "PERCEPTION, REASON and MEMORY - and find a way out.\n" +
                "You may only make five mistakes. After that, the experiment begins again.\n\n" +
                "<color=#c9a258>CONTROLS</color>\n" +
                "W A S D  -  move            Mouse  -  look            Shift  -  run\n" +
                "E  or  Left click  -  examine and use\n" +
                "Right click  or  Esc  -  put down / go back\n" +
                "Tab  -  evidence and current objective            Esc  -  pause\n\n" +
                "<color=#c9a258>A WARNING</color>\n" +
                "Read everything. Remember everything. Trust nothing.",
                21, UIFactory.Ink, TextAnchor.UpperLeft);
            UIFactory.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -95f), new Vector2(760f, 480f));
            body.lineSpacing = 1.15f;

            // Settings card
            settings = Card(panel, "Settings", "SETTINGS");
            volumeValue = SettingRow(settings.transform, "Volume", -130f, () => GameSettings.SetVolume(GameSettings.MasterVolume - 0.1f), () => GameSettings.SetVolume(GameSettings.MasterVolume + 0.1f));
            sensitivityValue = SettingRow(settings.transform, "Mouse sensitivity", -220f, () => GameSettings.SetSensitivity(GameSettings.MouseSensitivity - 0.1f), () => GameSettings.SetSensitivity(GameSettings.MouseSensitivity + 0.1f));
            fullscreenValue = SettingRow(settings.transform, "Fullscreen", -310f, () => GameSettings.SetFullscreen(!GameSettings.Fullscreen), () => GameSettings.SetFullscreen(!GameSettings.Fullscreen));

            root.SetActive(false);
        }

        public void Open(Action start, Action quit)
        {
            onStart = start;
            onQuit = quit;
            howTo.SetActive(false);
            settings.SetActive(false);
            menuColumn.SetActive(true);
            root.SetActive(true);
        }

        public void Close() { root.SetActive(false); }

        /// <summary>Back / Esc: closes an open card, otherwise does nothing.</summary>
        public void HandleBack()
        {
            if (howTo.activeSelf || settings.activeSelf) ShowMenu();
        }

        private void Show(GameObject card)
        {
            GameManager.Instance?.Audio.Play(SfxId.Paper, 0.5f);
            howTo.SetActive(card == howTo);
            settings.SetActive(card == settings);
            RefreshSettings();
        }

        private void ShowMenu()
        {
            howTo.SetActive(false);
            settings.SetActive(false);
        }

        private void RefreshSettings()
        {
            volumeValue.text = Mathf.RoundToInt(GameSettings.MasterVolume * 100f) + "%";
            sensitivityValue.text = GameSettings.MouseSensitivity.ToString("0.0") + "x";
            fullscreenValue.text = GameSettings.Fullscreen ? "ON" : "OFF";
        }

        // ------------------------------------------------------------ builders

        private static Image Icon(Transform parent, string name, Sprite sprite, Color color, Vector2 pos, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            UIFactory.Place(img.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            return img;
        }

        private static void Rule(Transform parent, Vector2 centre, float width)
        {
            var r = UIFactory.Panel(parent, "Rule", new Color(Blood.r, Blood.g, Blood.b, 0.8f), false);
            r.GetComponent<Image>().raycastTarget = false;
            UIFactory.Place(r, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), centre, new Vector2(width, 2f));
        }

        private void MenuButton(Transform parent, string label, Sprite icon, Vector2 topLeft, Action click, bool primary)
        {
            var btn = UIFactory.Button(parent, label.Replace(" ", ""), "", 30, () =>
            {
                GameManager.Instance?.Audio.Play(SfxId.Click, 0.7f);
                click();
            }, new Color(0.055f, 0.05f, 0.05f, 0.88f));
            var rt = btn.GetComponent<RectTransform>();
            UIFactory.Place(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), topLeft, new Vector2(420f, 70f));

            var glow = UIFactory.Panel(rt, "Glow", new Color(Blood.r, Blood.g, Blood.b, 0.22f), false);
            glow.GetComponent<Image>().raycastTarget = false;
            UIFactory.Stretch(glow, -6f);
            glow.SetAsFirstSibling();

            var ic = Icon(rt, "Icon", icon, Silver, new Vector2(56f, -35f), 40f);
            var text = UIFactory.Label(rt, "Text", label, 30, Silver, TextAnchor.MiddleLeft);
            UIFactory.Place(text.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(110f, 0f), new Vector2(300f, 60f));

            var fx = btn.gameObject.AddComponent<MenuButtonFx>();
            fx.Configure(btn.GetComponent<Outline>(), glow.GetComponent<Image>(), text, ic, primary);
        }

        private GameObject Card(Transform parent, string name, string titleText)
        {
            var card = UIFactory.Panel(parent, name, new Color(0.035f, 0.03f, 0.03f, 0.94f), false);
            UIFactory.Place(card, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-120f, 0f), new Vector2(860f, 640f));
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Blood.r, Blood.g, Blood.b, 0.7f);
            outline.effectDistance = new Vector2(2f, -2f);
            var title = UIFactory.Label(card, "Title", titleText, 34, Silver, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(700f, 50f));
            var back = UIFactory.Button(card, "Back", "BACK", 22, () =>
            {
                GameManager.Instance?.Audio.Play(SfxId.Click, 0.7f);
                ShowMenu();
            }, new Color(0.08f, 0.07f, 0.07f, 1f));
            UIFactory.Place(back.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(220f, 50f));
            card.gameObject.SetActive(false);
            return card.gameObject;
        }

        private Text SettingRow(Transform card, string label, float y, Action minus, Action plus)
        {
            var name = UIFactory.Label(card, label, label, 24, UIFactory.Ink, TextAnchor.MiddleLeft);
            UIFactory.Place(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-170f, y), new Vector2(360f, 50f));
            var value = UIFactory.Label(card, label + "Value", "", 24, UIFactory.Brass, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place(value.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(200f, y), new Vector2(120f, 50f));
            var less = UIFactory.Button(card, label + "Minus", "<", 26, () => { minus(); RefreshSettings(); GameManager.Instance?.Audio.Play(SfxId.Click, 0.6f); });
            UIFactory.Place(less.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(110f, y), new Vector2(50f, 46f));
            var more = UIFactory.Button(card, label + "Plus", ">", 26, () => { plus(); RefreshSettings(); GameManager.Instance?.Audio.Play(SfxId.Click, 0.6f); });
            UIFactory.Place(more.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(290f, y), new Vector2(50f, 46f));
            return value;
        }
    }

    /// <summary>Red frame and glow on hover; the primary button keeps it while nothing else is hovered.</summary>
    public class MenuButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly List<MenuButtonFx> all = new List<MenuButtonFx>();
        private Outline frame;
        private Image glow;
        private Text label;
        private Image icon;
        private bool primary;
        private bool hovered;

        public void Configure(Outline outline, Image glowImage, Text text, Image iconImage, bool isPrimary)
        {
            frame = outline;
            glow = glowImage;
            label = text;
            icon = iconImage;
            primary = isPrimary;
            frame.effectDistance = new Vector2(2f, -2f);
        }

        private void OnEnable() { all.Add(this); hovered = false; Refresh(); }
        private void OnDisable() { all.Remove(this); }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            GameManager.Instance?.Audio.Play(SfxId.Click, 0.25f);
            foreach (var b in all) b.Refresh();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            foreach (var b in all) b.Refresh();
        }

        private void Refresh()
        {
            bool anyHover = false;
            foreach (var b in all) anyHover |= b.hovered;
            bool lit = hovered || (primary && !anyHover);
            var red = new Color(0.85f, 0.14f, 0.1f);
            if (frame != null) frame.effectColor = lit ? red : new Color(0.45f, 0.43f, 0.4f, 0.6f);
            if (glow != null) glow.enabled = lit;
            if (label != null) label.color = lit ? Color.white : new Color(0.8f, 0.79f, 0.76f);
            if (icon != null) icon.color = lit ? Color.white : new Color(0.8f, 0.79f, 0.76f);
        }
    }
}
