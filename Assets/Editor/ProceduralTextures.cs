using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Voss.EditorTools
{
    /// <summary>
    /// Tileable surface textures generated in code (no imported art): laboratory wall tiles, painted
    /// plaster, floor tiles, ceiling panels, concrete, grime decals and hazard stripes. Each comes with
    /// a normal map derived from its height so grout, seams and pits catch the light. Files are written
    /// once to Assets/VossGenerated/Textures; delete them (or bump Version) to regenerate.
    /// </summary>
    public static class ProceduralTextures
    {
        public const string Folder = VossBuildKit.GeneratedFolder + "/Textures";
        private const int Version = 2;

        public struct Pair { public Texture2D Albedo; public Texture2D Normal; }

        // Physical size each texture covers, in metres (used for material tiling with world-scaled UVs).
        public const float WallTileMetres = 1.6f;   // 8 x 8 tiles of 20 cm (the tile line at 1.2 m is 6 rows)
        public const float PlasterMetres = 2.0f;
        public const float FloorMetres = 1.6f;      // 4 x 4 tiles of 40 cm
        public const float CeilingMetres = 1.2f;    // 2 x 2 panels of 60 cm
        public const float ConcreteMetres = 2.0f;
        public const float HazardMetres = 0.5f;

        // Concept-art pass: industrial theatre underground, art-deco mirror hall, alchemist's study.
        public const float CrackedMetres = 3.0f;
        public const float WetSlabMetres = 2.0f;
        public const float NavyMetres = 2.0f;
        public const float MarbleMetres = 2.4f;
        public const float WarmPlasterMetres = 2.5f;
        public const float StoneMetres = 1.2f;
        public const float CurtainMetres = 1.0f;

        public static Pair CrackedConcrete() => Get("CrackedConcrete3", 512, GenCrackedConcrete, 1.6f);
        public static Pair WetSlabs() => Get("WetSlabs3", 512, GenWetSlabs, 1.8f);
        public static Pair NavyWall() => Get("NavyWall3", 512, GenNavyWall, 1.2f);
        public static Pair DarkMarble() => Get("DarkMarble", 512, GenDarkMarble, 1f);
        public static Pair WarmPlaster() => Get("WarmPlaster", 512, GenWarmPlaster, 2.5f);
        public static Pair StoneFloor() => Get("StoneFloor", 512, GenStoneFloor, 3f);
        public static Pair Curtain() => Get("Curtain", 256, GenCurtain, 6f);
        public static Texture2D DecoInlay() => Single("DecoInlay", 704, 1024, (w, h) => GenDecoInlay(w, h, false), false);
        public static Texture2D DecoInlayMetal() => Single("DecoInlayMetal", 704, 1024, (w, h) => GenDecoInlay(w, h, true), false, true);
        public static Texture2D Rug() => Single("Rug", 512, 768, GenRug, false);
        public static Texture2D Schematics() => Single("Schematics", 1024, 512, GenSchematics, true);
        public static Texture2D Cobweb() => Single("Cobweb", 256, 256, GenCobweb, true);

        public static Pair WallTile() => Get("WallTile", 512, GenWallTile, 4f);
        public static Pair Plaster() => Get("Plaster", 512, GenPlaster, 2f);
        public static Pair FloorTile() => Get("FloorTile", 512, GenFloorTile, 3f);
        public static Pair Ceiling() => Get("Ceiling", 512, GenCeiling, 3f);
        public static Pair Concrete() => Get("Concrete", 512, GenConcrete, 3f);
        public static Pair Hazard() => Get("Hazard", 256, GenHazard, 1f);
        public static Texture2D Stain() => Get("Stain", 256, GenStain, 0f, true).Albedo;

        // ------------------------------------------------------------ pipeline

        private static Pair Get(string name, int size, Func<int, (Color[] color, float[] height)> gen, float normalStrength, bool alpha = false)
        {
            VossBuildKit.EnsureFolder(Folder);
            string albedoPath = $"{Folder}/T_{name}_v{Version}.png";
            string normalPath = $"{Folder}/T_{name}_v{Version}_N.png";
            bool wantNormal = normalStrength > 0f;
            if (!File.Exists(albedoPath) || (wantNormal && !File.Exists(normalPath)))
            {
                var (color, height) = gen(size);
                Write(albedoPath, color, size);
                AssetDatabase.ImportAsset(albedoPath);
                Configure(albedoPath, false, alpha);
                if (wantNormal)
                {
                    Write(normalPath, NormalFromHeight(height, size, normalStrength), size);
                    AssetDatabase.ImportAsset(normalPath);
                    Configure(normalPath, true, false);
                }
            }
            return new Pair
            {
                Albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath),
                Normal = wantNormal ? AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath) : null
            };
        }

        /// <summary>One non-tiling or alpha texture (decals, floor inlays, rugs).</summary>
        private static Texture2D Single(string name, int w, int h, Func<int, int, Color[]> gen, bool alpha, bool linear = false)
        {
            VossBuildKit.EnsureFolder(Folder);
            string path = $"{Folder}/T_{name}_v{Version}.png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
                tex.SetPixels(gen(w, h));
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.textureType = TextureImporterType.Default;
                imp.sRGBTexture = !linear;
                imp.alphaIsTransparency = alpha;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.npotScale = TextureImporterNPOTScale.None;
                imp.mipmapEnabled = true;
                imp.filterMode = FilterMode.Trilinear;
                imp.anisoLevel = 8;
                imp.maxTextureSize = 2048;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void Write(string path, Color[] pixels, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void Configure(string path, bool normal, bool alpha)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            imp.sRGBTexture = !normal;
            imp.alphaIsTransparency = alpha;
            imp.wrapMode = alpha ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            imp.mipmapEnabled = true;
            imp.filterMode = FilterMode.Trilinear;
            imp.anisoLevel = 4;
            imp.maxTextureSize = 1024;
            imp.SaveAndReimport();
        }

        private static Color[] NormalFromHeight(float[] h, int n, float strength)
        {
            var result = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = h[y * n + (x + 1) % n] - h[y * n + (x - 1 + n) % n];
                    float dy = h[((y + 1) % n) * n + x] - h[((y - 1 + n) % n) * n + x];
                    var nv = new Vector3(-dx * strength, -dy * strength, 1f).normalized;
                    result[y * n + x] = new Color(nv.x * 0.5f + 0.5f, nv.y * 0.5f + 0.5f, nv.z * 0.5f + 0.5f, 1f);
                }
            return result;
        }

        // --------------------------------------------------------------- noise

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)x * 374761393u + (uint)y * 668265263u + (uint)seed * 2246822519u;
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777215f;
            }
        }

        private static int Wrap(int v, int p) => ((v % p) + p) % p;

        /// <summary>Tileable value noise: x, y in lattice units, repeating every <paramref name="period"/>.</summary>
        private static float Value(float x, float y, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            int ax = Wrap(x0, period), bx = Wrap(x0 + 1, period), ay = Wrap(y0, period), by = Wrap(y0 + 1, period);
            float a = Mathf.Lerp(Hash(ax, ay, seed), Hash(bx, ay, seed), fx);
            float b = Mathf.Lerp(Hash(ax, by, seed), Hash(bx, by, seed), fx);
            return Mathf.Lerp(a, b, fy);
        }

        /// <summary>Tileable fractal noise over u, v in [0,1).</summary>
        private static float Fbm(float u, float v, int basePeriod, int octaves, int seed)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            int p = basePeriod;
            for (int o = 0; o < octaves; o++)
            {
                sum += amp * Value(u * p, v * p, p, seed + o * 31);
                norm += amp;
                amp *= 0.5f;
                p *= 2;
            }
            return sum / norm;
        }

        private static float S(float a, float b, float x) => Mathf.Clamp01((x - a) / (b - a));

        // ---------------------------------------------------------- generators

        /// <summary>Off-white ceramic tiles with dark grout, uneven glaze, grime and a few stained tiles.</summary>
        private static (Color[], float[]) GenWallTile(int n)
        {
            var c = new Color[n * n];
            var h = new float[n * n];
            int tiles = 8, cell = n / tiles;
            var grout = new Color(0.46f, 0.47f, 0.44f);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    int tx = x / cell, ty = y / cell, lx = x % cell, ly = y % cell;
                    float edge = Mathf.Min(Mathf.Min(lx, cell - 1 - lx), Mathf.Min(ly, cell - 1 - ly));
                    float grime = Fbm(u, v, 4, 5, 3);
                    float speck = Fbm(u, v, 64, 2, 5);
                    float r = Hash(tx, ty, 11);
                    var tile = new Color(0.80f, 0.84f, 0.80f) * (0.93f + 0.12f * r);
                    if (Hash(tx, ty, 12) > 0.86f) tile = Color.Lerp(tile, new Color(0.62f, 0.58f, 0.44f), 0.45f); // yellowed
                    tile *= Mathf.Lerp(0.7f, 1.05f, grime) * (0.97f + 0.06f * speck);
                    // grime gathers along the grout
                    float nearGrout = 1f - S(2f, 7f, edge);
                    tile = Color.Lerp(tile, tile * 0.75f, nearGrout * (1f - grime));
                    bool isGrout = edge < 1.5f;
                    c[y * n + x] = isGrout ? grout * Mathf.Lerp(0.6f, 1f, grime) : tile;
                    c[y * n + x].a = 1f;
                    h[y * n + x] = isGrout ? 0f : 0.35f + 0.65f * S(1f, 4f, edge) + speck * 0.03f;
                }
            return (c, h);
        }

        /// <summary>Institutional green-grey paint over plaster: mottled, water-streaked, peeling in places.</summary>
        private static (Color[], float[]) GenPlaster(int n)
        {
            var c = new Color[n * n];
            var h = new float[n * n];
            var paint = new Color(0.42f, 0.48f, 0.46f);
            var under = new Color(0.66f, 0.63f, 0.56f);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float mottle = Fbm(u, v, 4, 6, 21);
                    float streak = Value(u * 24f, v * 3f, 24, 7) * 0.6f + Value(u * 48f, v * 6f, 48, 8) * 0.4f;
                    float peel = Fbm(u, v, 6, 5, 33);
                    var col = paint * Mathf.Lerp(0.8f, 1.1f, mottle);
                    col = Color.Lerp(col, new Color(0.30f, 0.27f, 0.2f), S(0.55f, 0.85f, streak) * 0.45f);
                    float flake = S(0.68f, 0.72f, peel);
                    col = Color.Lerp(col, under * Mathf.Lerp(0.8f, 1f, mottle), flake);
                    col.a = 1f;
                    c[y * n + x] = col;
                    h[y * n + x] = mottle * 0.25f + (1f - flake) * 0.4f;
                }
            return (c, h);
        }

        /// <summary>Worn two-tone vinyl floor tiles with scuffs and dirt.</summary>
        private static (Color[], float[]) GenFloorTile(int n)
        {
            var c = new Color[n * n];
            var h = new float[n * n];
            int tiles = 4, cell = n / tiles;
            var a = new Color(0.30f, 0.32f, 0.31f);
            var b = new Color(0.17f, 0.18f, 0.18f);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    int tx = x / cell, ty = y / cell, lx = x % cell, ly = y % cell;
                    float edge = Mathf.Min(Mathf.Min(lx, cell - 1 - lx), Mathf.Min(ly, cell - 1 - ly));
                    float grime = Fbm(u, v, 4, 5, 41);
                    float scuff = Fbm(u, v, 32, 3, 43);
                    var col = ((tx + ty) % 2 == 0 ? a : b) * (0.95f + 0.1f * Hash(tx, ty, 44));
                    col *= Mathf.Lerp(0.65f, 1.08f, grime);
                    col = Color.Lerp(col, col * 1.25f, S(0.7f, 0.8f, scuff) * 0.5f);
                    if (edge < 1f) col *= 0.45f;
                    col.a = 1f;
                    c[y * n + x] = col;
                    h[y * n + x] = (edge < 1f ? 0f : 0.6f) + scuff * 0.05f;
                }
            return (c, h);
        }

        /// <summary>Suspended ceiling: perforated panels in a dark T-bar grid, some water-stained.</summary>
        private static (Color[], float[]) GenCeiling(int n)
        {
            var c = new Color[n * n];
            var h = new float[n * n];
            int panels = 2, cell = n / panels;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    int px = x / cell, py = y / cell, lx = x % cell, ly = y % cell;
                    float edge = Mathf.Min(Mathf.Min(lx, cell - 1 - lx), Mathf.Min(ly, cell - 1 - ly));
                    float dirt = Fbm(u, v, 4, 5, 51);
                    var col = new Color(0.58f, 0.58f, 0.55f) * Mathf.Lerp(0.85f, 1.05f, dirt);
                    if ((x / 6 + y / 6) % 2 == 0 && Hash(x / 3, y / 3, 52) > 0.55f) col *= 0.82f; // perforation
                    if (Hash(px, py, 53) > 0.5f)
                    {
                        float stain = Fbm(u, v, 3, 4, 54 + px * 7 + py * 13);
                        float ring = S(0.55f, 0.6f, stain) - S(0.6f, 0.7f, stain) * 0.6f;
                        col = Color.Lerp(col, new Color(0.42f, 0.34f, 0.22f), Mathf.Clamp01(ring) * 0.6f);
                    }
                    bool bar = edge < 4f;
                    if (bar) col = new Color(0.16f, 0.16f, 0.17f);
                    col.a = 1f;
                    c[y * n + x] = col;
                    h[y * n + x] = bar ? 1f : 0.55f;
                }
            return (c, h);
        }

        /// <summary>Raw concrete with pits, form-work seams every metre and tie holes.</summary>
        private static (Color[], float[]) GenConcrete(int n)
        {
            var c = new Color[n * n];
            var h = new float[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float big = Fbm(u, v, 4, 6, 61);
                    float fine = Fbm(u, v, 64, 2, 62);
                    float pit = fine < 0.18f ? 1f : 0f;
                    float g = 0.36f * Mathf.Lerp(0.78f, 1.15f, big) * (0.94f + 0.12f * fine);
                    var col = new Color(g, g * 1.01f, g * 1.03f);
                    float seam = Mathf.Abs(Mathf.Repeat(v * 2f, 1f) - 0.5f) > 0.497f ? 1f : 0f;
                    var hole = new Vector2(Mathf.Repeat(u * 2f, 1f) - 0.5f, Mathf.Repeat(v * 4f, 1f) - 0.5f);
                    float tie = hole.magnitude < 0.035f ? 1f : 0f;
                    col *= 1f - 0.45f * Mathf.Max(seam, tie) - 0.3f * pit;
                    col.a = 1f;
                    c[y * n + x] = col;
                    h[y * n + x] = big * 0.5f + fine * 0.15f - 0.5f * Mathf.Max(seam, tie) - 0.25f * pit;
                }
            return (c, h);
        }

        /// <summary>Dirty yellow and black diagonal warning stripes.</summary>
        private static (Color[], float[]) GenHazard(int n)
        {
            var c = new Color[n * n];
            var h = new float[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    bool yellow = Mathf.Repeat((u + v) * 4f, 1f) < 0.5f;
                    float dirt = Fbm(u, v, 4, 5, 71);
                    var col = (yellow ? new Color(0.78f, 0.6f, 0.08f) : new Color(0.06f, 0.06f, 0.05f)) * Mathf.Lerp(0.55f, 1.05f, dirt);
                    col.a = 1f;
                    c[y * n + x] = col;
                    h[y * n + x] = dirt * 0.2f;
                }
            return (c, h);
        }

        /// <summary>Irregular dark blotch with soft alpha edges, for grime decals.</summary>
        private static (Color[], float[]) GenStain(int n)
        {
            var c = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float d = new Vector2(u - 0.5f, v - 0.5f).magnitude * 2f;
                    float shape = Fbm(u, v, 4, 5, 81);
                    float a = S(0.95f, 0.45f, d + (shape - 0.5f) * 0.7f) * Mathf.Lerp(0.5f, 1f, Fbm(u, v, 16, 3, 82));
                    c[y * n + x] = new Color(0.1f, 0.08f, 0.05f, a * 0.85f);
                }
            return (c, new float[n * n]);
        }

        // ===================================================== concept-art pass

        /// <summary>Ridged noise: thin bright ridges where the noise crosses its mid value (cracks, veins).</summary>
        private static float Ridge(float u, float v, int period, int octaves, int seed) => 1f - Mathf.Abs(Fbm(u, v, period, octaves, seed) * 2f - 1f);

        /// <summary>Warm grey concrete, cracked, stained and streaked: the underground of the theatre.</summary>
        private static (Color[], float[]) GenCrackedConcrete(int n)
        {
            var c = new Color[n * n];
            var h = new float[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float big = Fbm(u, v, 3, 6, 101);
                    float fine = Fbm(u, v, 48, 2, 102);
                    float crack = S(0.978f, 0.996f, Ridge(u, v, 3, 4, 103)) * 0.8f;
                    float hair = S(0.988f, 0.998f, Ridge(u, v, 6, 3, 104)) * 0.35f;
                    float streak = Value(u * 20f, v * 2f, 20, 105);
                    float g = 0.44f * Mathf.Lerp(0.72f, 1.12f, big) * (0.93f + 0.14f * fine);
                    var col = new Color(g * 1.04f, g, g * 0.95f);
                    col = Color.Lerp(col, new Color(0.22f, 0.17f, 0.13f), S(0.6f, 0.9f, streak) * 0.35f);
                    col *= 1f - 0.5f * Mathf.Max(crack, hair);
                    if (fine < 0.12f) col *= 0.82f;
                    col.a = 1f;
                    c[y * n + x] = col;
                    h[y * n + x] = big * 0.25f + fine * 0.15f - 0.5f * Mathf.Max(crack, hair) - (fine < 0.12f ? 0.1f : 0f);
                }
            return (c, h);
        }

        /// <summary>Cracked concrete slabs, wet in patches; smoothness in alpha so puddles and damp areas shine.</summary>
        private static (Color[], float[]) GenWetSlabs(int n)
        {
            var c = new Color[n * n];
            var h = new float[n * n];
            int slabs = 2, cell = n / slabs;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    int lx = x % cell, ly = y % cell;
                    float edge = Mathf.Min(Mathf.Min(lx, cell - 1 - lx), Mathf.Min(ly, cell - 1 - ly));
                    float big = Fbm(u, v, 3, 5, 111);
                    float wet = S(0.6f, 0.7f, Fbm(u, v, 3, 4, 112));
                    float crack = S(0.978f, 0.995f, Ridge(u, v, 3, 4, 113));
                    float g = 0.36f * Mathf.Lerp(0.75f, 1.1f, big);
                    var col = new Color(g * 1.03f, g, g * 0.96f);
                    col *= Mathf.Lerp(1f, 0.55f, wet);
                    col *= 1f - 0.5f * crack;
                    bool seam = edge < 2f;
                    if (seam) col *= 0.45f;
                    col.a = Mathf.Lerp(0.2f, 0.9f, wet) * (seam ? 0.5f : 1f);
                    c[y * n + x] = col;
                    h[y * n + x] = (seam ? 0f : 0.5f) + big * 0.3f - crack * 0.5f;
                }
            return (c, h);
        }

        /// <summary>Deep navy lacquer with soft mottling and faint vertical brush marks.</summary>
        private static (Color[], float[]) GenNavyWall(int n)
        {
            var c = new Color[n * n];
            var h = new float[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float m = Fbm(u, v, 4, 5, 121);
                    float brush = Value(u * 64f, v * 4f, 64, 122);
                    var col = new Color(0.06f, 0.1f, 0.26f) * Mathf.Lerp(0.8f, 1.15f, m) * (0.95f + 0.1f * brush);
                    col.a = 1f;
                    c[y * n + x] = col;
                    h[y * n + x] = brush * 0.15f + m * 0.1f;
                }
            return (c, h);
        }

        /// <summary>Polished black marble with pale grey veins.</summary>
        private static (Color[], float[]) GenDarkMarble(int n)
        {
            var c = new Color[n * n];
            var h = new float[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float vein = S(0.93f, 0.99f, Ridge(u + Fbm(u, v, 2, 3, 131) * 0.3f, v, 3, 5, 132));
                    float cloud = Fbm(u, v, 4, 5, 133);
                    var col = new Color(0.05f, 0.055f, 0.075f) * Mathf.Lerp(0.8f, 1.3f, cloud);
                    col = Color.Lerp(col, new Color(0.35f, 0.37f, 0.42f), vein * 0.55f);
                    col.a = 1f;
                    c[y * n + x] = col;
                    h[y * n + x] = 0.5f;
                }
            return (c, h);
        }

        /// <summary>
        /// Art-deco floor for the mirror hall (one image across the room): black marble with a gold
        /// sunburst, nested diamonds and a border. With metal=true returns the metallic/smoothness map.
        /// </summary>
        private static Color[] GenDecoInlay(int w, int h, bool metal)
        {
            var c = new Color[w * h];
            float aspect = w / (float)h;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)w, v = y / (float)h;
                    float px = (u - 0.5f) * aspect, py = v - 0.5f;   // centred, isotropic units
                    float r = Mathf.Sqrt(px * px + py * py);
                    float ang = Mathf.Atan2(py, px);
                    float line = 0f;
                    // sunburst rays (16), fading out from the centre
                    float ray = Mathf.Abs(Mathf.Sin(ang * 8f));
                    if (r > 0.03f && r < 0.42f) line = Mathf.Max(line, S(0.035f, 0.0f, ray * r * 2f) * S(0.42f, 0.36f, r));
                    // concentric rings
                    foreach (float ring in new[] { 0.06f, 0.16f, 0.3f }) line = Mathf.Max(line, S(0.006f, 0f, Mathf.Abs(r - ring)));
                    // nested diamonds |x|+|y|
                    float d = Mathf.Abs(px) + Mathf.Abs(py);
                    foreach (float k in new[] { 0.36f, 0.46f }) line = Mathf.Max(line, S(0.006f, 0f, Mathf.Abs(d - k)));
                    // double border
                    float bx = Mathf.Min(u, 1f - u) * aspect, by = Mathf.Min(v, 1f - v);
                    float b = Mathf.Min(bx, by);
                    line = Mathf.Max(line, S(0.006f, 0f, Mathf.Abs(b - 0.03f)));
                    line = Mathf.Max(line, S(0.004f, 0f, Mathf.Abs(b - 0.05f)));
                    // corner fans
                    foreach (var corner in new[] { new Vector2(0.05f, 0.05f), new Vector2(aspect - 0.05f, 0.05f), new Vector2(0.05f, 0.95f), new Vector2(aspect - 0.05f, 0.95f) })
                    {
                        var q = new Vector2(u * aspect, v) - corner;
                        float cr = q.magnitude;
                        if (cr < 0.12f) line = Mathf.Max(line, S(0.02f, 0f, Mathf.Abs(Mathf.Sin(Mathf.Atan2(q.y, q.x) * 6f)) * cr * 3f) * 0.9f);
                    }
                    line = Mathf.Clamp01(line);
                    if (metal)
                    {
                        c[y * w + x] = new Color(line, line, line, Mathf.Lerp(0.82f, 0.7f, line));
                    }
                    else
                    {
                        float vein = S(0.93f, 0.99f, Ridge(u * 2f, v * 2f, 6, 4, 141));
                        var marble = new Color(0.045f, 0.05f, 0.07f) * Mathf.Lerp(0.85f, 1.25f, Fbm(u, v, 4, 4, 142));
                        marble = Color.Lerp(marble, new Color(0.3f, 0.32f, 0.36f), vein * 0.4f);
                        var col = Color.Lerp(marble, new Color(0.78f, 0.6f, 0.26f), line);
                        col.a = 1f;
                        c[y * w + x] = col;
                    }
                }
            return c;
        }

        /// <summary>Ochre plaster of an old study: warm, cracked, smoke-darkened toward the top.</summary>
        private static (Color[], float[]) GenWarmPlaster(int n)
        {
            var c = new Color[n * n];
            var h = new float[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float m = Fbm(u, v, 4, 6, 151);
                    float crack = S(0.965f, 0.99f, Ridge(u, v, 3, 5, 152));
                    float patch = S(0.62f, 0.7f, Fbm(u, v, 5, 4, 153));
                    var col = new Color(0.52f, 0.41f, 0.29f) * Mathf.Lerp(0.78f, 1.12f, m);
                    col = Color.Lerp(col, new Color(0.62f, 0.55f, 0.44f), patch * 0.5f);
                    col *= 1f - 0.6f * crack;
                    col.a = 1f;
                    c[y * n + x] = col;
                    h[y * n + x] = m * 0.35f - crack * 0.6f + patch * 0.1f;
                }
            return (c, h);
        }

        /// <summary>Grey-brown flagstones with dark mortar.</summary>
        private static (Color[], float[]) GenStoneFloor(int n)
        {
            var c = new Color[n * n];
            var h = new float[n * n];
            int cells = 2, cell = n / cells;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    int tx = x / cell, ty = y / cell, lx = x % cell, ly = y % cell;
                    float edge = Mathf.Min(Mathf.Min(lx, cell - 1 - lx), Mathf.Min(ly, cell - 1 - ly));
                    float m = Fbm(u, v, 6, 5, 161);
                    var col = new Color(0.33f, 0.31f, 0.29f) * (0.9f + 0.2f * Hash(tx, ty, 162)) * Mathf.Lerp(0.75f, 1.1f, m);
                    bool mortar = edge < 4f;
                    if (mortar) col = new Color(0.12f, 0.11f, 0.1f);
                    col.a = mortar ? 0.2f : 0.55f;
                    c[y * n + x] = col;
                    h[y * n + x] = mortar ? 0f : 0.5f + 0.3f * S(4f, 14f, edge) + m * 0.2f;
                }
            return (c, h);
        }

        /// <summary>Heavy velvet: vertical folds shaded into the colour, with matching normals.</summary>
        private static (Color[], float[]) GenCurtain(int n)
        {
            var c = new Color[n * n];
            var h = new float[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float fold = Mathf.Sin(u * Mathf.PI * 8f + Mathf.Sin(u * Mathf.PI * 2f) * 1.2f);
                    float nap = Fbm(u, v, 32, 2, 171);
                    var col = new Color(0.42f, 0.03f, 0.04f) * Mathf.Lerp(0.45f, 1.15f, fold * 0.5f + 0.5f) * (0.92f + 0.12f * nap);
                    col.a = 1f;
                    c[y * n + x] = col;
                    h[y * n + x] = fold * 0.5f;
                }
            return (c, h);
        }

        /// <summary>Persian rug: crimson field, navy and gold borders, a central medallion.</summary>
        private static Color[] GenRug(int w, int h)
        {
            var c = new Color[w * h];
            var crimson = new Color(0.42f, 0.07f, 0.06f);
            var navy = new Color(0.08f, 0.1f, 0.2f);
            var gold = new Color(0.62f, 0.45f, 0.18f);
            var cream = new Color(0.62f, 0.55f, 0.42f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)w, v = y / (float)h;
                    float bx = Mathf.Min(u, 1f - u) * (w / (float)h), by = Mathf.Min(v, 1f - v);
                    float b = Mathf.Min(bx, by);
                    Color col;
                    if (b < 0.035f) col = navy;
                    else if (b < 0.045f) col = gold;
                    else if (b < 0.09f) col = (Mathf.Sin(u * 90f) * Mathf.Sin(v * 60f) > 0.2f) ? gold : navy;
                    else if (b < 0.1f) col = cream;
                    else
                    {
                        float px = (u - 0.5f) * 1.6f, py = (v - 0.5f);
                        float diamond = Mathf.Abs(px) + Mathf.Abs(py);
                        if (diamond < 0.14f) col = (diamond < 0.07f) ? gold : navy;
                        else if (diamond < 0.16f) col = cream;
                        else
                        {
                            float motif = Mathf.Sin(u * 40f) * Mathf.Sin(v * 28f);
                            col = motif > 0.65f ? navy : (motif < -0.75f ? gold : crimson);
                        }
                    }
                    col *= Mathf.Lerp(0.7f, 1.05f, Fbm(u, v, 6, 4, 181)) * (0.92f + 0.12f * Fbm(u, v, 64, 2, 182));
                    col.a = 1f;
                    c[y * w + x] = col;
                }
            return c;
        }

        // ------------------------------------------------- line drawing (decals)

        private static void Stamp(float[] a, int w, int h, float x, float y, float r)
        {
            int x0 = Mathf.Max(0, (int)(x - r - 1)), x1 = Mathf.Min(w - 1, (int)(x + r + 1));
            int y0 = Mathf.Max(0, (int)(y - r - 1)), y1 = Mathf.Min(h - 1, (int)(y + r + 1));
            for (int yy = y0; yy <= y1; yy++)
                for (int xx = x0; xx <= x1; xx++)
                {
                    float d = Mathf.Sqrt((xx - x) * (xx - x) + (yy - y) * (yy - y));
                    float k = Mathf.Clamp01(r + 0.5f - d);
                    int i = yy * w + xx;
                    if (k > a[i]) a[i] = k;
                }
        }

        private static void Line(float[] a, int w, int h, Vector2 p, Vector2 q, float r)
        {
            int steps = Mathf.CeilToInt((q - p).magnitude * 2f) + 1;
            for (int i = 0; i <= steps; i++) { var t = Vector2.Lerp(p, q, i / (float)steps); Stamp(a, w, h, t.x, t.y, r); }
        }

        private static void Curve(float[] a, int w, int h, Func<float, Vector2> f, float r, int steps = 400)
        {
            for (int i = 0; i <= steps; i++) { var t = f(i / (float)steps); Stamp(a, w, h, t.x, t.y, r); }
        }

        private static void Ellipse(float[] a, int w, int h, Vector2 c, Vector2 radii, float rot, float r)
        {
            float cs = Mathf.Cos(rot), sn = Mathf.Sin(rot);
            Curve(a, w, h, t =>
            {
                float ang = t * Mathf.PI * 2f;
                var e = new Vector2(Mathf.Cos(ang) * radii.x, Mathf.Sin(ang) * radii.y);
                return c + new Vector2(e.x * cs - e.y * sn, e.x * sn + e.y * cs);
            }, r, 600);
        }

        /// <summary>
        /// Charcoal sketches for the study walls: DNA helix, atom, Vitruvian figure, a brain,
        /// molecules, a gear and a sine wave. Dark lines in alpha on transparent.
        /// </summary>
        private static Color[] GenSchematics(int w, int h)
        {
            var a = new float[w * h];
            const float t = 1.6f;
            // DNA helix
            Curve(a, w, h, s => new Vector2(60f + Mathf.Sin(s * 12f) * 34f, 40f + s * 430f), t);
            Curve(a, w, h, s => new Vector2(60f + Mathf.Sin(s * 12f + Mathf.PI) * 34f, 40f + s * 430f), t);
            for (int i = 0; i < 22; i++)
            {
                float s = i / 21f, y = 40f + s * 430f;
                Line(a, w, h, new Vector2(60f + Mathf.Sin(s * 12f) * 34f, y), new Vector2(60f + Mathf.Sin(s * 12f + Mathf.PI) * 34f, y), 1f);
            }
            // Brain: outline and folds
            var bc = new Vector2(230f, 360f);
            Ellipse(a, w, h, bc, new Vector2(95f, 70f), 0f, t);
            for (int i = 0; i < 5; i++)
            {
                float yy = -45f + i * 22f;
                Curve(a, w, h, s => bc + new Vector2(-80f + s * 160f, yy + Mathf.Sin(s * 18f + i) * 7f), 1f, 200);
            }
            Line(a, w, h, bc + new Vector2(0f, -70f), bc + new Vector2(0f, 70f), 1.2f);
            Curve(a, w, h, s => bc + new Vector2(40f + s * 30f, -70f - s * 40f), t, 60); // stem
            // Vitruvian figure
            var vc = new Vector2(500f, 270f);
            Ellipse(a, w, h, vc, new Vector2(150f, 150f), 0f, t);
            Line(a, w, h, vc + new Vector2(-132f, -132f), vc + new Vector2(132f, -132f), t);
            Line(a, w, h, vc + new Vector2(132f, -132f), vc + new Vector2(132f, 132f), t);
            Line(a, w, h, vc + new Vector2(132f, 132f), vc + new Vector2(-132f, 132f), t);
            Line(a, w, h, vc + new Vector2(-132f, 132f), vc + new Vector2(-132f, -132f), t);
            Ellipse(a, w, h, vc + new Vector2(0f, 100f), new Vector2(16f, 20f), 0f, t);       // head
            Line(a, w, h, vc + new Vector2(0f, 80f), vc + new Vector2(0f, -30f), t);           // body
            Line(a, w, h, vc + new Vector2(-130f, 55f), vc + new Vector2(130f, 55f), t);       // arms out
            Line(a, w, h, vc + new Vector2(0f, 55f), vc + new Vector2(-120f, 115f), t);        // arms up
            Line(a, w, h, vc + new Vector2(0f, 55f), vc + new Vector2(120f, 115f), t);
            Line(a, w, h, vc + new Vector2(0f, -30f), vc + new Vector2(-20f, -132f), t);       // legs
            Line(a, w, h, vc + new Vector2(0f, -30f), vc + new Vector2(20f, -132f), t);
            Line(a, w, h, vc + new Vector2(0f, -30f), vc + new Vector2(-90f, -110f), t);
            Line(a, w, h, vc + new Vector2(0f, -30f), vc + new Vector2(90f, -110f), t);
            // Atom
            var ac = new Vector2(760f, 380f);
            for (int i = 0; i < 3; i++) Ellipse(a, w, h, ac, new Vector2(80f, 26f), i * Mathf.PI / 3f, t);
            Stamp(a, w, h, ac.x, ac.y, 7f);
            // Molecules (hexagons)
            foreach (var hc in new[] { new Vector2(700f, 150f), new Vector2(752f, 180f), new Vector2(752f, 120f) })
                for (int k = 0; k < 6; k++)
                {
                    float a0 = k * Mathf.PI / 3f, a1 = (k + 1) * Mathf.PI / 3f;
                    Line(a, w, h, hc + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * 30f, hc + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * 30f, t);
                }
            // Gear
            var gc = new Vector2(920f, 220f);
            Ellipse(a, w, h, gc, new Vector2(55f, 55f), 0f, t);
            Ellipse(a, w, h, gc, new Vector2(18f, 18f), 0f, t);
            for (int k = 0; k < 12; k++)
            {
                float ang = k * Mathf.PI / 6f;
                var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                Line(a, w, h, gc + dir * 55f, gc + dir * 72f, 4f);
            }
            // Sine wave with axis
            Line(a, w, h, new Vector2(160f, 120f), new Vector2(420f, 120f), 1.2f);
            Curve(a, w, h, s => new Vector2(160f + s * 260f, 120f + Mathf.Sin(s * Mathf.PI * 6f) * 40f * (1f - s * 0.5f)), t);

            var c = new Color[w * h];
            for (int i = 0; i < c.Length; i++)
            {
                float rough = 0.75f + 0.25f * Hash(i % w, i / w, 191);
                c[i] = new Color(0.12f, 0.09f, 0.06f, a[i] * 0.85f * rough);
            }
            return c;
        }

        /// <summary>Corner cobweb: strands radiating from the top-left corner with sagging cross threads.</summary>
        private static Color[] GenCobweb(int w, int h)
        {
            var a = new float[w * h];
            var origin = new Vector2(0f, h - 1f);
            int strands = 7;
            for (int k = 0; k < strands; k++)
            {
                float ang = -Mathf.PI * 0.5f * (k + 0.5f) / strands;
                var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                Line(a, w, h, origin, origin + dir * 250f, 0.7f);
            }
            for (int ring = 1; ring <= 7; ring++)
            {
                float rr = ring * 32f;
                for (int k = 0; k < strands - 1; k++)
                {
                    float a0 = -Mathf.PI * 0.5f * (k + 0.5f) / strands, a1 = -Mathf.PI * 0.5f * (k + 1.5f) / strands;
                    var p = origin + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * rr;
                    var q = origin + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * rr;
                    var mid = (p + q) * 0.5f - (origin - (p + q) * 0.5f).normalized * rr * 0.08f;
                    Curve(a, w, h, s => Vector2.Lerp(Vector2.Lerp(p, mid, s), Vector2.Lerp(mid, q, s), s), 0.6f, 80);
                }
            }
            var c = new Color[w * h];
            for (int i = 0; i < c.Length; i++) c[i] = new Color(0.85f, 0.85f, 0.88f, a[i] * 0.7f);
            return c;
        }
    }
}
