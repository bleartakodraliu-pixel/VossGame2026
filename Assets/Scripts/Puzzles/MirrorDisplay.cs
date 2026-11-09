using UnityEngine;
using Voss.Core;
using Voss.Player;
using Voss.Interaction;
using Voss.UI;

namespace Voss.Puzzles
{
    /// <summary>
    /// One exhibit in the Perception gallery. PlanarMirror renders the real room; the plant alone
    /// substitutes a deliberately wrong-handed prop to preserve the observation puzzle.
    /// </summary>
    public class MirrorDisplay : MonoBehaviour, IInteractable
    {
        [SerializeField] private string displayId;
        [SerializeField] private bool isImpossible;
        [SerializeField] private Transform reflectionRoot;
        [SerializeField] private Renderer mirrorRenderer;
        [SerializeField] private PerceptionPuzzle puzzle;

        private Material mirrorMaterial;
        private bool selectable = true;
        private PlanarMirror planarMirror;

        private void Awake()
        {
            if (mirrorRenderer == null) mirrorRenderer = GetComponent<Renderer>();
            var shader = Resources.Load<Shader>("Shaders/PlanarMirror");
            if (shader == null || mirrorRenderer == null) return;
            mirrorMaterial = new Material(shader);
            mirrorRenderer.material = mirrorMaterial;
            planarMirror = GetComponent<PlanarMirror>();
            if (planarMirror == null) planarMirror = gameObject.AddComponent<PlanarMirror>();
            planarMirror.Configure(transform.parent.Find("Object"), reflectionRoot, isImpossible, mirrorMaterial);
        }

        private void OnDestroy()
        {
            if (mirrorMaterial == null) return;
            if (Application.isPlaying) Destroy(mirrorMaterial);
            else DestroyImmediate(mirrorMaterial);
        }

        public string DisplayId => displayId;
        public bool IsImpossible => isImpossible;

        public void Configure(string id, bool impossible, Transform reflection, Renderer mirror, PerceptionPuzzle owner)
        {
            displayId = id;
            isImpossible = impossible;
            reflectionRoot = reflection;
            mirrorRenderer = mirror;
            puzzle = owner;
        }

        private Material MirrorMaterial
        {
            get
            {
                if (mirrorMaterial == null && mirrorRenderer != null) mirrorMaterial = mirrorRenderer.material;
                return mirrorMaterial;
            }
        }

        public string GetInteractionPrompt() => selectable ? "Select this reflection" : string.Empty;
        public bool CanInteract() => selectable && puzzle != null && !puzzle.IsSolved;

        public void Interact(PlayerInteractor player)
        {
            var ui = UIManager.Instance;
            if (ui == null || puzzle == null) return;
            ui.Choice.Open("THE IMPOSSIBLE REFLECTION",
                $"Declare the reflection of the {displayId} physically impossible?",
                new[] { "YES — THIS ONE", "NO — KEEP OBSERVING" },
                i =>
                {
                    ui.Choice.Close();
                    if (i == 0) puzzle.Select(this);
                }, true, null);
        }

        public void SetSelectable(bool value) { selectable = value; }

        public void SetReflectionVisible(bool visible)
        {
            planarMirror?.SetReflectionVisible(visible);
            if (reflectionRoot != null) reflectionRoot.gameObject.SetActive(visible && isImpossible);
        }

        public void SetMirrorGlow(Color color, float intensity)
        {
            var m = MirrorMaterial;
            if (m == null) return;
            if (intensity > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * intensity);
            }
            else
            {
                m.SetColor("_EmissionColor", Color.black);
            }
        }

        public void SetMirrorOpaqueBlack()
        {
            var m = MirrorMaterial;
            if (m == null) return;
            m.SetColor("_EmissionColor", Color.black);
            m.SetColor("_BaseColor", new Color(0.01f, 0.01f, 0.01f, 1f));
            planarMirror?.SetReflectionVisible(false);
        }
    }
}
