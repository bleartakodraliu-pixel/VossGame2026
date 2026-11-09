using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voss.Core;
using Voss.UI;

namespace Voss.Player
{
    /// <summary>
    /// Examine mode: moves a copy of the target in front of the camera, lets the player rotate it,
    /// then puts it back. The original is never modified, so world state stays consistent.
    /// </summary>
    public class InspectionController : MonoBehaviour
    {
        [SerializeField] private float holdDistance = 0.65f;
        [SerializeField] private float rotateSpeed = 4f;
        [SerializeField] private float moveInTime = 0.35f;

        private Camera cam;
        private FirstPersonController fps;
        private GameObject heldCopy;
        private Transform heldOriginal;
        private Action onEnd;
        private bool inspecting;
        private Coroutine routine;
        private Quaternion readableRotation;
        private Light inspectionLight;
        private readonly Dictionary<Renderer, bool> originalVisibility = new Dictionary<Renderer, bool>();

        public bool IsInspecting => inspecting;
        public int LastEndFrame { get; private set; } = -1;

        private void Awake()
        {
            fps = GetComponent<FirstPersonController>();
            cam = GetComponentInChildren<Camera>();
        }

        public void BeginInspection(Transform target, float scaleMultiplier = 1f, Action ended = null, string description = null)
        {
            if (inspecting || target == null || cam == null) return;
            inspecting = true;
            onEnd = ended;
            heldOriginal = target;

            heldCopy = Instantiate(target.gameObject);
            heldCopy.name = target.name + "_Inspect";
            foreach (var c in heldCopy.GetComponentsInChildren<Collider>()) c.enabled = false;
            foreach (var mb in heldCopy.GetComponentsInChildren<MonoBehaviour>())
                if (!(mb is WorldText)) mb.enabled = false;
            heldCopy.transform.SetParent(cam.transform, true);

            // The held copy replaces the visible original, so nearby surfaces never overlap it.
            originalVisibility.Clear();
            foreach (var renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                originalVisibility[renderer] = renderer.enabled;
                renderer.enabled = false;
            }

            // Hold the copy at a size that reads clearly regardless of the source scale.
            var bounds = GetBounds(heldCopy);
            float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z, 0.01f);
            float targetScale = (0.36f / size) * scaleMultiplier;
            heldCopy.transform.localScale = target.lossyScale * targetScale;

            // Present the front lettering upright first; dragging still allows reading the back.
            readableRotation = Quaternion.identity;
            var texts = heldCopy.GetComponentsInChildren<TextMesh>();
            TextMesh front = texts.Length > 0 ? texts[0] : null;
            foreach (var text in texts) if (text.name == "Front") { front = text; break; }
            if (front != null)
            {
                Quaternion relative = Quaternion.Inverse(heldCopy.transform.rotation) * front.transform.rotation;
                readableRotation = Quaternion.Inverse(relative);
            }

            if (inspectionLight == null)
            {
                var lightObject = new GameObject("InspectionLight");
                lightObject.transform.SetParent(cam.transform, false);
                lightObject.transform.localPosition = new Vector3(0f, 0.12f, 0.1f);
                inspectionLight = lightObject.AddComponent<Light>();
                inspectionLight.type = LightType.Point;
                inspectionLight.color = new Color(1f, 0.95f, 0.85f);
                inspectionLight.intensity = 1.1f;
                inspectionLight.range = 1.5f;
                inspectionLight.shadows = LightShadows.None;
            }
            inspectionLight.enabled = true;

            if (fps != null) fps.SetControlLocked(true);
            if (!string.IsNullOrEmpty(description)) GameManager.Instance?.Subtitles.Show(description, 4f);

            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(MoveIn());
        }

        private IEnumerator MoveIn()
        {
            Vector3 start = heldCopy.transform.localPosition;
            Quaternion startRot = heldCopy.transform.localRotation;

            // Measure where the visual centre lands at the hold point, then correct for it.
            heldCopy.transform.localPosition = new Vector3(0f, 0f, holdDistance);
            heldCopy.transform.localRotation = readableRotation;
            Vector3 centreLocal = cam.transform.InverseTransformPoint(GetBounds(heldCopy).center);
            Vector3 end = new Vector3(0f, 0f, holdDistance) - (centreLocal - new Vector3(0f, 0f, holdDistance));
            heldCopy.transform.localPosition = start;
            heldCopy.transform.localRotation = startRot;

            float t = 0f;
            while (t < moveInTime)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / moveInTime);
                heldCopy.transform.localPosition = Vector3.Lerp(start, end, k);
                heldCopy.transform.localRotation = Quaternion.Slerp(startRot, readableRotation, k);
                yield return null;
            }
            heldCopy.transform.localPosition = end;
            heldCopy.transform.localRotation = readableRotation;
            routine = null;
        }

        private void Update()
        {
            if (!inspecting || heldCopy == null || routine != null) return;
            if (VossInput.LeftMouseHeld())
            {
                Vector2 d = VossInput.LookDelta() * rotateSpeed;
                heldCopy.transform.Rotate(cam.transform.up, -d.x, Space.World);
                heldCopy.transform.Rotate(cam.transform.right, d.y, Space.World);
            }
            // Keep text still while reading; only deliberate dragging turns the object over.
        }

        public void EndInspection()
        {
            if (!inspecting) return;
            inspecting = false;
            LastEndFrame = Time.frameCount;
            if (routine != null) StopCoroutine(routine);
            routine = null;
            if (heldCopy != null) Destroy(heldCopy);
            heldCopy = null;
            foreach (var pair in originalVisibility)
                if (pair.Key != null) pair.Key.enabled = pair.Value;
            originalVisibility.Clear();
            if (inspectionLight != null) inspectionLight.enabled = false;
            heldOriginal = null;
            if (fps != null) fps.SetControlLocked(false);
            UIManager.Instance?.SetPrompt(string.Empty);
            var cb = onEnd; onEnd = null;
            cb?.Invoke();
        }

        private static Bounds GetBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.2f);
            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }
    }
}
