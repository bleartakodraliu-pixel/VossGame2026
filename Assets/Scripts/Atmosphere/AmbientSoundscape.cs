using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.Player;

namespace Voss.Atmosphere
{
    /// <summary>
    /// Random one-shots somewhere around the player (drips, creaks, knocks, distant steps, whispers),
    /// plus an optional looping room tone. With a trigger collider it only plays while the player is
    /// inside that volume; without one it plays everywhere in the scene.
    /// </summary>
    public class AmbientSoundscape : MonoBehaviour
    {
        [SerializeField] private SfxId[] sounds = { SfxId.Drip, SfxId.Creak };
        [SerializeField] private Vector2 interval = new Vector2(8f, 20f);
        [SerializeField] private Vector2 distance = new Vector2(4f, 9f);
        [SerializeField] [Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private bool roomTone;
        [SerializeField] [Range(0f, 1f)] private float roomToneVolume = 0.35f;

        private AudioSource tone;
        private float next;
        private bool zoned;
        private bool inside;

        public void Configure(SfxId[] clips, Vector2 every, Vector2 dist, float vol, bool loopRoomTone = false, float toneVolume = 0.35f)
        {
            sounds = clips;
            interval = every;
            distance = dist;
            volume = vol;
            roomTone = loopRoomTone;
            roomToneVolume = toneVolume;
        }

        private void Start()
        {
            var col = GetComponent<Collider>();
            zoned = col != null && col.isTrigger;
            next = Time.time + Random.Range(interval.x, interval.y) * 0.5f;

            var audio = GameManager.Instance != null ? GameManager.Instance.Audio : null;
            if (roomTone && audio != null)
            {
                tone = gameObject.AddComponent<AudioSource>();
                tone.clip = audio.GetClip(SfxId.RoomTone, out float v);
                tone.loop = true;
                tone.spatialBlend = 0f;
                tone.volume = roomToneVolume * v;
                tone.Play();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<FirstPersonController>() != null) inside = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<FirstPersonController>() != null) inside = false;
        }

        private void Update()
        {
            if (sounds == null || sounds.Length == 0) return;
            if (zoned && !inside) return;
            if (Time.time < next) return;
            next = Time.time + Random.Range(interval.x, interval.y);

            var gm = GameManager.Instance;
            var cam = Camera.main;
            if (gm == null || cam == null || gm.State.IsLost) return;

            Vector2 dir = Random.insideUnitCircle.normalized;
            float d = Random.Range(distance.x, distance.y);
            Vector3 pos = cam.transform.position + new Vector3(dir.x, 0f, dir.y) * d + Vector3.up * Random.Range(-1.2f, 1.2f);
            gm.Audio.PlayAt(sounds[Random.Range(0, sounds.Length)], pos, volume);
        }
    }
}
