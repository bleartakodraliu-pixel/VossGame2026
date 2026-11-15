using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
        public const string AtmosphereFolder = GeneratedFolder + "/Atmosphere";

        // ------------------------------------------------------------ materials

        public static class Mats
        {
            public static Material Floor, Wall, Ceiling, BlackMetal, Brass, Glass, Mirror, Card, Silhouette,
                Screen, Projection, Niche, Velvet, Wood, Paper, Accent, SignRed, SignGreen, DarkGlass,
                OldPaper, Cork, Rust, Chalkboard, Crt, Leather, Photo, RedString, Porcelain, Grey, WhitePanel, Fabric,
                // Laboratory surfaces (textured, world-scaled UVs)
                LabWall, LabTile, LabFloor, LabCeiling, Concrete, ConcreteFloor, ConcreteCeiling, Hazard, Stain,
                Tube, TubeOff, Steel, DoorPaint, Trim, Liquid,
                // Concept-art pass
                Cracked, WetFloor, Navy, NavyCeiling, Marble, DecoInlay, Gold, WarmPlaster, StoneFloor, Rug,
                Schematics, Cobweb, Curtain, Puddle, Cardboard, RedLiquid, DnaGlow, WarmBulb, OldWood, FlightCase, AmberGlass;

            public static void Load()
            {
                Floor = Lit("Floor", new Color(0.13f, 0.13f, 0.14f), 0.1f, 0.35f);
                Wall = Lit("Wall", new Color(0.20f, 0.20f, 0.21f), 0.05f, 0.25f);
                Ceiling = Lit("Ceiling", new Color(0.10f, 0.10f, 0.11f), 0.05f, 0.2f);
                BlackMetal = Lit("BlackMetal", new Color(0.06f, 0.06f, 0.07f), 0.8f, 0.55f);
                Brass = Lit("Brass", new Color(0.46f, 0.36f, 0.19f), 0.9f, 0.55f); // aged, not new
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
                // Set dressing
                OldPaper = Lit("OldPaper", new Color(0.72f, 0.66f, 0.5f), 0f, 0.15f);
                Cork = Lit("Cork", new Color(0.36f, 0.25f, 0.15f), 0f, 0.1f);
                Rust = Lit("Rust", new Color(0.3f, 0.16f, 0.09f), 0.4f, 0.25f);
                Chalkboard = Lit("Chalkboard", new Color(0.07f, 0.09f, 0.08f), 0f, 0.1f);
                Crt = Emissive("Crt", new Color(0.55f, 0.7f, 0.62f), 0.9f);
                Leather = Lit("Leather", new Color(0.12f, 0.07f, 0.05f), 0f, 0.35f);
                Photo = Lit("Photo", new Color(0.32f, 0.29f, 0.25f), 0f, 0.5f);
                RedString = Lit("RedString", new Color(0.55f, 0.04f, 0.03f), 0f, 0.2f);
                Porcelain = Lit("Porcelain", new Color(0.78f, 0.74f, 0.68f), 0f, 0.55f);
                Grey = Lit("Grey", new Color(0.42f, 0.42f, 0.42f), 0f, 0.1f);
                WhitePanel = Lit("WhitePanel", new Color(0.86f, 0.86f, 0.84f), 0f, 0.1f);
                Fabric = Lit("Fabric", new Color(0.62f, 0.62f, 0.6f), 0f, 0.05f);

                // Laboratory. LabWall is the painted plaster; walls built with it get a tiled lower half.
                LabWall = Textured("LabPlaster", ProceduralTextures.Plaster(), ProceduralTextures.PlasterMetres, Color.white, 0f, 0.2f, 0.6f);
                LabTile = Textured("LabTile", ProceduralTextures.WallTile(), ProceduralTextures.WallTileMetres, Color.white, 0f, 0.65f, 1f);
                LabFloor = Textured("LabFloor", ProceduralTextures.FloorTile(), ProceduralTextures.FloorMetres, Color.white, 0f, 0.45f, 0.8f);
                LabCeiling = Textured("LabCeiling", ProceduralTextures.Ceiling(), ProceduralTextures.CeilingMetres, Color.white, 0f, 0.1f, 0.8f);
                Concrete = Textured("Concrete", ProceduralTextures.Concrete(), ProceduralTextures.ConcreteMetres, Color.white, 0f, 0.15f, 1f);
                ConcreteFloor = Textured("ConcreteFloor", ProceduralTextures.Concrete(), ProceduralTextures.ConcreteMetres, new Color(0.75f, 0.75f, 0.75f), 0f, 0.35f, 0.8f);
                ConcreteCeiling = Textured("ConcreteCeiling", ProceduralTextures.Concrete(), ProceduralTextures.ConcreteMetres, new Color(0.5f, 0.5f, 0.52f), 0f, 0.1f, 0.6f);
                Hazard = Textured("Hazard", ProceduralTextures.Hazard(), ProceduralTextures.HazardMetres, Color.white, 0f, 0.3f, 0.3f);
                // Grime decal: plain alpha blend, no reflections, so only the blotch itself shows.
                Stain = Transparent("Stain", Color.white, 0f);
                Stain.SetTexture("_BaseMap", ProceduralTextures.Stain());
                Stain.SetTexture("_MainTex", ProceduralTextures.Stain());
                Stain.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                Stain.SetFloat("_SpecularHighlights", 0f);
                Stain.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                Stain.SetFloat("_EnvironmentReflections", 0f);
                Stain.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                Tube = Emissive("Tube", new Color(0.78f, 0.86f, 1f), 2.2f);
                TubeOff = Lit("TubeOff", new Color(0.32f, 0.33f, 0.34f), 0f, 0.6f);
                Steel = Lit("Steel", new Color(0.52f, 0.54f, 0.56f), 0.85f, 0.55f);
                DoorPaint = Lit("DoorPaint", new Color(0.16f, 0.22f, 0.22f), 0.3f, 0.35f);
                Trim = Lit("Trim", new Color(0.12f, 0.15f, 0.15f), 0.2f, 0.3f);
                Liquid = Transparent("Liquid", new Color(0.35f, 0.42f, 0.18f, 0.55f), 0.9f);

                // Concept-art pass: industrial theatre underground, art-deco mirror hall, alchemist's study.
                Cracked = Textured("Cracked", ProceduralTextures.CrackedConcrete(), ProceduralTextures.CrackedMetres, Color.white, 0f, 0.2f, 1f);
                WetFloor = Textured("WetFloor", ProceduralTextures.WetSlabs(), ProceduralTextures.WetSlabMetres, Color.white, 0f, 0.5f, 0.6f);
                SmoothnessFromAlbedo(WetFloor);
                Navy = Textured("Navy", ProceduralTextures.NavyWall(), ProceduralTextures.NavyMetres, Color.white, 0f, 0.45f, 0.5f);
                NavyCeiling = Textured("NavyCeiling", ProceduralTextures.NavyWall(), ProceduralTextures.NavyMetres, new Color(0.6f, 0.6f, 0.65f), 0f, 0.3f, 0.3f);
                Marble = Textured("Marble", ProceduralTextures.DarkMarble(), ProceduralTextures.MarbleMetres, Color.white, 0f, 0.85f, 0.2f);
                DecoInlay = Lit("DecoInlay", Color.white, 0f, 1f);
                DecoInlay.SetTexture("_BaseMap", ProceduralTextures.DecoInlay());
                DecoInlay.SetTexture("_MainTex", ProceduralTextures.DecoInlay());
                DecoInlay.SetTexture("_MetallicGlossMap", ProceduralTextures.DecoInlayMetal());
                DecoInlay.EnableKeyword("_METALLICSPECGLOSSMAP");
                Gold = Lit("Gold", new Color(0.74f, 0.56f, 0.24f), 1f, 0.7f);
                WarmPlaster = Textured("WarmPlaster", ProceduralTextures.WarmPlaster(), ProceduralTextures.WarmPlasterMetres, Color.white, 0f, 0.15f, 0.8f);
                StoneFloor = Textured("StoneFloor", ProceduralTextures.StoneFloor(), ProceduralTextures.StoneMetres, Color.white, 0f, 0.6f, 1f);
                SmoothnessFromAlbedo(StoneFloor);
                Rug = Lit("Rug", Color.white, 0f, 0.05f);
                Rug.SetTexture("_BaseMap", ProceduralTextures.Rug());
                Rug.SetTexture("_MainTex", ProceduralTextures.Rug());
                Schematics = Decal("Schematics", ProceduralTextures.Schematics());
                Cobweb = Decal("Cobweb", ProceduralTextures.Cobweb());
                Curtain = Textured("CurtainVelvet", ProceduralTextures.Curtain(), ProceduralTextures.CurtainMetres, Color.white, 0f, 0.3f, 1f);
                Puddle = Lit("Puddle", new Color(0.03f, 0.03f, 0.035f), 0f, 0.97f);
                Puddle.SetTexture("_BaseMap", ProceduralTextures.Stain());
                Puddle.SetFloat("_AlphaClip", 1f);
                Puddle.SetFloat("_Cutoff", 0.35f);
                Puddle.EnableKeyword("_ALPHATEST_ON");
                Puddle.renderQueue = (int)RenderQueue.AlphaTest;
                Cardboard = Lit("Cardboard", new Color(0.5f, 0.38f, 0.24f), 0f, 0.1f);
                RedLiquid = Transparent("RedLiquid", new Color(0.75f, 0.05f, 0.04f, 0.5f), 0.9f);
                RedLiquid.EnableKeyword("_EMISSION");
                RedLiquid.SetColor("_EmissionColor", new Color(0.9f, 0.05f, 0.03f) * 1.2f);
                DnaGlow = Emissive("DnaGlow", new Color(1f, 0.3f, 0.25f), 3f);
                WarmBulb = Emissive("WarmBulb", new Color(1f, 0.78f, 0.45f), 2.5f);
                AmberGlass = Emissive("AmberGlass", new Color(0.95f, 0.62f, 0.28f), 1.6f);
                OldWood = Lit("OldWood", new Color(0.3f, 0.18f, 0.1f), 0f, 0.35f);
                FlightCase = Lit("FlightCase", new Color(0.05f, 0.05f, 0.055f), 0.2f, 0.4f);
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

        /// <summary>Smoothness read from the albedo alpha (wet patches shine, dry ones do not).</summary>
        private static void SmoothnessFromAlbedo(Material m)
        {
            m.SetFloat("_SmoothnessTextureChannel", 1f);
            m.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            EditorUtility.SetDirty(m);
        }

        /// <summary>Alpha-blended overlay with no reflections (drawings on walls, cobwebs, grime).</summary>
        public static Material Decal(string name, Texture2D texture)
        {
            var m = Transparent(name, Color.white, 0f);
            m.SetTexture("_BaseMap", texture);
            m.SetTexture("_MainTex", texture);
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.SetFloat("_SpecularHighlights", 0f);
            m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            m.SetFloat("_EnvironmentReflections", 0f);
            m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>URP Lit with albedo + normal map; tiling assumes UVs measured in metres (see SurfaceBox).</summary>
        public static Material Textured(string name, ProceduralTextures.Pair tex, float metresPerTexture, Color tint, float metallic, float smoothness, float bump)
        {
            var m = Lit(name, tint, metallic, smoothness);
            m.SetTexture("_BaseMap", tex.Albedo);
            m.SetTexture("_MainTex", tex.Albedo);
            m.SetTextureScale("_BaseMap", Vector2.one / metresPerTexture);
            m.SetTextureScale("_MainTex", Vector2.one / metresPerTexture);
            if (tex.Normal != null)
            {
                m.SetTexture("_BumpMap", tex.Normal);
                m.SetFloat("_BumpScale", bump);
                m.EnableKeyword("_NORMALMAP");
            }
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
            SurfaceBox(parent, name + "_Floor", c + Vector3.down * 0.1f, s, floor);
            SurfaceBox(parent, name + "_Ceiling", c + Vector3.up * (height + 0.1f), s, ceiling);
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

        /// <summary>Wall with several openings. Each gap is (centre, width, height) along the wall.</summary>
        public static void WallWithGaps(Transform parent, string name, bool alongX, float fixedCoord, float start, float end,
            float height, Material mat, params Vector3[] gaps)
        {
            var sorted = new List<Vector3>(gaps);
            sorted.Sort((a, b) => a.x.CompareTo(b.x));
            float cursor = start;
            int n = 0;
            foreach (var g in sorted)
            {
                float g0 = g.x - g.y * 0.5f;
                float g1 = g.x + g.y * 0.5f;
                if (g0 > cursor) Segment(parent, name + "_" + n++, alongX, fixedCoord, cursor, g0, 0f, height, mat, 0.3f);
                if (g.z < height) Segment(parent, name + "_Lintel" + n++, alongX, fixedCoord, g0, g1, g.z, height, mat, 0.3f);
                cursor = Mathf.Max(cursor, g1);
            }
            if (cursor < end) Segment(parent, name + "_" + n, alongX, fixedCoord, cursor, end, 0f, height, mat, 0.3f);
        }

        /// <summary>Height where laboratory walls change from tiles to paint (a whole number of 15 cm tiles).</summary>
        public const float TileLine = 1.2f;

        /// <summary>Straight wall piece between a and b, from y0 to y1. Walls in LabWall get tiles, trim and a skirting.</summary>
        public static void Segment(Transform parent, string name, bool alongX, float fixedCoord, float a, float b, float y0, float y1, Material mat, float thickness = 0.3f)
        {
            if (mat == Mats.LabWall && mat != null)
            {
                if (y0 < TileLine) Piece(parent, name + "_Tiles", alongX, fixedCoord, a, b, y0, Mathf.Min(y1, TileLine), Mats.LabTile, thickness, true);
                if (y1 > TileLine) Piece(parent, name, alongX, fixedCoord, a, b, Mathf.Max(y0, TileLine), y1, Mats.LabWall, thickness, true);
                if (y0 < TileLine && y1 > TileLine) Piece(parent, name + "_Trim", alongX, fixedCoord, a, b, TileLine - 0.025f, TileLine + 0.025f, Mats.Trim, thickness + 0.04f, false);
                if (y0 <= 0f) Piece(parent, name + "_Skirting", alongX, fixedCoord, a, b, 0f, 0.12f, Mats.Trim, thickness + 0.03f, false);
                return;
            }
            Piece(parent, name, alongX, fixedCoord, a, b, y0, y1, mat, thickness, true);
        }

        private static void Piece(Transform parent, string name, bool alongX, float fixedCoord, float a, float b, float y0, float y1, Material mat, float thickness, bool collider)
        {
            float len = b - a;
            float mid = (a + b) * 0.5f;
            Vector3 center = alongX ? new Vector3(mid, (y0 + y1) * 0.5f, fixedCoord) : new Vector3(fixedCoord, (y0 + y1) * 0.5f, mid);
            Vector3 size = alongX ? new Vector3(len, y1 - y0, thickness) : new Vector3(thickness, y1 - y0, len);
            SurfaceBox(parent, name, center, size, mat, collider);
        }

        /// <summary>
        /// Box whose UVs are measured in metres in the parent's space, so textures keep their real size
        /// and line up across neighbouring pieces (a tiled wall split by a door stays continuous).
        /// The mesh is stored in the scene.
        /// </summary>
        public static GameObject SurfaceBox(Transform parent, string name, Vector3 center, Vector3 size, Material mat, bool collider = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.AddComponent<MeshFilter>().sharedMesh = WorldUvBox(center, size, name);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (collider) go.AddComponent<BoxCollider>().size = size;
            return go;
        }

        private static Mesh WorldUvBox(Vector3 center, Vector3 size, string name)
        {
            var half = size * 0.5f;
            var verts = new List<Vector3>(24);
            var normals = new List<Vector3>(24);
            var tangents = new List<Vector4>(24);
            var uvs = new List<Vector2>(24);
            var tris = new List<int>(36);

            void Face(Vector3 n, Vector3 v)
            {
                Vector3 u = Vector3.Cross(n, v); // screen-right when looking at the face from outside
                Vector3 fc = Vector3.Scale(n, half), eu = Vector3.Scale(u, half), ev = Vector3.Scale(v, half);
                int i = verts.Count;
                foreach (var p in new[] { fc - eu - ev, fc + eu - ev, fc + eu + ev, fc - eu + ev })
                {
                    verts.Add(p);
                    normals.Add(n);
                    tangents.Add(new Vector4(u.x, u.y, u.z, -1f));
                    Vector3 w = p + center;
                    uvs.Add(new Vector2(Vector3.Dot(w, u), Vector3.Dot(w, v)));
                }
                tris.AddRange(new[] { i, i + 3, i + 2, i, i + 2, i + 1 });
            }

            Face(Vector3.right, Vector3.up);
            Face(Vector3.left, Vector3.up);
            Face(Vector3.forward, Vector3.up);
            Face(Vector3.back, Vector3.up);
            Face(Vector3.up, Vector3.forward);
            Face(Vector3.down, Vector3.forward);

            var mesh = new Mesh { name = "Surface_" + name };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetTangents(tangents);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
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

        /// <summary>Exponential distance fog: far walls and corridor ends dissolve into the dark.</summary>
        public static void Fog(Color color, float density)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = color;
            RenderSettings.fogDensity = density;
        }

        // ------------------------------------------------------- post-process

        /// <summary>Shared horror grade: cold, desaturated, heavy vignette, film grain, soft bloom on lamps.</summary>
        public static VolumeProfile HorrorProfile(string name, float exposure, float vignette, float grain, Color tint)
        {
            EnsureFolder(AtmosphereFolder);
            string path = $"{AtmosphereFolder}/VP_{name}.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            var tone = Component<Tonemapping>(profile);
            tone.mode.Override(TonemappingMode.Neutral);

            var color = Component<ColorAdjustments>(profile);
            color.postExposure.Override(exposure);
            color.contrast.Override(16f);
            color.saturation.Override(-12f);
            color.colorFilter.Override(tint);

            var v = Component<Vignette>(profile);
            v.intensity.Override(vignette);
            v.smoothness.Override(0.45f);
            v.color.Override(Color.black);

            var g = Component<FilmGrain>(profile);
            g.type.Override(FilmGrainLookup.Medium3);
            g.intensity.Override(grain);
            g.response.Override(0.75f);

            var b = Component<Bloom>(profile);
            b.threshold.Override(0.8f);
            b.intensity.Override(0.85f);
            b.scatter.Override(0.65f);

            var ca = Component<ChromaticAberration>(profile);
            ca.intensity.Override(0.03f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static T Component<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out var existing)) return existing;
            var c = profile.Add<T>(false);
            c.name = typeof(T).Name;
            c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(c, profile);
            return c;
        }

        public static Volume GlobalVolume(Transform parent, VolumeProfile profile)
        {
            var go = new GameObject("PostProcessVolume");
            go.transform.SetParent(parent, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = profile;
            return volume;
        }

        /// <summary>Lets the camera run the URP post-processing stack (off by default on new cameras).</summary>
        public static void EnablePostProcessing(Camera cam)
        {
            var data = cam.GetComponent<UniversalAdditionalCameraData>();
            if (data == null) data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
        }

        // ------------------------------------------------------------ particles

        /// <summary>Slow drifting dust in a box volume (local space of parent).</summary>
        public static ParticleSystem Dust(Transform parent, string name, Vector3 center, Vector3 size, float rate, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 14f;
            main.startSpeed = 0.05f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.018f);
            main.startColor = new Color(color.r, color.g, color.b, color.a * 0.5f);
            main.maxParticles = 600;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.002f;
            main.prewarm = true;
            var emission = ps.emission;
            emission.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = size;
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.08f;
            noise.frequency = 0.2f;

            string path = $"{MaterialFolder}/M_Dust.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                mat = new Material(shader);
                mat.SetColor("_BaseColor", new Color(1f, 0.95f, 0.85f, 0.5f));
                mat.SetColor("_Color", new Color(1f, 0.95f, 0.85f, 0.5f));
                if (mat.HasProperty("_Surface")) { mat.SetFloat("_Surface", 1f); mat.SetOverrideTag("RenderType", "Transparent"); mat.renderQueue = 3000; mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); }
                AssetDatabase.CreateAsset(mat, path);
            }
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            return ps;
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
            EnablePostProcessing(cam);

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
