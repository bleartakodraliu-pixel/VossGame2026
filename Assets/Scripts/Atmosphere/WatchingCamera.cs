using UnityEngine;

namespace Voss.Atmosphere
{
    /// <summary>Surveillance camera that slowly sweeps, and turns to follow the player once they are close.</summary>
    public class WatchingCamera : MonoBehaviour
    {
        [SerializeField] private Transform head;
        [SerializeField] private Renderer led;
        [SerializeField] private float range = 12f;
        [SerializeField] private float turnSpeed = 35f;
        [SerializeField] private float sweepDegrees = 35f;

        private Quaternion rest;

        public void Configure(Transform pivot, Renderer indicator)
        {
            head = pivot;
            led = indicator;
        }

        private void Start()
        {
            if (head != null) rest = head.rotation;
        }

        private void Update()
        {
            if (led != null) led.enabled = Mathf.Repeat(Time.time, 1.4f) < 0.18f;
            var cam = Camera.main;
            if (head == null || cam == null) return;

            Vector3 to = cam.transform.position - head.position;
            Quaternion target = to.magnitude <= range
                ? Quaternion.LookRotation(to.normalized, Vector3.up)
                : rest * Quaternion.Euler(0f, Mathf.Sin(Time.time * 0.25f) * sweepDegrees, 0f);
            head.rotation = Quaternion.RotateTowards(head.rotation, target, turnSpeed * Time.deltaTime);
        }
    }
}
