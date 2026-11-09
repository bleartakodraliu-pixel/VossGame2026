using System.Collections;
using UnityEngine;
using Voss.Core;
using Voss.Interaction;
using Voss.Player;
using Voss.UI;

namespace Voss.Puzzles
{
    /// <summary>World-space terminal that opens the shared keypad UI and routes the answer to a puzzle.</summary>
    public class KeypadController : MonoBehaviour, IInteractable
    {
        [SerializeField] private PuzzleBase target;
        [SerializeField] private string prompt = "Enter Code";
        [SerializeField] private string title = "ENTER CODE";
        [SerializeField] [TextArea] private string subtitle;
        [SerializeField] private int digits = 4;
        [SerializeField] private string wrongFeedback = "REJECTED";
        [SerializeField] private string solvedPrompt = "Terminal idle";
        [SerializeField] private bool showAttemptsOnOpen = true;

        public void Configure(PuzzleBase puzzle, string promptText, string titleText, string subtitleText, int digitCount, string wrongText)
        {
            target = puzzle;
            prompt = promptText;
            title = titleText;
            subtitle = subtitleText;
            digits = digitCount;
            wrongFeedback = wrongText;
        }

        public string GetInteractionPrompt()
        {
            if (target != null && target.IsSolved) return solvedPrompt;
            return prompt;
        }

        public bool CanInteract() => true;

        public void Interact(PlayerInteractor player)
        {
            var ui = UIManager.Instance;
            var gm = GameManager.Instance;
            if (ui == null || target == null) return;

            if (target.IsSolved)
            {
                gm?.Subtitles.Show("The mechanism has already accepted its answer.", 2.5f);
                return;
            }
            if (gm != null && !gm.State.PuzzleInputAllowed)
            {
                gm.Subtitles.Show("Input disabled.", 2f);
                return;
            }

            if (showAttemptsOnOpen && gm != null) ui.ShowAttempts(gm.State.AttemptsRemaining, 4f);
            ui.Keypad.Open(title, subtitle, digits, OnSubmit, null);
        }

        private void OnSubmit(string value)
        {
            var ui = UIManager.Instance;
            if (ui == null || target == null) return;

            bool ok = target.Submit(value);
            if (ok)
            {
                ui.Keypad.ShowFeedback("ACCEPTED", false);
                ui.Keypad.Lock();
                StartCoroutine(CloseAfter(0.9f));
            }
            else
            {
                var gm = GameManager.Instance;
                if (gm != null && !gm.State.PuzzleInputAllowed) { ui.Keypad.Close(); return; }
                ui.Keypad.ShowFeedback(wrongFeedback, true);
            }
        }

        private IEnumerator CloseAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            UIManager.Instance?.Keypad.Close();
        }
    }
}
