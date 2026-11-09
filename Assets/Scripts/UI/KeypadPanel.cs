using System;
using UnityEngine;
using UnityEngine.UI;
using Voss.Audio;
using Voss.Core;

namespace Voss.UI
{
    /// <summary>Modal numeric keypad used by every code entry in the game.</summary>
    public class KeypadPanel
    {
        private readonly GameObject root;
        private readonly Text title;
        private readonly Text subtitle;
        private readonly Text display;
        private readonly Text feedback;
        private readonly Button enterButton;

        private string buffer = string.Empty;
        private int maxDigits = 4;
        private Action<string> onSubmit;
        private Action onCancel;
        private bool locked;

        public bool IsOpen => root.activeSelf;

        public KeypadPanel(Transform parent)
        {
            var panel = UIFactory.Panel(parent, "KeypadPanel", new Color(0f, 0f, 0f, 0.75f));
            root = panel.gameObject;

            var card = UIFactory.Panel(panel, "Card", UIFactory.PanelBg, false);
            UIFactory.Center(card, new Vector2(420f, 620f));

            title = UIFactory.Label(card, "Title", "ENTER CODE", 26, UIFactory.Brass, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(380f, 40f));

            subtitle = UIFactory.Label(card, "Subtitle", "", 15, UIFactory.Dim);
            UIFactory.Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(380f, 60f));

            var displayBg = UIFactory.Panel(card, "DisplayBg", new Color(0.01f, 0.02f, 0.02f, 1f), false);
            UIFactory.Place(displayBg, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(340f, 60f));
            display = UIFactory.Label(displayBg, "Display", "", 34, UIFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(display.rectTransform);

            var grid = UIFactory.Empty(card, "Grid");
            UIFactory.Place(grid, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -215f), new Vector2(340f, 300f));
            UIFactory.Grid(grid, new Vector2(100f, 64f), new Vector2(12f, 12f), 3);

            string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "CLR", "0", "OK" };
            foreach (var k in keys)
            {
                string key = k;
                if (key == "OK")
                {
                    enterButton = UIFactory.Button(grid, "Key_OK", "OK", 20, Submit, new Color(0.18f, 0.15f, 0.08f, 1f));
                }
                else if (key == "CLR")
                {
                    UIFactory.Button(grid, "Key_CLR", "CLR", 18, Clear);
                }
                else
                {
                    UIFactory.Button(grid, "Key_" + key, key, 24, () => Append(key));
                }
            }

            feedback = UIFactory.Label(card, "Feedback", "", 15, UIFactory.Danger);
            UIFactory.Place(feedback.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 62f), new Vector2(380f, 44f));

            var cancel = UIFactory.Button(card, "Cancel", "STEP BACK  (ESC)", 15, Cancel);
            UIFactory.Place(cancel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(200f, 40f));

            root.SetActive(false);
        }

        public void Open(string titleText, string subtitleText, int digits, Action<string> submit, Action cancel)
        {
            maxDigits = Mathf.Clamp(digits, 1, 8);
            onSubmit = submit;
            onCancel = cancel;
            buffer = string.Empty;
            locked = false;
            enterButton.interactable = true;
            title.text = titleText ?? "ENTER CODE";
            subtitle.text = subtitleText ?? string.Empty;
            feedback.text = string.Empty;
            RefreshDisplay();
            root.SetActive(true);
            GameManager.Instance?.Audio.Play(SfxId.TerminalActivate, 0.6f);
        }

        public void Close()
        {
            root.SetActive(false);
            onSubmit = null;
            onCancel = null;
        }

        public void ShowFeedback(string text, bool error = true)
        {
            feedback.color = error ? UIFactory.Danger : UIFactory.Accept;
            feedback.text = text ?? string.Empty;
        }

        /// <summary>Prevents further submissions (e.g. after a correct answer while the reward plays).</summary>
        public void Lock() { locked = true; enterButton.interactable = false; }

        public void HandleBack()
        {
            if (!IsOpen) return;
            Cancel();
        }

        private void Append(string digit)
        {
            if (locked) return;
            if (buffer.Length >= maxDigits) return;
            buffer += digit;
            feedback.text = string.Empty;
            RefreshDisplay();
            GameManager.Instance?.Audio.Play(SfxId.Click, 0.5f);
        }

        private void Clear()
        {
            if (locked) return;
            buffer = string.Empty;
            feedback.text = string.Empty;
            RefreshDisplay();
            GameManager.Instance?.Audio.Play(SfxId.Click, 0.4f);
        }

        private void Submit()
        {
            if (locked) return;
            if (string.IsNullOrEmpty(buffer))
            {
                ShowFeedback("Enter a value.");
                return;
            }
            var cb = onSubmit;
            cb?.Invoke(buffer);
            buffer = string.Empty;
            RefreshDisplay();
        }

        private void Cancel()
        {
            var cb = onCancel;
            Close();
            cb?.Invoke();
        }

        private void RefreshDisplay()
        {
            var s = buffer;
            while (s.Length < maxDigits) s += "_";
            display.text = string.Join(" ", s.ToCharArray());
        }
    }
}
