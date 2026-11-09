using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.Interaction;
using Voss.Player;
using Voss.UI;

namespace Voss.Narrative
{
    /// <summary>
    /// Scene 03: the empty theatre. Stage lights rise as the player approaches, a silhouette appears in
    /// the chamber glass and is gone when they look back, the final recording plays, the exit opens.
    /// </summary>
    public class TheatreEndingController : MonoBehaviour
    {
        [SerializeField] private FirstPersonController player;
        [SerializeField] private PlayerTriggerZone approachZone;
        [SerializeField] private PlayerTriggerZone chamberZone;
        [SerializeField] private PlayerTriggerZone exitZone;
        [SerializeField] private List<Light> stageLights = new List<Light>();
        [SerializeField] private GameObject glassSilhouette;
        [SerializeField] private Light projectorLight;
        [SerializeField] private GameObject exitSign;
        [SerializeField] private DoorController exitDoor;
        [SerializeField] private TextMesh completeSign;

        public static readonly string[] FinalDialogue =
        {
            "You came here looking for a man.",
            "But I wanted you to question an idea.",
            "The trick was never disappearing.",
            "The trick was making the world keep looking for me."
        };

        public static readonly string[] EndingText =
        {
            "THE VOSS EXPERIMENT",
            "YOU DID NOT FIND VOSS.",
            "YOU FOUND THE TRUTH."
        };

        private readonly Dictionary<Light, float> baseIntensity = new Dictionary<Light, float>();
        private bool lightsRaised;
        private bool silhouetteDone;
        private bool recordingDone;
        private bool exiting;

        public void Configure(FirstPersonController fps, PlayerTriggerZone approach, PlayerTriggerZone chamber, PlayerTriggerZone exit,
            List<Light> lights, GameObject silhouette, Light projector, GameObject sign, DoorController door, TextMesh complete)
        {
            player = fps;
            approachZone = approach;
            chamberZone = chamber;
            exitZone = exit;
            stageLights = lights;
            glassSilhouette = silhouette;
            projectorLight = projector;
            exitSign = sign;
            exitDoor = door;
            completeSign = complete;
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            var ui = UIManager.Instance;
            if (ui != null) ui.EvidenceEnabled = true;
            gm?.State.SetStage(GameStage.Ending);
            gm?.Audio.PlayMusic(MusicId.Ending, 5f);

            foreach (var l in stageLights)
            {
                if (l == null) continue;
                baseIntensity[l] = l.intensity;
                l.intensity = 0f;
            }
            if (glassSilhouette != null) glassSilhouette.SetActive(false);
            if (projectorLight != null) projectorLight.enabled = false;
            if (exitSign != null) exitSign.SetActive(false);
            if (completeSign != null) completeSign.text = string.Empty;

            if (approachZone != null) approachZone.OnPlayerEnter.AddListener(RaiseStageLights);
            if (chamberZone != null) chamberZone.OnPlayerEnter.AddListener(OnReachedChamber);
            if (exitZone != null) { exitZone.Once = false; exitZone.OnPlayerEnter.AddListener(OnExit); }
        }

        private void RaiseStageLights()
        {
            if (lightsRaised) return;
            lightsRaised = true;
            StartCoroutine(RaiseRoutine());
        }

        private IEnumerator RaiseRoutine()
        {
            var gm = GameManager.Instance;
            gm?.Audio.Play(SfxId.MetalMechanism, 0.4f);
            float t = 0f;
            const float dur = 5f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / dur);
                foreach (var l in stageLights)
                {
                    if (l == null) continue;
                    l.intensity = Mathf.Lerp(0f, baseIntensity.TryGetValue(l, out var b) ? b : 1f, k);
                }
                yield return null;
            }
        }

        private void OnReachedChamber()
        {
            if (silhouetteDone) return;
            silhouetteDone = true;
            RaiseStageLights();
            StartCoroutine(SilhouetteRoutine());
        }

        private IEnumerator SilhouetteRoutine()
        {
            var gm = GameManager.Instance;
            if (player == null) player = FindFirstObjectByType<FirstPersonController>();

            gm?.Audio.StopMusic(1.2f);
            yield return new WaitForSeconds(1.0f);

            // The silhouette is in the glass only while the player is looking at it.
            if (glassSilhouette != null) glassSilhouette.SetActive(true);
            gm?.Audio.Play(SfxId.Footstep, 0.4f);
            float shown = 0f;
            bool lookedAway = false;
            while (shown < 6f && glassSilhouette != null && player != null)
            {
                shown += Time.deltaTime;
                Vector3 toSil = glassSilhouette.transform.position - player.CameraPivot.position;
                float dot = Vector3.Dot(player.CameraPivot.forward, toSil.normalized);
                if (shown > 1.5f && dot < 0.3f) { lookedAway = true; break; }
                yield return null;
            }
            if (!lookedAway) yield return new WaitForSeconds(0.3f);
            if (glassSilhouette != null) glassSilhouette.SetActive(false);

            // Nobody there. When they look back, nothing in the glass either.
            yield return new WaitForSeconds(2.0f);
            StartCoroutine(FinalRecording());
        }

        private IEnumerator FinalRecording()
        {
            var gm = GameManager.Instance;
            gm?.Audio.Play(SfxId.Projector, 1f);
            if (projectorLight != null) projectorLight.enabled = true;
            yield return new WaitForSeconds(1.5f);

            if (gm != null)
            {
                foreach (var line in FinalDialogue)
                {
                    float dur = Mathf.Clamp(1.6f + line.Length * 0.06f, 2.5f, 6f);
                    gm.VossSays(line, dur);
                    yield return new WaitForSeconds(dur + 0.8f);
                }
            }

            // Recording ends. Exit sign. EXPERIMENT 001 — COMPLETE.
            if (projectorLight != null) projectorLight.enabled = false;
            gm?.Audio.Play(SfxId.PowerDown, 0.5f);
            yield return new WaitForSeconds(1.2f);

            if (exitSign != null) exitSign.SetActive(true);
            gm?.Audio.Play(SfxId.TerminalActivate, 0.6f);
            if (completeSign != null) completeSign.text = "EXPERIMENT 001 — COMPLETE";
            UIManager.Instance?.ShowMessage("EXPERIMENT 001 — COMPLETE", "", 5f, 0f, UIFactory.Brass);
            yield return new WaitForSeconds(1.5f);

            if (exitDoor != null) { exitDoor.UnlockManually(); exitDoor.Open(); }
            recordingDone = true;
            gm?.Audio.PlayMusic(MusicId.Ending, 4f);
        }

        private void OnExit()
        {
            if (!recordingDone || exiting) return;
            exiting = true;
            StartCoroutine(ExitRoutine());
        }

        private IEnumerator ExitRoutine()
        {
            var gm = GameManager.Instance;
            var ui = UIManager.Instance;
            if (player != null) player.SetControlLocked(true);
            if (ui != null) yield return ui.FadeTo(1f, 2.5f);
            gm?.Audio.StopMusic(3f);
            yield return new WaitForSeconds(1.0f);

            if (ui != null)
            {
                ui.ShowMessage(EndingText[0], "", 0f, 1f, UIFactory.Brass);
                yield return new WaitForSeconds(3.5f);
                ui.ShowMessage(EndingText[1], "", 0f, 1f);
                yield return new WaitForSeconds(3.5f);
                ui.ShowMessage(EndingText[2], "", 0f, 1f);
                yield return new WaitForSeconds(4.5f);
                ui.HideMessage();
            }
            gm?.CompleteGame();
            yield return new WaitForSeconds(1.0f);
            gm?.ReturnToMainMenu();
        }
    }
}
