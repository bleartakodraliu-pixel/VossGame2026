namespace Voss.Core
{
    public enum GameStage
    {
        Introduction,
        Perception,
        Reason,
        Memory,
        FinalInvestigation,
        Ending,
        Lost
    }

    public enum GameStatus
    {
        NotStarted,
        Playing,
        Lost,
        Completed
    }

    public enum PuzzleState
    {
        Locked,
        Active,
        Solved
    }

    public enum DoorState
    {
        Locked,
        Closed,
        Opening,
        Open
    }

    /// <summary>Scene names used by the build. Keep in sync with the editor scene generator.</summary>
    public static class SceneNames
    {
        public const string MainMenu = "00_MainMenu";
        public const string Introduction = "VossIntroduction";    // playable theatre: envelope, letter, laboratory door
        public const string Prologue = "00b_Prologue";            // the vanishing, seen from the audience
        public const string Invitation = "01_Invitation";
        public const string Arrival = "01b_TheatreArrival";       // the abandoned theatre and its hidden elevator
        public const string Laboratory = "02_VossLaboratory";
        public const string Theatre = "03_EmptyTheatre";
    }

    /// <summary>Identifiers for evidence items shared between puzzles and the evidence menu.</summary>
    public static class EvidenceIds
    {
        public const string Ticket = "voss_ticket";
        public const string Letter = "voss_letter";
        public const string Read = "card_read";
        public const string Backwards = "card_backwards";
    }

    /// <summary>Canonical puzzle answers. Kept in one place so logic and QA agree.</summary>
    public static class Answers
    {
        public const string TicketCode = "7314";
        public const string ReasonAnswer = "4";
        public const string MetaCode = "4137";
        public const int FinalCorrectChoiceIndex = 3; // "He Never Entered"
    }
}
