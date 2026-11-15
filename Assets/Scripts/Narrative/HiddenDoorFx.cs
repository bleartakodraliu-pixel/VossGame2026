using System.Collections.Generic;
using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.Interaction;

namespace Voss.Narrative
{
    /// <summary>The section of wall that is a door: when it rises, the hall shakes with it.</summary>
    public class HiddenDoorFx : MonoBehaviour
    {
        [SerializeField] private DoorController door;
        [SerializeField] private ParticleSystem dust;
        [SerializeField] private List<Light> insideLights = new List<Light>();

        public void Configure(DoorController target, ParticleSystem burst, List<Light> lights)
        {
            door = target;
            dust = burst;
            insideLights = lights;
        }

        private void OnEnable() { if (door != null) door.Opened += OnOpened; }
        private void OnDisable() { if (door != null) door.Opened -= OnOpened; }

        private void Start()
        {
            bool open = door != null && (door.State == DoorState.Open || door.State == DoorState.Opening);
            foreach (var l in insideLights) if (l != null) l.enabled = open;
        }

        private void OnOpened(DoorController d)
        {
            var gm = GameManager.Instance;
            gm?.Audio.PlayAt(SfxId.DoorSlam, transform.position + Vector3.up * 1.5f, 1f);
            gm?.Audio.PlayAt(SfxId.PowerDown, transform.position + Vector3.up * 1.5f, 1f);
            if (dust != null) dust.Play();
            foreach (var l in insideLights) if (l != null) l.enabled = true;
        }
    }
}
