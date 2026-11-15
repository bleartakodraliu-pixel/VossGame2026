using UnityEngine;

namespace Voss.Atmosphere
{
    /// <summary>Slow constant rotation around a local axis (gears, the helix in the tank).</summary>
    public class Spin : MonoBehaviour
    {
        [SerializeField] private Vector3 axis = Vector3.forward;
        [SerializeField] private float degreesPerSecond = 10f;

        public void Configure(Vector3 localAxis, float speed)
        {
            axis = localAxis;
            degreesPerSecond = speed;
        }

        private void Update()
        {
            transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
