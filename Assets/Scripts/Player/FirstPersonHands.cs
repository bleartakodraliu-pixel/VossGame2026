using UnityEngine;
using UnityEngine.Rendering;
using Voss.Core;
using Voss.UI;

namespace Voss.Player
{
    /// <summary>Camera-local gloved hands with walking/sprint motion, reach and inspection poses.</summary>
    [DefaultExecutionOrder(50)]
    public class FirstPersonHands : MonoBehaviour
    {
        public const int HandsLayer = 8;
        private Transform rig;
        private Transform left;
        private Transform right;
        private FirstPersonController controller;
        private InspectionController inspection;
        private Material gloves;
        private Material sleeves;
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
            gloves = MakeMaterial(new Color(0.62f, 0.58f, 0.49f), 0.3f);
            sleeves = MakeMaterial(new Color(0.065f, 0.075f, 0.09f), 0.1f);
            left = BuildHand(-1f);
            right = BuildHand(1f);
        }

        private static Material MakeMaterial(Color color, float smoothness)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.16f);
            return material;
        }

        private Transform BuildHand(float side)
        {
            var hand = new GameObject(side < 0 ? "LeftHand" : "RightHand").transform;
            hand.SetParent(rig, false);
            Part(hand, "Sleeve", PrimitiveType.Capsule, new Vector3(0f, -0.07f, -0.12f),
                new Vector3(0.082f, 0.14f, 0.082f), Quaternion.Euler(55f, 0f, 0f), sleeves);
            Part(hand, "Cuff", PrimitiveType.Cube, new Vector3(0f, -0.005f, -0.055f),
                new Vector3(0.082f, 0.052f, 0.045f), Quaternion.identity, sleeves);
            Part(hand, "Palm", PrimitiveType.Sphere, Vector3.zero,
                new Vector3(0.083f, 0.04f, 0.105f), Quaternion.identity, gloves);
            for (int i = 0; i < 4; i++)
            {
                float length = i == 0 || i == 3 ? 0.032f : 0.039f;
                Part(hand, "Finger" + i, PrimitiveType.Capsule, new Vector3((i - 1.5f) * 0.019f, -0.002f, 0.065f),
                    new Vector3(0.019f, length, 0.019f), Quaternion.Euler(78f, 0f, 0f), gloves);
            }
            Part(hand, "Thumb", PrimitiveType.Capsule, new Vector3(-side * 0.048f, -0.006f, 0.015f),
                new Vector3(0.023f, 0.035f, 0.023f), Quaternion.Euler(65f, 0f, side * 38f), gloves);
            return hand;
        }

        private static void Part(Transform parent, string name, PrimitiveType type, Vector3 position,
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
            Vector3 leftRest = new Vector3(-0.23f, -0.245f + bob, 0.43f);
            Vector3 rightRest = new Vector3(0.23f, -0.245f - bob, 0.43f);
            if (examining)
            {
                leftRest = new Vector3(-0.19f, -0.13f, 0.58f);
                rightRest = new Vector3(0.19f, -0.13f, 0.58f);
            }
            float reach = examining ? 0f : Mathf.Sin(reachTime * Mathf.PI);
            float smoothing = 1f - Mathf.Exp(-18f * Time.deltaTime);
            left.localPosition = Vector3.Lerp(left.localPosition, leftRest, smoothing);
            right.localPosition = Vector3.Lerp(right.localPosition,
                rightRest + new Vector3(-0.09f, 0.1f, 0.2f) * reach, smoothing);
            left.localRotation = Quaternion.Euler(-8f, 10f, -8f);
            right.localRotation = Quaternion.Euler(-8f - reach * 15f, -10f, 8f + reach * 12f);
        }

        private void OnDestroy()
        {
            if (rig != null) Destroy(rig.gameObject);
            if (gloves != null) Destroy(gloves);
            if (sleeves != null) Destroy(sleeves);
        }
    }
}
