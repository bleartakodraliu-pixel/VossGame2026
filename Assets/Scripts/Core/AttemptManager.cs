using System;
using UnityEngine;

namespace Voss.Core
{
    /// <summary>
    /// Thin service around the attempt counter. Puzzles call Deduct(); UI listens to events.
    /// The counter itself lives in GameState so it is shared across the whole game.
    /// </summary>
    public class AttemptManager : MonoBehaviour
    {
        public event Action<int> AttemptsChanged;
        public event Action AttemptsExhausted;

        private GameState state;

        public int Remaining => state != null ? state.AttemptsRemaining : 0;
        public bool HasAttempts => Remaining > 0;

        public void Bind(GameState gameState)
        {
            if (state != null) state.AttemptsChanged -= OnStateAttemptsChanged;
            state = gameState;
            if (state != null) state.AttemptsChanged += OnStateAttemptsChanged;
        }

        private void OnDestroy()
        {
            if (state != null) state.AttemptsChanged -= OnStateAttemptsChanged;
        }

        /// <summary>Deducts one attempt. Returns true if the player still has attempts left.</summary>
        public bool Deduct()
        {
            if (state == null) return false;
            if (!state.PuzzleInputAllowed) return false;
            int left = state.DeductAttempt();
            return left > 0;
        }

        private void OnStateAttemptsChanged(int remaining)
        {
            AttemptsChanged?.Invoke(remaining);
            if (remaining <= 0) AttemptsExhausted?.Invoke();
        }
    }
}
