using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voss.Atmosphere;
using Voss.Audio;
using Voss.Core;

namespace Voss.Narrative
{
    /// <summary>
    /// The Main Hall remembers each test. After PERCEPTION a fluorescent fitting dies and the room tone
    /// drops; after REASON the red lamps burn harder and the hall starts to whisper; the observation
    /// monitors log each result. Silent restore when the scene is reloaded mid-game.
    /// </summary>
    public class HallProgression : MonoBehaviour
    {
        [SerializeField] private Light failingLight;
        [SerializeField] private List<Light> redLamps = new List<Light>();
        [SerializeField] private GameObject whispers;
        [SerializeField] private AudioSource roomTone;
        [SerializeField] private List<TextMesh> monitors = new List<TextMesh>();

        private int applied = -1;
        private float searchUntil;

        public void Configure(Light failing, List<Light> reds, GameObject hallWhispers, List<TextMesh> monitorTexts)
        {
            failingLight = failing;
            redLamps = reds;
            whispers = hallWhispers;
            monitors = monitorTexts;
        }

        private void Start()
        {
            searchUntil = Time.time + 5f;
            if (whispers != null) whispers.SetActive(false);
            var gm = GameManager.Instance;
            if (gm == null) return;
            int stage = Stage(gm.State);
            for (int s = 1; s <= stage; s++) Apply(s, false);
            applied = stage;
        }

        private static int Stage(GameState s) => s.MetaCodeSolved ? 3 : s.ReasonSolved ? 2 : s.PerceptionSolved ? 1 : 0;

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (roomTone == null && Time.time < searchUntil && Time.frameCount % 15 == 0) FindRoomTone();
            int stage = Stage(gm.State);
            while (applied < stage) { applied++; if (applied > 0) Apply(applied, true); }
        }

        /// <summary>The room tone is the AudioSource a soundscape adds in its Start; looked for briefly, not every frame.</summary>
        private void FindRoomTone()
        {
            foreach (var sc in FindObjectsByType<AmbientSoundscape>(FindObjectsInactive.Exclude))
            {
                var src = sc.GetComponent<AudioSource>();
                if (src == null) continue;
                roomTone = src;
                int stage = applied;
                if (stage >= 2) roomTone.pitch = 0.85f;
                else if (stage >= 1) roomTone.pitch = 0.92f;
                return;
            }
        }

        private void Apply(int stage, bool live)
        {
            switch (stage)
            {
                case 1:
                    if (failingLight != null)
                    {
                        if (live) StartCoroutine(Die(failingLight));
                        else failingLight.gameObject.SetActive(false);
                    }
                    if (roomTone != null) roomTone.pitch = 0.92f;
                    SetMonitors("CHAMBER 01\nPASSED");
                    break;
                case 2:
                    foreach (var l in redLamps) if (l != null) l.intensity *= 1.8f;
                    if (whispers != null) whispers.SetActive(true);
                    if (roomTone != null) roomTone.pitch = 0.85f;
                    SetMonitors("CHAMBER 02\nPASSED");
                    break;
                case 3:
                    SetMonitors("SUBJECT 7314\nCOMPLETE?");
                    break;
            }
        }

        private void SetMonitors(string text)
        {
            foreach (var m in monitors) if (m != null) m.text = text;
        }

        private IEnumerator Die(Light l)
        {
            var gm = GameManager.Instance;
            for (int i = 0; i < 7; i++)
            {
                l.enabled = i % 2 == 1;
                yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
            }
            gm?.Audio.PlayAt(SfxId.DoorLock, l.transform.position, 0.8f);
            l.gameObject.SetActive(false);
        }
    }
}
