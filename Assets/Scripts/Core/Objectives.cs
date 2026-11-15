using System;

namespace Voss.Core
{
    /// <summary>The current goal in one line, derived purely from progression state (shown in the evidence panel).</summary>
    public static class Objectives
    {
        public static string For(GameState s, Func<string, bool> hasEvidence)
        {
            if (s == null) return string.Empty;
            bool Has(string id) => hasEvidence == null || hasEvidence(id);

            if (s.FinalTruthSolved) return "Leave the laboratory.";
            if (s.MetaCodeSolved) return "Go back to the hall. Watch the performance again, then answer the final question.";
            if (s.RememberedTicketCode) return "MEMORY - What was the first thing he gave you?";
            if (s.ReasonSolved) return Has(EvidenceIds.Backwards)
                ? "Follow the corridor. The door to MEMORY asks for a number."
                : "Take what the Black Box was hiding.";
            if (s.PerceptionSolved) return Has(EvidenceIds.Read)
                ? "REASON - Find the value of the BLACK BOX."
                : "Take the card from the hidden compartment.";
            return "PERCEPTION - Find the reflection that cannot exist.";
        }
    }
}
