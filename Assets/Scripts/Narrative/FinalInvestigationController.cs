using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.Interaction;
using Voss.Player;
using Voss.Puzzles;
using Voss.UI;

namespace Voss.Narrative
{
    /// <summary>
    /// THE REAL FINAL TEST. Illuminates all three rooms, opens the way back to the hall (whose walls
    /// carry the three principles), switches on the performance reconstruction and arms the final
    /// question terminal.
    /// </summary>
    public class FinalInvestigationController : MonoBehaviour
    {
        [SerializeField] private PerceptionPuzzle perception;
        [SerializeField] private List<Light> reasonLights = new List<Light>();
        [SerializeField] private List<Light> memoryLights = new List<Light>();
        [SerializeField] private List<Light> hallLights = new List<Light>();
        [SerializeField] private GameObject perceptionPrinciple;
        [SerializeField] private GameObject reasonPrinciple;
        [SerializeField] private GameObject memoryPrinciple;
        [SerializeField] private VideoInvestigationController video;
        [SerializeField] private FinalQuestionTerminal questionTerminal;
        [SerializeField] private List<DoorController> wayBackDoors = new List<DoorController>();
        [SerializeField] private List<Light> wayBackLights = new List<Light>();

        private bool begun;

        public static readonly string[] FinalWords = { "DO NOT TRUST WHAT YOU SEE.", "DO NOT TRUST YOUR FIRST ANSWER.", "REMEMBER WHAT YOU ALREADY KNOW." };

        public void Configure(PerceptionPuzzle p, List<Light> reason, List<Light> memory, List<Light> hall,
            GameObject principleP, GameObject principleR, GameObject principleM,
            VideoInvestigationController videoController, FinalQuestionTerminal terminal, List<DoorController> doors, List<Light> corridorLights)
        {
            perception = p;
            reasonLights = reason;
            memoryLights = memory;
            hallLights = hall;
            perceptionPrinciple = principleP;
            reasonPrinciple = principleR;
            memoryPrinciple = principleM;
            video = videoController;
            questionTerminal = terminal;
            wayBackDoors = doors ?? new List<DoorController>();
            wayBackLights = corridorLights ?? new List<Light>();
        }

        private void Start()
        {
            SetPrinciples(false);
            foreach (var l in wayBackLights) if (l != null) l.enabled = false;
            var gm = GameManager.Instance;
            if (gm != null && gm.State.MetaCodeSolved && !gm.State.FinalTruthSolved)
            {
                // Scene reloaded mid-finale: restore without the cinematic.
                begun = true;
                ApplyFinaleWorldState();
            }
        }

        private void SetPrinciples(bool on)
        {
            if (perceptionPrinciple != null) perceptionPrinciple.SetActive(on);
            if (reasonPrinciple != null) reasonPrinciple.SetActive(on);
            if (memoryPrinciple != null) memoryPrinciple.SetActive(on);
        }

        public void Begin()
        {
            if (begun) return;
            begun = true;
            StartCoroutine(BeginRoutine());
        }

        private IEnumerator BeginRoutine()
        {
            var gm = GameManager.Instance;
            var ui = UIManager.Instance;
            gm?.State.SetStage(GameStage.FinalInvestigation);

            yield return new WaitForSeconds(0.5f);
            gm?.Audio.Play(SfxId.PowerDown, 0.5f);
            yield return new WaitForSeconds(1.0f);

            // Every room lights at once.
            gm?.Audio.Play(SfxId.MetalMechanism, 0.9f);
            ApplyFinaleWorldState();
            yield return new WaitForSeconds(0.8f);

            if (ui != null) yield return ui.ShowMessageSequence(new[] { "THE REAL FINAL TEST" }, 3f, 0f, UIFactory.Brass);
            if (gm != null)
            {
                gm.VossSays("Three principles. One performance. Watch it again.", 4f);
                yield return new WaitForSeconds(4.2f);
                gm.VossSays("The way back is open. The screen in the hall is waiting for you.", 4f);
            }
            gm?.Audio.PlayMusic(MusicId.Drone, 6f);
        }

        private void ApplyFinaleWorldState()
        {
            if (perception != null) perception.IlluminateFully();
            foreach (var l in reasonLights) if (l != null) { l.enabled = true; l.intensity *= 1.3f; }
            foreach (var l in memoryLights) if (l != null) { l.enabled = true; l.color = new Color(0.85f, 0.92f, 1f); }
            foreach (var l in hallLights) if (l != null) l.enabled = true;
            SetPrinciples(true);
            foreach (var l in wayBackLights) if (l != null) l.enabled = true;
            foreach (var d in wayBackDoors) if (d != null) { d.UnlockManually(); d.Open(); }
            video?.MakeAvailable();
            questionTerminal?.Arm();
        }
    }
}
