using System;
using UnityEngine;
using UnityEngine.UI;
using Voss.Audio;
using Voss.Core;

namespace Voss.UI
{
    /// <summary>
    /// Reading view for Voss's letter: aged dark paper, the ticket pinned at the corner, no time limit,
    /// closed only with "Take Letter" (or Enter). The letter stays readable later from the evidence menu.
    /// </summary>
    public class LetterPanel
    {
        private readonly GameObject root;
        private readonly Text body;
        private Action onTake;

        public bool IsOpen => root.activeSelf;

        public LetterPanel(Transform parent)
        {
            var backdrop = UIFactory.Panel(parent, "LetterPanel", new Color(0f, 0f, 0f, 0.78f));
            root = backdrop.gameObject;

            var paper = UIFactory.Panel(backdrop, "Paper", new Color(0.55f, 0.47f, 0.34f, 1f), false);
            UIFactory.Center(paper, new Vector2(660f, 800f), new Vector2(0f, 20f));
            var edge = paper.gameObject.AddComponent<Outline>();
            edge.effectColor = new Color(0.16f, 0.1f, 0.06f, 1f);
            edge.effectDistance = new Vector2(3f, -3f);
            var shadow = paper.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            shadow.effectDistance = new Vector2(10f, -10f);

            // Darkened margins give the sheet an aged look.
            var inner = UIFactory.Panel(paper, "Inner", new Color(0.66f, 0.58f, 0.43f, 1f), false);
            UIFactory.Stretch(inner, 26f);
            inner.GetComponent<Image>().raycastTarget = false;

            var crest = UIFactory.Label(inner, "Crest", "V", 46, new Color(0.45f, 0.06f, 0.05f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place(crest.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(100f, 60f));

            body = UIFactory.Label(inner, "Body", "", 25, new Color(0.13f, 0.08f, 0.05f), TextAnchor.UpperLeft, FontStyle.Italic);
            UIFactory.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(520f, 520f));
            body.lineSpacing = 1.1f;

            // The ticket, pinned to the bottom of the letter.
            var ticket = UIFactory.Panel(inner, "Ticket", new Color(0.86f, 0.79f, 0.6f, 1f), false);
            UIFactory.Place(ticket, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(26f, 96f), new Vector2(230f, 96f));
            ticket.localRotation = Quaternion.Euler(0f, 0f, 4f);
            ticket.gameObject.AddComponent<Outline>().effectColor = new Color(0.3f, 0.2f, 0.1f, 0.8f);
            var admit = UIFactory.Label(ticket, "Admit", "VOSS  -  PRIVATE PERFORMANCE", 11, new Color(0.25f, 0.15f, 0.08f), TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Place(admit.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(220f, 20f));
            var number = UIFactory.Label(ticket, "Number", "7314", 40, new Color(0.12f, 0.07f, 0.04f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place(number.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(220f, 60f));

            var take = UIFactory.Button(paper, "TakeLetter", "Take Letter", 24, Take, new Color(0.08f, 0.05f, 0.04f, 0.95f));
            UIFactory.Place(take.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-36f, 34f), new Vector2(230f, 56f));
            var outline = take.GetComponent<Outline>();
            outline.effectColor = new Color(0.82f, 0.13f, 0.1f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);

            var hint = UIFactory.Label(backdrop, "Hint", "Read as long as you like.", 15, UIFactory.Dim, TextAnchor.MiddleCenter, FontStyle.Italic);
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(600f, 30f));

            root.SetActive(false);
        }

        public void Open(string text, Action take)
        {
            body.text = text;
            onTake = take;
            root.SetActive(true);
        }

        public void Close() { root.SetActive(false); }

        /// <summary>Enter also takes the letter, for players who prefer the keyboard.</summary>
        public void Update()
        {
            if (IsOpen && VossInput.SkipPressed()) Take();
        }

        private void Take()
        {
            if (!IsOpen) return;
            GameManager.Instance?.Audio.Play(SfxId.Paper, 0.9f);
            root.SetActive(false);
            var cb = onTake;
            onTake = null;
            cb?.Invoke();
        }
    }

    /// <summary>Small, quiet current-objective line in the top-left corner.</summary>
    public class ObjectiveHud
    {
        private readonly GameObject root;
        private readonly Text text;
        private readonly CanvasGroup group;
        private float target;

        public ObjectiveHud(Transform parent)
        {
            var r = UIFactory.Empty(parent, "Objective");
            UIFactory.Place(r, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -32f), new Vector2(620f, 70f));
            root = r.gameObject;
            group = root.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            var bar = UIFactory.Panel(r, "Bar", new Color(0.82f, 0.13f, 0.1f, 0.9f), false);
            bar.GetComponent<Image>().raycastTarget = false;
            UIFactory.Place(bar, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -4f), new Vector2(3f, 52f));
            var label = UIFactory.Label(r, "Label", "OBJECTIVE", 14, UIFactory.Brass, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -2f), new Vector2(600f, 22f));
            text = UIFactory.Label(r, "Text", "", 21, UIFactory.Ink, TextAnchor.UpperLeft);
            UIFactory.Place(text.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -24f), new Vector2(600f, 40f));
            text.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(1f, -1f);
            group.alpha = 0f;
        }

        public void Show(string objective)
        {
            text.text = objective;
            target = 1f;
        }

        public void Hide() { target = 0f; }

        public void HideInstant() { target = 0f; group.alpha = 0f; }

        public void Update()
        {
            group.alpha = Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime * 1.5f);
        }
    }
}
