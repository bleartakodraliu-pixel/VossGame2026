using System;

namespace Voss.Core
{
    /// <summary>The current goal in one line, derived purely from progression state (shown in the evidence panel).</summary>
    public static class Objectives
    {
        /// <summary>Set by the introduction scene while the player is still in the theatre; cleared on leaving.</summary>
        public static string Introduction;

        public static string For(GameState s, Func<string, bool> hasEvidence)
        {
            if (s == null) return string.Empty;
            if (s.CurrentStage == GameStage.Introduction && !string.IsNullOrEmpty(Introduction)) return Introduction;
            bool Has(string id) => hasEvidence == null || hasEvidence(id);

            if (s.FinalTruthSolved) return "Experiment completed.";
            if (s.MetaCodeSolved) return "Return to the Main Hall. Go through the door that was not there before. Watch both screens, then answer: how did Voss disappear?";
            if (s.RememberedTicketCode) return "MEMORY - What was the first thing he gave you?";
            if (s.ReasonSolved) return Has(EvidenceIds.Backwards)
                ? "Return to the Main Hall. The door to MEMORY asks for a number."
                : "Take what the Black Box was hiding.";
            if (s.PerceptionSolved) return Has(EvidenceIds.Read)
                ? "REASON - The second door in the Main Hall is open. Find the value of the BLACK BOX."
                : "Take the card from the hidden compartment.";
            return "PERCEPTION - Find the reflection that cannot exist.";
        }
    }
}
