using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.Player;
using Voss.UI;

namespace Voss.Narrative
{
    /// <summary>
    /// The playable theatre seen behind the main menu. Tracks the one piece of progress that matters
    /// here (the letter has been taken), and then quietly points the player at LABORATORY 7314:
    /// the objective line, a red lamp above the door, a light on the stairs, the hum of the door.
    /// </summary>
    public class IntroductionManager : MonoBehaviour
    {
        public const string FirstObjective = "Something was left for you on the table.";
        public const string DoorObjective = "Find the entrance to Voss's laboratory.";

        [SerializeField] private FirstPersonController player;
        [SerializeField] private List<Light> guidanceLights = new List<Light>();
        [SerializeField] private List<GameObject> guidanceObjects = new List<GameObject>();

        public bool LetterCollected { get; private set; }
        public FirstPersonController Player => player;

        public void Configure(FirstPersonController fps, List<Light> lights, List<GameObject> objects)
        {
            player = fps;
            guidanceLights = lights;
            guidanceObjects = objects;
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            var ui = UIManager.Instance;
            if (ui != null) { ui.EvidenceEnabled = true; ui.SetCursorFree(false); }
            gm?.State.SetStage(GameStage.Introduction);
            gm?.Audio.PlayMusic(MusicId.Drone, 4f);
            Objectives.Introduction = FirstObjective;
            SetGuidance(false);

            // Scene reloaded after the letter was already taken.
            if (gm != null && gm.Evidence.Has(EvidenceIds.Letter)) OnLetterCollected(false);
        }

        private void SetGuidance(bool on)
        {
            foreach (var l in guidanceLights) if (l != null) l.enabled = on;
            foreach (var o in guidanceObjects) if (o != null) o.SetActive(on);
        }

        public void OnLetterCollected(bool announce = true)
        {
            if (LetterCollected) return;
            LetterCollected = true;
            Objectives.Introduction = DoorObjective;
            UIManager.Instance?.ShowObjective(DoorObjective);
            if (announce) StartCoroutine(WakeTheDoor());
            else SetGuidance(true);
        }

        /// <summary>The way on comes alive: a stutter, then the lamps settle.</summary>
        private IEnumerator WakeTheDoor()
        {
            var gm = GameManager.Instance;
            yield return new WaitForSeconds(1.2f);
            if (guidanceLights.Count > 0 && guidanceLights[0] != null)
                gm?.Audio.PlayAt(SfxId.DoorLock, guidanceLights[0].transform.position, 0.8f);
            for (int i = 0; i < 5; i++)
            {
                SetGuidance(i % 2 == 0);
                yield return new WaitForSeconds(Random.Range(0.06f, 0.16f));
            }
            SetGuidance(true);
            yield return new WaitForSeconds(2.5f);
            if (guidanceLights.Count > 0 && guidanceLights[0] != null)
                gm?.Audio.PlayAt(SfxId.Whisper, guidanceLights[0].transform.position, 1f);
        }

        /// <summary>Called by the laboratory door on the way out.</summary>
        public void Leaving()
        {
            Objectives.Introduction = null;
            UIManager.Instance?.HideObjective();
        }

        private void OnDestroy()
        {
            if (Objectives.Introduction == FirstObjective || Objectives.Introduction == DoorObjective) Objectives.Introduction = null;
        }
    }
}
