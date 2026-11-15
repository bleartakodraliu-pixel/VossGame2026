using UnityEngine;
using UnityEngine.UI;

namespace Voss.UI
{
    /// <summary>Full-screen black card for narrated story text (prologue, scene transitions). Sits under the fade.</summary>
    public class StoryPanel
    {
        private readonly GameObject root;
        private readonly Text text;
        private readonly Text hint;

        public bool IsOpen => root.activeSelf;

        public StoryPanel(Transform parent)
        {
            var panel = UIFactory.Panel(parent, "StoryPanel", Color.black);
            panel.GetComponent<Image>().raycastTarget = false;
            root = panel.gameObject;

            text = UIFactory.Label(panel, "StoryText", "", 34, UIFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Normal);
            UIFactory.Center(text.rectTransform, new Vector2(1300f, 320f));

            hint = UIFactory.Label(panel, "SkipHint", "", 15, UIFactory.Dim, TextAnchor.LowerRight);
            UIFactory.Place(hint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 30f), new Vector2(400f, 30f));

            root.SetActive(false);
        }

        public void Open(string skipHint)
        {
            SetText(string.Empty, 0f);
            hint.text = skipHint ?? string.Empty;
            root.SetActive(true);
        }

        public void Close()
        {
            root.SetActive(false);
        }

        public void SetText(string value, float alpha)
        {
            text.text = value ?? string.Empty;
            var c = text.color;
            c.a = Mathf.Clamp01(alpha);
            text.color = c;
        }
    }
}
