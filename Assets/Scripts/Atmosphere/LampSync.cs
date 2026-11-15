using UnityEngine;

namespace Voss.Atmosphere
{
    /// <summary>
    /// Keeps a fixture's glowing tube in step with its light: when the light is off, drops out in a
    /// flicker or dies in a blackout, the tube goes dark too.
    /// </summary>
    public class LampSync : MonoBehaviour
    {
        [SerializeField] private Light source;
        [SerializeField] private Renderer[] glow;
        [SerializeField] private Material on;
        [SerializeField] private Material off;

        private float reference;
        private bool initialised;
        private bool lastLit;

        public void Configure(Light light, Renderer[] renderers, Material lit, Material unlit)
        {
            source = light;
            glow = renderers;
            on = lit;
            off = unlit;
        }

        private void Start()
        {
            if (source != null) reference = Mathf.Max(0.01f, source.intensity);
        }

        private void LateUpdate()
        {
            bool lit = source != null && source.enabled && source.gameObject.activeInHierarchy && source.intensity > reference * 0.25f;
            if (initialised && lit == lastLit) return;
            initialised = true;
            lastLit = lit;
            foreach (var r in glow) if (r != null) r.sharedMaterial = lit ? on : off;
        }
    }
}
