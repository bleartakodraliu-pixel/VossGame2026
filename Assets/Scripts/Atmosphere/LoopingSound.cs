using UnityEngine;
using Voss.Audio;
using Voss.Core;

namespace Voss.Atmosphere
{
    /// <summary>A positional loop (CRT static, a buzzing lamp) audible only within a few metres.</summary>
    public class LoopingSound : MonoBehaviour
    {
        [SerializeField] private SfxId sound = SfxId.Static;
        [SerializeField] [Range(0f, 1f)] private float volume = 0.5f;
        [SerializeField] private float maxDistance = 6f;

        public void Configure(SfxId sfx, float vol, float distance)
        {
            sound = sfx;
            volume = vol;
            maxDistance = distance;
        }

        private void Start()
        {
            var audio = GameManager.Instance != null ? GameManager.Instance.Audio : null;
            if (audio == null) return;
            var source = gameObject.AddComponent<AudioSource>();
            source.clip = audio.GetClip(sound, out float v);
            source.loop = true;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 0.5f;
            source.maxDistance = maxDistance;
            source.volume = volume * v;
            source.time = Random.Range(0f, Mathf.Max(0f, source.clip.length - 0.1f));
            source.Play();
        }
    }
}
