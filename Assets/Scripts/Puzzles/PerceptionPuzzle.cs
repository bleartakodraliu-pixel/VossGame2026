using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voss.Audio;
using Voss.Core;

namespace Voss.Puzzles
{
    /// <summary>
    /// Room 1: identify the single reflection with an impossible left-right relationship.
    /// Input is a display id; evaluation is pure. Presentation runs the reveal sequence.
    /// </summary>
    public class PerceptionPuzzle : PuzzleBase
    {
        [SerializeField] private List<MirrorDisplay> displays = new List<MirrorDisplay>();
        [SerializeField] private List<Light> roomLights = new List<Light>();
        [SerializeField] private Light feedbackLight;
        [SerializeField] private Transform compartmentDoor;
        [SerializeField] private GameObject readCard;
        [SerializeField] private float compartmentDrop = 0.9f;

        private readonly Dictionary<Light, float> baseIntensity = new Dictionary<Light, float>();
        private readonly Dictionary<Light, Color> baseColor = new Dictionary<Light, Color>();

        public void Configure(List<MirrorDisplay> mirrorDisplays, List<Light> lights, Light feedback, Transform compartment, GameObject card)
        {
            displays = mirrorDisplays;
            roomLights = lights;
            feedbackLight = feedback;
            compartmentDoor = compartment;
            readCard = card;
        }

        private void Start()
        {
            foreach (var l in roomLights)
            {
                if (l == null) continue;
                baseIntensity[l] = l.intensity;
                baseColor[l] = l.color;
            }
            if (feedbackLight != null) feedbackLight.enabled = false;
            if (readCard != null) readCard.SetActive(false);

            var gm = GameManager.Instance;
            if (gm != null && gm.State.PerceptionSolved) RestoreSolvedPresentation();
        }

        public static bool IsCorrectSelection(IEnumerable<MirrorDisplay> all, string selectedId)
        {
            foreach (var d in all)
            {
                if (d != null && d.DisplayId == selectedId) return d.IsImpossible;
            }
            return false;
        }

        public override bool Evaluate(string input) => IsCorrectSelection(displays, input);

        public void Select(MirrorDisplay display)
        {
            if (display == null) return;
            Submit(display.DisplayId);
        }

        protected override void ApplyStateChanges()
        {
            GameManager.Instance?.State.MarkPerceptionSolved();
        }

        protected override void OnWrong(string input)
        {
            var gm = GameManager.Instance;
            gm?.Audio.Play(SfxId.Reject);
            gm?.VossSays("You looked. You did not observe.", 3f);
            StartCoroutine(RedPulse());
        }

        private IEnumerator RedPulse()
        {
            if (feedbackLight == null) yield break;
            feedbackLight.color = new Color(1f, 0.08f, 0.05f);
            feedbackLight.enabled = true;
            float t = 0f;
            while (t < 0.9f)
            {
                t += Time.deltaTime;
                feedbackLight.intensity = Mathf.Lerp(6f, 0f, t / 0.9f);
                yield return null;
            }
            feedbackLight.enabled = false;
        }

        protected override void OnCorrect()
        {
            foreach (var d in displays) if (d != null) d.SetSelectable(false);
            StartCoroutine(RevealSequence());
        }

        private IEnumerator RevealSequence()
        {
            var gm = GameManager.Instance;
            gm?.Audio.Play(SfxId.Confirm);
            gm?.Audio.Play(SfxId.PowerDown, 0.6f);

            // 1. Room lights off.
            foreach (var l in roomLights) if (l != null) l.enabled = false;
            yield return new WaitForSeconds(1.2f);

            // 2. Mirrors illuminate one by one.
            foreach (var d in displays)
            {
                if (d == null) continue;
                d.SetMirrorGlow(new Color(0.75f, 0.85f, 1f), 2.5f);
                gm?.Audio.Play(SfxId.MirrorMechanism, 0.7f);
                yield return new WaitForSeconds(0.55f);
            }
            yield return new WaitForSeconds(0.8f);

            // 3. All reflections disappear.
            foreach (var d in displays) if (d != null) d.SetReflectionVisible(false);
            gm?.Audio.Play(SfxId.MirrorMechanism, 1f);
            yield return new WaitForSeconds(1.0f);

            // 4. The impossible mirror becomes opaque black; others dim.
            foreach (var d in displays)
            {
                if (d == null) continue;
                if (d.IsImpossible) d.SetMirrorOpaqueBlack();
                else d.SetMirrorGlow(new Color(0.75f, 0.85f, 1f), 0.4f);
            }
            yield return new WaitForSeconds(0.8f);

            // 5. Hidden compartment opens.
            gm?.Audio.Play(SfxId.MetalMechanism);
            if (compartmentDoor != null)
            {
                Vector3 start = compartmentDoor.localPosition;
                Vector3 end = start + Vector3.down * compartmentDrop;
                float t = 0f;
                while (t < 1.4f)
                {
                    t += Time.deltaTime;
                    compartmentDoor.localPosition = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t / 1.4f));
                    yield return null;
                }
                compartmentDoor.localPosition = end;
            }
            if (readCard != null) readCard.SetActive(true);

            // 6. Soft light returns so the player can find the card.
            foreach (var l in roomLights)
            {
                if (l == null) continue;
                l.enabled = true;
                l.intensity = baseIntensity.TryGetValue(l, out var b) ? b * 0.45f : 1f;
            }
            gm?.VossSays("Observation is a decision. You made one.", 3.5f);
        }

        private void RestoreSolvedPresentation()
        {
            MarkSolvedSilently();
            foreach (var d in displays)
            {
                if (d == null) continue;
                d.SetSelectable(false);
                d.SetReflectionVisible(false);
                if (d.IsImpossible) d.SetMirrorOpaqueBlack(); else d.SetMirrorGlow(new Color(0.75f, 0.85f, 1f), 0.4f);
            }
            if (compartmentDoor != null) compartmentDoor.localPosition += Vector3.down * compartmentDrop;
            if (readCard != null) readCard.SetActive(true);
        }

        /// <summary>Called by the final investigation to show the room's principle.</summary>
        public void IlluminateFully()
        {
            foreach (var l in roomLights)
            {
                if (l == null) continue;
                l.enabled = true;
                l.intensity = baseIntensity.TryGetValue(l, out var b) ? b * 1.3f : 1.5f;
                l.color = new Color(0.85f, 0.92f, 1f);
            }
        }
    }
}
