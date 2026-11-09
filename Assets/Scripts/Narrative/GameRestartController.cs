using System.Collections;
using UnityEngine;
using Voss.Core;
using Voss.Player;
using Voss.UI;

namespace Voss.Narrative
{
    /// <summary>
    /// ESC pause menu for gameplay scenes: Resume / Restart Experiment / Main Menu.
    /// Restart goes through GameManager so every system resets from one place.
    /// </summary>
    public class GameRestartController : MonoBehaviour
    {
        private PlayerInteractor player;
        private int modalClosedFrame = -1;
        private bool opening;

        private void Start()
        {
            player = FindFirstObjectByType<PlayerInteractor>();
        }

        private void Update()
        {
            var ui = UIManager.Instance;
            var gm = GameManager.Instance;
            if (ui == null || gm == null) return;

            // Remember frames in which ESC was used for something else, so one press does one thing.
            if (ui.IsModalOpen) { modalClosedFrame = Time.frameCount; return; }
            if (player != null && player.Inspection != null)
            {
                if (player.Inspection.IsInspecting) { modalClosedFrame = Time.frameCount; return; }
                if (player.Inspection.LastEndFrame == Time.frameCount) return;
            }
            if (modalClosedFrame == Time.frameCount) return;
            if (opening || gm.Transitions.IsTransitioning) return;
            if (!VossInput.BackPressed()) return;

            StartCoroutine(OpenNextFrame());
        }

        private IEnumerator OpenNextFrame()
        {
            opening = true;
            yield return null; // avoid the same ESC press being consumed by UIManager as "close"
            var ui = UIManager.Instance;
            var gm = GameManager.Instance;
            if (ui != null && gm != null && !ui.IsModalOpen)
            {
                ui.Choice.Open("PAUSED", "", new[] { "RESUME", "RESTART EXPERIMENT", "RETURN TO MAIN MENU" }, i =>
                {
                    ui.Choice.Close();
                    if (i == 1) gm.RestartExperiment();
                    else if (i == 2) gm.ReturnToMainMenu();
                }, true, null, new Color(0f, 0f, 0f, 0.7f));
            }
            opening = false;
        }

        public void Restart() => GameManager.Instance?.RestartExperiment();
        public void ReturnToMenu() => GameManager.Instance?.ReturnToMainMenu();
    }
}
