using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Voss.Player;
using Voss.UI;

namespace Voss.EditorTools
{
    /// <summary>
    /// Shared helpers for the scene generators: materials saved as assets, primitives, walls,
    /// 3D text, lights and the player rig. Everything is graybox but clearly named so real
    /// assets can replace pieces one at a time.
    /// </summary>
    public static class VossBuildKit
    {
        public const string GeneratedFolder = "Assets/VossGenerated";
        public const string MaterialFolder = GeneratedFolder + "/Materials";
        public const string ScenesFolder = "Assets/Scenes";

        // ------------------------------------------------------------ materials

        public static class Mats
        {
            public static Material Floor, Wall, Ceiling, BlackMetal, Brass, Glass, Mirror, Card, Silhouette,
                Screen, Projection, Niche, Velvet, Wood, Paper, Accent, SignRed, SignGreen, DarkGlass;

            public static void Load()
            {
                Floor = Lit("Floor", new Color(0.13f, 0.13f, 0.14f), 0.1f, 0.35f);
                Wall = Lit("Wall", new Color(0.20f, 0.20f, 0.21f), 0.05f, 0.25f);
                Ceiling = Lit("Ceiling", new Color(0.10f, 0.10f, 0.11f), 0.05f, 0.2f);
                BlackMetal = Lit("BlackMetal", new Color(0.06f, 0.06f, 0.07f), 0.8f, 0.55f);
                Brass = Lit("Brass", new Color(0.72f, 0.56f, 0.28f), 0.9f, 0.7f);
                Glass = Transparent("Glass", new Color(0.7f, 0.85f, 1f, 0.18f), 0.95f);
                DarkGlass = Transparent("DarkGlass", new Color(0.4f, 0.5f, 0.6f, 0.35f), 0.95f);
                Mirror = LoadOrCreate("MirrorPane");
                Mirror.shader = Resources.Load<Shader>("Shaders/PlanarMirror");
                Mirror.SetColor("_BaseColor", new Color(0.93f, 0.96f, 1f));
                Mirror.SetColor("_EmissionColor", Color.black);
                Mirror.SetFloat("_ReflectionEnabled", 1f);
                Mirror.renderQueue = (int)RenderQueue.Geometry;
                EditorUtility.SetDirty(Mirror);
                Card = Lit("Card", new Color(0.92f, 0.88f, 0.78f), 0f, 0.3f);
                Silhouette = Lit("Silhouette", new Color(0.015f, 0.015f, 0.02f), 0f, 0.05f);
                Screen = Lit("Screen", new Color(0.02f, 0.02f, 0.025f), 0f, 0.6f);
                Projection = Transparent("Projection", new Color(0.05f, 0.05f, 0.08f, 1f), 0.1f);
                Niche = Lit("Niche", new Color(0.04f, 0.04f, 0.05f), 0f, 0.1f);
                Velvet = Lit("Velvet", new Color(0.30f, 0.05f, 0.06f), 0f, 0.15f);
                Wood = Lit("Wood", new Color(0.28f, 0.19f, 0.12f), 0f, 0.4f);
                Paper = Lit("Paper", new Color(0.85f, 0.80f, 0.68f), 0f, 0.2f);
                Accent = Lit("Accent", new Color(0.35f, 0.42f, 0.5f), 0.3f, 0.5f);
                SignRed = Emissive("SignRed", new Color(0.9f, 0.1f, 0.08f), 2f);
                SignGreen = Emissive("SignGreen", new Color(0.2f, 0.9f, 0.35f), 2f);
            }
        }

        private static Shader LitShader()
        {
            var s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            return s;
        }

        public static Material Lit(string name, Color color, float metallic, float smoothness)
        {
            var m = LoadOrCreate(name);
            m.shader = LitShader();
            m.SetColor("_BaseColor", color);
            m.SetColor("_Color", color);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Glossiness", smoothness);
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material Emissive(string name, Color color, float intensity)
        {
            var m = Lit(name, color * 0.6f, 0f, 0.4f);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", color * intensity);
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material Transparent(string name, Color color, float smoothness)
        {
            var m = LoadOrCreate(name);
            m.shader = LitShader();
            m.SetColor("_BaseColor", color);
            m.SetColor("_Color", color);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_AlphaClip", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material LoadOrCreate(string name)
        {
            EnsureFolder(MaterialFolder);
            string path = $"{MaterialFolder}/M_{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(LitShader());
                AssetDatabase.CreateAsset(m, path);
            }
            return m;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            string leaf = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        // ----------------------------------------------------------- geometry

        public static GameObject Group(Transform parent, string name, Vector3 pos = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            return go;
        }

        public static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject Prim(Transform parent, string name, PrimitiveType type, Vector3 center, Vector3 size, Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject Quad(Transform parent, string name, Vector3 center, Vector2 size, Vector3 facing, Material mat, bool collider = false, Vector3? up = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            go.transform.localRotation = Quaternion.LookRotation(-facing, up ?? Vector3.up); // quad faces -Z of its own frame
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        /// <summary>Floor + ceiling slab for a rectangular region.</summary>
        public static void FloorAndCeiling(Transform parent, string name, Vector2 min, Vector2 max, float height, Material floor, Material ceiling)
        {
            var c = new Vector3((min.x + max.x) * 0.5f, 0f, (min.y + max.y) * 0.5f);
            var s = new Vector3(max.x - min.x, 0.2f, max.y - min.y);
            Box(parent, name + "_Floor", c + Vector3.down * 0.1f, s, floor);
            Box(parent, name + "_Ceiling", c + Vector3.up * (height + 0.1f), s, ceiling);
        }

        /// <summary>Wall along X (constant Z) or along Z (constant X), with an optional door gap.</summary>
        public static void Wall(Transform parent, string name, bool alongX, float fixedCoord, float start, float end,
            float height, Material mat, float gapCenter = float.NaN, float gapWidth = 0f, float gapHeight = 3.2f, float thickness = 0.3f)
        {
            if (float.IsNaN(gapCenter) || gapWidth <= 0f)
            {
                Segment(parent, name, alongX, fixedCoord, start, end, 0f, height, mat, thickness);
                return;
            }
            float g0 = gapCenter - gapWidth * 0.5f;
            float g1 = gapCenter + gapWidth * 0.5f;
            if (g0 > start) Segment(parent, name + "_A", alongX, fixedCoord, start, g0, 0f, height, mat, thickness);
            if (g1 < end) Segment(parent, name + "_B", alongX, fixedCoord, g1, end, 0f, height, mat, thickness);
            if (gapHeight < height) Segment(parent, name + "_Lintel", alongX, fixedCoord, g0, g1, gapHeight, height, mat, thickness);
        }

        private static void Segment(Transform parent, string name, bool alongX, float fixedCoord, float a, float b, float y0, float y1, Material mat, float thickness)
        {
            float len = b - a;
            float mid = (a + b) * 0.5f;
            Vector3 center = alongX ? new Vector3(mid, (y0 + y1) * 0.5f, fixedCoord) : new Vector3(fixedCoord, (y0 + y1) * 0.5f, mid);
            Vector3 size = alongX ? new Vector3(len, y1 - y0, thickness) : new Vector3(thickness, y1 - y0, len);
            Box(parent, name, center, size, mat);
        }

        // ---------------------------------------------------------------- text

        private static Font font;
        public static Font Font3D
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return font;
            }
        }

        /// <param name="lineHeight">Approximate height of one line of text in metres.</param>
        public static TextMesh Text3D(Transform parent, string name, string text, Vector3 pos, Vector3 facing, float lineHeight, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal, int fontSize = 64, Vector3? up = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.LookRotation(-facing, up ?? Vector3.up); // text reads from its -Z side, like a quad
            var tm = go.AddComponent<TextMesh>();
            tm.font = Font3D;
            tm.fontSize = fontSize;
            tm.characterSize = lineHeight / (fontSize * 0.1f); // TextMesh glyph height ~= fontSize * characterSize * 0.1
            tm.anchor = anchor;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            tm.fontStyle = style;
            tm.text = text;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = Resources.Load<Material>("WorldText");
            r.shadowCastingMode = ShadowCastingMode.Off;
            go.AddComponent<WorldText>();
            return tm;
        }

        // -------------------------------------------------------------- lights

        public static Light PointLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float range, bool shadows = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            return l;
        }

        public static Light SpotLight(Transform parent, string name, Vector3 pos, Vector3 lookAt, Color color, float intensity, float range, float angle)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.LookAt(parent != null ? parent.TransformPoint(lookAt) : lookAt);
            var l = go.AddComponent<Light>();
            l.type = LightType.Spot;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.spotAngle = angle;
            l.shadows = LightShadows.Soft;
            return l;
        }

        public static void DarkAmbient(Color ambient)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
        }

        // -------------------------------------------------------------- player

        public static PlayerInteractor Player(Transform parent, Vector3 position, float yaw, bool movement = true)
        {
            var player = new GameObject("Player");
            player.transform.SetParent(parent, false);
            player.transform.position = position;
            player.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.3f;

            var camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            cam.fieldOfView = 65f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGo.AddComponent<AudioListener>();

            var fps = player.AddComponent<FirstPersonController>();
            fps.MovementEnabled = movement;
            player.AddComponent<InspectionController>();
            player.AddComponent<FirstPersonHands>();
            var interactor = player.AddComponent<PlayerInteractor>();
            return interactor;
        }

        public static BoxCollider TriggerBox(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            var bc = go.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = size;
            return bc;
        }
    }
}
