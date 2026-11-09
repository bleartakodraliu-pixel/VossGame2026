using System;
using UnityEngine;

namespace Voss.Audio
{
    /// <summary>
    /// Generates simple placeholder clips at runtime so every SFX and music cue is audible
    /// without any imported audio. Replace via AudioLibrary when real assets exist.
    /// </summary>
    public static class ProceduralAudio
    {
        private const int SampleRate = 44100;

        public static AudioClip CreateSfx(SfxId id)
        {
            switch (id)
            {
                case SfxId.DoorLock:        return Build("sfx_doorlock", 0.35f, t => Noise(t) * Env(t, 0.005f, 0.3f) * 0.6f + Sine(t, 90f) * Env(t, 0.01f, 0.2f));
                case SfxId.DoorOpen:        return Build("sfx_dooropen", 1.2f, t => Noise(t) * 0.15f * Env(t, 0.2f, 1.0f) + Sine(t, 60f + t * 20f) * 0.25f * Env(t, 0.3f, 1.0f));
                case SfxId.MetalMechanism:  return Build("sfx_mech", 0.6f, t => Sine(t, 220f * Mathf.Exp(-t * 4f)) * Env(t, 0.005f, 0.4f) * 0.5f + Noise(t) * 0.1f * Env(t, 0.001f, 0.15f));
                case SfxId.TerminalActivate:return Build("sfx_terminal", 0.5f, t => Square(t, 880f) * 0.12f * Env(t, 0.01f, 0.1f) + Sine(t, 440f) * 0.2f * Env(t, 0.1f, 0.4f));
                case SfxId.Reject:          return Build("sfx_reject", 0.7f, t => Sine(t, 110f) * 0.4f * Env(t, 0.01f, 0.5f) + Sine(t, 82f) * 0.3f * Env(t, 0.05f, 0.6f));
                case SfxId.Confirm:         return Build("sfx_confirm", 0.9f, t => Sine(t, 523f) * 0.25f * Env(t, 0.01f, 0.5f) + Sine(t, 784f) * 0.2f * Env(t - 0.15f, 0.01f, 0.6f));
                case SfxId.MirrorMechanism: return Build("sfx_mirror", 0.8f, t => Sine(t, 1500f * Mathf.Exp(-t * 2f)) * 0.2f * Env(t, 0.005f, 0.6f) + Noise(t) * 0.05f * Env(t, 0.01f, 0.5f));
                case SfxId.BlackBoxOpen:    return Build("sfx_box", 1.0f, t => Sine(t, 70f) * 0.35f * Env(t, 0.01f, 0.3f) + Noise(t) * 0.1f * Env(t - 0.3f, 0.05f, 0.5f));
                case SfxId.Projector:       return Build("sfx_projector", 1.5f, t => Noise(t) * 0.08f * Env(t, 0.3f, 1.2f) + Square(t, 24f) * 0.05f * Env(t, 0.2f, 1.3f));
                case SfxId.Applause:        return Build("sfx_applause", 4.0f, t => Noise(t) * 0.18f * (0.6f + 0.4f * Mathf.PerlinNoise(t * 30f, 0.5f)) * Env(t, 0.8f, 3.0f));
                case SfxId.Elevator:        return Build("sfx_elevator", 2.5f, t => Sine(t, 55f) * 0.3f * Env(t, 0.4f, 2.0f) + Noise(t) * 0.04f * Env(t, 0.5f, 2.0f));
                case SfxId.Footstep:        return Build("sfx_step", 0.15f, t => Noise(t) * 0.25f * Env(t, 0.002f, 0.1f));
                case SfxId.VossVoice:       return Build("sfx_voice", 0.6f, t => Sine(t, 130f + 10f * Mathf.Sin(t * 12f)) * 0.12f * Env(t, 0.05f, 0.5f));
                case SfxId.Click:           return Build("sfx_click", 0.08f, t => Square(t, 2000f) * 0.15f * Env(t, 0.001f, 0.05f));
                case SfxId.Paper:           return Build("sfx_paper", 0.3f, t => Noise(t) * 0.12f * Env(t, 0.02f, 0.25f));
                case SfxId.PowerDown:       return Build("sfx_powerdown", 1.5f, t => Sine(t, 300f * Mathf.Exp(-t * 2.5f)) * 0.3f * Env(t, 0.01f, 1.3f));
                default:                    return Build("sfx_default", 0.2f, t => Sine(t, 440f) * 0.2f * Env(t, 0.01f, 0.15f));
            }
        }

        public static AudioClip CreateMusic(MusicId id)
        {
            switch (id)
            {
                case MusicId.Drone:
                    return Build("music_drone", 12f, t => (Sine(t, 55f) * 0.18f + Sine(t, 82.4f) * 0.08f + Sine(t, 110f) * 0.05f * (0.5f + 0.5f * Mathf.Sin(t * 0.4f))) * Loop(t, 12f), loop: true);
                case MusicId.Resolve:
                    return Build("music_resolve", 10f, t => (Sine(t, 261.6f) * 0.12f + Sine(t, 329.6f) * 0.1f + Sine(t, 392f) * 0.1f + Sine(t, 523.2f) * 0.06f) * Env(t, 1.5f, 9f));
                case MusicId.Ending:
                    return Build("music_ending", 16f, t => (Sine(t, 65.4f) * 0.15f + Sine(t, 98f) * 0.07f + Sine(t, 196f) * 0.04f * (0.5f + 0.5f * Mathf.Sin(t * 0.25f))) * Loop(t, 16f), loop: true);
                default:
                    return null;
            }
        }

        // ------------------------------------------------------------ helpers

        private static AudioClip Build(string name, float seconds, Func<float, float> f, bool loop = false)
        {
            int n = Mathf.CeilToInt(seconds * SampleRate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                data[i] = Mathf.Clamp(f(t), -1f, 1f);
            }
            var clip = AudioClip.Create(name, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Sine(float t, float hz) => Mathf.Sin(2f * Mathf.PI * hz * t);
        private static float Square(float t, float hz) => Mathf.Sin(2f * Mathf.PI * hz * t) > 0f ? 1f : -1f;

        private static readonly System.Random rng = new System.Random(1337);
        private static float Noise(float t) => (float)(rng.NextDouble() * 2.0 - 1.0);

        /// <summary>Attack/decay envelope.</summary>
        private static float Env(float t, float attack, float decay)
        {
            if (t < 0f) return 0f;
            if (t < attack) return t / Mathf.Max(attack, 0.0001f);
            float d = (t - attack) / Mathf.Max(decay, 0.0001f);
            return Mathf.Clamp01(1f - d);
        }

        /// <summary>Smooth fade at both ends so a looping clip has no click.</summary>
        private static float Loop(float t, float length)
        {
            float edge = 0.5f;
            if (t < edge) return t / edge;
            if (t > length - edge) return (length - t) / edge;
            return 1f;
        }
    }
}
