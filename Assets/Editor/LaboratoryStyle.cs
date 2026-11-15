using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Voss.Atmosphere;
using Voss.Audio;
using K = Voss.EditorTools.VossBuildKit;

namespace Voss.EditorTools
{
    /// <summary>
    /// Visual style pass after the concept art: the hall and corridors as the damp, red-lit underground
    /// of a theatre (cracked concrete, beams, wet floor, red cage lamps, a glowing specimen tank, a
    /// curtained stage corner, crates and flight cases); PERCEPTION as an art-deco mirror hall (navy and
    /// gold, black marble with a gold sunburst, velvet, cobwebs); REASON as an alchemist's study (ochre
    /// plaster covered in sketches, brass gears, books, lanterns, a Persian rug). Purely visual.
    /// </summary>
    public static class LaboratoryStyle
    {
        private static readonly Color RedLamp = new Color(1f, 0.12f, 0.08f);

        public static void Build(Transform root, Transform[] rooms)
        {
            var hall = root.Find("Entrance");
            var style = K.Group(root, "Style").transform;
            Hall(hall, style);
            Perception(rooms[0]);
            Reason(rooms[1]);
            Memory(rooms[2]);
            Corridors(style);
        }

        // ================================================================= hall

        private static void Hall(Transform hall, Transform style)
        {
            var h = K.Group(hall, "Underground").transform;

            // Heavy concrete beams across the ceiling.
            foreach (float x in new[] { -12f, -4f, 4f, 12f })
                K.SurfaceBox(h, "Beam", new Vector3(x, 4.75f, 0f), new Vector3(0.45f, 0.5f, 14f), K.Mats.Cracked, false);

            // Red cage lamps along both long walls.
            RedCageLamp(h, new Vector3(-13.6f, 2.7f, 6.8f), Vector3.back, 1.4f, false);
            RedCageLamp(h, new Vector3(3f, 2.7f, 6.8f), Vector3.back, 1.4f, true);
            RedCageLamp(h, new Vector3(10.3f, 2.7f, 6.8f), Vector3.back, 1.4f, false);
            RedCageLamp(h, new Vector3(-13f, 2.7f, -6.8f), Vector3.forward, 1.4f, true);
            RedCageLamp(h, new Vector3(6.8f, 2.8f, -6.8f), Vector3.forward, 1.4f, false);
            RedCageLamp(h, new Vector3(12.5f, 2.7f, -6.8f), Vector3.forward, 1.4f, false);

            SpecimenTank(h, new Vector3(-14.6f, 0f, 0.8f));
            StageCorner(h);
            Shelving(h, new Vector3(-3.2f, 0f, 6.48f));
            Envelopes(h);

            FlightCase(h, new Vector3(14.8f, 0f, 4.9f), new Vector3(0.8f, 0.6f, 0.55f), 8f);
            FlightCase(h, new Vector3(14.75f, 0.6f, 4.85f), new Vector3(0.7f, 0.45f, 0.5f), -6f);
            FlightCase(h, new Vector3(14.8f, 0f, -2.6f), new Vector3(0.9f, 0.7f, 0.6f), 0f);
            FlightCase(h, new Vector3(-2.4f, 0f, -5.9f), new Vector3(0.8f, 0.55f, 0.55f), 12f);

            foreach (var (p, size, yaw) in new[] { (new Vector3(-6f, 0f, 1.5f), new Vector2(1.6f, 1f), 20f), (new Vector3(3.5f, 0f, -2.8f), new Vector2(1.2f, 0.8f), 70f),
                     (new Vector3(9f, 0f, 2.5f), new Vector2(2f, 1.2f), -15f), (new Vector3(-2.5f, 0f, -4.6f), new Vector2(1f, 0.7f), 40f),
                     (new Vector3(12.5f, 0f, 0.5f), new Vector2(1.4f, 0.9f), 0f), (new Vector3(-11f, 0f, 2.8f), new Vector2(1.2f, 0.9f), 110f) })
                Puddle(h, p, size, yaw);

            EdgeStripe(h, new Vector3(-15.5f, 0f, 5.75f), new Vector3(15.5f, 0f, 5.75f));
            EdgeStripe(h, new Vector3(-15.5f, 0f, -5.55f), new Vector3(15.5f, 0f, -5.55f));

            Cobweb(h, new Vector3(-15.4f, 4.4f, 6.83f), Vector3.back, 1.2f, false);
            Cobweb(h, new Vector3(15.4f, 4.4f, -6.83f), Vector3.forward, 1.2f, false);

            Probe(h, new Vector3(0f, 2.5f, 0f), new Vector3(32f, 5f, 14f));
        }

        /// <summary>Glass tank of glowing red fluid with a slowly turning helix, wired into the ceiling.</summary>
        private static void SpecimenTank(Transform parent, Vector3 at)
        {
            var t = K.Group(parent, "SpecimenTank", at).transform;
            K.Prim(t, "Base", PrimitiveType.Cylinder, new Vector3(0f, 0.2f, 0f), new Vector3(1.35f, 0.2f, 1.35f), K.Mats.BlackMetal);
            K.Prim(t, "BaseRing", PrimitiveType.Cylinder, new Vector3(0f, 0.42f, 0f), new Vector3(1.2f, 0.03f, 1.2f), K.Mats.Steel, false);
            K.Prim(t, "Glass", PrimitiveType.Cylinder, new Vector3(0f, 1.45f, 0f), new Vector3(1.1f, 1.0f, 1.1f), K.Mats.Glass, false);
            K.Prim(t, "Fluid", PrimitiveType.Cylinder, new Vector3(0f, 1.38f, 0f), new Vector3(1.02f, 0.92f, 1.02f), K.Mats.RedLiquid, false);
            K.Prim(t, "Cap", PrimitiveType.Cylinder, new Vector3(0f, 2.55f, 0f), new Vector3(1.3f, 0.1f, 1.3f), K.Mats.Steel);
            K.Prim(t, "CapTop", PrimitiveType.Cylinder, new Vector3(0f, 2.72f, 0f), new Vector3(0.7f, 0.08f, 0.7f), K.Mats.BlackMetal, false);
            foreach (float a in new[] { 0f, 120f, 240f })
            {
                var dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                K.Box(t, "Strut", dir * 0.58f + Vector3.up * 1.45f, new Vector3(0.05f, 2.1f, 0.05f), K.Mats.Steel, false);
            }
            K.Prim(t, "PipeA", PrimitiveType.Cylinder, new Vector3(-0.25f, 3.85f, 0.1f), new Vector3(0.12f, 1.15f, 0.12f), K.Mats.Rust, false);
            K.Prim(t, "PipeB", PrimitiveType.Cylinder, new Vector3(0.2f, 3.85f, -0.15f), new Vector3(0.08f, 1.15f, 0.08f), K.Mats.Steel, false);

            var helix = K.Group(t, "Helix", new Vector3(0f, 1.4f, 0f)).transform;
            for (int i = 0; i < 18; i++)
            {
                float y = -0.75f + i * 0.088f, a = i * 0.55f;
                var p1 = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.22f + Vector3.up * y;
                var p2 = new Vector3(Mathf.Cos(a + Mathf.PI), 0f, Mathf.Sin(a + Mathf.PI)) * 0.22f + Vector3.up * y;
                K.Prim(helix, "BaseA", PrimitiveType.Sphere, p1, Vector3.one * 0.06f, K.Mats.DnaGlow, false);
                K.Prim(helix, "BaseB", PrimitiveType.Sphere, p2, Vector3.one * 0.06f, K.Mats.DnaGlow, false);
                var rung = K.Box(helix, "Rung", (p1 + p2) * 0.5f, new Vector3(0.44f, 0.012f, 0.012f), K.Mats.Porcelain, false);
                rung.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
            }
            helix.gameObject.AddComponent<Spin>().Configure(Vector3.up, 14f);

            var glow = K.PointLight(t, "TankGlow", new Vector3(0f, 1.5f, 0f), new Color(1f, 0.15f, 0.1f), 2.6f, 6.5f);
            glow.gameObject.AddComponent<LightFlicker>().Configure(0.12f, new Vector2(8f, 16f), 0.1f);
            t.gameObject.AddComponent<LoopingSound>().Configure(SfxId.RoomTone, 0.35f, 5f);

            // Control console beside it.
            var c = K.Group(parent, "TankConsole", at + new Vector3(-0.15f, 0f, -1.4f)).transform;
            K.Box(c, "Body", new Vector3(0f, 0.5f, 0f), new Vector3(0.5f, 1f, 0.45f), K.Mats.DoorPaint);
            var panel = K.Box(c, "Panel", new Vector3(0.12f, 1.05f, 0f), new Vector3(0.3f, 0.04f, 0.42f), K.Mats.Steel, false);
            panel.transform.localRotation = Quaternion.Euler(0f, 0f, 30f);
            K.Box(c, "Screen", new Vector3(0.2f, 0.82f, 0f), new Vector3(0.02f, 0.18f, 0.28f), K.Mats.SignRed, false);
            for (int i = 0; i < 4; i++) K.Box(c, "Button", new Vector3(0.18f, 1.1f, -0.12f + i * 0.08f), new Vector3(0.03f, 0.03f, 0.04f), i % 2 == 0 ? K.Mats.SignRed : K.Mats.Brass, false);
        }

        /// <summary>The observation window dressed as a little stage: riser, velvet drapes, pelmet, masks, a red spot.</summary>
        private static void StageCorner(Transform parent)
        {
            var s = K.Group(parent, "StageCorner").transform;
            K.SurfaceBox(s, "Riser", new Vector3(15.1f, 0.15f, 1f), new Vector3(1.5f, 0.3f, 5.4f), K.Mats.OldWood);
            K.Box(s, "RiserLip", new Vector3(14.36f, 0.31f, 1f), new Vector3(0.04f, 0.03f, 5.4f), K.Mats.Gold, false);
            Drape(s, new Vector3(15.74f, 1.75f, -1.45f), new Vector3(0.12f, 2.9f, 0.9f));
            Drape(s, new Vector3(15.74f, 1.75f, 3.45f), new Vector3(0.12f, 2.9f, 0.9f));
            K.SurfaceBox(s, "Pelmet", new Vector3(15.74f, 2.75f, 1f), new Vector3(0.16f, 0.5f, 5.8f), K.Mats.Curtain, false);
            K.Box(s, "PelmetTrim", new Vector3(15.65f, 2.5f, 1f), new Vector3(0.03f, 0.04f, 5.8f), K.Mats.Gold, false);
            Mask(s, new Vector3(15.8f, 3.45f, 0.55f), Vector3.left, true, -12f);
            Mask(s, new Vector3(15.8f, 3.4f, 1.45f), Vector3.left, false, 12f);
            K.SpotLight(s, "StageSpot", new Vector3(12.5f, 4.4f, 1f), new Vector3(15.8f, 1.6f, 1f), new Color(1f, 0.14f, 0.1f), 9f, 9f, 55f);
        }

        private static void Drape(Transform parent, Vector3 center, Vector3 size)
        {
            K.SurfaceBox(parent, "Drape", center, size, K.Mats.Curtain, false);
            var pool = size; pool.y = 0.12f; pool += new Vector3(size.x > size.z ? 0.15f : 0.1f, 0f, size.z > size.x ? 0.15f : 0.1f);
            K.SurfaceBox(parent, "DrapePool", new Vector3(center.x, center.y - size.y * 0.5f + 0.06f, center.z), pool, K.Mats.Curtain, false);
            var tie = size; tie.y = 0.06f; tie += Vector3.one * 0.03f; tie.y = 0.06f;
            K.Box(parent, "Tieback", new Vector3(center.x, center.y - size.y * 0.5f + 1.1f, center.z), tie, K.Mats.Gold, false);
        }

        /// <summary>Theatre mask: pale face, dark eyes, a mouth that smiles or grieves.</summary>
        private static void Mask(Transform parent, Vector3 at, Vector3 facing, bool comedy, float tilt)
        {
            var m = K.Group(parent, comedy ? "MaskComedy" : "MaskTragedy", at).transform;
            m.localRotation = Quaternion.LookRotation(facing, Vector3.up) * Quaternion.Euler(0f, 0f, tilt);
            K.Prim(m, "Face", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.36f, 0.46f, 0.12f), K.Mats.Porcelain, false);
            foreach (float x in new[] { -0.08f, 0.08f })
            {
                var eye = K.Prim(m, "Eye", PrimitiveType.Sphere, new Vector3(x, 0.07f, 0.05f), new Vector3(0.08f, comedy ? 0.04f : 0.06f, 0.03f), K.Mats.Silhouette, false);
                eye.transform.localRotation = Quaternion.Euler(0f, 0f, (comedy ? 1f : -1f) * Mathf.Sign(x) * 15f);
            }
            for (int i = 0; i < 5; i++)
            {
                float x = -0.08f + i * 0.04f;
                float y = -0.1f + (comedy ? -1f : 1f) * (0.04f - Mathf.Abs(x) * 0.6f);
                K.Box(m, "Mouth", new Vector3(x, y, 0.055f), new Vector3(0.045f, 0.015f, 0.02f), K.Mats.Silhouette, false);
            }
        }

        /// <summary>Industrial shelving packed with archive boxes and binders.</summary>
        private static void Shelving(Transform parent, Vector3 at)
        {
            var s = K.Group(parent, "ArchiveShelving", at).transform;
            const float w = 2.8f, d = 0.45f;
            foreach (float x in new[] { -w * 0.5f, w * 0.5f })
                foreach (float z in new[] { -d * 0.5f, d * 0.5f })
                    K.Box(s, "Post", new Vector3(x, 1.1f, z), new Vector3(0.04f, 2.2f, 0.04f), K.Mats.Steel, false);
            float[] levels = { 0.1f, 0.65f, 1.2f, 1.75f };
            var binders = new[] { K.Mats.Silhouette, K.Mats.Velvet, K.Mats.DoorPaint, K.Mats.Navy, K.Mats.OldPaper };
            var rnd = new System.Random(23);
            foreach (float y in levels)
            {
                K.Box(s, "Shelf", new Vector3(0f, y, 0f), new Vector3(w, 0.03f, d), K.Mats.Steel);
                float x = -w * 0.5f + 0.06f;
                while (x < w * 0.5f - 0.2f)
                {
                    if (rnd.NextDouble() < 0.55)
                    {
                        float bw = 0.32f + (float)rnd.NextDouble() * 0.14f, bh = 0.24f + (float)rnd.NextDouble() * 0.14f;
                        if (x + bw > w * 0.5f - 0.05f) break;
                        var box = K.Box(s, "ArchiveBox", new Vector3(x + bw * 0.5f, y + 0.015f + bh * 0.5f, 0f), new Vector3(bw, bh, 0.38f), K.Mats.Cardboard, false);
                        box.transform.localRotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 6f - 3f, 0f);
                        K.Box(s, "BoxLabel", new Vector3(x + bw * 0.5f, y + 0.015f + bh * 0.55f, -0.192f), new Vector3(bw * 0.4f, bh * 0.25f, 0.004f), K.Mats.OldPaper, false);
                        x += bw + 0.04f;
                    }
                    else
                    {
                        int count = 4 + rnd.Next(5);
                        for (int i = 0; i < count && x < w * 0.5f - 0.08f; i++)
                        {
                            K.Box(s, "Binder", new Vector3(x + 0.03f, y + 0.165f, 0f), new Vector3(0.055f, 0.3f, 0.26f), binders[rnd.Next(binders.Length)], false);
                            x += 0.06f;
                        }
                        x += 0.05f;
                    }
                }
            }
        }

        /// <summary>Black envelopes with red wax seals among the subject files.</summary>
        private static void Envelopes(Transform parent)
        {
            var spots = new[] { (new Vector3(-13.0f, 0.785f, 6.15f), 22f), (new Vector3(-12.85f, 0.796f, 6.45f), -12f), (new Vector3(-11.85f, 0.797f, 6.05f), 35f) };
            foreach (var (p, yaw) in spots)
            {
                var e = K.Group(parent, "BlackEnvelope", p).transform;
                e.localRotation = Quaternion.Euler(0f, yaw, 0f);
                K.Box(e, "Body", Vector3.zero, new Vector3(0.22f, 0.006f, 0.13f), K.Mats.Silhouette, false);
                K.Box(e, "FlapEdgeA", new Vector3(-0.055f, 0.004f, 0.02f), new Vector3(0.13f, 0.002f, 0.006f), K.Mats.FlightCase, false).transform.localRotation = Quaternion.Euler(0f, 30f, 0f);
                K.Box(e, "FlapEdgeB", new Vector3(0.055f, 0.004f, 0.02f), new Vector3(0.13f, 0.002f, 0.006f), K.Mats.FlightCase, false).transform.localRotation = Quaternion.Euler(0f, -30f, 0f);
                K.Prim(e, "WaxSeal", PrimitiveType.Cylinder, new Vector3(0f, 0.006f, -0.01f), new Vector3(0.04f, 0.004f, 0.04f), K.Mats.RedString, false);
            }
        }

        // =========================================================== perception

        private static void Perception(Transform p)
        {
            var s = K.Group(p, "ArtDeco").transform;

            // Black marble with a gold sunburst inlay across the whole floor.
            var inlay = K.Quad(s, "DecoFloor", new Vector3(0f, 0.003f, 17f), new Vector2(8.1f, 11.7f), Vector3.up, K.Mats.DecoInlay, false, Vector3.forward);
            inlay.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            // Gold frieze, skirting and pilasters on navy walls.
            const float top = 3.85f;
            K.Box(s, "Frieze_W", new Vector3(-4.03f, top, 17f), new Vector3(0.04f, 0.07f, 11.7f), K.Mats.Gold, false);
            K.Box(s, "Frieze_E", new Vector3(4.03f, top, 17f), new Vector3(0.04f, 0.07f, 11.7f), K.Mats.Gold, false);
            K.Box(s, "Frieze_N", new Vector3(0f, top, 22.83f), new Vector3(8.1f, 0.07f, 0.04f), K.Mats.Gold, false);
            K.Box(s, "Frieze_S", new Vector3(0f, top, 11.17f), new Vector3(8.1f, 0.07f, 0.04f), K.Mats.Gold, false);
            K.Box(s, "Frieze2_W", new Vector3(-4.03f, top + 0.14f, 17f), new Vector3(0.03f, 0.03f, 11.7f), K.Mats.Gold, false);
            K.Box(s, "Frieze2_E", new Vector3(4.03f, top + 0.14f, 17f), new Vector3(0.03f, 0.03f, 11.7f), K.Mats.Gold, false);
            K.Box(s, "Frieze2_N", new Vector3(0f, top + 0.14f, 22.83f), new Vector3(8.1f, 0.03f, 0.03f), K.Mats.Gold, false);
            K.Box(s, "Frieze2_S", new Vector3(0f, top + 0.14f, 11.17f), new Vector3(8.1f, 0.03f, 0.03f), K.Mats.Gold, false);
            foreach (float x in new[] { -4.03f, 4.03f })
            {
                K.Box(s, "Skirting", new Vector3(x, 0.08f, 17f), new Vector3(0.04f, 0.1f, 11.7f), K.Mats.Gold, false);
                foreach (float z in new[] { 12.2f, 15.6f, 18.4f, 21.8f })
                    K.Box(s, "Pilaster", new Vector3(x, (top + 0.12f) * 0.5f, z), new Vector3(0.035f, top - 0.12f, 0.06f), K.Mats.Gold, false);
            }
            foreach (float x in new[] { -3f, -1.35f, 1.35f })
                K.Box(s, "Pilaster", new Vector3(x, (top + 0.12f) * 0.5f, 22.83f), new Vector3(0.06f, top - 0.12f, 0.035f), K.Mats.Gold, false);
            foreach (float x in new[] { -1.5f, 1.5f, -3.8f, 3.8f })
                K.Box(s, "Pilaster", new Vector3(x, (top + 0.12f) * 0.5f, 11.17f), new Vector3(0.06f, top - 0.12f, 0.035f), K.Mats.Gold, false);

            // Gold frames for the illusions and the eye chart, an EXIT sign over the far door.
            foreach (float x in new[] { -2.7f, 2.7f }) Frame(s, new Vector3(x, 2.1f, 11.19f), new Vector2(1.6f, 1.1f), Vector3.forward);
            Frame(s, new Vector3(-4.02f, 2.0f, 17f), new Vector2(0.75f, 1.1f), Vector3.right);
            ExitSign(s, new Vector3(2.8f, 3.72f, 22.78f), Vector3.back);

            // Velvet drapes framing the room.
            Drape(s, new Vector3(-3.99f, 2.2f, 11.85f), new Vector3(0.12f, 4.4f, 1.2f));
            Drape(s, new Vector3(3.99f, 2.2f, 11.85f), new Vector3(0.12f, 4.4f, 1.2f));
            Drape(s, new Vector3(-3.55f, 2.2f, 22.79f), new Vector3(1.1f, 4.4f, 0.12f));

            // Cobwebs in the corners and on two of the mirrors.
            Cobweb(s, new Vector3(-3.6f, 4.05f, 22.82f), Vector3.back, 0.9f, false);
            Cobweb(s, new Vector3(3.6f, 4.05f, 22.82f), Vector3.back, 0.9f, true);
            Cobweb(s, new Vector3(3.6f, 4.05f, 11.18f), Vector3.forward, 0.9f, false);
            Cobweb(s, new Vector3(-4.0f, 2.45f, 13.35f), Vector3.right, 0.6f, false);
            Cobweb(s, new Vector3(4.0f, 2.45f, 20.7f), Vector3.left, 0.6f, false);

            // Backstage clutter: an open costume trunk, crates, a side table with a mask and an armillary sphere.
            Trunk(s, new Vector3(-2.9f, 0f, 12.55f));
            Crate(s, new Vector3(3.2f, 0f, 12.5f), 0.6f, 0f);
            Crate(s, new Vector3(3.2f, 0.6f, 12.5f), 0.48f, 14f);
            SideTable(s, new Vector3(-2.6f, 0f, 22.3f));

            // The dress form wears a length of red velvet.
            var mannequin = p.Find("Props/Mannequin");
            if (mannequin != null)
            {
                var drape = K.SurfaceBox(mannequin, "RedDrape", new Vector3(0.16f, 1.25f, 0.05f), new Vector3(0.1f, 0.95f, 0.32f), K.Mats.Curtain, false);
                drape.transform.localRotation = Quaternion.Euler(0f, 0f, -10f);
            }

            Probe(s, new Vector3(0f, 2.2f, 17f), new Vector3(8.4f, 4.5f, 12f));
        }

        private static void Frame(Transform parent, Vector3 center, Vector2 size, Vector3 facing)
        {
            var f = K.Group(parent, "GoldFrame", center).transform;
            f.localRotation = Quaternion.LookRotation(facing, Vector3.up);
            const float b = 0.05f;
            K.Box(f, "T", new Vector3(0f, size.y * 0.5f + b * 0.5f, 0f), new Vector3(size.x + b * 2f, b, 0.03f), K.Mats.Gold, false);
            K.Box(f, "B", new Vector3(0f, -size.y * 0.5f - b * 0.5f, 0f), new Vector3(size.x + b * 2f, b, 0.03f), K.Mats.Gold, false);
            K.Box(f, "L", new Vector3(-size.x * 0.5f - b * 0.5f, 0f, 0f), new Vector3(b, size.y, 0.03f), K.Mats.Gold, false);
            K.Box(f, "R", new Vector3(size.x * 0.5f + b * 0.5f, 0f, 0f), new Vector3(b, size.y, 0.03f), K.Mats.Gold, false);
        }

        private static void ExitSign(Transform parent, Vector3 at, Vector3 facing)
        {
            var e = K.Group(parent, "ExitSign", at).transform;
            e.localRotation = Quaternion.LookRotation(facing, Vector3.up);
            K.Box(e, "Box", Vector3.zero, new Vector3(0.6f, 0.22f, 0.06f), K.Mats.SignRed, false);
            K.Text3D(e, "Text", "EXIT", new Vector3(0f, 0f, 0.035f), Vector3.forward, 0.13f, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, 48);
        }

        private static void Trunk(Transform parent, Vector3 at)
        {
            var t = K.Group(parent, "CostumeTrunk", at).transform;
            K.Box(t, "Chest", new Vector3(0f, 0.22f, 0f), new Vector3(0.9f, 0.44f, 0.55f), K.Mats.OldWood, false);
            K.Box(t, "Band", new Vector3(0f, 0.22f, 0f), new Vector3(0.92f, 0.05f, 0.57f), K.Mats.Brass, false);
            var lid = K.Group(t, "LidPivot", new Vector3(0f, 0.44f, 0.275f)).transform;
            lid.localRotation = Quaternion.Euler(-105f, 0f, 0f);
            K.Box(lid, "Lid", new Vector3(0f, 0.025f, -0.275f), new Vector3(0.9f, 0.05f, 0.55f), K.Mats.OldWood, false);
            var cloth = K.SurfaceBox(t, "Cloth", new Vector3(0.05f, 0.4f, -0.2f), new Vector3(0.6f, 0.1f, 0.35f), K.Mats.Curtain, false);
            cloth.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
            var clap = K.Group(t, "Clapperboard", new Vector3(-0.15f, 0.48f, -0.05f)).transform;
            clap.localRotation = Quaternion.Euler(-60f, 15f, 0f);
            K.Box(clap, "Slate", Vector3.zero, new Vector3(0.3f, 0.22f, 0.015f), K.Mats.Silhouette, false);
            for (int i = 0; i < 6; i++)
                K.Box(clap, "Stripe", new Vector3(-0.125f + i * 0.05f, 0.125f, 0f), new Vector3(0.05f, 0.04f, 0.017f), i % 2 == 0 ? K.Mats.WhitePanel : K.Mats.Silhouette, false);
            var col = t.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.25f, 0f);
            col.size = new Vector3(0.9f, 0.5f, 0.55f);
        }

        private static void Crate(Transform parent, Vector3 at, float size, float yaw)
        {
            var c = K.Group(parent, "Crate", at).transform;
            c.localRotation = Quaternion.Euler(0f, yaw, 0f);
            K.Box(c, "Body", new Vector3(0f, size * 0.5f, 0f), Vector3.one * size, K.Mats.OldWood);
            foreach (float y in new[] { 0.08f, 0.5f, 0.92f })
                K.Box(c, "Slat", new Vector3(0f, size * y, 0f), new Vector3(size + 0.01f, 0.04f, size + 0.01f), K.Mats.Wood, false);
        }

        private static void SideTable(Transform parent, Vector3 at)
        {
            var t = K.Group(parent, "SideTable", at).transform;
            K.Prim(t, "Top", PrimitiveType.Cylinder, new Vector3(0f, 0.74f, 0f), new Vector3(0.7f, 0.02f, 0.7f), K.Mats.OldWood, false);
            K.Prim(t, "Stem", PrimitiveType.Cylinder, new Vector3(0f, 0.37f, 0f), new Vector3(0.07f, 0.36f, 0.07f), K.Mats.OldWood, false);
            K.Prim(t, "Foot", PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f), new Vector3(0.4f, 0.02f, 0.4f), K.Mats.OldWood, false);
            var col = t.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.4f, 0f);
            col.size = new Vector3(0.7f, 0.8f, 0.7f);
            K.Box(t, "Book", new Vector3(-0.15f, 0.785f, 0.1f), new Vector3(0.2f, 0.05f, 0.26f), K.Mats.Leather, false).transform.localRotation = Quaternion.Euler(0f, 20f, 0f);
            var mask = K.Group(t, "Mask", new Vector3(0.12f, 0.8f, -0.12f)).transform;
            mask.localRotation = Quaternion.Euler(-70f, 30f, 0f);
            K.Prim(mask, "Face", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.18f, 0.24f, 0.07f), K.Mats.Porcelain, false);
            foreach (float x in new[] { -0.045f, 0.045f })
                K.Prim(mask, "Eye", PrimitiveType.Sphere, new Vector3(x, 0.035f, 0.03f), new Vector3(0.045f, 0.025f, 0.02f), K.Mats.Silhouette, false);
            // Armillary sphere: three gold rings on a stand.
            var arm = K.Group(t, "Armillary", new Vector3(0.1f, 0.75f, 0.15f)).transform;
            K.Prim(arm, "Stand", PrimitiveType.Cylinder, new Vector3(0f, 0.08f, 0f), new Vector3(0.02f, 0.08f, 0.02f), K.Mats.Gold, false);
            K.Prim(arm, "Foot", PrimitiveType.Cylinder, new Vector3(0f, 0.01f, 0f), new Vector3(0.14f, 0.01f, 0.14f), K.Mats.Gold, false);
            var sphere = K.Group(arm, "Rings", new Vector3(0f, 0.3f, 0f)).transform;
            Ring(sphere, Quaternion.identity, 0.13f);
            Ring(sphere, Quaternion.Euler(90f, 0f, 0f), 0.13f);
            Ring(sphere, Quaternion.Euler(0f, 0f, 23f) * Quaternion.Euler(90f, 60f, 0f), 0.12f);
            K.Prim(sphere, "Core", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.05f, K.Mats.Brass, false);
            sphere.gameObject.AddComponent<Spin>().Configure(Vector3.up, 6f);
        }

        /// <summary>Circle of short gold bars in the local XZ plane of <paramref name="rotation"/>.</summary>
        private static void Ring(Transform parent, Quaternion rotation, float radius)
        {
            var g = K.Group(parent, "Ring").transform;
            g.localRotation = rotation;
            int n = 20;
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                var seg = K.Box(g, "Seg", new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius, new Vector3(0.012f, 0.012f, radius * 2f * Mathf.PI / n + 0.004f), K.Mats.Gold, false);
                seg.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
            }
        }

        // =============================================================== reason

        private static void Reason(Transform r)
        {
            var s = K.Group(r, "Study").transform;

            // Sketches all over the ochre walls.
            Sketch(s, new Vector3(-4.02f, 3.55f, 14f), Vector3.right, new Vector2(3.2f, 1.6f));
            Sketch(s, new Vector3(-4.02f, 2.3f, 19.5f), Vector3.right, new Vector2(3.2f, 1.6f));
            Sketch(s, new Vector3(4.02f, 1.95f, 13f), Vector3.left, new Vector2(3f, 1.5f));

            // Brass clockwork on the far wall.
            var gears = K.Group(s, "Clockwork", new Vector3(0f, 0f, 22.78f)).transform;
            gears.localRotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
            // In this frame +x points to the room's west, so x is mirrored.
            Gear(gears, new Vector3(3.0f, 2.6f, 0f), 0.55f, 16, 12f);
            Gear(gears, new Vector3(2.25f, 2.05f, 0.03f), 0.3f, 10, -22f);
            Gear(gears, new Vector3(3.62f, 3.18f, 0.03f), 0.22f, 8, -30f);
            K.Prim(gears, "Pipe", PrimitiveType.Cylinder, new Vector3(2.9f, 3.65f, 0.08f), new Vector3(0.07f, 1.0f, 0.07f), K.Mats.Brass, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            K.Prim(gears, "PipeDown", PrimitiveType.Cylinder, new Vector3(1.95f, 3.28f, 0.08f), new Vector3(0.07f, 0.37f, 0.07f), K.Mats.Brass, false);
            K.Box(gears, "Gauges", new Vector3(1.95f, 2.8f, 0.08f), new Vector3(0.28f, 0.34f, 0.12f), K.Mats.Brass, false);
            K.Prim(gears, "ButtonA", PrimitiveType.Cylinder, new Vector3(1.95f, 2.87f, 0.15f), new Vector3(0.07f, 0.01f, 0.07f), K.Mats.Silhouette, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            K.Prim(gears, "ButtonB", PrimitiveType.Cylinder, new Vector3(1.95f, 2.73f, 0.15f), new Vector3(0.07f, 0.01f, 0.07f), K.Mats.Silhouette, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            Bookshelf(s, new Vector3(3.0f, 0f, 11.38f));

            var rug = K.Quad(s, "PersianRug", new Vector3(0f, 0.004f, 17f), new Vector2(3.2f, 4.8f), Vector3.up, K.Mats.Rug, false, Vector3.forward);
            rug.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            Lantern(s, new Vector3(3.52f, 0.76f, 12.75f));
            Lantern(s, new Vector3(2.52f, 0.8f, 18.7f));
            K.Box(s, "BookA", new Vector3(3.42f, 0.83f, 19.3f), new Vector3(0.24f, 0.06f, 0.3f), K.Mats.Leather, false).transform.localRotation = Quaternion.Euler(0f, 8f, 0f);
            K.Box(s, "BookB", new Vector3(3.42f, 0.885f, 19.3f), new Vector3(0.22f, 0.05f, 0.28f), K.Mats.DoorPaint, false).transform.localRotation = Quaternion.Euler(0f, -6f, 0f);
            var scroll = K.Prim(s, "Scroll", PrimitiveType.Cylinder, new Vector3(2.65f, 0.83f, 19.32f), new Vector3(0.05f, 0.16f, 0.05f), K.Mats.OldPaper, false);
            scroll.transform.localRotation = Quaternion.Euler(0f, 30f, 90f);

            // The far door: warning stripes and a caged red lamp.
            var exitPanel = r.Find("Exit_REASON/Panel");
            if (exitPanel != null) K.SurfaceBox(exitPanel, "HazardPlate", new Vector3(0f, 0.2f, 0f), new Vector3(1.9f, 0.36f, 0.275f), K.Mats.Hazard, false);
            RedCageLamp(s, new Vector3(2.8f, 3.5f, 22.7f), Vector3.back, 0f, false);

            Cobweb(s, new Vector3(-3.6f, 4.05f, 22.82f), Vector3.back, 0.9f, false);
            Cobweb(s, new Vector3(3.6f, 4.05f, 11.18f), Vector3.forward, 0.8f, false);
        }

        private static void Sketch(Transform parent, Vector3 at, Vector3 facing, Vector2 size)
        {
            var q = K.Quad(parent, "Sketches", at, size, facing, K.Mats.Schematics);
            q.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        private static void Gear(Transform parent, Vector3 at, float radius, int teeth, float speed)
        {
            var g = K.Group(parent, "Gear", at).transform;
            var disc = K.Prim(g, "Disc", PrimitiveType.Cylinder, Vector3.zero, new Vector3(radius * 2f, 0.025f, radius * 2f), K.Mats.Brass, false);
            disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var hole = K.Prim(g, "Inner", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.002f), new Vector3(radius * 1.4f, 0.026f, radius * 1.4f), K.Mats.Rust, false);
            hole.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            for (int i = 0; i < 4; i++)
            {
                var spoke = K.Box(g, "Spoke", new Vector3(0f, 0f, 0.004f), new Vector3(radius * 1.45f, 0.05f, 0.03f), K.Mats.Brass, false);
                spoke.transform.localRotation = Quaternion.Euler(0f, 0f, i * 45f);
            }
            K.Prim(g, "Hub", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.02f), new Vector3(radius * 0.4f, 0.04f, radius * 0.4f), K.Mats.Brass, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            for (int i = 0; i < teeth; i++)
            {
                float a = i * 360f / teeth;
                var tooth = K.Box(g, "Tooth", Quaternion.Euler(0f, 0f, a) * new Vector3(0f, radius + 0.035f, 0f), new Vector3(radius * 0.22f, 0.08f, 0.03f), K.Mats.Brass, false);
                tooth.transform.localRotation = Quaternion.Euler(0f, 0f, a);
            }
            g.gameObject.AddComponent<Spin>().Configure(Vector3.forward, speed);
        }

        private static void Bookshelf(Transform parent, Vector3 at)
        {
            var b = K.Group(parent, "Bookshelf", at).transform;
            const float w = 1.8f, h = 2.2f, d = 0.4f;
            K.Box(b, "Back", new Vector3(0f, h * 0.5f, -d * 0.5f + 0.01f), new Vector3(w, h, 0.02f), K.Mats.OldWood, false);
            K.Box(b, "SideL", new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.04f, h, d), K.Mats.OldWood, false);
            K.Box(b, "SideR", new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.04f, h, d), K.Mats.OldWood, false);
            float[] levels = { 0.05f, 0.6f, 1.15f, 1.7f, 2.2f };
            foreach (float y in levels) K.Box(b, "Shelf", new Vector3(0f, y, 0f), new Vector3(w, 0.03f, d), K.Mats.OldWood, false);
            var covers = new[] { K.Mats.Leather, K.Mats.Velvet, K.Mats.DoorPaint, K.Mats.Navy, K.Mats.OldWood, K.Mats.Silhouette };
            var rnd = new System.Random(41);
            for (int l = 0; l < 3; l++)
            {
                float x = -w * 0.5f + 0.05f;
                while (x < w * 0.5f - 0.1f)
                {
                    float bw = 0.03f + (float)rnd.NextDouble() * 0.035f, bh = 0.2f + (float)rnd.NextDouble() * 0.12f;
                    if (rnd.NextDouble() < 0.08) { x += 0.12f; continue; }
                    var book = K.Box(b, "Book", new Vector3(x + bw * 0.5f, levels[l] + 0.015f + bh * 0.5f, 0.02f), new Vector3(bw, bh, 0.24f), covers[rnd.Next(covers.Length)], false);
                    if (rnd.NextDouble() < 0.1) book.transform.localRotation = Quaternion.Euler(0f, 0f, 12f);
                    x += bw + 0.004f;
                }
            }
            for (int i = 0; i < 5; i++)
            {
                float x = -0.7f + i * 0.35f;
                var flask = K.Group(b, "Flask", new Vector3(x, levels[3] + 0.015f, 0.03f)).transform;
                K.Prim(flask, "Bulb", PrimitiveType.Sphere, new Vector3(0f, 0.08f, 0f), Vector3.one * 0.16f, K.Mats.Glass, false);
                K.Prim(flask, "Fill", PrimitiveType.Sphere, new Vector3(0f, 0.065f, 0f), Vector3.one * 0.12f, i % 2 == 0 ? K.Mats.RedLiquid : K.Mats.Liquid, false);
                K.Prim(flask, "Neck", PrimitiveType.Cylinder, new Vector3(0f, 0.2f, 0f), new Vector3(0.04f, 0.06f, 0.04f), K.Mats.Glass, false);
            }
            Lantern(b, new Vector3(0.55f, levels[4] + 0.015f, 0f));
            var col = b.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, h * 0.5f, 0f);
            col.size = new Vector3(w, h, d);
        }

        /// <summary>Oil lantern with a glowing glass (emissive only, so the room's blackout stays dark around it).</summary>
        private static void Lantern(Transform parent, Vector3 at)
        {
            var l = K.Group(parent, "Lantern", at).transform;
            K.Prim(l, "Base", PrimitiveType.Cylinder, new Vector3(0f, 0.03f, 0f), new Vector3(0.14f, 0.03f, 0.14f), K.Mats.Brass, false);
            K.Prim(l, "Glow", PrimitiveType.Cylinder, new Vector3(0f, 0.14f, 0f), new Vector3(0.1f, 0.08f, 0.1f), K.Mats.AmberGlass, false);
            foreach (float a in new[] { 0f, 90f, 180f, 270f })
                K.Box(l, "Wire", Quaternion.Euler(0f, a, 0f) * new Vector3(0.055f, 0.14f, 0f), new Vector3(0.008f, 0.17f, 0.008f), K.Mats.Brass, false);
            K.Prim(l, "Cap", PrimitiveType.Cylinder, new Vector3(0f, 0.25f, 0f), new Vector3(0.12f, 0.025f, 0.12f), K.Mats.Brass, false);
            K.Box(l, "Handle", new Vector3(0f, 0.31f, 0f), new Vector3(0.1f, 0.01f, 0.01f), K.Mats.Brass, false);
        }

        // =============================================================== memory

        private static void Memory(Transform m)
        {
            var s = K.Group(m, "Underground").transform;
            RedCageLamp(s, new Vector3(-4.03f, 2.8f, 13f), Vector3.right, 1.2f, true);
            RedCageLamp(s, new Vector3(4.03f, 2.8f, 21.5f), Vector3.left, 1.2f, false);
            Puddle(s, new Vector3(1.6f, 0f, 14.5f), new Vector2(1.3f, 0.8f), 30f);
            Puddle(s, new Vector3(-1.8f, 0f, 19.2f), new Vector2(1f, 0.7f), -20f);
            Cobweb(s, new Vector3(-3.6f, 4.05f, 22.82f), Vector3.back, 0.9f, false);
            Cobweb(s, new Vector3(3.6f, 4.05f, 11.18f), Vector3.forward, 0.9f, false);
        }

        // ============================================================ corridors

        private static void Corridors(Transform s)
        {
            var c = K.Group(s, "Corridors").transform;
            foreach (float z in new[] { 8.5f, 14.8f, 22.5f, 29.5f }) Portal(c, false, z, 10.8f, 13.2f);
            Portal(c, true, -0.2f, 24.6f, 27f);
            Portal(c, true, 8f, 47f, 49.4f);
            Portal(c, false, 45.5f, 3.6f, 6f);

            RedCageLamp(c, new Vector3(-3f, 2.4f, 24.77f), Vector3.forward, 1f, false);
            RedCageLamp(c, new Vector3(10.97f, 2.4f, 7.9f), Vector3.right, 1f, true);
            RedCageLamp(c, new Vector3(13.03f, 2.4f, 26.5f), Vector3.left, 1f, false);

            EdgeStripe(c, new Vector3(11.1f, 0f, 7.5f), new Vector3(11.1f, 0f, 30.5f));
            EdgeStripe(c, new Vector3(12.9f, 0f, 7.5f), new Vector3(12.9f, 0f, 30.5f));
            EdgeStripe(c, new Vector3(-3.8f, 0f, 24.9f), new Vector3(0.8f, 0f, 24.9f));

            Puddle(c, new Vector3(12f, 0f, 20f), new Vector2(1.2f, 0.8f), 10f);
            Puddle(c, new Vector3(11.8f, 0f, 12f), new Vector2(0.9f, 0.6f), 60f);
            Puddle(c, new Vector3(-2f, 0f, 25.6f), new Vector2(1f, 0.7f), 0f);
            Puddle(c, new Vector3(9.5f, 0f, 47.8f), new Vector2(1.1f, 0.6f), 90f);

            Cobweb(c, new Vector3(12.65f, 2.8f, 7.17f), Vector3.forward, 0.8f, false);
            Cobweb(c, new Vector3(5.6f, 2.8f, 49.23f), Vector3.back, 0.8f, true);
        }

        /// <summary>Concrete portal frame across a corridor (pillars against both walls and a lintel).</summary>
        private static void Portal(Transform parent, bool corridorAlongX, float at, float wallA, float wallB)
        {
            float a = wallA + 0.15f, b = wallB - 0.15f; // inner faces
            if (corridorAlongX)
            {
                K.SurfaceBox(parent, "PortalPillar", new Vector3(at, 1.6f, a + 0.075f), new Vector3(0.3f, 3.2f, 0.15f), K.Mats.Cracked);
                K.SurfaceBox(parent, "PortalPillar", new Vector3(at, 1.6f, b - 0.075f), new Vector3(0.3f, 3.2f, 0.15f), K.Mats.Cracked);
                K.SurfaceBox(parent, "PortalLintel", new Vector3(at, 3.03f, (a + b) * 0.5f), new Vector3(0.3f, 0.34f, b - a), K.Mats.Cracked, false);
            }
            else
            {
                K.SurfaceBox(parent, "PortalPillar", new Vector3(a + 0.075f, 1.6f, at), new Vector3(0.15f, 3.2f, 0.3f), K.Mats.Cracked);
                K.SurfaceBox(parent, "PortalPillar", new Vector3(b - 0.075f, 1.6f, at), new Vector3(0.15f, 3.2f, 0.3f), K.Mats.Cracked);
                K.SurfaceBox(parent, "PortalLintel", new Vector3((a + b) * 0.5f, 3.03f, at), new Vector3(b - a, 0.34f, 0.3f), K.Mats.Cracked, false);
            }
        }

        // ============================================================== helpers

        /// <summary>Wall-mounted red bulb in a wire cage; optionally lights its surroundings red.</summary>
        private static void RedCageLamp(Transform parent, Vector3 at, Vector3 facing, float intensity, bool flicker)
        {
            var t = K.Group(parent, "RedCageLamp", at).transform;
            t.localRotation = Quaternion.LookRotation(facing, Vector3.up);
            K.Box(t, "Plate", new Vector3(0f, 0f, 0.01f), new Vector3(0.16f, 0.22f, 0.02f), K.Mats.BlackMetal, false);
            K.Prim(t, "Bulb", PrimitiveType.Sphere, new Vector3(0f, 0f, 0.09f), new Vector3(0.11f, 0.14f, 0.11f), K.Mats.SignRed, false);
            K.Box(t, "CageV", new Vector3(0f, 0f, 0.09f), new Vector3(0.008f, 0.18f, 0.15f), K.Mats.BlackMetal, false);
            K.Box(t, "CageH", new Vector3(0f, 0f, 0.09f), new Vector3(0.15f, 0.008f, 0.15f), K.Mats.BlackMetal, false);
            K.Box(t, "CageF", new Vector3(0f, 0f, 0.165f), new Vector3(0.15f, 0.18f, 0.008f), K.Mats.BlackMetal, false);
            if (intensity <= 0f) return;
            var l = K.PointLight(t, "Light", new Vector3(0f, 0f, 0.3f), RedLamp, intensity, 5.5f);
            if (flicker) l.gameObject.AddComponent<LightFlicker>().Configure(0.15f, new Vector2(4f, 10f), 0.35f);
        }

        private static void FlightCase(Transform parent, Vector3 at, Vector3 size, float yaw)
        {
            var f = K.Group(parent, "FlightCase", at).transform;
            f.localRotation = Quaternion.Euler(0f, yaw, 0f);
            K.Box(f, "Body", new Vector3(0f, size.y * 0.5f, 0f), size, K.Mats.FlightCase);
            foreach (float x in new[] { -0.5f, 0.5f })
                foreach (float z in new[] { -0.5f, 0.5f })
                    K.Box(f, "Edge", new Vector3(x * size.x, size.y * 0.5f, z * size.z), new Vector3(0.03f, size.y + 0.01f, 0.03f), K.Mats.Steel, false);
            K.Box(f, "RimTop", new Vector3(0f, size.y, 0f), new Vector3(size.x + 0.02f, 0.025f, size.z + 0.02f), K.Mats.Steel, false);
            K.Box(f, "RimMid", new Vector3(0f, size.y * 0.55f, 0f), new Vector3(size.x + 0.015f, 0.02f, size.z + 0.015f), K.Mats.Steel, false);
            foreach (float x in new[] { -0.25f, 0.25f })
                K.Box(f, "Latch", new Vector3(x * size.x, size.y * 0.55f, -size.z * 0.5f - 0.012f), new Vector3(0.06f, 0.05f, 0.015f), K.Mats.Steel, false);
        }

        private static void Puddle(Transform parent, Vector3 at, Vector2 size, float yaw)
        {
            var q = K.Quad(parent, "Puddle", at + Vector3.up * 0.0045f, size, Vector3.up, K.Mats.Puddle, false, Quaternion.Euler(0f, yaw, 0f) * Vector3.forward);
            q.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        private static void EdgeStripe(Transform parent, Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            Vector3 size = Mathf.Abs(d.x) > Mathf.Abs(d.z) ? new Vector3(Mathf.Abs(d.x), 0.004f, 0.1f) : new Vector3(0.1f, 0.004f, Mathf.Abs(d.z));
            var s = K.SurfaceBox(parent, "EdgeStripe", (from + to) * 0.5f + Vector3.up * 0.0025f, size, K.Mats.Hazard, false);
            s.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>Corner cobweb on a wall. The texture's top-left corner is the anchored corner; flip mirrors it.</summary>
        private static void Cobweb(Transform parent, Vector3 center, Vector3 facing, float size, bool flip)
        {
            var q = K.Quad(parent, "Cobweb", center, new Vector2(size, size), facing, K.Mats.Cobweb);
            if (flip) { var sc = q.transform.localScale; sc.x = -sc.x; q.transform.localScale = sc; }
            q.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>Realtime reflection probe captured once at start, so wet floors and marble pick up the red lamps.</summary>
        private static void Probe(Transform parent, Vector3 center, Vector3 size)
        {
            var go = K.Group(parent, "ReflectionProbe", center);
            var p = go.AddComponent<ReflectionProbe>();
            p.mode = ReflectionProbeMode.Realtime;
            p.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            p.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
            p.size = size;
            p.resolution = 128;
            p.boxProjection = true;
            p.intensity = 0.7f;
            p.backgroundColor = Color.black;
            p.clearFlags = ReflectionProbeClearFlags.SolidColor;
        }
    }
}
