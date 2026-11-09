using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Voss.UI
{
    /// <summary>Modal multiple-choice panel (final question, failure screen, main menu).</summary>
    public class ChoicePanel
    {
        private readonly GameObject root;
        private readonly Image backdrop;
        private readonly Text title;
        private readonly Text body;
        private readonly RectTransform buttonHost;
        private readonly List<Button> buttons = new List<Button>();
        private readonly RectTransform card;
        private Action<int> onChoice;
        private bool allowClose;
        private Action onClose;

        public bool IsOpen => root.activeSelf;

        public ChoicePanel(Transform parent, string name)
        {
            var panel = UIFactory.Panel(parent, name, new Color(0f, 0f, 0f, 0.82f));
            root = panel.gameObject;
            backdrop = panel.GetComponent<Image>();

            card = UIFactory.Panel(panel, "Card", UIFactory.PanelBg, false);
            UIFactory.Center(card, new Vector2(620f, 480f));

            title = UIFactory.Label(card, "Title", "", 30, UIFactory.Brass, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(580f, 50f));

            body = UIFactory.Label(card, "Body", "", 17, UIFactory.Dim);
            UIFactory.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(560f, 90f));

            buttonHost = UIFactory.Empty(card, "Buttons");
            UIFactory.Place(buttonHost, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(420f, 270f));
            UIFactory.VerticalGroup(buttonHost, 10f, 0);

            root.SetActive(false);
        }

        public void Open(string titleText, string bodyText, IList<string> options, Action<int> choice,
            bool closable = false, Action closed = null, Color? backdropColor = null)
        {
            title.text = titleText ?? string.Empty;
            body.text = bodyText ?? string.Empty;
            onChoice = choice;
            allowClose = closable;
            onClose = closed;
            backdrop.color = backdropColor ?? new Color(0f, 0f, 0f, 0.82f);

            foreach (var b in buttons) UnityEngine.Object.Destroy(b.gameObject);
            buttons.Clear();

            for (int i = 0; i < options.Count; i++)
            {
                int index = i;
                var btn = UIFactory.Button(buttonHost, "Option_" + i, options[i], 18, () => Select(index));
                UIFactory.Height(btn, 52f);
                buttons.Add(btn);
            }

            float h = 200f + options.Count * 62f;
            card.sizeDelta = new Vector2(620f, Mathf.Max(360f, h));
            buttonHost.sizeDelta = new Vector2(420f, options.Count * 62f);
            root.SetActive(true);
        }

        public void SetInteractable(bool value)
        {
            foreach (var b in buttons) b.interactable = value;
        }

        public void Close()
        {
            root.SetActive(false);
            onChoice = null;
        }

        public void HandleBack()
        {
            if (!IsOpen || !allowClose) return;
            Close();
            onClose?.Invoke();
        }

        private void Select(int index)
        {
            var cb = onChoice;
            cb?.Invoke(index);
        }
    }
}
