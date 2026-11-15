using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Voss.Audio
{
    /// <summary>
    /// Central 2D audio service. Loads an optional AudioLibrary from Resources and falls back
    /// to procedural placeholder clips. Music is cross-faded; silence is a deliberate state.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private const string LibraryResourcePath = "VossAudioLibrary";
        private const int SfxVoices = 6;

        private AudioLibrary library;
        private readonly Dictionary<SfxId, AudioClip> sfxCache = new Dictionary<SfxId, AudioClip>();
        private readonly Dictionary<MusicId, AudioClip> musicCache = new Dictionary<MusicId, AudioClip>();
        private readonly List<AudioSource> sfxSources = new List<AudioSource>();
        private AudioSource musicA;
        private AudioSource musicB;
        private bool musicOnA = true;
        private Coroutine musicFade;

        public MusicId CurrentMusic { get; private set; } = MusicId.None;

        private void Awake()
        {
            library = Resources.Load<AudioLibrary>(LibraryResourcePath);
            for (int i = 0; i < SfxVoices; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                sfxSources.Add(s);
            }
            musicA = CreateMusicSource();
            musicB = CreateMusicSource();
        }

        private AudioSource CreateMusicSource()
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = true;
            s.spatialBlend = 0f;
            s.volume = 0f;
            return s;
        }

        // ---------------------------------------------------------------- SFX

        public void Play(SfxId id, float volumeScale = 1f)
        {
            var clip = GetSfx(id, out float vol);
            if (clip == null) return;
            var src = GetFreeSource();
            src.pitch = 1f;
            src.PlayOneShot(clip, vol * volumeScale);
        }

        /// <summary>Positional one-shot for world feedback (doors, mechanisms).</summary>
        public void PlayAt(SfxId id, Vector3 position, float volumeScale = 1f)
        {
            var clip = GetSfx(id, out float vol);
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, vol * volumeScale);
        }

        /// <summary>Raw clip for components that own their own AudioSource (loops, ticking props).</summary>
        public AudioClip GetClip(SfxId id, out float volume) => GetSfx(id, out volume);

        private AudioClip GetSfx(SfxId id, out float volume)
        {
            volume = 1f;
            if (library != null && library.TryGetSfx(id, out var libClip, out volume)) return libClip;
            if (!sfxCache.TryGetValue(id, out var clip))
            {
                clip = ProceduralAudio.CreateSfx(id);
                sfxCache[id] = clip;
            }
            return clip;
        }

        private AudioSource GetFreeSource()
        {
            foreach (var s in sfxSources) if (!s.isPlaying) return s;
            return sfxSources[0];
        }

        // -------------------------------------------------------------- music

        public void PlayMusic(MusicId id, float fadeSeconds = 2f)
        {
            if (id == CurrentMusic) return;
            CurrentMusic = id;
            if (musicFade != null) StopCoroutine(musicFade);
            musicFade = StartCoroutine(CrossFade(id, fadeSeconds));
        }

        public void StopMusic(float fadeSeconds = 0.05f)
        {
            if (CurrentMusic == MusicId.None && !musicA.isPlaying && !musicB.isPlaying) return;
            CurrentMusic = MusicId.None;
            if (musicFade != null) StopCoroutine(musicFade);
            musicFade = StartCoroutine(CrossFade(MusicId.None, fadeSeconds));
        }

        private IEnumerator CrossFade(MusicId id, float seconds)
        {
            var from = musicOnA ? musicA : musicB;
            var to = musicOnA ? musicB : musicA;
            musicOnA = !musicOnA;

            float targetVol = 0f;
            if (id != MusicId.None)
            {
                var clip = GetMusic(id, out targetVol);
                if (clip != null)
                {
                    to.clip = clip;
                    to.volume = 0f;
                    to.Play();
                }
                else targetVol = 0f;
            }

            float fromStart = from.volume;
            float t = 0f;
            seconds = Mathf.Max(0.01f, seconds);
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                from.volume = Mathf.Lerp(fromStart, 0f, k);
                to.volume = Mathf.Lerp(0f, targetVol, k);
                yield return null;
            }
            from.Stop();
            from.volume = 0f;
            to.volume = targetVol;
            musicFade = null;
        }

        private AudioClip GetMusic(MusicId id, out float volume)
        {
            volume = 0.5f;
            if (library != null && library.TryGetMusic(id, out var libClip, out volume)) return libClip;
            if (!musicCache.TryGetValue(id, out var clip))
            {
                clip = ProceduralAudio.CreateMusic(id);
                musicCache[id] = clip;
            }
            return clip;
        }
    }
}
