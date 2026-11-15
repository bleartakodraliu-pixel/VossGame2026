using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Voss.Core;
using Voss.UI;

namespace Voss.Player
{
    /// <summary>
    /// Camera-local human hands: rounded palms, three-jointed fingers of natural length with nails,
    /// an opposable thumb, wrists and coat sleeves. Fingers relax, open as a hand reaches out and
    /// close around whatever is being examined; hands bob while walking.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class FirstPersonHands : MonoBehaviour
    {
        public const int HandsLayer = 8;

        private class Hand
        {
            public Transform Root;
            public readonly List<Transform[]> Fingers = new List<Transform[]>(); // index..pinky, 3 joints each
            public Transform[] Thumb;
            public float Curl;
        }

        // Finger proportions (metres): proximal, middle, distal lengths and base width.
        private static readonly float[,] FingerSizes =
        {
            { 0.040f, 0.024f, 0.020f, 0.0180f }, // index
            { 0.044f, 0.027f, 0.021f, 0.0185f }, // middle
            { 0.041f, 0.025f, 0.020f, 0.0172f }, // ring
            { 0.032f, 0.019f, 0.018f, 0.0150f }, // little
        };
        private static readonly float[] FingerX = { 0.029f, 0.010f, -0.009f, -0.027f }; // from the thumb side
        private static readonly float[] FingerZ = { 0.092f, 0.096f, 0.093f, 0.085f };

        private Transform rig;
        private Hand left;
        private Hand right;
        private FirstPersonController controller;
        private InspectionController inspection;
        private Material skin;
        private Material nails;
        private Material sleeves;
        private Material cuffs;
        private Mesh palmMesh;
        private float reachTime = 1f;
        private float bobTime;

        private void Start()
        {
            controller = GetComponent<FirstPersonController>();
            inspection = GetComponent<InspectionController>();
            var camera = GetComponentInChildren<Camera>();
            if (camera == null) { enabled = false; return; }
            camera.cullingMask &= ~(1 << Voss.Puzzles.PlanarMirror.ReflectionOnlyLayer);
            rig = new GameObject("FirstPersonHands").transform;
            rig.SetParent(camera.transform, false);
            skin = MakeMaterial(new Color(0.76f, 0.57f, 0.46f), 0.38f, 0.12f);
            nails = MakeMaterial(new Color(0.8f, 0.62f, 0.56f), 0.75f, 0.06f);
            sleeves = MakeMaterial(new Color(0.09f, 0.085f, 0.08f), 0.15f, 0.06f);
            cuffs = MakeMaterial(new Color(0.72f, 0.73f, 0.74f), 0.2f, 0.1f);
            palmMesh = RoundedBox(new Vector3(0.082f, 0.03f, 0.096f), 0.013f, 4);
            left = BuildHand(-1f);
            right = BuildHand(1f);
        }

        private static Material MakeMaterial(Color color, float smoothness, float glow)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * glow); // keeps hands readable in the dark
            return material;
        }

        /// <summary>
        /// Hand frame: +z along the fingers, +y back of the hand, palm facing down. side -1 = left.
        /// With the palm down the thumb is on the inner side, so "thumb side" is -side along x.
        /// </summary>
        private Hand BuildHand(float side)
        {
            float t = -side; // thumb-side sign along x
            var hand = new Hand();
            hand.Root = new GameObject(side < 0 ? "LeftHand" : "RightHand").transform;
            hand.Root.SetParent(rig, false);
            var r = hand.Root;

            // Sleeve, shirt cuff and wrist.
            Part(r, "Sleeve", PrimitiveType.Cylinder, new Vector3(0f, -0.004f, -0.2f), new Vector3(0.078f, 0.12f, 0.07f), Quaternion.Euler(90f, 0f, 0f), sleeves);
            Part(r, "SleeveHem", PrimitiveType.Cylinder, new Vector3(0f, -0.004f, -0.082f), new Vector3(0.082f, 0.006f, 0.074f), Quaternion.Euler(90f, 0f, 0f), sleeves);
            Part(r, "Cuff", PrimitiveType.Cylinder, new Vector3(0f, -0.004f, -0.066f), new Vector3(0.066f, 0.014f, 0.058f), Quaternion.Euler(90f, 0f, 0f), cuffs);
            Part(r, "Wrist", PrimitiveType.Capsule, new Vector3(0f, -0.002f, -0.03f), new Vector3(0.056f, 0.042f, 0.034f), Quaternion.Euler(90f, 0f, 0f), skin);

            // Palm: rounded slab, slightly wider at the knuckles, with a fleshy heel.
            var palm = Part(r, "Palm", PrimitiveType.Cube, new Vector3(0f, 0f, 0.046f), Vector3.one, Quaternion.identity, skin);
            palm.GetComponent<MeshFilter>().sharedMesh = palmMesh;
            Part(r, "Heel", PrimitiveType.Sphere, new Vector3(-t * 0.012f, -0.008f, 0.014f), new Vector3(0.05f, 0.026f, 0.045f), Quaternion.identity, skin);
            Part(r, "ThumbMound", PrimitiveType.Sphere, new Vector3(t * 0.026f, -0.009f, 0.03f), new Vector3(0.034f, 0.026f, 0.05f), Quaternion.Euler(0f, t * 20f, 0f), skin);
            for (int i = 0; i < 4; i++)
                Part(r, "Knuckle", PrimitiveType.Sphere, new Vector3(t * FingerX[i], 0.005f, FingerZ[i] - 0.004f), new Vector3(0.018f, 0.014f, 0.019f), Quaternion.identity, skin);

            // Fingers: three hinged segments each, tapering, nail on the tip.
            for (int f = 0; f < 4; f++)
            {
                var joints = new Transform[3];
                Transform parent = r;
                Vector3 at = new Vector3(t * FingerX[f], 0.001f, FingerZ[f]);
                float width = FingerSizes[f, 3];
                for (int j = 0; j < 3; j++)
                {
                    float len = FingerSizes[f, j];
                    var joint = new GameObject("Finger" + f + "_" + j).transform;
                    joint.SetParent(parent, false);
                    joint.localPosition = at;
                    joint.localRotation = Quaternion.Euler(0f, -t * (f - 1.2f) * 2.5f * (j == 0 ? 1f : 0f), 0f); // slight natural fan
                    float w = width * (1f - j * 0.09f);
                    Part(joint, "Bone", PrimitiveType.Capsule, new Vector3(0f, 0f, len * 0.5f), new Vector3(w, len * 0.5f + w * 0.5f, w * 0.9f), Quaternion.Euler(90f, 0f, 0f), skin);
                    if (j == 2)
                        Part(joint, "Nail", PrimitiveType.Sphere, new Vector3(0f, w * 0.36f, len * 0.55f), new Vector3(w * 0.66f, 0.004f, len * 0.5f), Quaternion.identity, nails);
                    joints[j] = joint;
                    parent = joint;
                    at = new Vector3(0f, 0f, len);
                }
                hand.Fingers.Add(joints);
            }

            // Thumb: metacarpal angled out from the side of the palm, then two segments.
            var thumb = new Transform[3];
            Transform tParent = r;
            Vector3 tAt = new Vector3(t * 0.028f, -0.008f, 0.026f);
            float[] tLen = { 0.03f, 0.028f, 0.024f };
            for (int j = 0; j < 3; j++)
            {
                var joint = new GameObject("Thumb_" + j).transform;
                joint.SetParent(tParent, false);
                joint.localPosition = tAt;
                // Forward and out to the thumb side, a little below the palm; the nail faces outward and up.
                if (j == 0) joint.localRotation = Quaternion.LookRotation(new Vector3(t * 0.62f, -0.22f, 0.75f).normalized, new Vector3(t * 0.6f, 0.8f, 0f).normalized);
                float w = 0.022f * (1f - j * 0.08f);
                Part(joint, "Bone", PrimitiveType.Capsule, new Vector3(0f, 0f, tLen[j] * 0.5f), new Vector3(w, tLen[j] * 0.5f + w * 0.5f, w * 0.9f), Quaternion.Euler(90f, 0f, 0f), skin);
                if (j == 2)
                    Part(joint, "Nail", PrimitiveType.Sphere, new Vector3(0f, w * 0.36f, tLen[j] * 0.55f), new Vector3(w * 0.68f, 0.004f, tLen[j] * 0.5f), Quaternion.identity, nails);
                thumb[j] = joint;
                tParent = joint;
                tAt = new Vector3(0f, 0f, tLen[j]);
            }
            hand.Thumb = thumb;
            hand.Curl = 0.35f;
            Pose(hand, side, hand.Curl);
            return hand;
        }

        /// <summary>curl 0 = open flat hand, 1 = closed grip.</summary>
        private static void Pose(Hand hand, float side, float curl)
        {
            for (int f = 0; f < hand.Fingers.Count; f++)
            {
                var joints = hand.Fingers[f];
                float extra = f * 0.06f; // outer fingers curl a little more
                Bend(joints[0], Mathf.Lerp(4f, 62f, Mathf.Clamp01(curl + extra)));
                Bend(joints[1], Mathf.Lerp(6f, 78f, Mathf.Clamp01(curl + extra)));
                Bend(joints[2], Mathf.Lerp(3f, 48f, Mathf.Clamp01(curl + extra)));
            }
            hand.Thumb[1].localRotation = Quaternion.Euler(Mathf.Lerp(4f, 30f, curl), 0f, 0f);
            hand.Thumb[2].localRotation = Quaternion.Euler(Mathf.Lerp(6f, 35f, curl), 0f, 0f);
        }

        private static void Bend(Transform joint, float degrees)
        {
            var e = joint.localEulerAngles;
            joint.localRotation = Quaternion.Euler(degrees, e.y, 0f); // +x bends toward the palm
        }

        private static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 position,
            Vector3 scale, Quaternion rotation, Material material)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.layer = HandsLayer;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.transform.localRotation = rotation;
            var collider = part.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            var renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return part;
        }

        /// <summary>Box of the given size whose edges are rounded with the given radius (no stretching of the rounding).</summary>
        private static Mesh RoundedBox(Vector3 size, float radius, int seg)
        {
            var half = size * 0.5f;
            var inner = half - Vector3.one * radius;
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var tris = new List<int>();
            int n = seg * 2 + 2; // grid per face edge
            Vector3[] normals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var fn in normals)
            {
                Vector3 v = Mathf.Abs(fn.y) > 0.5f ? Vector3.forward : Vector3.up;
                Vector3 u = Vector3.Cross(fn, v);
                int start = verts.Count;
                for (int y = 0; y <= n; y++)
                    for (int x = 0; x <= n; x++)
                    {
                        Vector3 cube = fn + u * (x / (float)n * 2f - 1f) + v * (y / (float)n * 2f - 1f); // on the unit cube
                        Vector3 p = Vector3.Scale(cube, half);
                        Vector3 clamped = new Vector3(Mathf.Clamp(p.x, -inner.x, inner.x), Mathf.Clamp(p.y, -inner.y, inner.y), Mathf.Clamp(p.z, -inner.z, inner.z));
                        Vector3 dir = (p - clamped).normalized;
                        verts.Add(clamped + dir * radius);
                        norms.Add(dir);
                    }
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        int i = start + y * (n + 1) + x;
                        tris.AddRange(new[] { i, i + n + 1, i + n + 2, i, i + n + 2, i + 1 });
                    }
            }
            var mesh = new Mesh { name = "PalmMesh" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        public void PlayInteract() { reachTime = 0f; }

        private void LateUpdate()
        {
            if (rig == null) return;
            reachTime = Mathf.Min(1f, reachTime + Time.deltaTime / 0.5f);
            bool examining = inspection != null && inspection.IsInspecting;
            var ui = UIManager.Instance;
            var gm = GameManager.Instance;
            bool visible = gm != null && gm.State.Status == GameStatus.Playing &&
                (ui == null || (!ui.IsModalOpen && !controller.ControlLocked) || examining || reachTime < 1f);
            rig.gameObject.SetActive(visible);
            if (!visible) return;

            bool moving = controller != null && controller.IsMoving && !examining && (ui == null || !ui.BlocksGameplay);
            float rate = controller != null && controller.IsSprinting ? 13f : 8f;
            if (moving) bobTime += Time.deltaTime * rate;
            float bob = moving ? Mathf.Sin(bobTime) * 0.012f : Mathf.Sin(Time.time * 1.7f) * 0.002f;
            Vector3 leftRest = new Vector3(-0.23f, -0.228f + bob, 0.43f);
            Vector3 rightRest = new Vector3(0.23f, -0.228f - bob, 0.43f);
            if (examining)
            {
                leftRest = new Vector3(-0.19f, -0.13f, 0.58f);
                rightRest = new Vector3(0.19f, -0.13f, 0.58f);
            }
            float reach = examining ? 0f : Mathf.Sin(reachTime * Mathf.PI);
            float smoothing = 1f - Mathf.Exp(-18f * Time.deltaTime);
            left.Root.localPosition = Vector3.Lerp(left.Root.localPosition, leftRest, smoothing);
            right.Root.localPosition = Vector3.Lerp(right.Root.localPosition,
                rightRest + new Vector3(-0.09f, 0.1f, 0.2f) * reach, smoothing);
            // Palms turned slightly toward each other, as hands hang when relaxed.
            left.Root.localRotation = Quaternion.Euler(-8f, 12f, 16f);
            right.Root.localRotation = Quaternion.Euler(-8f - reach * 15f, -12f, -16f + reach * 10f);

            // Fingers: relaxed at rest, open while reaching, closed around what is being examined.
            float idle = 0.2f + Mathf.Sin(Time.time * 0.9f) * 0.02f;
            float leftTarget = examining ? 0.75f : idle;
            float rightTarget = examining ? 0.75f : Mathf.Lerp(idle, 0.02f, reach);
            float curlRate = 1f - Mathf.Exp(-12f * Time.deltaTime);
            left.Curl = Mathf.Lerp(left.Curl, leftTarget, curlRate);
            right.Curl = Mathf.Lerp(right.Curl, rightTarget, curlRate);
            Pose(left, -1f, left.Curl);
            Pose(right, 1f, right.Curl);
        }

        private void OnDestroy()
        {
            if (rig != null) Destroy(rig.gameObject);
            foreach (var m in new[] { skin, nails, sleeves, cuffs }) if (m != null) Destroy(m);
            if (palmMesh != null) Destroy(palmMesh);
        }
    }
}
