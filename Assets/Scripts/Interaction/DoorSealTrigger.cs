using UnityEngine;
using Voss.Player;

namespace Voss.Interaction
{
    /// <summary>Trigger volume on the room side of a door. Tells the door the player has entered.</summary>
    [RequireComponent(typeof(Collider))]
    public class DoorSealTrigger : MonoBehaviour
    {
        [SerializeField] private DoorController door;

        public void Bind(DoorController target) { door = target; }

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (door == null) return;
            if (other.GetComponentInParent<FirstPersonController>() == null) return;
            door.NotifyPlayerPassed();
        }
    }
}
