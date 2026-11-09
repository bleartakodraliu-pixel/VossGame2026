using System.Collections;
using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.Interaction;
using Voss.Player;

namespace Voss.Interaction
{
    /// <summary>The Black Box: locked until Reason is solved, then opens by itself to reveal a card and no key.</summary>
    public class BlackBoxController : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform lid;
        [SerializeField] private GameObject backwardsCard;
        [SerializeField] private float openAngle = 110f;
        [SerializeField] private float openSeconds = 1.6f;

        private bool opened;

        public bool IsOpen => opened;

        public void Configure(Transform lidTransform, GameObject card)
        {
            lid = lidTransform;
            backwardsCard = card;
        }

        private void Start()
        {
            if (backwardsCard != null) backwardsCard.SetActive(false);
        }

        public string GetInteractionPrompt() => opened ? string.Empty : "Open";
        public bool CanInteract() => !opened;

        public void Interact(PlayerInteractor player)
        {
            var gm = GameManager.Instance;
            gm?.Audio.PlayAt(SfxId.DoorLock, transform.position, 0.7f);
            gm?.Subtitles.Show("Locked. The balance decides when it opens.", 2.5f);
        }

        public Coroutine OpenBox()
        {
            if (opened) return null;
            opened = true;
            return StartCoroutine(OpenRoutine());
        }

        public void SetOpenInstant()
        {
            opened = true;
            if (lid != null) lid.localRotation = Quaternion.Euler(openAngle, 0f, 0f);
            if (backwardsCard != null) backwardsCard.SetActive(true);
        }

        private IEnumerator OpenRoutine()
        {
            var gm = GameManager.Instance;
            gm?.Audio.PlayAt(SfxId.MetalMechanism, transform.position, 0.9f);
            yield return new WaitForSeconds(0.6f);
            gm?.Audio.PlayAt(SfxId.BlackBoxOpen, transform.position, 1f);
            if (lid != null)
            {
                Quaternion start = lid.localRotation;
                Quaternion end = Quaternion.Euler(openAngle, 0f, 0f);
                float t = 0f;
                while (t < openSeconds)
                {
                    t += Time.deltaTime;
                    lid.localRotation = Quaternion.Slerp(start, end, Mathf.SmoothStep(0f, 1f, t / openSeconds));
                    yield return null;
                }
                lid.localRotation = end;
            }
            if (backwardsCard != null) backwardsCard.SetActive(true);
        }
    }
}
