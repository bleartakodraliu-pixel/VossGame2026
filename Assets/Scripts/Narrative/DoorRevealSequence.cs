using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.Interaction;

namespace Voss.Narrative
{
    /// <summary>
    /// Opens (or just unlocks) the way to the next room once its test is solved. Waits for a progression
    /// flag, optionally for the reward card to be taken and for the player to pass a gate zone (back in
    /// the hall), then plays a short reveal: an optional blackout, the door unlocks or rises, the corridor
    /// beyond lights up lamp by lamp, Voss comments. A fallback timer covers a card that is never taken.
    /// </summary>
    public class DoorRevealSequence : MonoBehaviour
    {
        private static readonly Color LockedLamp = new Color(1f, 0.12f, 0.08f);
        private static readonly Color OpenLamp = new Color(0.75f, 0.85f, 1f);

        [SerializeField] private DoorController door;
        [SerializeField] private GameFlag requiredFlag = GameFlag.None;
        [SerializeField] private string evidenceId;
        [SerializeField] private float fallbackSeconds = 45f;
        [SerializeField] private float delay = 1.2f;
        [SerializeField] private List<Light> revealLights = new List<Light>();
        [SerializeField] private List<Light> blackoutLights = new List<Light>();
        [SerializeField] private Light doorLamp;
        [SerializeField] private string vossLine;
        [SerializeField] private bool openOnReveal = true;
        [SerializeField] private PlayerTriggerZone gateZone;

        private bool done;
        private bool gatePassed;
        private float flagTime = -1f;

        public void Configure(DoorController target, GameFlag flag, string evidence, List<Light> reveal, List<Light> blackout, Light lamp, string line)
        {
            door = target;
            requiredFlag = flag;
            evidenceId = evidence;
            revealLights = reveal ?? new List<Light>();
            blackoutLights = blackout ?? new List<Light>();
            doorLamp = lamp;
            vossLine = line;
        }

        /// <summary>Seconds between the flag (and card) and the door moving. 0 = at once.</summary>
        public void SetDelay(float seconds) { delay = Mathf.Max(0f, seconds); }

        /// <summary>Hub variant: only unlock, and wait until the player walks through <paramref name="gate"/>.</summary>
        public void ConfigureGate(PlayerTriggerZone gate, bool openDoor)
        {
            gateZone = gate;
            openOnReveal = openDoor;
        }

        private void Start()
        {
            if (gateZone != null)
            {
                gateZone.Once = false;
                gateZone.OnPlayerEnter.AddListener(() => gatePassed = true);
            }
            foreach (var l in revealLights) if (l != null) l.enabled = false;
            if (doorLamp != null) doorLamp.color = LockedLamp;

            // Scene reloaded after the reveal already happened: restore without the cinematic.
            var gm = GameManager.Instance;
            if (gm != null && GameFlags.Check(gm.State, requiredFlag) && HasEvidence(gm))
            {
                done = true;
                if (doorLamp != null) doorLamp.color = OpenLamp;
                foreach (var l in revealLights) if (l != null) l.enabled = true;
                if (door != null) { door.UnlockManually(); if (openOnReveal) door.Open(); }
            }
        }

        private bool HasEvidence(GameManager gm) => string.IsNullOrEmpty(evidenceId) || gm.Evidence.Has(evidenceId);

        private void Update()
        {
            if (done) return;
            var gm = GameManager.Instance;
            if (gm == null || !GameFlags.Check(gm.State, requiredFlag)) return;
            if (flagTime < 0f) { flagTime = Time.time; gatePassed = false; }
            if (!HasEvidence(gm) && Time.time - flagTime < fallbackSeconds) return;
            if (gateZone != null && !gatePassed) return;
            done = true;
            StartCoroutine(Reveal());
        }

        private IEnumerator Reveal()
        {
            var gm = GameManager.Instance;
            yield return new WaitForSeconds(delay);

            var wasOn = new List<Light>();
            if (blackoutLights.Count > 0)
            {
                gm?.Audio.Play(SfxId.PowerDown, 0.8f);
                foreach (var l in blackoutLights) if (l != null && l.enabled) wasOn.Add(l);
                for (int i = 0; i < 4; i++)
                {
                    SetLights(wasOn, i % 2 == 1);
                    yield return new WaitForSeconds(Random.Range(0.05f, 0.14f));
                }
                SetLights(wasOn, false);
                yield return new WaitForSeconds(0.7f);
                if (door != null) gm?.Audio.PlayAt(SfxId.Whisper, door.transform.position + Vector3.up * 1.6f, 1f);
                yield return new WaitForSeconds(1.6f);
            }

            if (door != null)
            {
                gm?.Audio.PlayAt(SfxId.MetalMechanism, door.transform.position, 1f);
                if (doorLamp != null) doorLamp.color = OpenLamp;
                yield return new WaitForSeconds(0.4f);
                door.UnlockManually();
                if (openOnReveal) door.Open();
                else gm?.Audio.PlayAt(SfxId.DoorLock, door.transform.position + Vector3.up * 1.5f, 1f);
            }
            yield return new WaitForSeconds(0.7f);

            foreach (var l in revealLights)
            {
                if (l == null) continue;
                l.enabled = true;
                gm?.Audio.PlayAt(SfxId.Click, l.transform.position, 0.6f);
                yield return new WaitForSeconds(0.35f);
            }

            if (wasOn.Count > 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    SetLights(wasOn, i % 2 == 0);
                    yield return new WaitForSeconds(Random.Range(0.06f, 0.12f));
                }
                SetLights(wasOn, true);
            }

            if (!string.IsNullOrEmpty(vossLine)) gm?.VossSays(vossLine, 3.8f);
        }

        private static void SetLights(List<Light> lights, bool on)
        {
            foreach (var l in lights) if (l != null) l.enabled = on;
        }
    }
}
