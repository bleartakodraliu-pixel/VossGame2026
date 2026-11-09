using System.Collections;
using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.Interaction;
using Voss.Player;
using Voss.UI;

namespace Voss.Puzzles
{
    /// <summary>
    /// A diegetic text screen. Shows lines with a typewriter effect, can flash temporary text,
    /// and optionally opens the keypad for a target puzzle after its intro has played.
    /// </summary>
    public class TerminalController : MonoBehaviour, IInteractable
    {
        [SerializeField] private TextMesh screen;
        [SerializeField] private PuzzleBase target;
        [SerializeField] private string[] introLines = new string[0];
        [SerializeField] private string prompt = "Use Terminal";
        [SerializeField] private string keypadTitle = "FINAL TEST";
        [SerializeField] private string keypadSubtitle = "WHAT WAS THE FIRST THING I GAVE YOU?";
        [SerializeField] private int digits = 4;
        [SerializeField] private string wrongFeedback = "LOOK BACKWARD.";
        [SerializeField] private float charsPerSecond = 40f;

        private string[] currentLines = new string[0];
        private Coroutine typing;
        private Coroutine flashing;
        private bool introPlayed;
        private bool busy;

        public bool IntroPlayed => introPlayed;

        public void Configure(TextMesh textScreen, PuzzleBase puzzle, string[] intro, string title, string subtitle, int digitCount, string wrongText)
        {
            screen = textScreen;
            target = puzzle;
            introLines = intro ?? new string[0];
            keypadTitle = title;
            keypadSubtitle = subtitle;
            digits = digitCount;
            wrongFeedback = wrongText;
        }

        private void Awake()
        {
            if (screen != null) screen.text = string.Empty;
        }

        public string GetInteractionPrompt()
        {
            if (busy) return string.Empty;
            if (target != null && target.IsSolved) return "Read terminal";
            return prompt;
        }

        public bool CanInteract() => !busy;

        public void Interact(PlayerInteractor player)
        {
            if (busy) return;
            var gm = GameManager.Instance;
            gm?.Audio.PlayAt(SfxId.TerminalActivate, transform.position, 0.7f);

            if (!introPlayed)
            {
                StartCoroutine(PlayIntroThen(target != null && !target.IsSolved));
                return;
            }
            if (target != null && !target.IsSolved) OpenKeypad();
            else SetLines(currentLines.Length > 0 ? currentLines : introLines);
        }

        private IEnumerator PlayIntroThen(bool openKeypad)
        {
            busy = true;
            currentLines = introLines;
            yield return TypeLines(introLines, 1.1f);
            introPlayed = true;
            busy = false;
            if (openKeypad) OpenKeypad();
        }

        private void OpenKeypad()
        {
            var gm = GameManager.Instance;
            var ui = UIManager.Instance;
            if (ui == null || target == null) return;
            if (gm != null && !gm.State.PuzzleInputAllowed)
            {
                gm.Subtitles.Show("Input disabled.", 2f);
                return;
            }
            if (gm != null) ui.ShowAttempts(gm.State.AttemptsRemaining, 4f);
            ui.Keypad.Open(keypadTitle, keypadSubtitle, digits, OnSubmit, null);
        }

        private void OnSubmit(string value)
        {
            var ui = UIManager.Instance;
            if (ui == null || target == null) return;
            if (target.Submit(value))
            {
                ui.Keypad.ShowFeedback("ACCEPTED", false);
                ui.Keypad.Lock();
                StartCoroutine(CloseKeypadAfter(0.8f));
            }
            else
            {
                var gm = GameManager.Instance;
                if (gm != null && !gm.State.PuzzleInputAllowed) { ui.Keypad.Close(); return; }
                ui.Keypad.ShowFeedback(wrongFeedback, true);
            }
        }

        private IEnumerator CloseKeypadAfter(float s)
        {
            yield return new WaitForSeconds(s);
            UIManager.Instance?.Keypad.Close();
        }

        // ------------------------------------------------------------ display

        public void SetLines(string[] lines)
        {
            currentLines = lines ?? new string[0];
            if (typing != null) StopCoroutine(typing);
            typing = StartCoroutine(TypeLines(currentLines, 0.15f));
        }

        public void SetInstant(string[] lines)
        {
            currentLines = lines ?? new string[0];
            if (typing != null) StopCoroutine(typing);
            if (screen != null) screen.text = string.Join("\n", currentLines);
        }

        public void Flash(string[] lines, float seconds)
        {
            if (flashing != null) StopCoroutine(flashing);
            flashing = StartCoroutine(FlashRoutine(lines, seconds));
        }

        private IEnumerator FlashRoutine(string[] lines, float seconds)
        {
            var keep = currentLines;
            if (typing != null) StopCoroutine(typing);
            if (screen != null) screen.text = string.Join("\n", lines);
            yield return new WaitForSeconds(seconds);
            if (screen != null) screen.text = string.Join("\n", keep);
            flashing = null;
        }

        private IEnumerator TypeLines(string[] lines, float linePause)
        {
            if (screen == null) yield break;
            screen.text = string.Empty;
            var gm = GameManager.Instance;
            string acc = string.Empty;
            foreach (var line in lines)
            {
                string l = line ?? string.Empty;
                for (int i = 0; i < l.Length; i++)
                {
                    acc += l[i];
                    screen.text = acc + "_";
                    if (i % 3 == 0) gm?.Audio.Play(SfxId.Click, 0.15f);
                    yield return new WaitForSeconds(1f / Mathf.Max(charsPerSecond, 1f));
                }
                acc += "\n";
                screen.text = acc;
                yield return new WaitForSeconds(linePause);
            }
            screen.text = acc.TrimEnd('\n');
            typing = null;
        }
    }
}
