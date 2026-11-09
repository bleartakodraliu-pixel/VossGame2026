using System;
using UnityEngine;
using Voss.Core;

namespace Voss.Puzzles
{
    /// <summary>
    /// Shared puzzle skeleton: state machine, attempt deduction, idempotent completion and events.
    /// Subclasses provide pure evaluation (Evaluate) and presentation hooks (OnCorrect/OnWrong),
    /// so logic can be tested without visuals.
    /// </summary>
    public abstract class PuzzleBase : MonoBehaviour, IPuzzle, IResettable
    {
        [SerializeField] private string puzzleId;
        [SerializeField] private bool deductsAttempts = true;

        private PuzzleState state = PuzzleState.Active;

        public string PuzzleId => puzzleId;

        /// <summary>Editor/builder use only.</summary>
        public void SetPuzzleId(string id) { puzzleId = id; }
        public PuzzleState State => state;
        public bool IsSolved => state == PuzzleState.Solved;

        public event Action<PuzzleBase> Solved;
        public event Action<PuzzleBase, string> Failed;

        /// <summary>Pure decision: is this input the solution? No side effects allowed.</summary>
        public abstract bool Evaluate(string input);

        protected abstract void OnCorrect();
        protected abstract void OnWrong(string input);

        public bool Submit(string input)
        {
            if (state == PuzzleState.Solved) return true;           // never reward twice
            if (state == PuzzleState.Locked) return false;

            var gm = GameManager.Instance;
            if (gm != null && !gm.State.PuzzleInputAllowed) return false; // zero attempts: input disabled

            string clean = (input ?? string.Empty).Trim();
            if (Evaluate(clean))
            {
                state = PuzzleState.Solved;
                ApplyStateChanges();
                OnCorrect();
                Solved?.Invoke(this);
                return true;
            }

            if (deductsAttempts && gm != null) gm.Attempts.Deduct();
            OnWrong(clean);
            Failed?.Invoke(this, clean);
            return false;
        }

        /// <summary>Write the puzzle's completion into global state. Must be idempotent.</summary>
        protected abstract void ApplyStateChanges();

        public void SetLocked(bool locked)
        {
            if (state == PuzzleState.Solved) return;
            state = locked ? PuzzleState.Locked : PuzzleState.Active;
        }

        /// <summary>Restores a solved presentation when a scene loads with the puzzle already solved.</summary>
        protected void MarkSolvedSilently()
        {
            state = PuzzleState.Solved;
        }

        public virtual void ResetState()
        {
            state = PuzzleState.Active;
        }
    }
}
