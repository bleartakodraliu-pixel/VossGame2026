using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.Player;

namespace Voss.Atmosphere
{
    /// <summary>
    /// One-shot scripted scare on a trigger volume. Once its flag is met and the player walks in:
    /// lamps die one after another (each with a heavy relay clunk), then a sound plays at a chosen
    /// spot and an optional subtitle appears. Small, quiet, and never repeated.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ScareTrigger : MonoBehaviour
    {
        [SerializeField] private GameFlag requiredFlag = GameFlag.None;
        [SerializeField] private float delay = 0.3f;
        [SerializeField] private List<Light> lightsOut = new List<Light>();
        [SerializeField] private float lightsOutInterval = 0.8f;
        [SerializeField] private bool playSound;
        [SerializeField] private SfxId sound = SfxId.DistantSteps;
        [SerializeField] private Transform soundAt;
        [SerializeField] [Range(0f, 1f)] private float soundVolume = 1f;
        [SerializeField] private string subtitle;

        private bool fired;

        public void Configure(GameFlag flag, List<Light> lamps, bool withSound, SfxId sfx, Transform at, float volume, string line)
        {
            requiredFlag = flag;
            lightsOut = lamps ?? new List<Light>();
            playSound = withSound;
            sound = sfx;
            soundAt = at;
            soundVolume = volume;
            subtitle = line;
        }

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (fired || other.GetComponentInParent<FirstPersonController>() == null) return;
            var gm = GameManager.Instance;
            if (gm != null && !GameFlags.Check(gm.State, requiredFlag)) return;
            fired = true;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            var gm = GameManager.Instance;
            yield return new WaitForSeconds(delay);

            foreach (var l in lightsOut)
            {
                if (l == null || !l.enabled) continue;
                l.enabled = false;
                gm?.Audio.PlayAt(SfxId.DoorLock, l.transform.position, 0.7f);
                yield return new WaitForSeconds(lightsOutInterval);
            }

            if (playSound) gm?.Audio.PlayAt(sound, soundAt != null ? soundAt.position : transform.position, soundVolume);
            if (!string.IsNullOrEmpty(subtitle)) gm?.Subtitles.Show(subtitle, 3f);
        }
    }
}
