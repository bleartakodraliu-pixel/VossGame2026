using System;
using System.Collections.Generic;
using UnityEngine;

namespace Voss.Audio
{
    public enum SfxId
    {
        DoorLock,
        DoorOpen,
        MetalMechanism,
        TerminalActivate,
        Reject,
        Confirm,
        MirrorMechanism,
        BlackBoxOpen,
        Projector,
        Applause,
        Elevator,
        Footstep,
        VossVoice,
        Click,
        Paper,
        PowerDown
    }

    public enum MusicId
    {
        None,
        Drone,
        Resolve,
        Ending
    }

    /// <summary>
    /// Optional asset: create via Assets > Create > Voss > Audio Library and place it at
    /// Resources/VossAudioLibrary. Any clip left empty falls back to a procedural placeholder,
    /// so the game is fully audible before real assets exist.
    /// </summary>
    [CreateAssetMenu(fileName = "VossAudioLibrary", menuName = "Voss/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        [Serializable]
        public class SfxEntry
        {
            public SfxId id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
        }

        [Serializable]
        public class MusicEntry
        {
            public MusicId id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 0.5f;
        }

        public List<SfxEntry> sfx = new List<SfxEntry>();
        public List<MusicEntry> music = new List<MusicEntry>();

        public bool TryGetSfx(SfxId id, out AudioClip clip, out float volume)
        {
            foreach (var e in sfx)
            {
                if (e.id == id && e.clip != null) { clip = e.clip; volume = e.volume; return true; }
            }
            clip = null; volume = 1f; return false;
        }

        public bool TryGetMusic(MusicId id, out AudioClip clip, out float volume)
        {
            foreach (var e in music)
            {
                if (e.id == id && e.clip != null) { clip = e.clip; volume = e.volume; return true; }
            }
            clip = null; volume = 0.5f; return false;
        }
    }
}
