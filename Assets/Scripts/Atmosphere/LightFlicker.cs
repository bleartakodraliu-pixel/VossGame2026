using UnityEngine;

namespace Voss.Atmosphere
{
    /// <summary>
    /// An unreliable lamp: a subtle continuous wobble, occasional stutters and brief dropouts.
    /// Works on top of whatever intensity other scripts set (external changes become the new base)
    /// and does nothing while the light is disabled, so puzzle and finale lighting keep working.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class LightFlicker : MonoBehaviour
    {
        [SerializeField] private float wobble = 0.08f;
        [SerializeField] private Vector2 stutterInterval = new Vector2(6f, 18f);
        [SerializeField] [Range(0f, 1f)] private float dropoutChance = 0.3f;
        [SerializeField] private float dropoutSeconds = 0.6f;

        private Light lamp;
        private float baseIntensity;
        private float lastWritten = float.NaN;
        private float nextStutter;
        private float stutterUntil;
        private float dropUntil;
        private float seed;

        public void Configure(float wobbleAmount, Vector2 interval, float dropout)
        {
            wobble = wobbleAmount;
            stutterInterval = interval;
            dropoutChance = dropout;
        }

        private void Awake()
        {
            lamp = GetComponent<Light>();
            baseIntensity = lamp.intensity;
            seed = Random.value * 100f;
            Schedule();
        }

        private void Schedule()
        {
            nextStutter = Time.time + Random.Range(stutterInterval.x, stutterInterval.y);
        }

        private void Update()
        {
            if (!lamp.enabled) return;
            if (lamp.intensity != lastWritten) baseIntensity = lamp.intensity; // someone else set it

            float t = Time.time;
            if (t >= nextStutter)
            {
                if (Random.value < dropoutChance) dropUntil = t + dropoutSeconds * Random.Range(0.6f, 1.4f);
                else stutterUntil = t + Random.Range(0.25f, 0.7f);
                Schedule();
            }

            float k = 1f + (Mathf.PerlinNoise(seed, t * 3f) - 0.5f) * 2f * wobble;
            if (t < dropUntil) k = 0.02f;
            else if (t < stutterUntil && Random.value < 0.5f) k *= 0.1f;

            lastWritten = baseIntensity * k;
            lamp.intensity = lastWritten;
        }
    }
}
