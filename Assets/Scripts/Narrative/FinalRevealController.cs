using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.Player;
using Voss.UI;

namespace Voss.Narrative
{
    /// <summary>
    /// Plays after the right answer, in the final theatre: the house lights go down, a projector lights
    /// Voss's silhouette inside the empty glass chamber while an audience that is not there applauds,
    /// Voss explains, the projector dies and the chamber is empty. Then his final message, and the
    /// victory screen (Play Again / Main Menu). The game is over, and it says so.
    /// </summary>
    public class FinalRevealController : MonoBehaviour
    {
        public const string FinalMessage = "The greatest illusion is not what you see.\nIt's what you believe.";

        [SerializeField] private Light projectorLight;
        [SerializeField] private GameObject chamberProjection;
        [SerializeField] private Transform chamber;
        [SerializeField] private List<Light> houseLights = new List<Light>();
        [SerializeField] private VideoInvestigationController video;
        [SerializeField] private FirstPersonController player;

        public static readonly string[] VossDialogue =
        {
            "For years, everyone asked me the same question.",
            "How did Voss escape?",
            "Nobody asked whether I entered.",
            "They applauded a beam of light in an empty glass box.",
            "I was testing whether you could stop believing the story I gave you.",
        };

        private bool played;

        public void Configure(Light projector, GameObject projection, Transform glassChamber, List<Light> house,
            VideoInvestigationController videoController, FirstPersonController fps)
        {
            projectorLight = projector;
            chamberProjection = projection;
            chamber = glassChamber;
            houseLights = house;
            video = videoController;
            player = fps;
        }

        private void Start()
        {
            if (projectorLight != null) projectorLight.enabled = false;
            SetProjection(false);
            // Scene reloaded after the end: show the end again rather than replaying it.
            var gm = GameManager.Instance;
            if (gm != null && gm.State.FinalTruthSolved)
            {
                played = true;
                ShowVictory();
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
            if (player == null) player = FindAnyObjectByType<FirstPersonController>();
            if (player != null) player.SetControlLocked(true);
            if (ui != null) { ui.CloseGameplayModals(); ui.HideObjective(); }
            video?.ShowFinalFrame();
            Vector3 focus = chamber != null ? chamber.position + Vector3.up * 1.4f : transform.position;
            var look = StartCoroutine(KeepLooking(focus));

            // House lights down.
            gm?.Audio.StopMusic(1.5f);
            gm?.Audio.Play(SfxId.PowerDown, 0.6f);
            var baseIntensity = new Dictionary<Light, float>();
            foreach (var l in houseLights) if (l != null) baseIntensity[l] = l.intensity;
            for (float t = 0f; t < 1.5f; t += Time.deltaTime)
            {
                float k = 1f - Mathf.SmoothStep(0f, 1f, t / 1.5f) * 0.85f;
                foreach (var kv in baseIntensity) kv.Key.intensity = kv.Value * k;
                yield return null;
            }
            yield return new WaitForSeconds(0.8f);

            // The projector: a silhouette appears in the empty glass, and the house applauds it.
            gm?.Audio.Play(SfxId.Projector, 0.9f);
            if (projectorLight != null) projectorLight.enabled = true;
            for (int i = 0; i < 6; i++)
            {
                SetProjection(i % 2 == 0);
                yield return new WaitForSeconds(Random.Range(0.08f, 0.2f));
            }
            SetProjection(true);
            gm?.Audio.Play(SfxId.Applause, 0.8f);
            yield return new WaitForSeconds(2.5f);

            if (gm != null)
            {
                foreach (var line in VossDialogue)
                {
                    float dur = Mathf.Clamp(1.4f + line.Length * 0.055f, 2.4f, 5.5f);
                    gm.VossSays(line, dur);
                    yield return new WaitForSeconds(dur + 0.5f);
                }
            }

            // The projector dies. The chamber was always empty.
            gm?.Audio.Play(SfxId.PowerDown, 0.8f);
            for (int i = 0; i < 5; i++)
            {
                SetProjection(i % 2 == 1);
                if (projectorLight != null) projectorLight.enabled = i % 2 == 1;
                yield return new WaitForSeconds(Random.Range(0.06f, 0.16f));
            }
            SetProjection(false);
            if (projectorLight != null) projectorLight.enabled = false;
            yield return new WaitForSeconds(1.6f);

            // Voss's final message.
            gm?.Audio.Play(SfxId.VossVoice, 0.9f);
            ui?.ShowMessage(FinalMessage, "— E. VOSS", 0f, 0.88f, UIFactory.Brass);
            yield return new WaitForSeconds(6.5f);
            ui?.HideMessage();

            StopCoroutine(look);
            gm?.State.MarkFinalTruthSolved();
            ShowVictory();
        }

        private IEnumerator KeepLooking(Vector3 point)
        {
            while (true)
            {
                if (player != null) player.LookTowards(point, 2.5f);
                yield return null;
            }
        }

        private void ShowVictory()
        {
            var gm = GameManager.Instance;
            gm?.CompleteGame();
            if (player != null) player.SetControlLocked(true);
            gm?.Audio.Play(SfxId.Confirm, 0.7f);
            gm?.Audio.PlayMusic(MusicId.Ending, 3f);
            UIManager.Instance?.ShowVictory(
                () => GameManager.Instance?.RestartExperiment(),
                () => GameManager.Instance?.ReturnToMainMenu());
        }

        private void SetProjection(bool on)
        {
            if (chamberProjection != null && chamberProjection.activeSelf != on) chamberProjection.SetActive(on);
        }
    }
}
