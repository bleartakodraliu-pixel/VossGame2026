using UnityEngine;
using Voss.Core;
using Voss.Interaction;

namespace Voss.Narrative
{
    /// <summary>
    /// Status lamp beside a chamber door in the Main Hall: red while locked or sealed, warm when the
    /// way is open (or, for a door opened by other means, once its signal light has come on), a pale
    /// cold blue once the chamber's test is complete.
    /// </summary>
    public class DoorStatusLamp : MonoBehaviour
    {
        private static readonly Color Locked = new Color(1f, 0.1f, 0.06f);
        private static readonly Color Open = new Color(1f, 0.72f, 0.38f);
        private static readonly Color Done = new Color(0.45f, 0.75f, 1f);

        [SerializeField] private DoorController door;
        [SerializeField] private Light availableSignal;
        [SerializeField] private GameFlag completedFlag = GameFlag.None;
        [SerializeField] private Light lamp;
        [SerializeField] private Renderer bulb;

        private Material bulbMat;
        private Color shown = new Color(-1f, 0f, 0f);

        public void Configure(DoorController target, Light available, GameFlag completed, Light light, Renderer glass)
        {
            door = target;
            availableSignal = available;
            completedFlag = completed;
            lamp = light;
            bulb = glass;
        }

        private void Start()
        {
            if (bulb != null) { bulbMat = bulb.material; bulbMat.EnableKeyword("_EMISSION"); }
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            bool done = completedFlag != GameFlag.None && GameFlags.Check(gm.State, completedFlag);
            bool open = (door != null && door.State != DoorState.Locked)
                || (availableSignal != null && availableSignal.enabled && availableSignal.gameObject.activeInHierarchy);
            Color c = done ? Done : open ? Open : Locked;
            if (lamp != null && lamp.color != c) lamp.color = Color.Lerp(lamp.color, c, Time.deltaTime * 4f);
            if (bulbMat != null && c != shown) { shown = c; bulbMat.SetColor("_EmissionColor", c * 2f); }
        }
    }
}
