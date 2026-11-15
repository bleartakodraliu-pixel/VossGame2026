using System.Collections.Generic;
using UnityEngine;

namespace Voss.Atmosphere
{
    /// <summary>
    /// Shadow-casting point lights are expensive (six shadow maps each), and URP renders them for every
    /// light whose range touches the view, even behind a wall. The laboratory's areas are sealed from
    /// each other, so only the lights of the area the player stands in need to cast shadows. Checked a
    /// few times a second; lights keep their colour, intensity and on/off state.
    /// </summary>
    public class ShadowZones : MonoBehaviour
    {
        [System.Serializable]
        public class Zone
        {
            public string name;
            public Bounds area;
            public List<Light> lights = new List<Light>();
        }

        [SerializeField] private List<Zone> zones = new List<Zone>();
        [SerializeField] private float interval = 0.2f;

        private readonly Dictionary<Light, LightShadows> authored = new Dictionary<Light, LightShadows>();
        private readonly HashSet<Light> wanted = new HashSet<Light>();
        private float next;

        public void AddZone(string name, Bounds area, List<Light> lights)
        {
            zones.Add(new Zone { name = name, area = area, lights = lights });
        }

        private void Start()
        {
            foreach (var z in zones)
                foreach (var l in z.lights)
                    if (l != null && !authored.ContainsKey(l)) authored[l] = l.shadows == LightShadows.None ? LightShadows.Soft : l.shadows;
            Apply();
        }

        private void Update()
        {
            if (Time.time < next) return;
            next = Time.time + interval;
            Apply();
        }

        private void Apply()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 eye = cam.transform.position;
            wanted.Clear();
            foreach (var z in zones)
                if (z.area.Contains(eye))
                    foreach (var l in z.lights) if (l != null) wanted.Add(l);
            foreach (var kv in authored)
            {
                var target = wanted.Contains(kv.Key) ? kv.Value : LightShadows.None;
                if (kv.Key != null && kv.Key.shadows != target) kv.Key.shadows = target;
            }
        }
    }
}
