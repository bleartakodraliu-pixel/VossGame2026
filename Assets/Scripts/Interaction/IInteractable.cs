using Voss.Player;

namespace Voss.Interaction
{
    /// <summary>Anything the player can look at and use. Implemented by doors, props, terminals, puzzles.</summary>
    public interface IInteractable
    {
        /// <summary>Contextual verb shown on the HUD, e.g. "Inspect", "Use Terminal".</summary>
        string GetInteractionPrompt();

        /// <summary>Whether the object currently accepts interaction. Locked objects still return true
        /// so they can give feedback instead of failing silently; see Interact.</summary>
        bool CanInteract();

        void Interact(PlayerInteractor player);
    }
}
