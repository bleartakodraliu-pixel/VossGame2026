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
    /// Scene 01b: the abandoned Voss Theatre. The auditorium doors open slowly by themselves; footsteps
    /// cross the dark stage although nobody is there. Inside, a single spotlight snaps on over a hidden
    /// elevator. When the player steps in, the gate closes, the car sinks below the stage and a voice
    /// says "Welcome back. I've been waiting for you." The lights die, and the laboratory begins.
    /// </summary>
    public class TheatreArrivalSequence : MonoBehaviour
    {
        [SerializeField] private Transform doorLeft;
        [SerializeField] private Transform doorRight;
        [SerializeField] private PlayerTriggerZone insideZone;
        [SerializeField] private PlayerTriggerZone elevatorZone;
        [SerializeField] private Light elevatorSpot;
        [SerializeField] private Transform footstepsAt;
        [SerializeField] private Transform elevatorCar;
        [SerializeField] private Transform gate;
        [SerializeField] private Vector3 gateClosedOffset;
        [SerializeField] private FirstPersonController player;

        private bool spotOn;
        private bool descending;

        public void Configure(Transform left, Transform right, PlayerTriggerZone inside, PlayerTriggerZone elevator, Light spot,
            Transform steps, Transform car, Transform gateTransform, Vector3 gateClose, FirstPersonController fps)
        {
            doorLeft = left;
            doorRight = right;
            insideZone = inside;
            elevatorZone = elevator;
            elevatorSpot = spot;
            footstepsAt = steps;
            elevatorCar = car;
            gate = gateTransform;
            gateClosedOffset = gateClose;
            player = fps;
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            var ui = UIManager.Instance;
            if (ui != null) { ui.EvidenceEnabled = false; ui.SetCursorFree(false); }
            gm?.State.SetStage(GameStage.Introduction);
            gm?.Audio.PlayMusic(MusicId.Drone, 4f);
            if (elevatorSpot != null) elevatorSpot.enabled = false;
            if (insideZone != null) insideZone.OnPlayerEnter.AddListener(OnInside);
            if (elevatorZone != null) elevatorZone.OnPlayerEnter.AddListener(OnElevator);
            StartCoroutine(DoorsOpen());
        }

        private IEnumerator DoorsOpen()
        {
            var gm = GameManager.Instance;
            yield return new WaitForSeconds(2.5f);
            if (doorLeft != null) gm?.Audio.PlayAt(SfxId.Creak, doorLeft.position + Vector3.up, 1f);
            Quaternion l0 = doorLeft != null ? doorLeft.localRotation : Quaternion.identity;
            Quaternion r0 = doorRight != null ? doorRight.localRotation : Quaternion.identity;
            for (float t = 0f; t < 4.5f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 4.5f);
                if (doorLeft != null) doorLeft.localRotation = l0 * Quaternion.Euler(0f, -100f * k, 0f);
                if (doorRight != null) doorRight.localRotation = r0 * Quaternion.Euler(0f, 100f * k, 0f);
                yield return null;
            }
            yield return new WaitForSeconds(1.2f);
            // Somebody walks across the stage. Nobody is there.
            if (footstepsAt != null)
            {
                gm?.Audio.PlayAt(SfxId.DistantSteps, footstepsAt.position, 1f);
                yield return new WaitForSeconds(3.5f);
                gm?.Audio.PlayAt(SfxId.DistantSteps, footstepsAt.position + Vector3.right * 4f, 1f);
            }
        }

        private void OnInside()
        {
            if (spotOn) return;
            spotOn = true;
            StartCoroutine(SpotOn());
        }

        private IEnumerator SpotOn()
        {
            var gm = GameManager.Instance;
            yield return new WaitForSeconds(1.2f);
            if (elevatorSpot == null) yield break;
            gm?.Audio.PlayAt(SfxId.DoorLock, elevatorSpot.transform.position, 1f);
            for (int i = 0; i < 4; i++)
            {
                elevatorSpot.enabled = i % 2 == 0;
                yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
            }
            elevatorSpot.enabled = true;
        }

        private void OnElevator()
        {
            if (descending) return;
            descending = true;
            if (!spotOn) { spotOn = true; if (elevatorSpot != null) elevatorSpot.enabled = true; }
            StartCoroutine(Descend());
        }

        private IEnumerator Descend()
        {
            var gm = GameManager.Instance;
            var ui = UIManager.Instance;
            if (player == null) player = FindAnyObjectByType<FirstPersonController>();
            if (player != null) player.SetControlLocked(true);

            // The gate rattles shut.
            if (gate != null)
            {
                gm?.Audio.PlayAt(SfxId.DoorSlam, gate.position, 0.9f);
                Vector3 open = gate.localPosition, closed = open + gateClosedOffset;
                for (float t = 0f; t < 0.8f; t += Time.deltaTime)
                {
                    gate.localPosition = Vector3.Lerp(open, closed, Mathf.SmoothStep(0f, 1f, t / 0.8f));
                    yield return null;
                }
                gate.localPosition = closed;
            }
            yield return new WaitForSeconds(0.6f);

            gm?.Audio.Play(SfxId.Elevator, 0.9f);
            bool spoke = false, dark = false;
            Vector3 carStart = elevatorCar != null ? elevatorCar.position : Vector3.zero;
            Vector3 playerStart = player != null ? player.transform.position : Vector3.zero;
            for (float t = 0f; t < 4.2f; t += Time.deltaTime)
            {
                float drop = Mathf.Max(0f, t - 0.3f) * 0.45f;
                Vector3 shake = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * 0.004f;
                if (elevatorCar != null) elevatorCar.position = carStart + Vector3.down * drop;
                if (player != null) player.Teleport(playerStart + Vector3.down * drop + shake, player.transform.eulerAngles.y);
                if (!spoke && t > 0.9f) { spoke = true; gm?.VossSays("Welcome back. I've been waiting for you.", 3.6f); }
                if (!dark && t > 2.6f)
                {
                    dark = true;
                    gm?.Audio.Play(SfxId.PowerDown, 0.8f);
                    if (elevatorSpot != null) elevatorSpot.enabled = false;
                    if (ui != null) ui.FadeTo(1f, 0.25f);
                }
                yield return null;
            }
            yield return new WaitForSeconds(0.8f);
            gm?.Audio.StopMusic(1f);
            gm?.Transitions.LoadScene(SceneNames.Laboratory, 1.2f);
        }
    }
}
