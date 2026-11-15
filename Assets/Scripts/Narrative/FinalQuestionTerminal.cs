using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.Interaction;
using Voss.Player;
using Voss.UI;

namespace Voss.Narrative
{
    /// <summary>The last terminal, in front of the glass chamber: HOW DID VOSS DISAPPEAR?</summary>
    public class FinalQuestionTerminal : MonoBehaviour, IInteractable
    {
        [SerializeField] private FinalRevealController reveal;
        [SerializeField] private VideoInvestigationController video;
        [SerializeField] private TextMesh screen;

        private bool armed;
        private bool answered;

        public static readonly string Question = "HOW DID VOSS DISAPPEAR?";
        public static readonly string[] Options =
        {
            "He escaped through a hidden tunnel beneath the stage.",
            "He slipped out through a secret door in the chamber.",
            "A trap floor dropped him under the stage during the blackout.",
            "Voss never entered the glass chamber. The audience only saw a projection.",
        };

        public static bool IsCorrect(int index) => index == Answers.FinalCorrectChoiceIndex;

        public void Configure(FinalRevealController revealController, VideoInvestigationController videoController, TextMesh textScreen)
        {
            reveal = revealController;
            video = videoController;
            screen = textScreen;
        }

        private void Start()
        {
            if (screen != null) screen.text = string.Empty;
        }

        public void Arm()
        {
            armed = true;
            if (screen != null) screen.text = "FINAL QUESTION\n\n" + Question;
        }

        public string GetInteractionPrompt() => armed && !answered ? "Answer" : (answered ? "Terminal idle" : "Screen offline");
        public bool CanInteract() => true;

        public void Interact(PlayerInteractor player)
        {
            var gm = GameManager.Instance;
            var ui = UIManager.Instance;
            if (!armed)
            {
                gm?.Subtitles.Show("Dark. Whatever this asks, it is not asking yet.", 2.2f);
                return;
            }
            if (answered || ui == null) return;

            ui.Choice.Open("THE REAL FINAL TEST", Question, Options, OnChoice, true, null);
        }

        private void OnChoice(int index)
        {
            var gm = GameManager.Instance;
            var ui = UIManager.Instance;
            if (ui == null) return;

            if (IsCorrect(index))
            {
                answered = true;
                ui.Choice.Close();
                if (screen != null) screen.text = "HE NEVER ENTERED.";
                reveal?.Play();
                return;
            }

            // No attempt cost: this is the lesson, not the test. Point back to the evidence.
            gm?.Audio.Play(SfxId.Reject, 0.8f);
            ui.Choice.Close();
            string hint = video != null && !video.ViewedCameraB
                ? "You are still asking how he got out. Watch the second screen: Camera B."
                : "You are still asking how he got out. Ask whether he was ever in.";
            gm?.VossSays(hint, 4f);
        }
    }
}
