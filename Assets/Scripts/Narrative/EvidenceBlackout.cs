using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voss.Audio;
using Voss.Core;

namespace Voss.Narrative
{
    /// <summary>
    /// When a piece of evidence is taken, the room's lamps stutter and die for a moment and something
    /// whispers by the door. Used when the BACKWARDS card leaves the Black Box.
    /// </summary>
    public class EvidenceBlackout : MonoBehaviour
    {
        [SerializeField] private string evidenceId;
        [SerializeField] private List<Light> lights = new List<Light>();
        [SerializeField] private Transform whisperAt;

        private bool done;

        public void Configure(string evidence, List<Light> roomLights, Transform whisper)
        {
            evidenceId = evidence;
            lights = roomLights;
            whisperAt = whisper;
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            done = gm != null && gm.Evidence.Has(evidenceId);
        }

        private void Update()
        {
            if (done) return;
            var gm = GameManager.Instance;
            if (gm == null || !gm.Evidence.Has(evidenceId)) return;
            done = true;
            StartCoroutine(Blackout());
        }

        private IEnumerator Blackout()
        {
            var gm = GameManager.Instance;
            yield return new WaitForSeconds(1.0f);
            gm?.Audio.Play(SfxId.PowerDown, 0.8f);
            var on = new List<Light>();
            foreach (var l in lights) if (l != null && l.enabled) on.Add(l);
            for (int i = 0; i < 4; i++)
            {
                foreach (var l in on) l.enabled = i % 2 == 1;
                yield return new WaitForSeconds(Random.Range(0.05f, 0.14f));
            }
            foreach (var l in on) l.enabled = false;
            yield return new WaitForSeconds(0.7f);
            if (whisperAt != null) gm?.Audio.PlayAt(SfxId.Whisper, whisperAt.position, 1f);
            yield return new WaitForSeconds(1.8f);
            for (int i = 0; i < 3; i++)
            {
                foreach (var l in on) l.enabled = i % 2 == 0;
                yield return new WaitForSeconds(Random.Range(0.06f, 0.12f));
            }
            foreach (var l in on) l.enabled = true;
        }
    }
}
