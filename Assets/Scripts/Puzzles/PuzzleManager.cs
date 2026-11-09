using System.Collections.Generic;
using UnityEngine;
using Voss.Core;

namespace Voss.Puzzles
{
    /// <summary>
    /// Scene-level registry of puzzles. Keeps lock state in sync with progression so a puzzle can
    /// never be answered out of order even if room gating were bypassed.
    /// </summary>
    public class PuzzleManager : MonoBehaviour
    {
        [SerializeField] private PerceptionPuzzle perception;
        [SerializeField] private ReasonPuzzle reason;
        [SerializeField] private MemoryPuzzle memory;

        private readonly List<PuzzleBase> all = new List<PuzzleBase>();

        public PerceptionPuzzle Perception => perception;
        public ReasonPuzzle Reason => reason;
        public MemoryPuzzle Memory => memory;

        public void Configure(PerceptionPuzzle p, ReasonPuzzle r, MemoryPuzzle m)
        {
            perception = p; reason = r; memory = m;
        }

        private void Awake()
        {
            if (perception != null) all.Add(perception);
            if (reason != null) all.Add(reason);
            if (memory != null) all.Add(memory);
            foreach (var p in all)
            {
                p.Solved += OnPuzzleSolved;
                p.Failed += OnPuzzleFailed;
            }
        }

        private void OnEnable()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.State.FlagsChanged += SyncLocks;
            SyncLocks();
        }

        private void OnDisable()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.State.FlagsChanged -= SyncLocks;
        }

        private void SyncLocks()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            var s = gm.State;
            if (perception != null) perception.SetLocked(!s.PerceptionUnlocked);
            if (reason != null) reason.SetLocked(!s.ReasonUnlocked);
            if (memory != null) memory.SetLocked(!s.MemoryUnlocked);
        }

        private void OnPuzzleSolved(PuzzleBase p)
        {
            Debug.Log($"[Puzzle] Solved: {p.PuzzleId}");
            SyncLocks();
        }

        private void OnPuzzleFailed(PuzzleBase p, string input)
        {
            var gm = GameManager.Instance;
            Debug.Log($"[Puzzle] Wrong input '{input}' on {p.PuzzleId}. Attempts left: {(gm != null ? gm.State.AttemptsRemaining : -1)}");
        }
    }
}
