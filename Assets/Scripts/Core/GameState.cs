using System;
using UnityEngine;

namespace Voss.Core
{
    /// <summary>
    /// Pure data container for global game state. No presentation logic lives here.
    /// Changes are broadcast through events so UI and world objects can react.
    /// </summary>
    [Serializable]
    public class GameState
    {
        public const int MaxAttempts = 5;

        [SerializeField] private int attemptsRemaining = MaxAttempts;
        [SerializeField] private GameStage currentStage = GameStage.Introduction;
        [SerializeField] private GameStatus status = GameStatus.NotStarted;

        [SerializeField] private bool perceptionSolved;
        [SerializeField] private bool reasonSolved;
        [SerializeField] private bool memorySolved;
        [SerializeField] private bool hasReadClue;
        [SerializeField] private bool hasBackwardsClue;
        [SerializeField] private bool rememberedTicketCode;
        [SerializeField] private bool metaCodeSolved;
        [SerializeField] private bool finalTruthSolved;

        [SerializeField] private bool perceptionUnlocked = true;
        [SerializeField] private bool reasonUnlocked;
        [SerializeField] private bool memoryUnlocked;
        [SerializeField] private bool finalExitUnlocked;
        [SerializeField] private bool silhouetteEventPlayed;

        public event Action<int> AttemptsChanged;
        public event Action<GameStage> StageChanged;
        public event Action<GameStatus> StatusChanged;
        public event Action FlagsChanged;

        public int AttemptsRemaining => attemptsRemaining;
        public GameStage CurrentStage => currentStage;
        public GameStatus Status => status;

        public bool PerceptionSolved => perceptionSolved;
        public bool ReasonSolved => reasonSolved;
        public bool MemorySolved => memorySolved;
        public bool HasReadClue => hasReadClue;
        public bool HasBackwardsClue => hasBackwardsClue;
        public bool RememberedTicketCode => rememberedTicketCode;
        public bool MetaCodeSolved => metaCodeSolved;
        public bool FinalTruthSolved => finalTruthSolved;

        public bool PerceptionUnlocked => perceptionUnlocked;
        public bool ReasonUnlocked => reasonUnlocked;
        public bool MemoryUnlocked => memoryUnlocked;
        public bool FinalExitUnlocked => finalExitUnlocked;
        public bool SilhouetteEventPlayed => silhouetteEventPlayed;

        public bool IsLost => status == GameStatus.Lost;
        public bool PuzzleInputAllowed => status == GameStatus.Playing && attemptsRemaining > 0;

        public void Reset()
        {
            attemptsRemaining = MaxAttempts;
            currentStage = GameStage.Introduction;
            status = GameStatus.NotStarted;
            perceptionSolved = reasonSolved = memorySolved = false;
            hasReadClue = hasBackwardsClue = rememberedTicketCode = false;
            metaCodeSolved = finalTruthSolved = false;
            perceptionUnlocked = true;
            reasonUnlocked = memoryUnlocked = finalExitUnlocked = false;
            silhouetteEventPlayed = false;

            AttemptsChanged?.Invoke(attemptsRemaining);
            StageChanged?.Invoke(currentStage);
            StatusChanged?.Invoke(status);
            FlagsChanged?.Invoke();
        }

        public void SetStatus(GameStatus newStatus)
        {
            if (status == newStatus) return;
            status = newStatus;
            StatusChanged?.Invoke(status);
        }

        public void SetStage(GameStage stage)
        {
            if (currentStage == stage) return;
            currentStage = stage;
            StageChanged?.Invoke(stage);
        }

        /// <summary>Returns the new attempt count. Never goes below zero.</summary>
        public int DeductAttempt()
        {
            if (attemptsRemaining <= 0) return 0;
            attemptsRemaining = Mathf.Max(0, attemptsRemaining - 1);
            AttemptsChanged?.Invoke(attemptsRemaining);
            if (attemptsRemaining == 0)
            {
                SetStatus(GameStatus.Lost);
                SetStage(GameStage.Lost);
            }
            return attemptsRemaining;
        }

        public void MarkPerceptionSolved()
        {
            if (perceptionSolved) return;
            perceptionSolved = true;
            hasReadClue = true;
            reasonUnlocked = true;
            FlagsChanged?.Invoke();
        }

        public void MarkReasonSolved()
        {
            if (reasonSolved) return;
            reasonSolved = true;
            hasBackwardsClue = true;
            memoryUnlocked = true;
            FlagsChanged?.Invoke();
        }

        public void MarkTicketRemembered()
        {
            if (rememberedTicketCode) return;
            rememberedTicketCode = true;
            FlagsChanged?.Invoke();
        }

        public void MarkMemorySolved()
        {
            if (memorySolved) return;
            memorySolved = true;
            metaCodeSolved = true;
            FlagsChanged?.Invoke();
            SetStage(GameStage.FinalInvestigation);
        }

        public void MarkFinalTruthSolved()
        {
            if (finalTruthSolved) return;
            finalTruthSolved = true;
            finalExitUnlocked = true;
            FlagsChanged?.Invoke();
            SetStage(GameStage.Ending);
        }

        public void MarkSilhouetteEventPlayed()
        {
            silhouetteEventPlayed = true;
        }
    }
}
