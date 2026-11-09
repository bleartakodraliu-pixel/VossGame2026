using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Voss.Audio;
using Voss.Core;
using Voss.Narrative;

namespace Voss.UI
{
    /// <summary>Evidence review menu: list of discovered clues on the left, flip-able card on the right.</summary>
    public class EvidencePanel
    {
        private readonly GameObject root;
        private readonly RectTransform listHost;
        private readonly Text cardTitle;
        private readonly Text cardText;
        private readonly Text hint;
        private readonly List<Button> listButtons = new List<Button>();
        private EvidenceItem selected;
        private bool showingBack;

        public bool IsOpen => root.activeSelf;

        public EvidencePanel(Transform parent)
        {
            var panel = UIFactory.Panel(parent, "EvidencePanel", new Color(0f, 0f, 0f, 0.86f));
            root = panel.gameObject;

            var card = UIFactory.Panel(panel, "Card", UIFactory.PanelBg, false);
            UIFactory.Center(card, new Vector2(860f, 520f));

            var header = UIFactory.Label(card, "Header", "EVIDENCE", 26, UIFactory.Brass, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(header.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -20f), new Vector2(400f, 40f));

            listHost = UIFactory.Empty(card, "List");
            UIFactory.Place(listHost, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -76f), new Vector2(260f, 380f));
            var vg = UIFactory.VerticalGroup(listHost, 8f, 0);
            vg.childAlignment = TextAnchor.UpperCenter;

            var cardBg = UIFactory.Panel(card, "ClueCard", new Color(0.93f, 0.90f, 0.82f, 1f), false);
            UIFactory.Place(cardBg, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-40f, 10f), new Vector2(480f, 300f));

            cardTitle = UIFactory.Label(cardBg, "ClueTitle", "", 15, new Color(0.35f, 0.3f, 0.2f, 1f), TextAnchor.UpperCenter, FontStyle.Italic);
            UIFactory.Place(cardTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(440f, 24f));

            cardText = UIFactory.Label(cardBg, "ClueText", "", 30, new Color(0.08f, 0.07f, 0.06f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place(cardText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(440f, 220f));

            var flip = UIFactory.Button(card, "Flip", "TURN OVER", 15, Flip);
            UIFactory.Place(flip.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 30f), new Vector2(200f, 40f));

            hint = UIFactory.Label(card, "Hint", "TAB / ESC to close", 13, UIFactory.Dim, TextAnchor.MiddleLeft);
            UIFactory.Place(hint.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(28f, 24f), new Vector2(300f, 30f));

            root.SetActive(false);
        }

        public void Open()
        {
            Rebuild();
            root.SetActive(true);
            GameManager.Instance?.Audio.Play(SfxId.Paper, 0.6f);
        }

        public void Close()
        {
            root.SetActive(false);
        }

        private void Rebuild()
        {
            foreach (var b in listButtons) Object.Destroy(b.gameObject);
            listButtons.Clear();

            var gm = GameManager.Instance;
            var items = gm != null ? gm.Evidence.GetDiscovered() : new List<EvidenceItem>();

            if (items.Count == 0)
            {
                var none = UIFactory.Button(listHost, "None", "No evidence yet", 15, null);
                none.interactable = false;
                UIFactory.Height(none, 44f);
                listButtons.Add(none);
                selected = null;
                RefreshCard();
                return;
            }

            foreach (var item in items)
            {
                var it = item;
                var btn = UIFactory.Button(listHost, "Item_" + it.Id, it.Title, 16, () => Select(it));
                UIFactory.Height(btn, 44f);
                listButtons.Add(btn);
            }
            if (selected == null || !selected.Discovered) selected = items[0];
            showingBack = false;
            RefreshCard();
        }

        private void Select(EvidenceItem item)
        {
            selected = item;
            showingBack = false;
            RefreshCard();
            GameManager.Instance?.Audio.Play(SfxId.Paper, 0.4f);
        }

        private void Flip()
        {
            if (selected == null) return;
            showingBack = !showingBack;
            RefreshCard();
            GameManager.Instance?.Audio.Play(SfxId.Paper, 0.5f);
        }

        private void RefreshCard()
        {
            if (selected == null)
            {
                cardTitle.text = string.Empty;
                cardText.text = "Nothing collected.";
                return;
            }
            cardTitle.text = selected.Title + (showingBack ? "  (reverse)" : string.Empty);
            cardText.text = showingBack ? selected.BackText : selected.FrontText;
        }
    }
}
