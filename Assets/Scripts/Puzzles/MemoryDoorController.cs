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
    /// Keypad beside the MEMORY door. Asks for the number seen before entering the laboratory.
    /// Wrong answers never cost an attempt; the player is pointed at their evidence instead.
    /// </summary>
    public class MemoryDoorController : MonoBehaviour, IInteractable
    {
        [SerializeField] private DoorController memoryDoor;
        [SerializeField] private string question = "What number did you see before entering the laboratory?";

        public void Configure(DoorController door) { memoryDoor = door; }

        public static bool IsCorrect(string input) => (input ?? string.Empty).Trim() == Answers.TicketCode;

        public string GetInteractionPrompt()
        {
            var gm = GameManager.Instance;
            if (gm == null) return "Use keypad";
            if (gm.State.RememberedTicketCode) return "Keypad idle";
            if (!gm.State.MemoryUnlocked) return "Locked";
            return "Use keypad";
        }

        public bool CanInteract() => true;

        public void Interact(PlayerInteractor player)
        {
            var gm = GameManager.Instance;
            var ui = UIManager.Instance;
            if (gm == null || ui == null) return;

            if (gm.State.RememberedTicketCode)
            {
                if (memoryDoor != null && memoryDoor.State == DoorState.Locked) memoryDoor.UnlockManually();
                gm.Subtitles.Show("The door remembers you.", 2f);
                return;
            }
            if (!gm.State.MemoryUnlocked)
            {
                gm.Audio.PlayAt(SfxId.DoorLock, transform.position, 0.8f);
                gm.Subtitles.Show("Sealed. REASON has not been completed.", 2.5f);
                return;
            }

            ui.Keypad.Open("MEMORY", question, 4, OnSubmit, null);
        }

        private void OnSubmit(string value)
        {
            var gm = GameManager.Instance;
            var ui = UIManager.Instance;
            if (gm == null || ui == null) return;

            if (IsCorrect(value))
            {
                gm.State.MarkTicketRemembered();
                gm.Audio.Play(SfxId.Confirm);
                ui.Keypad.ShowFeedback("REMEMBERED", false);
                ui.Keypad.Lock();
                StartCoroutine(OpenDoorAfter(0.8f));
            }
            else
            {
                // Deliberately no attempt deduction: this is a reminder, not a test.
                gm.Audio.Play(SfxId.Click, 0.8f);
                ui.Keypad.ShowFeedback("That is not what you were given.\nReview your evidence (TAB).", true);
            }
        }

        private IEnumerator OpenDoorAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            UIManager.Instance?.Keypad.Close();
            if (memoryDoor != null)
            {
                memoryDoor.UnlockManually();
                memoryDoor.Open();
            }
        }
    }
}
