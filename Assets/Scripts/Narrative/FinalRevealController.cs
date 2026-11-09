using System.Collections;
using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.Interaction;
using Voss.Player;
using Voss.UI;

namespace Voss.Narrative
{
    /// <summary>
    /// Plays after "He Never Entered": black screen, distant applause, Voss's prerecorded message
    /// (subtitled over a projector glow), then the final laboratory exit unlocks.
    /// </summary>
    public class FinalRevealController : MonoBehaviour
    {
        [SerializeField] private DoorController exitDoor;
        [SerializeField] private Light projectorLight;
        [SerializeField] private GameObject exitSign;
        [SerializeField] private FirstPersonController player;

        public static readonly string[] VossDialogue =
        {
            "For years, everyone asked me the same question.",
            "How did Voss escape?",
            "Nobody asked whether I entered.",
            "That was the illusion.",
            "People believe illusions are created by hiding things.",
            "They are not.",
            "The strongest illusion happens when I give you half of the information...",
            "...and your mind creates the other half.",
            "You entered this place believing you were searching for the secret of my disappearance.",
            "You weren't.",
            "I was testing whether you could stop believing the story I gave you."
        };

        private bool played;

        public void Configure(DoorController exit, Light projector, GameObject sign, FirstPersonController fps)
        {
            exitDoor = exit;
            projectorLight = projector;
            exitSign = sign;
            player = fps;
        }

        private void Start()
        {
            if (projectorLight != null) projectorLight.enabled = false;
            if (exitSign != null) exitSign.SetActive(false);
            var gm = GameManager.Instance;
            if (gm != null && gm.State.FinalTruthSolved)
            {
                played = true;
                OpenExit();
            }
        }

        public void Play()
        {
            if (played) return;
            played = true;
            StartCoroutine(RevealRoutine());
        }

        private IEnumerator RevealRoutine()
        {
            var gm = GameManager.Instance;
            var ui = UIManager.Instance;
            if (player == null) player = FindFirstObjectByType<FirstPersonController>();
            if (player != null) player.SetControlLocked(true);
            if (ui != null) ui.CloseGameplayModals();

            gm?.Audio.StopMusic(1.5f);
            if (ui != null) yield return ui.FadeTo(1f, 2.0f);
            yield return new WaitForSeconds(1.0f);

            gm?.Audio.Play(SfxId.Applause, 0.9f);
            yield return new WaitForSeconds(3.0f);

            gm?.Audio.Play(SfxId.Projector, 0.8f);
            if (projectorLight != null) projectorLight.enabled = true;
            yield return new WaitForSeconds(1.2f);

            // Slow, controlled delivery over black.
            if (gm != null)
            {
                foreach (var line in VossDialogue)
                {
                    float dur = Mathf.Clamp(1.6f + line.Length * 0.06f, 2.2f, 6.5f);
                    gm.VossSays(line, dur);
                    yield return new WaitForSeconds(dur + 0.7f);
                }
            }

            gm?.State.MarkFinalTruthSolved();
            yield return new WaitForSeconds(1.0f);

            OpenExit();
            if (ui != null) yield return ui.FadeTo(0f, 2.0f);
            if (player != null) player.SetControlLocked(false);
            gm?.Audio.Play(SfxId.DoorOpen, 0.8f);
            gm?.Subtitles.Show("The final door is open.", 3f);
            gm?.Audio.PlayMusic(MusicId.Ending, 6f);
        }

        private void OpenExit()
        {
            if (exitSign != null) exitSign.SetActive(true);
            if (exitDoor != null)
            {
                exitDoor.UnlockManually();
                exitDoor.Open();
            }
        }
    }
}
