using Voss.Core;

namespace Voss.Puzzles
{
    public interface IPuzzle
    {
        string PuzzleId { get; }
        PuzzleState State { get; }
        /// <summary>Submit a candidate answer. Returns true when correct. Wrong answers cost an attempt.</summary>
        bool Submit(string input);
    }

    public interface IResettable
    {
        void ResetState();
    }
}
