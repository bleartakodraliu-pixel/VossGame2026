using System;
using UnityEngine;
using UnityEngine.UI;

namespace Voss.UI
{
    /// <summary>
    /// The end of the game, said plainly: title, EXPERIMENT COMPLETED, YOU HAVE SUCCESSFULLY ESCAPED!,
    /// and two choices (play again, main menu). Fades in over the darkened theatre.
    /// </summary>
    public class VictoryPanel
    {
        private static readonly Color Success = new Color(0.55f, 0.85f, 0.6f, 1f);

        private readonly GameObject root;
        private readonly CanvasGroup group;
        private readonly Button playAgain;
        private Action onPlayAgain;
        private Action onMenu;
        private float target;

        public bool IsOpen => root.activeSelf;

        public VictoryPanel(Transform parent)
        {
            var panel = UIFactory.Panel(parent, "VictoryPanel", new Color(0.01f, 0.01f, 0.012f, 0.93f));
            root = panel.gameObject;
            group = root.AddComponent<CanvasGroup>();

            var title = UIFactory.Label(panel, "Title", "THE VOSS EXPERIMENT", 34, UIFactory.Brass, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Center(title.rectTransform, new Vector2(1200f, 60f), new Vector2(0f, 250f));

            var rule = UIFactory.Panel(panel, "Rule", new Color(UIFactory.Brass.r, UIFactory.Brass.g, UIFactory.Brass.b, 0.6f), false);
            UIFactory.Center(rule, new Vector2(420f, 2f), new Vector2(0f, 212f));
            rule.GetComponent<Image>().raycastTarget = false;

            var completed = UIFactory.Label(panel, "Completed", "EXPERIMENT COMPLETED", 66, UIFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Center(completed.rectTransform, new Vector2(1500f, 100f), new Vector2(0f, 130f));
            completed.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2f, -2f);

            var escaped = UIFactory.Label(panel, "Escaped", "YOU HAVE SUCCESSFULLY ESCAPED!", 42, Success, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Center(escaped.rectTransform, new Vector2(1500f, 70f), new Vector2(0f, 45f));

            var line = UIFactory.Label(panel, "Line", "Voss never entered the chamber. You stopped believing the story.\nThank you for playing.", 22, UIFactory.Dim);
            UIFactory.Center(line.rectTransform, new Vector2(1200f, 80f), new Vector2(0f, -35f));

            var row = UIFactory.Empty(panel, "Buttons");
            UIFactory.Center(row, new Vector2(680f, 70f), new Vector2(0f, -160f));
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 40f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            playAgain = UIFactory.Button(row, "PlayAgain", "PLAY AGAIN", 24, () => Choose(true), new Color(0.16f, 0.13f, 0.08f, 1f));
            UIFactory.Button(row, "MainMenu", "MAIN MENU", 24, () => Choose(false));

            root.SetActive(false);
        }

        public void Open(Action playAgainAction, Action menuAction)
        {
            onPlayAgain = playAgainAction;
            onMenu = menuAction;
            group.alpha = 0f;
            group.interactable = true;
            target = 1f;
            root.SetActive(true);
            playAgain.Select();
        }

        public void Close()
        {
            root.SetActive(false);
            onPlayAgain = null;
            onMenu = null;
        }

        /// <summary>Fade-in; called from UIManager.Update while open.</summary>
        public void Update()
        {
            if (!IsOpen || group.alpha >= target) return;
            group.alpha = Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime * 0.8f);
        }

        private void Choose(bool again)
        {
            var cb = again ? onPlayAgain : onMenu;
            group.interactable = false;
            Close();
            cb?.Invoke();
        }
    }
}
