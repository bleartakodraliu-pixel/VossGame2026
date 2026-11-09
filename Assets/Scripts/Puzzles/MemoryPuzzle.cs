using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.Narrative;
using Voss.UI;

namespace Voss.Puzzles
{
    public static class MemoryPuzzleLogic
    {
        /// <summary>READ + BACKWARDS + the ticket number: reverse the first thing Voss gave the player.</summary>
        public static string ExpectedCode()
        {
            var chars = Answers.TicketCode.ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
        }

        public static bool IsCorrect(string input) => (input ?? string.Empty).Trim() == ExpectedCode();
    }

    /// <summary>
    /// Room 3 meta-puzzle. Correct code triggers the fake ending, then hands over to the
    /// final investigation. The exit never opens here by design.
    /// </summary>
    public class MemoryPuzzle : PuzzleBase
    {
        [SerializeField] private TerminalController terminal;
        [SerializeField] private List<Light> roomLights = new List<Light>();
        [SerializeField] private FinalInvestigationController finalInvestigation;

        private readonly Dictionary<Light, Color> baseColor = new Dictionary<Light, Color>();

        public void Configure(TerminalController term, List<Light> lights, FinalInvestigationController finale)
        {
            terminal = term;
            roomLights = lights;
            finalInvestigation = finale;
        }

        private void Start()
        {
            foreach (var l in roomLights) if (l != null) baseColor[l] = l.color;
            var gm = GameManager.Instance;
            if (gm != null && gm.State.MetaCodeSolved)
            {
                MarkSolvedSilently();
                if (terminal != null) terminal.SetLines(new[] { "CODE ACCEPTED", "", "BUT YOU STILL BELIEVE", "THE ILLUSION." });
            }
        }

        public override bool Evaluate(string input) => MemoryPuzzleLogic.IsCorrect(input);

        protected override void ApplyStateChanges()
        {
            GameManager.Instance?.State.MarkMemorySolved();
        }

        protected override void OnWrong(string input)
        {
            var gm = GameManager.Instance;
            gm?.Audio.Play(SfxId.Reject);
            gm?.VossSays("Look backward.", 2.5f);
            if (terminal != null) terminal.Flash(new[] { "REJECTED", "", "LOOK BACKWARD." }, 2.5f);
        }

        protected override void OnCorrect()
        {
            StartCoroutine(FakeEndingSequence());
        }

        private IEnumerator FakeEndingSequence()
        {
            var gm = GameManager.Instance;
            var ui = UIManager.Instance;

            if (terminal != null) terminal.SetLines(new[] { "CODE ACCEPTED" });
            gm?.Audio.Play(SfxId.Confirm);
            foreach (var l in roomLights) if (l != null) l.color = new Color(0.35f, 1f, 0.45f);
            gm?.Audio.PlayMusic(MusicId.Resolve, 1.5f);
            ui?.ShowMessage("EXPERIMENT COMPLETE", "", 0f, 0f, UIFactory.Accept);

            // Let the player believe they have won.
            yield return new WaitForSeconds(6.5f);

            // The exit does not open. The music stops dead.
            ui?.HideMessage();
            gm?.Audio.StopMusic(0.03f);
            gm?.Audio.Play(SfxId.PowerDown, 0.9f);
            foreach (var l in roomLights)
            {
                if (l == null) continue;
                l.color = baseColor.TryGetValue(l, out var c) ? c : Color.white;
            }
            yield return new WaitForSeconds(1.6f);

            if (terminal != null) terminal.SetLines(new[] { "YOU SOLVED THE PUZZLES.", "", "BUT YOU STILL BELIEVE", "THE ILLUSION." });
            if (ui != null) yield return ui.ShowMessageSequence(new[] { "YOU SOLVED THE PUZZLES.", "BUT YOU STILL BELIEVE THE ILLUSION." }, 3.4f, 0.35f);

            yield return new WaitForSeconds(0.8f);
            if (finalInvestigation != null) finalInvestigation.Begin();
        }
    }
}
