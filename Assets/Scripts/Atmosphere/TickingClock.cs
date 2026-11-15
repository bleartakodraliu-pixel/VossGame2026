using UnityEngine;
using Voss.Audio;
using Voss.Core;

namespace Voss.Atmosphere
{
    /// <summary>Wall clock: the second hand jumps once a second with a tick you only hear up close.</summary>
    public class TickingClock : MonoBehaviour
    {
        [SerializeField] private Transform secondHand;
        [SerializeField] [Range(0f, 1f)] private float volume = 0.6f;

        private AudioSource source;
        private float next;

        public void Configure(Transform hand) { secondHand = hand; }

        private void Start()
        {
            var audio = GameManager.Instance != null ? GameManager.Instance.Audio : null;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1f;
            source.maxDistance = 8f;
            if (audio != null) source.clip = audio.GetClip(SfxId.ClockTick, out _);
            next = Time.time + 1f;
        }

        private void Update()
        {
            if (Time.time < next) return;
            next += 1f;
            if (secondHand != null) secondHand.Rotate(0f, 0f, 6f, Space.Self); // clockwise seen from the clock face (+z)
            if (source != null && source.clip != null) source.PlayOneShot(source.clip, volume);
        }
    }
}
