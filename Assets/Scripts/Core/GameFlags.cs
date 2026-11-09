namespace Voss.Core
{
    /// <summary>Named progression conditions that doors, lights and events can wait on.</summary>
    public enum GameFlag
    {
        None,
        PerceptionUnlocked,
        ReasonUnlocked,
        MemoryUnlocked,
        PerceptionSolved,
        ReasonSolved,
        MemorySolved,
        MetaCodeSolved,
        FinalTruthSolved,
        FinalExitUnlocked
    }

    public static class GameFlags
    {
        public static bool Check(GameState state, GameFlag flag)
        {
            if (state == null) return flag == GameFlag.None;
            switch (flag)
            {
                case GameFlag.None: return true;
                case GameFlag.PerceptionUnlocked: return state.PerceptionUnlocked;
                case GameFlag.ReasonUnlocked: return state.ReasonUnlocked;
                case GameFlag.MemoryUnlocked: return state.MemoryUnlocked;
                case GameFlag.PerceptionSolved: return state.PerceptionSolved;
                case GameFlag.ReasonSolved: return state.ReasonSolved;
                case GameFlag.MemorySolved: return state.MemorySolved;
                case GameFlag.MetaCodeSolved: return state.MetaCodeSolved;
                case GameFlag.FinalTruthSolved: return state.FinalTruthSolved;
                case GameFlag.FinalExitUnlocked: return state.FinalExitUnlocked;
                default: return false;
            }
        }
    }
}
