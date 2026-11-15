using System.Collections;
using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.Interaction;
using Voss.Player;
using Voss.UI;

namespace Voss.Narrative
{
    /// <summary>
    /// The steel door marked LABORATORY 7314. Locked until the letter has been taken ("Something is
    /// missing. Check the table."). Then it unlocks and slides open onto the passage to the laboratory's
    /// Main Hall (shown only from that moment), the screen fades, "Welcome to the experiment." - and the
    /// existing laboratory scene begins.
    /// </summary>
    public class LaboratoryEntrance : MonoBehaviour, IInteractable
    {
        [SerializeField] private IntroductionManager manager;
        [SerializeField] private Transform leftLeaf;
        [SerializeField] private Transform rightLeaf;
        [SerializeField] private float slide = 1.05f;
        [SerializeField] private GameObject hallPreview;

        private bool entering;

        public void Configure(IntroductionManager introduction, Transform left, Transform right, GameObject preview = null)
        {
            manager = introduction;
            leftLeaf = left;
            rightLeaf = right;
            hallPreview = preview;
        }

        private void Start()
        {
            if (hallPreview != null) hallPreview.SetActive(false);
        }

        public string GetInteractionPrompt() => entering ? string.Empty : "Press E to enter";
        public bool CanInteract() => !entering;

        public void Interact(PlayerInteractor player)
        {
            if (entering) return;
            var gm = GameManager.Instance;
            if (manager == null || !manager.LetterCollected)
            {
                gm?.Audio.PlayAt(SfxId.DoorLock, transform.position + Vector3.up * 1.5f, 0.9f);
                gm?.Subtitles.Show("Something is missing. Check the table.", 3f);
                return;
            }
            entering = true;
            StartCoroutine(Enter(player));
        }

        private IEnumerator Enter(PlayerInteractor player)
        {
            var gm = GameManager.Instance;
            var ui = UIManager.Instance;
            if (player != null && player.Controller != null) player.Controller.SetControlLocked(true);

            // Unlock, then the leaves part.
            gm?.Audio.PlayAt(SfxId.MetalMechanism, transform.position + Vector3.up * 1.5f, 1f);
            yield return new WaitForSeconds(0.35f);
            gm?.Audio.PlayAt(SfxId.DoorLock, transform.position + Vector3.up * 1.5f, 0.8f);
            yield return new WaitForSeconds(0.5f);
            gm?.Audio.PlayAt(SfxId.DoorOpen, transform.position + Vector3.up * 1.5f, 1f);
            if (hallPreview != null) hallPreview.SetActive(true);
            Vector3 l0 = leftLeaf != null ? leftLeaf.localPosition : Vector3.zero;
            Vector3 r0 = rightLeaf != null ? rightLeaf.localPosition : Vector3.zero;
            for (float t = 0f; t < 1.8f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 1.8f);
                if (leftLeaf != null) leftLeaf.localPosition = l0 + Vector3.left * slide * k;
                if (rightLeaf != null) rightLeaf.localPosition = r0 + Vector3.right * slide * k;
                yield return null;
            }
            // A moment to see where the door leads.
            yield return new WaitForSeconds(hallPreview != null ? 1.6f : 0.6f);

            // Black, a few words, then the laboratory.
            manager?.Leaving();
            gm?.Audio.StopMusic(1.5f);
            if (ui != null)
            {
                yield return ui.FadeTo(1f, 1.2f);
                ui.Story.Open(null);
                yield return ui.FadeTo(0f, 0.3f);
                gm?.Audio.Play(SfxId.VossVoice, 0.8f);
                yield return StoryCards.Play(new[] { "Welcome to the experiment." }, false);
                yield return ui.FadeTo(1f, 0.4f);
                ui.Story.Close();
            }
            gm?.Transitions.LoadScene(SceneNames.Laboratory, 0.8f);
        }
    }
}
