using UnityEngine;
using Voss.Core;
using Voss.Player;

namespace Voss.Interaction
{
    /// <summary>A prop the player can pick up and examine. Optionally shows a subtitle note.</summary>
    public class InspectableObject : MonoBehaviour, IInteractable
    {
        [SerializeField] private string prompt = "Inspect";
        [SerializeField] [TextArea] private string note;
        [SerializeField] private float inspectScale = 1f;
        [SerializeField] private Transform inspectRoot;

        public string Note { get => note; set => note = value; }
        public string Prompt { get => prompt; set => prompt = value; }
        public Transform InspectRoot { get => inspectRoot; set => inspectRoot = value; }

        public string GetInteractionPrompt() => prompt;
        public bool CanInteract() => true;

        public void Interact(PlayerInteractor player)
        {
            if (player == null || player.Inspection == null) return;
            GameManager.Instance?.Audio.Play(Audio.SfxId.Paper, 0.4f);
            player.Inspection.BeginInspection(inspectRoot != null ? inspectRoot : transform, inspectScale, null, note);
        }
    }
}
