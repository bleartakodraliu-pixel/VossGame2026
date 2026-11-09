using UnityEngine;
using UnityEngine.Events;
using Voss.Player;

namespace Voss.Interaction
{
    /// <summary>Fires an event when the player enters. Optionally only once.</summary>
    [RequireComponent(typeof(Collider))]
    public class PlayerTriggerZone : MonoBehaviour
    {
        [SerializeField] private bool once = true;
        [SerializeField] private UnityEvent onPlayerEnter = new UnityEvent();

        private bool fired;

        public UnityEvent OnPlayerEnter => onPlayerEnter;
        public bool Once { get => once; set => once = value; }

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (once && fired) return;
            if (other.GetComponentInParent<FirstPersonController>() == null) return;
            fired = true;
            onPlayerEnter.Invoke();
        }

        public void ResetZone() { fired = false; }
    }
}
