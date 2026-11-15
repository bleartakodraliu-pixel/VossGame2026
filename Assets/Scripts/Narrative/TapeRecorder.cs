using System.Collections;
using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.Interaction;
using Voss.Player;

namespace Voss.Narrative
{
    /// <summary>Reel-to-reel recorder: plays one of Voss's rehearsal tapes as subtitled voice lines.</summary>
    public class TapeRecorder : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform reelA;
        [SerializeField] private Transform reelB;
        [SerializeField] [TextArea] private string[] lines =
        {
            "Session forty-one.",
            "Ask them what they saw, and they describe it perfectly.",
            "Ask them what they heard, and they describe it perfectly.",
            "Nobody remembers what they were not shown.",
            "Do you remember where you were sitting? I do."
        };

        private bool playing;
        private bool played;

        public void Configure(Transform a, Transform b) { reelA = a; reelB = b; }

        public string GetInteractionPrompt() => playing ? string.Empty : (played ? "Play the tape again" : "Press play");
        public bool CanInteract() => !playing;

        public void Interact(PlayerInteractor player)
        {
            if (playing) return;
            StartCoroutine(PlayTape());
        }

        private IEnumerator PlayTape()
        {
            var gm = GameManager.Instance;
            playing = true;
            gm?.Audio.PlayAt(SfxId.Click, transform.position, 1f);
            gm?.Audio.PlayAt(SfxId.Static, transform.position, 0.6f);
            yield return new WaitForSeconds(0.8f);
            foreach (var line in lines)
            {
                float dur = Mathf.Clamp(1.4f + line.Length * 0.055f, 2.2f, 5.5f);
                gm?.VossSays(line, dur);
                yield return new WaitForSeconds(dur + 0.5f);
            }
            gm?.Audio.PlayAt(SfxId.Click, transform.position, 1f);
            playing = false;
            played = true;
        }

        private void Update()
        {
            if (!playing) return;
            float deg = 220f * Time.deltaTime;
            if (reelA != null) reelA.Rotate(0f, deg, 0f, Space.Self);
            if (reelB != null) reelB.Rotate(0f, deg * 1.3f, 0f, Space.Self);
        }
    }
}
