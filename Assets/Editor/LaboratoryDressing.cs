using System.Collections.Generic;
using UnityEngine;
using Voss.Atmosphere;
using Voss.Audio;
using Voss.Interaction;
using K = Voss.EditorTools.VossBuildKit;

namespace Voss.EditorTools
{
    /// <summary>
    /// Architectural dressing that makes 02_VossLaboratory read as an underground research facility:
    /// fluorescent fittings that die with their lights, caged corridor bulbs, pipework, vents, signage,
    /// hazard thresholds, grime, a dark observation room behind glass, a wet bench and a specimen shelf.
    /// </summary>
    public static class LaboratoryDressing
    {
        private static readonly Color SignInk = new Color(0.08f, 0.08f, 0.07f);
        private static readonly Color Red = new Color(0.55f, 0.06f, 0.05f);

        public static void Build(Transform root, Transform[] rooms)
        {
            var d = K.Group(root, "Dressing").transform;
            var hall = root.Find("Entrance");

            // ------------------------------------------------ light fittings
            const float hallCeiling = 5f, roomCeiling = 4.5f, corridorCeiling = 3.2f;
            // Fluorescent tubes hang on rods below the concrete beams.
            const float drop = 0.6f;
            Troffer(d, "HallLights/HallLight_W", root, new Vector3(-8f, hallCeiling, 0f), drop);
            Troffer(d, "HallLights/HallLight_E", root, new Vector3(8f, hallCeiling, 0f), drop);
            Troffer(d, "HallLights/HallLight_S", root, new Vector3(0f, hallCeiling, -4f), drop);
            Troffer(d, "HallLights/HallLight_N", root, new Vector3(0f, hallCeiling, 4f), drop);
            foreach (var p in new[] { new Vector3(-8f, hallCeiling - drop, -4f), new Vector3(8f, hallCeiling - drop, 4f), new Vector3(8f, hallCeiling - drop, -4f), new Vector3(-8f, hallCeiling - drop, 4f) })
            {
                var dead = DeadTroffer(d, p, 0f);
                Rods(dead, drop);
            }
            // One fitting has come loose and hangs by a corner, buzzing.
            var hanging = DeadTroffer(d, new Vector3(-10f, 4.2f, -1.5f), 28f);
            var buzz = K.PointLight(hanging, "BrokenTubeLight", new Vector3(0f, -0.15f, 0f), new Color(0.7f, 0.8f, 1f), 0.6f, 4f);
            buzz.gameObject.AddComponent<LightFlicker>().Configure(0.3f, new Vector2(0.8f, 2.5f), 0.6f);
            hanging.Find("Tube").gameObject.AddComponent<LampSync>().Configure(buzz, new[] { hanging.Find("Tube").GetComponent<Renderer>() }, K.Mats.Tube, K.Mats.TubeOff);
            K.Box(hanging, "Chain", new Vector3(0.6f, 0.25f, 0f), new Vector3(0.015f, 0.5f, 0.015f), K.Mats.Steel, false);
            hanging.gameObject.AddComponent<LoopingSound>().Configure(SfxId.Static, 0.15f, 4f);

            // PERCEPTION gets a deco chandelier, REASON dome pendants, MEMORY bare fluorescents.
            DecoChandelier(rooms[0], rooms[0].Find("PerceptionLight_A")?.GetComponent<Light>(), new Vector3(0f, roomCeiling, 17f));
            foreach (var name in new[] { "ReasonLight_A", "ReasonLight_B" })
            {
                var l = rooms[1].Find(name);
                if (l != null) DomePendant(rooms[1], l.GetComponent<Light>(), rooms[1].InverseTransformPoint(l.position) + Vector3.up * 0.15f, roomCeiling);
            }
            foreach (var name in new[] { "MemoryLight_A", "MemoryLight_B" })
            {
                var l = rooms[2].Find(name);
                if (l == null) continue;
                var at = rooms[2].InverseTransformPoint(l.position);
                Troffer(rooms[2], name, rooms[2], new Vector3(at.x, roomCeiling, at.z), 0f);
            }
            for (int i = 0; i < 3; i++)
            {
                var corridorLight = rooms[i].Find("CorridorLight");
                if (corridorLight != null) CagedBulb(rooms[i], corridorLight.GetComponent<Light>(), new Vector3(0f, corridorCeiling, 9f));
            }
            // Interrogation lamp over the REASON scale.
            InterrogationLamp(rooms[1], rooms[1].Find("ScaleLight")?.GetComponent<Light>(), new Vector3(0f, 3.4f, 17f), roomCeiling);

            foreach (var group in new[] { "Lights_ToReason", "Lights_ToMemory", "Lights_WayBack" })
            {
                var g = root.Find(group);
                if (g == null) continue;
                foreach (Transform lamp in g)
                {
                    var l = lamp.GetComponent<Light>();
                    if (l != null) CagedBulb(d, l, new Vector3(lamp.position.x, corridorCeiling, lamp.position.z));
                }
            }

            // ------------------------------------------------------- pipework
            Pipe(d, new Vector3(-16f, 4.55f, 6.4f), new Vector3(16f, 4.55f, 6.4f), 0.07f, K.Mats.Rust, Vector3.forward);
            Pipe(d, new Vector3(-16f, 4.75f, 6.42f), new Vector3(16f, 4.75f, 6.42f), 0.045f, K.Mats.Steel, Vector3.forward);
            Pipe(d, new Vector3(-16f, 4.6f, -6.4f), new Vector3(16f, 4.6f, -6.4f), 0.06f, K.Mats.Rust, Vector3.back);
            Pipe(d, new Vector3(-10.5f, 0f, 6.42f), new Vector3(-10.5f, 4.55f, 6.42f), 0.05f, K.Mats.Rust, Vector3.forward);
            Valve(d, new Vector3(-10.5f, 1.45f, 6.3f));
            Gauge(d, new Vector3(-10.5f, 1.95f, 6.36f));
            Pipe(d, new Vector3(-4f, 2.95f, 26.72f), new Vector3(0.8f, 2.95f, 26.72f), 0.06f, K.Mats.Rust, Vector3.forward);
            Pipe(d, new Vector3(6.8f, 2.95f, 49.12f), new Vector3(13.2f, 2.95f, 49.12f), 0.06f, K.Mats.Rust, Vector3.forward);
            Pipe(d, new Vector3(12.92f, 2.95f, 7f), new Vector3(12.92f, 2.95f, 31f), 0.06f, K.Mats.Rust, Vector3.right);
            Pipe(d, new Vector3(12.95f, 2.75f, 7f), new Vector3(12.95f, 2.75f, 31f), 0.035f, K.Mats.Steel, Vector3.right);

            // --------------------------------------------------------- vents
            Vent(d, new Vector3(-13f, 4.1f, -6.83f), Vector3.forward);
            Vent(d, new Vector3(3f, 4.1f, 6.83f), Vector3.back);
            Vent(rooms[2], new Vector3(-4.03f, 3.9f, 22f), Vector3.right);

            // -------------------------------------------------------- signage
            Sign(hall, "Sign_Silence", new Vector3(6.8f, 1.7f, -6.83f), Vector3.forward, "SUBJECTS MUST\nREMAIN SILENT", new Vector2(0.9f, 0.45f), K.Mats.WhitePanel, Red);
            Sign(hall, "Sign_Chamber01", new Vector3(-8f, 4.18f, 6.83f), Vector3.back, "TEST CHAMBER 01", new Vector2(1.4f, 0.22f), K.Mats.WhitePanel, SignInk);
            Sign(hall, "Sign_Authorized", new Vector3(12f, 4.0f, 6.83f), Vector3.back, "AUTHORIZED PERSONNEL ONLY", new Vector2(1.7f, 0.25f), K.Mats.Hazard, SignInk);
            Sign(hall, "Sign_Level", new Vector3(0f, 3.35f, -6.83f), Vector3.forward, "B3", new Vector2(0.45f, 0.24f), K.Mats.Silhouette, new Color(1f, 0.15f, 0.1f));
            Sign(rooms[0], "Sign_Chamber", new Vector3(0f, 3.65f, 11.17f), Vector3.forward, "CHAMBER 01 - PERCEPTION", new Vector2(2.2f, 0.25f), K.Mats.Gold, SignInk);
            Sign(rooms[1], "Sign_Chamber", new Vector3(0f, 3.65f, 11.17f), Vector3.forward, "CHAMBER 02 - REASON", new Vector2(2.0f, 0.25f), K.Mats.WhitePanel, SignInk);
            Sign(rooms[1], "Sign_Recorded", new Vector3(4.03f, 2.4f, 19f), Vector3.left, "ALL ANSWERS\nARE RECORDED", new Vector2(0.9f, 0.4f), K.Mats.WhitePanel, Red);
            Sign(rooms[2], "Sign_Chamber", new Vector3(0f, 3.65f, 11.17f), Vector3.forward, "CHAMBER 03 - MEMORY", new Vector2(2.0f, 0.25f), K.Mats.WhitePanel, SignInk);
            K.Text3D(rooms[2], "Scrawl", "YOU HAVE BEEN\nHERE BEFORE", new Vector3(-4.02f, 2.45f, 17.6f), Vector3.right, 0.13f, new Color(0.42f, 0.04f, 0.03f), TextAnchor.MiddleCenter, FontStyle.Italic);
            Sign(d, "Sign_ToChamber02", new Vector3(-5.2f, 2.2f, 26.83f), Vector3.back, "CHAMBER 02  >", new Vector2(1.1f, 0.25f), K.Mats.WhitePanel, SignInk);

            // ------------------------------------------------ hazard thresholds
            Threshold(hall, new Vector3(-8f, 0f, 6.45f), new Vector2(2.2f, 0.5f));
            Threshold(hall, new Vector3(12f, 0f, 6.45f), new Vector2(2.2f, 0.5f));
            Threshold(hall, new Vector3(0f, 0f, -6.45f), new Vector2(3.2f, 0.5f));
            Threshold(hall, new Vector3(-15.55f, 0f, -3f), new Vector2(0.5f, 2.2f));
            Threshold(d, new Vector3(2f, 0f, 26.6f), new Vector2(2.2f, 0.5f));
            Threshold(d, new Vector3(12f, 0f, 47.4f), new Vector2(2.2f, 0.5f));
            Threshold(rooms[1], new Vector3(2.8f, 0f, 22.55f), new Vector2(2.2f, 0.5f));
            Threshold(rooms[2], new Vector3(0f, 0f, 22.55f), new Vector2(2.2f, 0.5f));

            // ---------------------------------------------------------- grime
            var rnd = new System.Random(731);
            Stains(hall, rnd, 16, new Vector2(-15f, 15f), new Vector2(-6f, 6f));
            for (int i = 1; i < 3; i++) Stains(rooms[i], rnd, 4, new Vector2(-3.8f, 3.8f), new Vector2(11.5f, 22.5f));
            Stains(d, rnd, 3, new Vector2(-3.8f, 3f), new Vector2(24.8f, 26.8f));
            Stains(d, rnd, 3, new Vector2(6.2f, 13f), new Vector2(47.2f, 49.2f));
            Stains(d, rnd, 6, new Vector2(11f, 13f), new Vector2(7.5f, 30.5f));
            Drain(hall, new Vector3(-3f, 0f, 1.5f));
            Drain(rooms[1], new Vector3(-1.5f, 0f, 21.5f));

            // ------------------------------------------------- furnished areas
            BuildControlRoom(hall);
            BuildWetBench(hall);
            BuildSpecimenShelf(hall);
            BuildIvStand(hall, new Vector3(7.0f, 0f, -3.35f));

            LaboratoryStyle.Build(root, rooms);
        }

        // ================================================================ lights

        /// <summary>Recessed fluorescent fitting whose diffuser follows the named light.</summary>
        private static void Troffer(Transform parent, string lightPath, Transform lightRoot, Vector3 ceilingPoint, float drop)
        {
            var light = lightRoot.Find(lightPath)?.GetComponent<Light>();
            var t = K.Group(parent, "Troffer", ceilingPoint - Vector3.up * drop).transform;
            if (drop > 0f) Rods(t, drop);
            K.Box(t, "Housing", new Vector3(0f, -0.035f, 0f), new Vector3(1.3f, 0.07f, 0.42f), K.Mats.Steel, false);
            var tube = K.Box(t, "Tube", new Vector3(0f, -0.075f, 0f), new Vector3(1.18f, 0.02f, 0.3f), K.Mats.Tube, false);
            if (light != null) t.gameObject.AddComponent<LampSync>().Configure(light, new[] { tube.GetComponent<Renderer>() }, K.Mats.Tube, K.Mats.TubeOff);
        }

        private static void Rods(Transform fitting, float drop)
        {
            foreach (float x in new[] { -0.5f, 0.5f })
                K.Box(fitting, "Rod", new Vector3(x, drop * 0.5f, 0f), new Vector3(0.012f, drop, 0.012f), K.Mats.Steel, false);
        }

        /// <summary>Stepped art-deco lantern of gold and amber glass hanging from a rod; glows with its light.</summary>
        private static void DecoChandelier(Transform parent, Light light, Vector3 ceilingPoint)
        {
            var t = K.Group(parent, "DecoChandelier", ceilingPoint).transform;
            K.Box(t, "Rod", new Vector3(0f, -0.25f, 0f), new Vector3(0.025f, 0.5f, 0.025f), K.Mats.Gold, false);
            var glow = new List<Renderer>();
            float y = -0.55f;
            float[] radii = { 0.75f, 0.6f, 0.42f, 0.22f };
            for (int i = 0; i < radii.Length; i++)
            {
                float r = radii[i];
                K.Prim(t, "Ring_" + i, PrimitiveType.Cylinder, new Vector3(0f, y, 0f), new Vector3(r * 2f + 0.06f, 0.02f, r * 2f + 0.06f), K.Mats.Gold, false);
                glow.Add(K.Prim(t, "Glass_" + i, PrimitiveType.Cylinder, new Vector3(0f, y - 0.08f, 0f), new Vector3(r * 2f, 0.07f, r * 2f), K.Mats.AmberGlass, false).GetComponent<Renderer>());
                for (int k = 0; k < 8; k++)
                {
                    float a = k * Mathf.PI / 4f;
                    K.Box(t, "Rib", new Vector3(Mathf.Cos(a) * r, y - 0.08f, Mathf.Sin(a) * r), new Vector3(0.02f, 0.16f, 0.02f), K.Mats.Gold, false);
                }
                y -= 0.16f;
            }
            K.Prim(t, "Finial", PrimitiveType.Sphere, new Vector3(0f, y, 0f), Vector3.one * 0.08f, K.Mats.Gold, false);
            if (light != null) t.gameObject.AddComponent<LampSync>().Configure(light, glow.ToArray(), K.Mats.AmberGlass, K.Mats.TubeOff);
        }

        /// <summary>Enamelled dome pendant with a warm bulb (the study's lamps).</summary>
        private static void DomePendant(Transform parent, Light light, Vector3 at, float ceiling)
        {
            var t = K.Group(parent, "DomePendant", at).transform;
            K.Box(t, "Cord", new Vector3(0f, (ceiling - at.y) * 0.5f, 0f), new Vector3(0.012f, ceiling - at.y, 0.012f), K.Mats.Silhouette, false);
            K.Prim(t, "Dome", PrimitiveType.Sphere, new Vector3(0f, 0.05f, 0f), new Vector3(0.55f, 0.3f, 0.55f), K.Mats.BlackMetal, false);
            K.Prim(t, "Rim", PrimitiveType.Cylinder, new Vector3(0f, -0.03f, 0f), new Vector3(0.56f, 0.01f, 0.56f), K.Mats.Brass, false);
            var bulb = K.Prim(t, "Bulb", PrimitiveType.Sphere, new Vector3(0f, -0.06f, 0f), new Vector3(0.12f, 0.12f, 0.12f), K.Mats.WarmBulb, false);
            if (light != null) t.gameObject.AddComponent<LampSync>().Configure(light, new[] { bulb.GetComponent<Renderer>() }, K.Mats.WarmBulb, K.Mats.TubeOff);
        }

        private static Transform DeadTroffer(Transform parent, Vector3 point, float tilt)
        {
            var t = K.Group(parent, "DeadTroffer", point).transform;
            t.localRotation = Quaternion.Euler(0f, 0f, tilt);
            K.Box(t, "Housing", new Vector3(0f, -0.035f, 0f), new Vector3(1.3f, 0.07f, 0.42f), K.Mats.Steel, false);
            K.Box(t, "Tube", new Vector3(0f, -0.075f, 0f), new Vector3(1.18f, 0.02f, 0.3f), K.Mats.TubeOff, false);
            return t;
        }

        private static void CagedBulb(Transform parent, Light light, Vector3 ceilingPoint)
        {
            var t = K.Group(parent, "CagedBulb", ceilingPoint).transform;
            K.Prim(t, "Base", PrimitiveType.Cylinder, new Vector3(0f, -0.02f, 0f), new Vector3(0.16f, 0.02f, 0.16f), K.Mats.Steel, false);
            var bulb = K.Prim(t, "Bulb", PrimitiveType.Sphere, new Vector3(0f, -0.13f, 0f), new Vector3(0.1f, 0.13f, 0.1f), K.Mats.Tube, false);
            K.Box(t, "CageA", new Vector3(0f, -0.13f, 0f), new Vector3(0.15f, 0.18f, 0.008f), K.Mats.Steel, false);
            K.Box(t, "CageB", new Vector3(0f, -0.13f, 0f), new Vector3(0.008f, 0.18f, 0.15f), K.Mats.Steel, false);
            K.Prim(t, "CageRing", PrimitiveType.Cylinder, new Vector3(0f, -0.2f, 0f), new Vector3(0.15f, 0.005f, 0.15f), K.Mats.Steel, false);
            if (light != null) t.gameObject.AddComponent<LampSync>().Configure(light, new[] { bulb.GetComponent<Renderer>() }, K.Mats.Tube, K.Mats.TubeOff);
        }

        private static void InterrogationLamp(Transform parent, Light light, Vector3 at, float ceiling)
        {
            var t = K.Group(parent, "InterrogationLamp", at).transform;
            K.Box(t, "Cord", new Vector3(0f, (ceiling - at.y) * 0.5f, 0f), new Vector3(0.012f, ceiling - at.y, 0.012f), K.Mats.Silhouette, false);
            K.Prim(t, "Shade", PrimitiveType.Sphere, new Vector3(0f, 0.08f, 0f), new Vector3(0.6f, 0.34f, 0.6f), K.Mats.BlackMetal, false);
            K.Prim(t, "Rim", PrimitiveType.Cylinder, new Vector3(0f, -0.01f, 0f), new Vector3(0.61f, 0.01f, 0.61f), K.Mats.Brass, false);
            var bulb = K.Prim(t, "Bulb", PrimitiveType.Sphere, new Vector3(0f, -0.04f, 0f), new Vector3(0.13f, 0.13f, 0.13f), K.Mats.WarmBulb, false);
            if (light != null) t.gameObject.AddComponent<LampSync>().Configure(light, new[] { bulb.GetComponent<Renderer>() }, K.Mats.WarmBulb, K.Mats.TubeOff);
        }

        // ============================================================== pipes

        /// <summary>Straight pipe from a to b with wall brackets every two metres (bracketDir points into the wall).</summary>
        private static void Pipe(Transform parent, Vector3 a, Vector3 b, float radius, Material mat, Vector3 bracketDir)
        {
            Vector3 dir = b - a;
            var p = K.Prim(parent, "Pipe", PrimitiveType.Cylinder, (a + b) * 0.5f, new Vector3(radius * 2f, dir.magnitude * 0.5f, radius * 2f), mat, false);
            p.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            int brackets = Mathf.Max(1, Mathf.FloorToInt(dir.magnitude / 2f));
            for (int i = 0; i <= brackets; i++)
            {
                Vector3 at = Vector3.Lerp(a, b, i / (float)brackets);
                K.Box(parent, "PipeBracket", at + bracketDir * (radius + 0.04f), new Vector3(0.04f, 0.04f, 0.04f) + Abs(bracketDir) * 0.08f, K.Mats.BlackMetal, false);
                K.Prim(parent, "PipeCollar", PrimitiveType.Cylinder, at, new Vector3(radius * 2.4f, 0.02f, radius * 2.4f), K.Mats.BlackMetal, false)
                    .transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            }
        }

        private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        private static void Valve(Transform parent, Vector3 at)
        {
            var w = K.Prim(parent, "ValveWheel", PrimitiveType.Cylinder, at, new Vector3(0.26f, 0.015f, 0.26f), K.Mats.RedString, false);
            w.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            K.Box(parent, "ValveSpokeA", at + Vector3.back * 0.02f, new Vector3(0.24f, 0.02f, 0.02f), K.Mats.RedString, false);
            K.Box(parent, "ValveSpokeB", at + Vector3.back * 0.02f, new Vector3(0.02f, 0.24f, 0.02f), K.Mats.RedString, false);
        }

        private static void Gauge(Transform parent, Vector3 at)
        {
            var g = K.Prim(parent, "Gauge", PrimitiveType.Cylinder, at, new Vector3(0.14f, 0.03f, 0.14f), K.Mats.Steel, false);
            g.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var face = K.Prim(parent, "GaugeFace", PrimitiveType.Cylinder, at + Vector3.back * 0.032f, new Vector3(0.12f, 0.002f, 0.12f), K.Mats.Paper, false);
            face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var needle = K.Box(parent, "GaugeNeedle", at + new Vector3(0.02f, 0.02f, -0.036f), new Vector3(0.05f, 0.005f, 0.002f), K.Mats.RedString, false);
            needle.transform.localRotation = Quaternion.Euler(0f, 0f, 40f); // well into the red
        }

        private static void Vent(Transform parent, Vector3 at, Vector3 facing)
        {
            var v = K.Group(parent, "Vent", at).transform;
            v.localRotation = Quaternion.LookRotation(facing, Vector3.up);
            K.Box(v, "Frame", Vector3.zero, new Vector3(0.7f, 0.35f, 0.03f), K.Mats.Steel, false);
            K.Box(v, "Dark", new Vector3(0f, 0f, 0.012f), new Vector3(0.62f, 0.27f, 0.01f), K.Mats.Silhouette, false);
            for (int i = 0; i < 5; i++)
            {
                var slat = K.Box(v, "Slat", new Vector3(0f, -0.11f + i * 0.055f, 0.02f), new Vector3(0.62f, 0.012f, 0.03f), K.Mats.Steel, false);
                slat.transform.localRotation = Quaternion.Euler(35f, 0f, 0f);
            }
        }

        // ============================================================ signage

        private static void Sign(Transform parent, string name, Vector3 at, Vector3 facing, string text, Vector2 size, Material plate, Color ink)
        {
            var s = K.Group(parent, name, at).transform;
            s.localRotation = Quaternion.LookRotation(facing, Vector3.up);
            K.Box(s, "Plate", Vector3.zero, new Vector3(size.x, size.y, 0.015f), plate, false);
            int lines = text.Split('\n').Length;
            K.Text3D(s, "Text", text, new Vector3(0f, 0f, 0.012f), Vector3.forward, size.y * 0.62f / lines, ink, TextAnchor.MiddleCenter, FontStyle.Bold, 48);
        }

        private static void Threshold(Transform parent, Vector3 at, Vector2 size)
        {
            K.SurfaceBox(parent, "HazardStripe", at + Vector3.up * 0.002f, new Vector3(size.x, 0.004f, size.y), K.Mats.Hazard, false)
                .GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static void Stains(Transform parent, System.Random rnd, int count, Vector2 xRange, Vector2 zRange)
        {
            for (int i = 0; i < count; i++)
            {
                float x = Mathf.Lerp(xRange.x, xRange.y, (float)rnd.NextDouble());
                float z = Mathf.Lerp(zRange.x, zRange.y, (float)rnd.NextDouble());
                float size = 0.6f + (float)rnd.NextDouble() * 1.4f;
                float yaw = (float)rnd.NextDouble() * 360f;
                var q = K.Quad(parent, "Stain", new Vector3(x, 0.006f + i * 0.0003f, z), new Vector2(size, size * (0.6f + (float)rnd.NextDouble() * 0.5f)),
                    Vector3.up, K.Mats.Stain, false, Quaternion.Euler(0f, yaw, 0f) * Vector3.forward);
                q.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private static void Drain(Transform parent, Vector3 at)
        {
            K.Box(parent, "Drain", at + Vector3.up * 0.003f, new Vector3(0.4f, 0.006f, 0.4f), K.Mats.Silhouette, false);
            for (int i = 0; i < 6; i++)
                K.Box(parent, "DrainBar", at + new Vector3(-0.17f + i * 0.068f, 0.007f, 0f), new Vector3(0.02f, 0.006f, 0.4f), K.Mats.Rust, false);
        }

        // ===================================================== furnished areas

        /// <summary>Dark room behind the observation glass: monitors still on, a chair turned to face the hall.</summary>
        private static void BuildControlRoom(Transform hall)
        {
            var c = K.Group(hall, "ControlRoom").transform;
            K.FloorAndCeiling(c, "ControlRoom", new Vector2(16f, -1.5f), new Vector2(20f, 3.5f), 3f, K.Mats.ConcreteFloor, K.Mats.ConcreteCeiling);
            K.Wall(c, "CR_E", false, 20f, -1.5f, 3.5f, 3f, K.Mats.Concrete);
            K.Wall(c, "CR_N", true, 3.5f, 16f, 20f, 3f, K.Mats.Concrete);
            K.Wall(c, "CR_S", true, -1.5f, 16f, 20f, 3f, K.Mats.Concrete);

            K.Box(c, "Console", new Vector3(19.45f, 0.4f, 1f), new Vector3(0.7f, 0.8f, 3.2f), K.Mats.DoorPaint);
            K.Box(c, "ConsoleTop", new Vector3(19.4f, 0.82f, 1f), new Vector3(0.8f, 0.04f, 3.3f), K.Mats.Steel, false);
            for (int i = 0; i < 3; i++)
            {
                float z = -0.1f + i * 1.1f;
                K.Box(c, "Monitor_" + i, new Vector3(19.55f, 1.1f, z), new Vector3(0.45f, 0.42f, 0.5f), K.Mats.BlackMetal, false);
                K.Box(c, "MonitorScreen_" + i, new Vector3(19.32f, 1.1f, z), new Vector3(0.02f, 0.32f, 0.4f), K.Mats.Crt, false);
                K.Text3D(c, "MonitorText_" + i, "CAM 0" + (i + 1) + "\nREC", new Vector3(19.305f, 1.1f, z), Vector3.left, 0.05f, new Color(0.08f, 0.15f, 0.1f), TextAnchor.MiddleCenter, FontStyle.Bold, 48);
            }
            for (int i = 0; i < 8; i++) K.Box(c, "Switch_" + i, new Vector3(19.1f, 0.85f, 0f + i * 0.25f), new Vector3(0.06f, 0.03f, 0.04f), i % 3 == 0 ? K.Mats.SignRed : K.Mats.Steel, false);
            K.Prim(c, "MicStand", PrimitiveType.Cylinder, new Vector3(19.1f, 1.0f, 2.2f), new Vector3(0.015f, 0.17f, 0.015f), K.Mats.Steel, false);
            K.Prim(c, "Mic", PrimitiveType.Sphere, new Vector3(19.05f, 1.2f, 2.2f), new Vector3(0.06f, 0.08f, 0.06f), K.Mats.BlackMetal, false);
            K.Prim(c, "Mug", PrimitiveType.Cylinder, new Vector3(19.15f, 0.89f, -0.6f), new Vector3(0.08f, 0.05f, 0.08f), K.Mats.Porcelain, false);

            // The chair faces the window: whoever sat here watched the hall.
            var chair = K.Group(c, "WatcherChair", new Vector3(18.1f, 0f, 1f)).transform;
            chair.localRotation = Quaternion.Euler(0f, -90f, 0f);
            K.Prim(chair, "Base", PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.6f, 0.02f, 0.6f), K.Mats.BlackMetal, false);
            K.Prim(chair, "Post", PrimitiveType.Cylinder, new Vector3(0f, 0.25f, 0f), new Vector3(0.06f, 0.2f, 0.06f), K.Mats.Steel, false);
            K.Box(chair, "Seat", new Vector3(0f, 0.48f, 0f), new Vector3(0.5f, 0.08f, 0.5f), K.Mats.Leather, false);
            K.Box(chair, "Back", new Vector3(0f, 0.85f, -0.24f), new Vector3(0.48f, 0.65f, 0.07f), K.Mats.Leather, false);

            var glow = K.PointLight(c, "MonitorGlow", new Vector3(18.9f, 1.2f, 1f), new Color(0.5f, 0.75f, 0.62f), 0.7f, 3.5f);
            glow.gameObject.AddComponent<LightFlicker>().Configure(0.2f, new Vector2(2f, 6f), 0.15f);
            K.PointLight(c, "RedLamp", new Vector3(17.5f, 2.7f, 3f), new Color(1f, 0.15f, 0.1f), 0.35f, 3f);
            c.gameObject.AddComponent<LoopingSound>().Configure(SfxId.Static, 0.2f, 6f);
        }

        /// <summary>Stainless wet bench on the south wall: sink, microscope, beakers and a rack of test tubes.</summary>
        private static void BuildWetBench(Transform hall)
        {
            var b = K.Group(hall, "WetBench", new Vector3(11f, 0f, -6.3f)).transform;
            K.Box(b, "Cabinets", new Vector3(0f, 0.42f, 0f), new Vector3(4f, 0.84f, 0.65f), K.Mats.DoorPaint);
            for (int i = 0; i < 5; i++)
                K.Box(b, "CabinetSeam", new Vector3(-1.6f + i * 0.8f, 0.42f, 0.33f), new Vector3(0.01f, 0.78f, 0.01f), K.Mats.Silhouette, false);
            K.Box(b, "Top", new Vector3(0f, 0.865f, 0f), new Vector3(4.1f, 0.05f, 0.7f), K.Mats.Steel, false);
            K.Box(b, "Splashback", new Vector3(0f, 1.15f, -0.33f), new Vector3(4.1f, 0.5f, 0.03f), K.Mats.Steel, false);
            // sink
            K.Box(b, "Sink", new Vector3(1.4f, 0.885f, 0.02f), new Vector3(0.6f, 0.012f, 0.42f), K.Mats.Silhouette, false);
            K.Prim(b, "Tap", PrimitiveType.Cylinder, new Vector3(1.4f, 1.05f, -0.22f), new Vector3(0.03f, 0.16f, 0.03f), K.Mats.Steel, false);
            K.Box(b, "Spout", new Vector3(1.4f, 1.2f, -0.12f), new Vector3(0.03f, 0.03f, 0.22f), K.Mats.Steel, false);
            // microscope
            var m = K.Group(b, "Microscope", new Vector3(-1.2f, 0.89f, 0f)).transform;
            K.Box(m, "Foot", new Vector3(0f, 0.02f, 0f), new Vector3(0.2f, 0.04f, 0.26f), K.Mats.BlackMetal, false);
            K.Box(m, "Arm", new Vector3(0f, 0.18f, -0.08f), new Vector3(0.05f, 0.32f, 0.05f), K.Mats.BlackMetal, false);
            K.Box(m, "Stage", new Vector3(0f, 0.12f, 0.03f), new Vector3(0.14f, 0.015f, 0.14f), K.Mats.BlackMetal, false);
            var tube = K.Prim(m, "Tube", PrimitiveType.Cylinder, new Vector3(0f, 0.28f, 0.0f), new Vector3(0.05f, 0.12f, 0.05f), K.Mats.Porcelain, false);
            tube.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
            // glassware
            var rnd = new System.Random(3);
            for (int i = 0; i < 5; i++)
            {
                float x = -0.4f + i * 0.22f, r = 0.07f + (float)rnd.NextDouble() * 0.05f, hgt = 0.1f + (float)rnd.NextDouble() * 0.12f;
                K.Prim(b, "Beaker", PrimitiveType.Cylinder, new Vector3(x, 0.89f + hgt * 0.5f, 0.05f), new Vector3(r, hgt * 0.5f, r), K.Mats.Glass, false);
                K.Prim(b, "Contents", PrimitiveType.Cylinder, new Vector3(x, 0.89f + hgt * 0.25f, 0.05f), new Vector3(r * 0.9f, hgt * 0.25f, r * 0.9f), K.Mats.Liquid, false);
            }
            K.Box(b, "TubeRack", new Vector3(0.75f, 0.94f, 0.1f), new Vector3(0.36f, 0.08f, 0.12f), K.Mats.Wood, false);
            for (int i = 0; i < 6; i++)
                K.Prim(b, "TestTube", PrimitiveType.Cylinder, new Vector3(0.6f + i * 0.06f, 1.0f, 0.1f), new Vector3(0.025f, 0.08f, 0.025f), i % 2 == 0 ? K.Mats.Liquid : K.Mats.Glass, false);
            var notes = K.Box(b, "Clipboard", new Vector3(-1.75f, 0.9f, 0.08f), new Vector3(0.24f, 0.01f, 0.32f), K.Mats.Wood, false);
            notes.transform.localRotation = Quaternion.Euler(0f, 12f, 0f);
        }

        /// <summary>Steel shelving on the north wall with specimen jars. One of them is looking back.</summary>
        private static void BuildSpecimenShelf(Transform hall)
        {
            var s = K.Group(hall, "SpecimenShelf", new Vector3(8f, 0f, 6.45f)).transform;
            foreach (float x in new[] { -1.65f, 1.65f })
                foreach (float z in new[] { -0.18f, 0.18f })
                    K.Box(s, "Post", new Vector3(x, 1.0f, z), new Vector3(0.04f, 2.0f, 0.04f), K.Mats.Steel, false);
            float[] levels = { 0.35f, 0.95f, 1.55f };
            foreach (float y in levels) K.Box(s, "Shelf", new Vector3(0f, y, 0f), new Vector3(3.35f, 0.03f, 0.42f), K.Mats.Steel);
            var rnd = new System.Random(77);
            int index = 0;
            foreach (float y in levels)
            {
                for (float x = -1.4f; x <= 1.4f; x += 0.42f)
                {
                    if (rnd.NextDouble() < 0.2) continue;
                    var jar = K.Group(s, "Jar_" + index, new Vector3(x, y + 0.015f, 0f)).transform;
                    float r = 0.15f + (float)rnd.NextDouble() * 0.06f, h = 0.22f + (float)rnd.NextDouble() * 0.12f;
                    K.Prim(jar, "Glass", PrimitiveType.Cylinder, new Vector3(0f, h * 0.5f, 0f), new Vector3(r, h * 0.5f, r), K.Mats.Glass, false);
                    K.Prim(jar, "Lid", PrimitiveType.Cylinder, new Vector3(0f, h + 0.01f, 0f), new Vector3(r * 1.02f, 0.012f, r * 1.02f), K.Mats.Rust, false);
                    K.Prim(jar, "Fluid", PrimitiveType.Cylinder, new Vector3(0f, h * 0.42f, 0f), new Vector3(r * 0.92f, h * 0.42f, r * 0.92f), K.Mats.Liquid, false);
                    bool eye = index % 3 == 1;
                    if (eye)
                    {
                        K.Prim(jar, "Eye", PrimitiveType.Sphere, new Vector3(0f, h * 0.45f, 0f), Vector3.one * 0.07f, K.Mats.Porcelain, false);
                        K.Prim(jar, "Iris", PrimitiveType.Sphere, new Vector3(0f, h * 0.45f, -0.03f), new Vector3(0.035f, 0.035f, 0.015f), K.Mats.Silhouette, false);
                    }
                    else K.Prim(jar, "Specimen", PrimitiveType.Sphere, new Vector3(0f, h * 0.35f, 0f), new Vector3(r * 0.5f, h * 0.35f, r * 0.45f), K.Mats.Leather, false);
                    K.Box(jar, "Label", new Vector3(0f, h * 0.55f, -r * 0.5f - 0.002f), new Vector3(r * 0.6f, h * 0.25f, 0.002f), K.Mats.OldPaper, false);
                    if (index == 4)
                    {
                        var col = jar.gameObject.AddComponent<BoxCollider>();
                        col.center = new Vector3(0f, h * 0.5f, 0f);
                        col.size = new Vector3(r, h, r);
                        var insp = jar.gameObject.AddComponent<InspectableObject>();
                        insp.Prompt = "Inspect jar";
                        insp.Note = "The label reads: OBSERVER. It has not stopped looking.";
                    }
                    index++;
                }
            }
        }

        private static void BuildIvStand(Transform hall, Vector3 at)
        {
            var t = K.Group(hall, "IVStand", at).transform;
            K.Box(t, "FootA", new Vector3(0f, 0.02f, 0f), new Vector3(0.5f, 0.03f, 0.04f), K.Mats.Steel, false);
            K.Box(t, "FootB", new Vector3(0f, 0.02f, 0f), new Vector3(0.04f, 0.03f, 0.5f), K.Mats.Steel, false);
            K.Prim(t, "Pole", PrimitiveType.Cylinder, new Vector3(0f, 0.95f, 0f), new Vector3(0.025f, 0.95f, 0.025f), K.Mats.Steel, false);
            K.Box(t, "Hook", new Vector3(0f, 1.88f, 0f), new Vector3(0.3f, 0.015f, 0.015f), K.Mats.Steel, false);
            K.Box(t, "Bag", new Vector3(0.12f, 1.7f, 0f), new Vector3(0.12f, 0.22f, 0.04f), K.Mats.Liquid, false);
            K.Box(t, "Line", new Vector3(0.12f, 1.2f, 0f), new Vector3(0.006f, 0.8f, 0.006f), K.Mats.Glass, false);
        }
    }
}
