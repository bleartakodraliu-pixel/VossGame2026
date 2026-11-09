using UnityEngine;
using UnityEngine.Events;
using Voss.Player;

namespace Voss.Interaction
{
    /// <summary>Generic interactable that forwards to a UnityEvent. Used by scripted sequences.</summary>
    public class RelayInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string prompt = "Use";
        [SerializeField] private bool interactable = true;
        [SerializeField] private UnityEvent onInteract = new UnityEvent();

        public UnityEvent OnInteract => onInteract;
        public string Prompt { get => prompt; set => prompt = value; }
        public bool Interactable { get => interactable; set => interactable = value; }

        public string GetInteractionPrompt() => interactable ? prompt : string.Empty;
        public bool CanInteract() => interactable;

        public void Interact(PlayerInteractor player)
        {
            if (!interactable) return;
            onInteract.Invoke();
        }
    }
}
