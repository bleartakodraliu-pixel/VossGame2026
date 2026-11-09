using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.Interaction;
using Voss.Puzzles;
using Voss.UI;

namespace Voss.Narrative
{
    /// <summary>
    /// Scene 02 opening: elevator doors open, the player steps out, the elevator seals,
    /// emergency lighting turns red and the experiment terminal activates.
    /// </summary>
    public class LabEntranceSequence : MonoBehaviour
    {
        [SerializeField] private Transform elevatorDoorLeft;
        [SerializeField] private Transform elevatorDoorRight;
        [SerializeField] private PlayerTriggerZone exitElevatorZone;
        [SerializeField] private List<Light> hallLights = new List<Light>();
        [SerializeField] private List<Light> emergencyLights = new List<Light>();
        [SerializeField] private TerminalController experimentTerminal;
        [SerializeField] private float doorSlide = 0.9f;

        private readonly Dictionary<Light, Color> baseColor = new Dictionary<Light, Color>();
        private bool played;
        private Vector3 leftClosed;
        private Vector3 rightClosed;

        public static readonly string[] MonitorText =
        {
            "VOSS EXPERIMENT ACTIVATED",
            "SUBJECT DETECTED",
            "THREE TESTS",
            "FIVE FAILURES ALLOWED",
            "TRUST NOTHING"
        };

        public void Configure(Transform left, Transform right, PlayerTriggerZone zone, List<Light> hall, List<Light> emergency, TerminalController terminal)
        {
            elevatorDoorLeft = left;
            elevatorDoorRight = right;
            exitElevatorZone = zone;
            hallLights = hall;
            emergencyLights = emergency;
            experimentTerminal = terminal;
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            var ui = UIManager.Instance;
            if (ui != null) ui.EvidenceEnabled = true;
            foreach (var l in hallLights) if (l != null) baseColor[l] = l.color;
            foreach (var l in emergencyLights) if (l != null) l.enabled = false;
            if (elevatorDoorLeft != null) leftClosed = elevatorDoorLeft.localPosition;
            if (elevatorDoorRight != null) rightClosed = elevatorDoorRight.localPosition;

            if (exitElevatorZone != null) exitElevatorZone.OnPlayerEnter.AddListener(OnPlayerLeftElevator);

            if (gm != null && gm.State.CurrentStage != GameStage.Introduction)
            {
                // Scene re-entered mid-game (editor): skip the cinematic.
                SetDoors(0f);
                played = true;
                experimentTerminal?.SetInstant(MonitorText);
                return;
            }
            StartCoroutine(OpenElevator());
        }

        private void SetDoors(float amount)
        {
            if (elevatorDoorLeft != null) elevatorDoorLeft.localPosition = leftClosed + Vector3.left * (doorSlide * amount);
            if (elevatorDoorRight != null) elevatorDoorRight.localPosition = rightClosed + Vector3.right * (doorSlide * amount);
        }

        private IEnumerator OpenElevator()
        {
            var gm = GameManager.Instance;
            yield return new WaitForSeconds(1.2f);
            gm?.Audio.Play(SfxId.Elevator, 0.8f);
            yield return Slide(0f, 1f, 1.8f);
        }

        private IEnumerator Slide(float from, float to, float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                SetDoors(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds)));
                yield return null;
            }
            SetDoors(to);
        }

        private void OnPlayerLeftElevator()
        {
            if (played) return;
            played = true;
            StartCoroutine(ActivationRoutine());
        }

        private IEnumerator ActivationRoutine()
        {
            var gm = GameManager.Instance;
            yield return new WaitForSeconds(0.8f);

            // Elevator closes and locks.
            gm?.Audio.Play(SfxId.Elevator, 0.7f);
            yield return Slide(1f, 0f, 1.4f);
            gm?.Audio.Play(SfxId.DoorLock, 1f);
            yield return new WaitForSeconds(0.6f);

            // Emergency red.
            gm?.Audio.Play(SfxId.PowerDown, 0.7f);
            foreach (var l in hallLights) if (l != null) l.enabled = false;
            foreach (var l in emergencyLights) if (l != null) l.enabled = true;
            yield return new WaitForSeconds(1.4f);

            // Monitor activates.
            gm?.Audio.Play(SfxId.TerminalActivate);
            experimentTerminal?.SetLines(MonitorText);
            gm?.State.SetStage(GameStage.Perception);
            UIManager.Instance?.ShowAttempts(gm != null ? gm.State.AttemptsRemaining : GameState.MaxAttempts, 5f);
            yield return new WaitForSeconds(7f);

            // Cold working light returns; emergency lights stay as low red accents.
            foreach (var l in hallLights)
            {
                if (l == null) continue;
                l.enabled = true;
            }
            foreach (var l in emergencyLights) if (l != null) l.intensity *= 0.35f;
            gm?.Audio.PlayMusic(MusicId.Drone, 4f);
            gm?.VossSays("Three doors. One of them is already open to you.", 3.5f);
        }
    }
}
