using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Voss.Player;

namespace Voss.Puzzles
{
    /// <summary>Opaque, perspective-correct mirror. Captures in LateUpdate, outside the URP render loop.</summary>
    [DefaultExecutionOrder(200)]
    public class PlanarMirror : MonoBehaviour
    {
        public const int MirrorLayer = 9;
        public const int ReflectionOnlyLayer = 10;
        private Camera reflectionCamera;
        private RenderTexture texture;
        private Renderer surface;
        private Material material;
        private Renderer[] realRenderers;
        private bool[] originalForceOff;
        private bool impossible;
        private bool reflectionVisible = true;
        private readonly UniversalRenderPipeline.SingleCameraRequest request = new UniversalRenderPipeline.SingleCameraRequest();

        public void Configure(Transform realObject, Transform reflectionDouble, bool isImpossible, Material mirrorMaterial)
        {
            surface = GetComponent<Renderer>();
            material = mirrorMaterial;
            impossible = isImpossible;
            gameObject.layer = MirrorLayer;
            if (realObject != null)
            {
                realRenderers = realObject.GetComponentsInChildren<Renderer>(true);
                originalForceOff = new bool[realRenderers.Length];
            }
            if (reflectionDouble != null && realObject != null)
            {
                // The one intentional error belongs to the plant puzzle, not the mirror math.
                reflectionDouble.localPosition = realObject.localPosition;
                reflectionDouble.localRotation = realObject.localRotation;
                reflectionDouble.localScale = new Vector3(-1f, 1f, 1f);
                foreach (var child in reflectionDouble.GetComponentsInChildren<Transform>(true))
                    child.gameObject.layer = ReflectionOnlyLayer;
                reflectionDouble.gameObject.SetActive(impossible);
            }
        }

        public void SetReflectionVisible(bool value)
        {
            reflectionVisible = value;
            if (material != null) material.SetFloat("_ReflectionEnabled", value ? 1f : 0f);
        }

        private void EnsureCamera()
        {
            if (reflectionCamera != null) return;
            var go = new GameObject("MirrorCamera_" + transform.parent.name);
            go.hideFlags = HideFlags.HideAndDontSave;
            reflectionCamera = go.AddComponent<Camera>();
            reflectionCamera.enabled = false;
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            data.renderShadows = false;
            data.requiresColorOption = CameraOverrideOption.Off;
            data.requiresDepthOption = CameraOverrideOption.Off;
            texture = new RenderTexture(768, 768, 24, RenderTextureFormat.ARGB32)
            {
                name = "Mirror_" + transform.parent.name,
                hideFlags = HideFlags.DontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                antiAliasing = 1
            };
            texture.Create();
            request.destination = texture;
            material.SetTexture("_ReflectionTex", texture);
        }

        private void LateUpdate()
        {
            if (!reflectionVisible || material == null || surface == null || !surface.enabled) return;
            var source = Camera.main;
            if (source == null || !source.isActiveAndEnabled) return;
            source.cullingMask &= ~(1 << ReflectionOnlyLayer);
            Vector3 normal = transform.forward;
            Vector3 point = transform.position + normal * 0.021f;
            Vector3 offset = source.transform.position - point;
            if (Vector3.Dot(normal, offset) <= 0.02f || offset.sqrMagnitude > 625f) return;
            if (!GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(source), surface.bounds)) return;

            EnsureCamera();
            reflectionCamera.CopyFrom(source);
            reflectionCamera.enabled = false;
            reflectionCamera.targetTexture = texture;
            reflectionCamera.rect = new Rect(0f, 0f, 1f, 1f);
            reflectionCamera.useOcclusionCulling = false;
            reflectionCamera.allowMSAA = false;
            reflectionCamera.allowHDR = false;
            reflectionCamera.cullingMask = source.cullingMask & ~((1 << MirrorLayer) | (1 << FirstPersonHands.HandsLayer));
            if (impossible) reflectionCamera.cullingMask |= 1 << ReflectionOnlyLayer;

            Matrix4x4 reflection = ReflectionMatrix(normal, point);
            reflectionCamera.transform.position = reflection.MultiplyPoint(source.transform.position);
            reflectionCamera.transform.rotation = Quaternion.LookRotation(
                Vector3.Reflect(source.transform.forward, normal), Vector3.Reflect(source.transform.up, normal));
            reflectionCamera.worldToCameraMatrix = source.worldToCameraMatrix * reflection;
            Matrix4x4 view = reflectionCamera.worldToCameraMatrix;
            Vector3 clipPoint = view.MultiplyPoint(point + normal * 0.01f);
            Vector3 clipNormal = view.MultiplyVector(normal).normalized;
            Vector4 clipPlane = new Vector4(clipNormal.x, clipNormal.y, clipNormal.z, -Vector3.Dot(clipPoint, clipNormal));
            reflectionCamera.projectionMatrix = source.projectionMatrix;
            reflectionCamera.projectionMatrix = reflectionCamera.CalculateObliqueMatrix(clipPlane);
            // SingleCameraRequest handles the render-target flip. Project with the display
            // convention so Metal/D3D reflections do not appear upside down when sampled.
            material.SetMatrix("_ReflectionVP", GL.GetGPUProjectionMatrix(reflectionCamera.projectionMatrix, false) * view);

            bool oldInvert = GL.invertCulling;
            try
            {
                if (impossible && realRenderers != null)
                    for (int i = 0; i < realRenderers.Length; i++)
                    {
                        originalForceOff[i] = realRenderers[i].forceRenderingOff;
                        realRenderers[i].forceRenderingOff = true;
                    }
                GL.invertCulling = !oldInvert;
                RenderPipeline.SubmitRenderRequest(reflectionCamera, request);
            }
            finally
            {
                GL.invertCulling = oldInvert;
                if (impossible && realRenderers != null)
                    for (int i = 0; i < realRenderers.Length; i++)
                        if (realRenderers[i] != null) realRenderers[i].forceRenderingOff = originalForceOff[i];
            }
        }

        public static Matrix4x4 ReflectionMatrix(Vector3 normal, Vector3 point)
        {
            normal.Normalize();
            float d = -Vector3.Dot(normal, point);
            var matrix = Matrix4x4.identity;
            matrix.m00 = 1f - 2f * normal.x * normal.x;
            matrix.m01 = -2f * normal.x * normal.y;
            matrix.m02 = -2f * normal.x * normal.z;
            matrix.m03 = -2f * d * normal.x;
            matrix.m10 = -2f * normal.y * normal.x;
            matrix.m11 = 1f - 2f * normal.y * normal.y;
            matrix.m12 = -2f * normal.y * normal.z;
            matrix.m13 = -2f * d * normal.y;
            matrix.m20 = -2f * normal.z * normal.x;
            matrix.m21 = -2f * normal.z * normal.y;
            matrix.m22 = 1f - 2f * normal.z * normal.z;
            matrix.m23 = -2f * d * normal.z;
            return matrix;
        }

        private void OnDestroy()
        {
            if (reflectionCamera != null)
            {
                if (Application.isPlaying) Destroy(reflectionCamera.gameObject);
                else DestroyImmediate(reflectionCamera.gameObject);
            }
            if (texture != null)
            {
                texture.Release();
                if (Application.isPlaying) Destroy(texture);
                else DestroyImmediate(texture);
            }
        }
    }
}
