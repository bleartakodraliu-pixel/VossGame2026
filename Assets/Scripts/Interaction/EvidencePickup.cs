using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.Player;

namespace Voss.Interaction
{
    /// <summary>A physical clue card. Taking it registers evidence; the card stays visible as a display afterwards.</summary>
    public class EvidencePickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private string evidenceId;
        [SerializeField] private string prompt = "Take card";
        [SerializeField] private string takenPrompt = "Examine card";
        [SerializeField] [TextArea] private string examineNote;
        [SerializeField] private bool hideAfterPickup;

        private bool taken;

        public void Configure(string id, string note)
        {
            evidenceId = id;
            examineNote = note;
        }

        public string GetInteractionPrompt() => taken ? takenPrompt : prompt;
        public bool CanInteract() => true;

        public void Interact(PlayerInteractor player)
        {
            var gm = GameManager.Instance;
            if (!taken)
            {
                taken = true;
                gm?.Evidence.Discover(evidenceId);
                gm?.Audio.Play(SfxId.Paper, 0.8f);
                gm?.Subtitles.Show("Added to evidence. (TAB to review)", 2.5f);
                if (hideAfterPickup) { gameObject.SetActive(false); return; }
            }
            player?.Inspection?.BeginInspection(transform, 1f, null, examineNote);
        }
    }
}
