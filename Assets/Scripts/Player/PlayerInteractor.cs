using UnityEngine;
using Voss.Core;
using Voss.Interaction;
using Voss.UI;

namespace Voss.Player
{
    /// <summary>Centre-screen raycast that finds IInteractable targets and drives the HUD prompt.</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        private string lastVerb;
        private string lastPrompt = string.Empty;

        [SerializeField] private Camera viewCamera;
        [SerializeField] private float interactDistance = 3.0f;
        [SerializeField] private LayerMask interactMask = ~0;

        private IInteractable current;
        private Collider currentCollider;
        private InspectionController inspection;
        private FirstPersonController fps;
        private FirstPersonHands hands;

        public Camera ViewCamera => viewCamera;
        public InspectionController Inspection => inspection;
        public FirstPersonController Controller => fps;
        public IInteractable CurrentTarget => current;

        private void Awake()
        {
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
            fps = GetComponent<FirstPersonController>();
            inspection = GetComponent<InspectionController>();
            if (inspection == null) inspection = gameObject.AddComponent<InspectionController>();
            hands = GetComponent<FirstPersonHands>();
            if (hands == null) hands = gameObject.AddComponent<FirstPersonHands>();
        }

        private void Update()
        {
            var ui = UIManager.Instance;
            if (ui == null || viewCamera == null) return;

            if (inspection.IsInspecting)
            {
                SetTarget(null, null);
                ui.SetPrompt("Rotate: drag mouse      Back: ESC / right click");
                if (VossInput.BackPressed()) inspection.EndInspection();
                return;
            }

            if (ui.BlocksGameplay || (fps != null && fps.ControlLocked))
            {
                SetTarget(null, null);
                ui.SetPrompt(string.Empty);
                return;
            }

            FindTarget();

            if (current != null)
            {
                string verb = current.GetInteractionPrompt();
                // Prompts written as a full sentence ("Press E to inspect") are shown as they are.
                // The formatted line is rebuilt only when the verb changes (no garbage every frame).
                if (verb != lastVerb)
                {
                    lastVerb = verb;
                    lastPrompt = string.IsNullOrEmpty(verb) ? string.Empty : verb.StartsWith("Press ") ? verb : $"[E]  {verb}";
                }
                ui.SetPrompt(lastPrompt);
                ui.SetCrosshairHighlight(true);
                if (VossInput.InteractPressed() && current.CanInteract())
                {
                    hands?.PlayInteract();
                    current.Interact(this);
                }
            }
            else
            {
                ui.SetPrompt(string.Empty);
                ui.SetCrosshairHighlight(false);
            }
        }

        private void FindTarget()
        {
            var ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
            if (Physics.Raycast(ray, out var hit, interactDistance, interactMask, QueryTriggerInteraction.Collide))
            {
                if (hit.collider == currentCollider && current != null) return;
                var interactable = hit.collider.GetComponentInParent<IInteractable>();
                var mb = interactable as MonoBehaviour;
                if (interactable != null && mb != null && mb.enabled && mb.gameObject.activeInHierarchy)
                {
                    SetTarget(interactable, hit.collider);
                    return;
                }
            }
            SetTarget(null, null);
        }

        private void SetTarget(IInteractable target, Collider col)
        {
            current = target;
            currentCollider = col;
        }
    }
}
