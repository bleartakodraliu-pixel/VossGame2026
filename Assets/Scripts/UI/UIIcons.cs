using System;
using System.Collections.Generic;
using UnityEngine;

namespace Voss.UI
{
    /// <summary>
    /// Small line icons drawn in code (the built-in font has no symbols): play, book, gear, power,
    /// the top-hat logo, a diamond ornament and a left-to-right shadow gradient. White, tinted by Image.color.
    /// </summary>
    public static class UIIcons
    {
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Play => Get("play", 64, a =>
        {
            Line(a, 64, new Vector2(18, 10), new Vector2(18, 54), 2.6f);
            Line(a, 64, new Vector2(18, 54), new Vector2(52, 32), 2.6f);
            Line(a, 64, new Vector2(52, 32), new Vector2(18, 10), 2.6f);
        });

        public static Sprite Book => Get("book", 64, a =>
        {
            Line(a, 64, new Vector2(32, 14), new Vector2(32, 50), 2.2f);
            Line(a, 64, new Vector2(32, 50), new Vector2(8, 54), 2.2f);
            Line(a, 64, new Vector2(8, 54), new Vector2(8, 18), 2.2f);
            Line(a, 64, new Vector2(8, 18), new Vector2(32, 14), 2.2f);
            Line(a, 64, new Vector2(32, 50), new Vector2(56, 54), 2.2f);
            Line(a, 64, new Vector2(56, 54), new Vector2(56, 18), 2.2f);
            Line(a, 64, new Vector2(56, 18), new Vector2(32, 14), 2.2f);
            for (int i = 0; i < 3; i++)
            {
                Line(a, 64, new Vector2(13, 40 - i * 8), new Vector2(27, 38 - i * 8), 1.3f);
                Line(a, 64, new Vector2(37, 38 - i * 8), new Vector2(51, 40 - i * 8), 1.3f);
            }
        });

        public static Sprite Gear => Get("gear", 64, a =>
        {
            Circle(a, 64, new Vector2(32, 32), 15f, 2.4f);
            Circle(a, 64, new Vector2(32, 32), 6f, 2.2f);
            for (int i = 0; i < 8; i++)
            {
                float ang = i * Mathf.PI / 4f;
                var d = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                Line(a, 64, new Vector2(32, 32) + d * 16f, new Vector2(32, 32) + d * 23f, 4f);
            }
        });

        public static Sprite Power => Get("power", 64, a =>
        {
            Arc(a, 64, new Vector2(32, 30), 18f, 2.6f, 120f, 420f);
            Line(a, 64, new Vector2(32, 34), new Vector2(32, 56), 2.8f);
        });

        public static Sprite Diamond => Get("diamond", 32, a =>
        {
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    if (Mathf.Abs(x - 15.5f) + Mathf.Abs(y - 15.5f) < 9f) a[y * 32 + x] = 1f;
        });

        /// <summary>Top hat: tall crown, narrow band (drawn separately in red), wide curved brim.</summary>
        public static Sprite HatCrown => Get("hatcrown", 128, a =>
        {
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 128; x++)
                {
                    float u = x - 63.5f;
                    bool crown = y > 34 && y < 112 && Mathf.Abs(u) < 26f + (y - 34) * 0.06f;
                    float brimY = 30f + u * u * 0.004f;
                    bool brim = Mathf.Abs(y - brimY) < 6f && Mathf.Abs(u) < 58f;
                    if (crown || brim) a[y * 128 + x] = 1f;
                }
        });

        public static Sprite HatBand => Get("hatband", 128, a =>
        {
            for (int y = 40; y < 52; y++)
                for (int x = 0; x < 128; x++)
                    if (Mathf.Abs(x - 63.5f) < 27f) a[y * 128 + x] = 1f;
        });

        /// <summary>Black on the left fading to clear: keeps the menu readable over the scene.</summary>
        public static Sprite LeftShade
        {
            get
            {
                if (cache.TryGetValue("shade", out var s)) return s;
                var tex = new Texture2D(256, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int x = 0; x < 256; x++)
                {
                    float t = x / 255f;
                    float alpha = Mathf.Clamp01(1f - Mathf.SmoothStep(0.2f, 1f, t));
                    for (int y = 0; y < 4; y++) tex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
                }
                tex.Apply();
                s = Sprite.Create(tex, new Rect(0, 0, 256, 4), new Vector2(0.5f, 0.5f));
                cache["shade"] = s;
                return s;
            }
        }

        // ---------------------------------------------------------------- drawing

        private static Sprite Get(string key, int size, Action<float[]> draw)
        {
            if (cache.TryGetValue(key, out var s)) return s;
            var a = new float[size * size];
            draw(a);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = new Color(1f, 1f, 1f, Mathf.Clamp01(a[i]));
            tex.SetPixels(px);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            cache[key] = s;
            return s;
        }

        private static void Stamp(float[] a, int n, Vector2 p, float r)
        {
            int x0 = Mathf.Max(0, (int)(p.x - r - 1)), x1 = Mathf.Min(n - 1, (int)(p.x + r + 1));
            int y0 = Mathf.Max(0, (int)(p.y - r - 1)), y1 = Mathf.Min(n - 1, (int)(p.y + r + 1));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float k = Mathf.Clamp01(r + 0.5f - Vector2.Distance(new Vector2(x, y), p));
                    if (k > a[y * n + x]) a[y * n + x] = k;
                }
        }

        private static void Line(float[] a, int n, Vector2 p, Vector2 q, float r)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(p, q) * 2f) + 1;
            for (int i = 0; i <= steps; i++) Stamp(a, n, Vector2.Lerp(p, q, i / (float)steps), r);
        }

        private static void Circle(float[] a, int n, Vector2 c, float radius, float r) => Arc(a, n, c, radius, r, 0f, 360f);

        private static void Arc(float[] a, int n, Vector2 c, float radius, float r, float fromDeg, float toDeg)
        {
            int steps = Mathf.CeilToInt(radius * (toDeg - fromDeg) * Mathf.Deg2Rad * 2f);
            for (int i = 0; i <= steps; i++)
            {
                float ang = Mathf.Lerp(fromDeg, toDeg, i / (float)steps) * Mathf.Deg2Rad;
                Stamp(a, n, c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radius, r);
            }
        }
    }
}
