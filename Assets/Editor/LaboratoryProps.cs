using System.Collections.Generic;
using UnityEngine;
using Voss.Atmosphere;
using Voss.Audio;
using Voss.Interaction;
using Voss.Narrative;
using K = Voss.EditorTools.VossBuildKit;

namespace Voss.EditorTools
{
    /// <summary>
    /// Set dressing for 02_VossLaboratory. Every object says something about Voss, his audience or
    /// the experiment; a few can be examined for story fragments and quiet hints. Props stay against
    /// walls so the walking routes and puzzle areas remain clear.
    /// </summary>
    public static class LaboratoryProps
    {
        private static readonly Color Ink = new Color(0.1f, 0.08f, 0.06f);
        private static readonly Color Chalk = new Color(0.85f, 0.85f, 0.8f);
        private static readonly Color BrassText = new Color(0.78f, 0.62f, 0.32f);
        private static readonly Color Pale = new Color(0.8f, 0.78f, 0.7f);

        /// <param name="rooms">PERCEPTION, REASON, MEMORY groups (room-local frames).</param>
        public static void Build(Transform root, Transform[] rooms)
        {
            var hall = K.Group(root.Find("Entrance"), "Props").transform;
            BuildEvidenceBoard(hall);
            BuildSubjectDesk(hall);
            BuildObservationChair(hall);
            BuildPosters(hall);

            BuildPerceptionProps(K.Group(rooms[0], "Props").transform);
            BuildReasonProps(K.Group(rooms[1], "Props").transform);
            BuildMemoryProps(K.Group(rooms[2], "Props").transform);
            BuildFinalRoomProps(K.Group(root.Find("FinalExperiment"), "Props").transform);

            var cams = K.Group(root, "SurveillanceCameras").transform;
            SurveillanceCamera(cams, new Vector3(-15.5f, 4.4f, 6.5f), new Vector3(1f, -0.4f, -1f));
            SurveillanceCamera(cams, new Vector3(15.5f, 4.4f, 6.5f), new Vector3(-1f, -0.4f, -1f));
            SurveillanceCamera(cams, new Vector3(-16.5f, 4.0f, 1.5f), new Vector3(-1f, -0.45f, -1f));
        }

        // ================================================================ hall

        /// <summary>Cork board on the south wall: headlines about the vanishing, red string to his photo.</summary>
        private static void BuildEvidenceBoard(Transform hall)
        {
            var b = K.Group(hall, "EvidenceBoard", new Vector3(-9f, 2.1f, -6.8f)).transform;
            b.localRotation = Quaternion.Euler(0f, 180f, 0f); // its front (local -z) faces the hall
            K.Box(b, "Cork", Vector3.zero, new Vector3(3.2f, 1.6f, 0.05f), K.Mats.Cork, false);
            K.Box(b, "Frame_T", new Vector3(0f, 0.82f, -0.01f), new Vector3(3.3f, 0.05f, 0.07f), K.Mats.Wood, false);
            K.Box(b, "Frame_B", new Vector3(0f, -0.82f, -0.01f), new Vector3(3.3f, 0.05f, 0.07f), K.Mats.Wood, false);
            K.Box(b, "Frame_L", new Vector3(-1.63f, 0f, -0.01f), new Vector3(0.05f, 1.68f, 0.07f), K.Mats.Wood, false);
            K.Box(b, "Frame_R", new Vector3(1.63f, 0f, -0.01f), new Vector3(0.05f, 1.68f, 0.07f), K.Mats.Wood, false);

            // His photograph at the centre.
            var photo = new Vector2(0f, 0.12f);
            K.Box(b, "VossPhoto", new Vector3(photo.x, photo.y, -0.03f), new Vector3(0.42f, 0.56f, 0.01f), K.Mats.Photo, false);
            K.Prim(b, "PhotoHead", PrimitiveType.Sphere, new Vector3(0f, 0.2f, -0.04f), new Vector3(0.12f, 0.15f, 0.01f), K.Mats.Silhouette, false);
            K.Box(b, "PhotoHat", new Vector3(0f, 0.3f, -0.04f), new Vector3(0.13f, 0.08f, 0.01f), K.Mats.Silhouette, false);
            K.Box(b, "PhotoBrim", new Vector3(0f, 0.26f, -0.04f), new Vector3(0.2f, 0.015f, 0.01f), K.Mats.Silhouette, false);
            K.Box(b, "PhotoShoulders", new Vector3(0f, 0.02f, -0.04f), new Vector3(0.3f, 0.16f, 0.01f), K.Mats.Silhouette, false);
            K.Text3D(b, "PhotoName", "E. VOSS", new Vector3(0f, -0.1f, -0.045f), Vector3.back, 0.045f, Pale, TextAnchor.MiddleCenter, FontStyle.Bold, 48);

            var clippings = new[]
            {
                (new Vector2(-1.1f, 0.45f), "ILLUSIONIST VANISHES\nBEFORE 400 WITNESSES"),
                (new Vector2(1.1f, 0.45f), "SEVEN SECONDS\nOF DARKNESS"),
                (new Vector2(-1.1f, -0.4f), "THEATRE SEALED.\nSEARCH CONTINUES."),
                (new Vector2(1.1f, -0.4f), "AUDIENCE MEMBERS\nGET BLACK ENVELOPES"),
                (new Vector2(0.55f, -0.6f), "WHERE IS VOSS?"),
            };
            for (int i = 0; i < clippings.Length; i++)
            {
                var (pos, text) = clippings[i];
                float w = i == 4 ? 0.5f : 0.74f;
                float h = i == 4 ? 0.16f : 0.3f;
                K.Box(b, "Clipping_" + i, new Vector3(pos.x, pos.y, -0.03f), new Vector3(w, h, 0.005f), K.Mats.OldPaper, false);
                K.Text3D(b, "Headline_" + i, text, new Vector3(pos.x, pos.y, -0.036f), Vector3.back, 0.045f, Ink, TextAnchor.MiddleCenter, FontStyle.Bold, 48);
                Thread(b, photo, pos, -0.045f);
            }

            // One handwritten card pinned among them can be taken down and read.
            Paper(b, "PinnedNote", new Vector3(-0.5f, -0.62f, -0.035f), Quaternion.Euler(-90f, 0f, 4f), new Vector2(0.24f, 0.14f),
                "Every headline asks\nwhere he went.", 0.022f, "Not one of them asks where he was standing.");
        }

        private static void Thread(Transform board, Vector2 a, Vector2 b, float z)
        {
            Vector2 d = b - a;
            var s = K.Box(board, "RedString", new Vector3((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f, z), new Vector3(d.magnitude, 0.008f, 0.004f), K.Mats.RedString, false);
            s.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            K.Prim(board, "Pin", PrimitiveType.Sphere, new Vector3(b.x, b.y + 0.12f, z - 0.01f), Vector3.one * 0.025f, K.Mats.RedString, false);
        }

        /// <summary>The files of the three subjects who came before. The numbers stop just short of the ticket.</summary>
        private static void BuildSubjectDesk(Transform hall)
        {
            var d = K.Group(hall, "SubjectDesk", new Vector3(-12.4f, 0f, -6.2f)).transform;
            d.localRotation = Quaternion.Euler(0f, 180f, 0f); // against the south wall, facing the hall
            Table(d, Vector3.zero, new Vector2(1.8f, 0.7f), 0.78f);

            Paper(d, "File_7311", new Vector3(-0.55f, 0.79f, -0.05f), Quaternion.Euler(0f, 8f, 0f), new Vector2(0.22f, 0.3f),
                "SUBJECT 7311\nTEST: PERCEPTION\nRESULT: FAILED\n\nStared at the reflections\nfor an hour. Never once\nlooked at the objects.\n\nSTATUS: ________",
                0.0125f, "Subject 7311. Someone was here before you.");
            Paper(d, "File_7312", new Vector3(0f, 0.79f, -0.08f), Quaternion.Euler(0f, -5f, 0f), new Vector2(0.22f, 0.3f),
                "SUBJECT 7312\nTEST: REASON\nRESULT: FAILED\n\nSearched the room for\na lock that fits the key.\nForty minutes.\n\nSTATUS: ________",
                0.0125f, "Subject 7312.");
            Paper(d, "File_7313", new Vector3(0.5f, 0.79f, -0.04f), Quaternion.Euler(0f, 12f, 0f), new Vector2(0.22f, 0.3f),
                "SUBJECT 7313\nFINAL QUESTION\nANSWER: HIDDEN TUNNEL\nRESULT: FAILED\n\nClosest yet.\nStill asking how.\n\nSTATUS: ________",
                0.0125f, "7311. 7312. 7313. Whose number comes next?");

            // Desk lamp: the one warm light in the hall, so the files get found.
            K.Prim(d, "LampBase", PrimitiveType.Cylinder, new Vector3(0.75f, 0.8f, 0.22f), new Vector3(0.14f, 0.015f, 0.14f), K.Mats.Brass, false);
            K.Box(d, "LampArm", new Vector3(0.75f, 1.0f, 0.2f), new Vector3(0.02f, 0.4f, 0.02f), K.Mats.Brass, false);
            K.Prim(d, "LampShade", PrimitiveType.Cylinder, new Vector3(0.68f, 1.18f, 0.12f), new Vector3(0.16f, 0.06f, 0.16f), K.Mats.BlackMetal, false);
            var lamp = K.PointLight(d, "DeskLamp", new Vector3(0.6f, 1.05f, 0f), new Color(1f, 0.72f, 0.42f), 1.1f, 3.5f);
            lamp.gameObject.AddComponent<LightFlicker>().Configure(0.06f, new Vector2(8f, 16f), 0.25f);

            // Filing cabinet with one drawer pulled out, papers spilled on the floor.
            var c = K.Group(hall, "FilingCabinet", new Vector3(-15.2f, 0f, -6.45f)).transform;
            c.localRotation = Quaternion.Euler(0f, 180f, 0f);
            K.Box(c, "Body", new Vector3(0f, 0.65f, 0f), new Vector3(0.55f, 1.3f, 0.6f), K.Mats.Rust);
            for (int i = 0; i < 3; i++) K.Box(c, "DrawerFront_" + i, new Vector3(0f, 0.25f + i * 0.4f, -0.305f), new Vector3(0.5f, 0.34f, 0.01f), K.Mats.BlackMetal, false);
            K.Box(c, "OpenDrawer", new Vector3(0f, 1.05f, -0.45f), new Vector3(0.48f, 0.3f, 0.35f), K.Mats.Rust);
            var rnd = new System.Random(7314);
            for (int i = 0; i < 7; i++)
            {
                var p = K.Box(hall, "LoosePaper_" + i, new Vector3(-15.2f + (float)rnd.NextDouble() * 2.4f, 0.004f, -6.2f + (float)rnd.NextDouble() * 1.6f),
                    new Vector3(0.21f, 0.004f, 0.29f), K.Mats.OldPaper, false);
                p.transform.localRotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
            }
        }

        /// <summary>A restraint chair facing a monitor that never stops watching.</summary>
        private static void BuildObservationChair(Transform hall)
        {
            var chair = K.Group(hall, "ObservationChair", new Vector3(7.5f, 0f, -2.5f));
            chair.transform.localRotation = Quaternion.Euler(0f, 90f, 0f); // faces the monitor (+x)
            var t = chair.transform;
            K.Box(t, "Seat", new Vector3(0f, 0.48f, 0f), new Vector3(0.55f, 0.08f, 0.55f), K.Mats.Leather, false);
            K.Box(t, "Back", new Vector3(0f, 0.95f, -0.26f), new Vector3(0.55f, 0.85f, 0.08f), K.Mats.Leather, false);
            K.Box(t, "HeadStrap", new Vector3(0f, 1.2f, -0.21f), new Vector3(0.5f, 0.05f, 0.02f), K.Mats.Leather, false);
            foreach (float x in new[] { -0.24f, 0.24f })
            {
                foreach (float z in new[] { -0.24f, 0.24f })
                    K.Box(t, "Leg", new Vector3(x, 0.22f, z), new Vector3(0.05f, 0.44f, 0.05f), K.Mats.BlackMetal, false);
                K.Box(t, "Arm", new Vector3(x * 1.25f, 0.68f, 0.02f), new Vector3(0.07f, 0.05f, 0.5f), K.Mats.BlackMetal, false);
                K.Box(t, "ArmPost", new Vector3(x * 1.25f, 0.58f, 0.2f), new Vector3(0.04f, 0.2f, 0.04f), K.Mats.BlackMetal, false);
                K.Box(t, "WristStrap", new Vector3(x * 1.25f, 0.71f, 0.08f), new Vector3(0.1f, 0.025f, 0.08f), K.Mats.Leather, false);
            }
            K.Box(t, "CutStrap", new Vector3(0.36f, 0.58f, 0.09f), new Vector3(0.012f, 0.22f, 0.05f), K.Mats.Leather, false);
            var col = chair.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.7f, 0f);
            col.size = new Vector3(0.75f, 1.4f, 0.7f);
            var insp = chair.AddComponent<InspectableObject>();
            insp.Prompt = "Inspect chair";
            insp.Note = "The straps have been cut. From the inside.";

            // Monitor on a rusted cart.
            var cart = K.Group(hall, "MonitorCart", new Vector3(9.6f, 0f, -2.5f)).transform;
            K.Box(cart, "Top", new Vector3(0f, 0.8f, 0f), new Vector3(0.6f, 0.04f, 0.7f), K.Mats.Rust);
            K.Box(cart, "Shelf", new Vector3(0f, 0.25f, 0f), new Vector3(0.6f, 0.03f, 0.7f), K.Mats.Rust, false);
            foreach (float x in new[] { -0.27f, 0.27f })
                foreach (float z in new[] { -0.32f, 0.32f })
                    K.Box(cart, "Post", new Vector3(x, 0.4f, z), new Vector3(0.03f, 0.8f, 0.03f), K.Mats.BlackMetal, false);
            K.Box(cart, "CrtBody", new Vector3(0.05f, 1.06f, 0f), new Vector3(0.5f, 0.46f, 0.55f), K.Mats.BlackMetal);
            K.Box(cart, "CrtScreen", new Vector3(-0.205f, 1.07f, 0f), new Vector3(0.02f, 0.34f, 0.44f), K.Mats.Crt, false);
            K.Text3D(cart, "CrtText", "OBSERVATION\nIN PROGRESS", new Vector3(-0.218f, 1.07f, 0f), Vector3.left, 0.04f, new Color(0.08f, 0.15f, 0.1f), TextAnchor.MiddleCenter, FontStyle.Bold, 48);
            var glow = K.PointLight(cart, "CrtGlow", new Vector3(-0.6f, 1.07f, 0f), new Color(0.55f, 0.78f, 0.65f), 0.7f, 3.2f);
            glow.gameObject.AddComponent<LightFlicker>().Configure(0.25f, new Vector2(2f, 5f), 0.2f);
            cart.gameObject.AddComponent<LoopingSound>().Configure(SfxId.Static, 0.35f, 5f);
        }

        private static void BuildPosters(Transform hall)
        {
            // West wall: the show itself.
            var p = K.Group(hall, "ShowPoster", new Vector3(-15.83f, 2.3f, 3.5f)).transform;
            K.Box(p, "Sheet", Vector3.zero, new Vector3(0.02f, 1.6f, 1.1f), K.Mats.Velvet, false);
            K.Text3D(p, "Name", "E. VOSS", new Vector3(0.02f, 0.55f, 0f), Vector3.right, 0.12f, BrassText, TextAnchor.MiddleCenter, FontStyle.Bold, 48);
            K.Text3D(p, "Presents", "presents", new Vector3(0.02f, 0.38f, 0f), Vector3.right, 0.06f, Pale, TextAnchor.MiddleCenter, FontStyle.Italic, 48);
            K.Text3D(p, "Title", "THE\nIMPOSSIBLE\nEXIT", new Vector3(0.02f, 0.0f, 0f), Vector3.right, 0.11f, BrassText, TextAnchor.MiddleCenter, FontStyle.Bold, 48);
            K.Text3D(p, "Footer", "ONE NIGHT ONLY", new Vector3(0.02f, -0.6f, 0f), Vector3.right, 0.06f, Pale, TextAnchor.MiddleCenter, FontStyle.Bold, 48);

            // South wall: what came after.
            var m = K.Group(hall, "MissingPoster", new Vector3(-2.7f, 2.2f, -6.83f)).transform;
            K.Box(m, "Sheet", Vector3.zero, new Vector3(0.95f, 1.3f, 0.02f), K.Mats.OldPaper, false);
            K.Text3D(m, "Title", "MISSING", new Vector3(0f, 0.48f, 0.02f), Vector3.forward, 0.13f, new Color(0.45f, 0.05f, 0.04f), TextAnchor.MiddleCenter, FontStyle.Bold, 48);
            K.Box(m, "Photo", new Vector3(0f, 0.08f, 0.015f), new Vector3(0.45f, 0.5f, 0.01f), K.Mats.Photo, false);
            K.Prim(m, "Head", PrimitiveType.Sphere, new Vector3(0f, 0.14f, 0.022f), new Vector3(0.13f, 0.16f, 0.01f), K.Mats.Silhouette, false);
            K.Box(m, "Hat", new Vector3(0f, 0.25f, 0.022f), new Vector3(0.14f, 0.08f, 0.01f), K.Mats.Silhouette, false);
            K.Text3D(m, "Body", "E. VOSS\nLAST SEEN ON STAGE\nSEVEN SECONDS BEFORE\nTHE LIGHTS RETURNED", new Vector3(0f, -0.4f, 0.02f), Vector3.forward, 0.045f, Ink, TextAnchor.MiddleCenter, FontStyle.Bold, 48);
        }

        // ========================================================== perception

        private static void BuildPerceptionProps(Transform p)
        {
            // Optical illusions either side of the entrance, facing into the room.
            var ml = IllusionPanel(p, "Illusion_Lines", new Vector3(-2.7f, 2.1f, 11.17f), "WHICH LINE IS LONGER?",
                "They are exactly the same length. You checked anyway.");
            var w = ml.transform;
            K.Box(w, "Sheet", Vector3.zero, new Vector3(1.6f, 1.1f, 0.02f), K.Mats.WhitePanel, false);
            MullerLyer(w, 0.22f, true);
            MullerLyer(w, -0.22f, false);

            var gc = IllusionPanel(p, "Illusion_Grey", new Vector3(2.7f, 2.1f, 11.17f), "WHICH GREY IS DARKER?",
                "The same grey. Your eyes compared. They did not measure.");
            var g = gc.transform;
            K.Box(g, "DarkHalf", new Vector3(-0.4f, 0f, 0f), new Vector3(0.8f, 1.1f, 0.02f), K.Mats.Silhouette, false);
            K.Box(g, "LightHalf", new Vector3(0.4f, 0f, 0f), new Vector3(0.8f, 1.1f, 0.02f), K.Mats.WhitePanel, false);
            K.Box(g, "GreyA", new Vector3(-0.4f, 0f, 0.015f), new Vector3(0.24f, 0.24f, 0.01f), K.Mats.Grey, false);
            K.Box(g, "GreyB", new Vector3(0.4f, 0f, 0.015f), new Vector3(0.24f, 0.24f, 0.01f), K.Mats.Grey, false);

            // Eye chart between the west niches. The last line was added later.
            var chart = K.Group(p, "EyeChart", new Vector3(-4.04f, 2.0f, 17f)).transform;
            K.Box(chart, "Sheet", Vector3.zero, new Vector3(0.02f, 1.1f, 0.75f), K.Mats.WhitePanel, false);
            var rows = new[] { ("E", 0.16f, 0.4f), ("F  P", 0.11f, 0.2f), ("T  O  Z", 0.08f, 0.04f), ("L  P  E  D", 0.06f, -0.1f), ("TRUST", 0.05f, -0.24f), ("NOTHING", 0.04f, -0.36f) };
            var chartText = new List<TextMesh>();
            foreach (var (text, size, y) in rows)
                chartText.Add(K.Text3D(chart, "Row_" + text.Replace(" ", ""), text, new Vector3(0.015f, y, 0f), Vector3.right, size, Ink, TextAnchor.MiddleCenter, FontStyle.Bold, 48));
            chartText[0].name = "Front";
            var chartCol = chart.gameObject.AddComponent<BoxCollider>();
            chartCol.size = new Vector3(0.1f, 1.1f, 0.75f);
            var ci = chart.gameObject.AddComponent<InspectableObject>();
            ci.Prompt = "Read chart";
            ci.Note = "The last two lines were not on the original chart.";

            // Mirror shards on the floor.
            var rnd = new System.Random(14);
            for (int i = 0; i < 9; i++)
            {
                var s = K.Box(p, "Shard_" + i, new Vector3(-1.6f + (float)rnd.NextDouble() * 3.2f, 0.003f, 20.6f + (float)rnd.NextDouble() * 1.8f),
                    new Vector3(0.05f + (float)rnd.NextDouble() * 0.12f, 0.004f, 0.03f + (float)rnd.NextDouble() * 0.08f), K.Mats.Glass, false);
                s.transform.localRotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
            }

            // A mannequin that is never where you left it.
            Mannequin(p, new List<Vector3> { new Vector3(-3.5f, 0f, 21.6f), new Vector3(-3.6f, 0f, 11.8f), new Vector3(3.6f, 0f, 11.8f) });
        }

        private static GameObject IllusionPanel(Transform parent, string name, Vector3 pos, string caption, string note)
        {
            var go = K.Group(parent, name, pos);
            K.Text3D(parent, name + "_Caption", caption, pos + new Vector3(0f, 0.7f, 0.02f), Vector3.forward, 0.07f, Pale, TextAnchor.MiddleCenter, FontStyle.Bold, 48);
            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(1.6f, 1.1f, 0.1f);
            var insp = go.AddComponent<InspectableObject>();
            insp.Prompt = "Look closer";
            insp.Note = note;
            return go;
        }

        /// <summary>A horizontal bar with arrow fins: outward fins read short, inward fins read long.</summary>
        private static void MullerLyer(Transform panel, float y, bool inwardFins)
        {
            const float half = 0.42f;
            K.Box(panel, "Bar", new Vector3(0f, y, 0.015f), new Vector3(half * 2f, 0.025f, 0.01f), K.Mats.BlackMetal, false);
            foreach (float end in new[] { -half, half })
            {
                float dir = Mathf.Sign(end) * (inwardFins ? 1f : -1f); // fins point away from the bar (inward) or back over it (outward)
                foreach (float up in new[] { 1f, -1f })
                {
                    Vector2 d = new Vector2(dir * 0.11f, up * 0.08f);
                    var fin = K.Box(panel, "Fin", new Vector3(end + d.x * 0.5f, y + d.y * 0.5f, 0.015f), new Vector3(d.magnitude, 0.022f, 0.01f), K.Mats.BlackMetal, false);
                    fin.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                }
            }
        }

        private static void Mannequin(Transform parent, List<Vector3> poses)
        {
            var m = K.Group(parent, "Mannequin", poses[0]);
            m.transform.localRotation = Quaternion.Euler(0f, 135f, 0f);
            var t = m.transform;
            K.Prim(t, "Base", PrimitiveType.Cylinder, new Vector3(0f, 0.015f, 0f), new Vector3(0.4f, 0.015f, 0.4f), K.Mats.BlackMetal, false);
            K.Box(t, "Pole", new Vector3(0f, 0.4f, 0f), new Vector3(0.04f, 0.8f, 0.04f), K.Mats.BlackMetal, false);
            K.Prim(t, "Torso", PrimitiveType.Capsule, new Vector3(0f, 1.3f, 0f), new Vector3(0.38f, 0.38f, 0.24f), K.Mats.Porcelain, false);
            K.Box(t, "Hips", new Vector3(0f, 0.92f, 0f), new Vector3(0.34f, 0.18f, 0.22f), K.Mats.Porcelain, false);
            K.Box(t, "Neck", new Vector3(0f, 1.7f, 0f), new Vector3(0.08f, 0.1f, 0.08f), K.Mats.Porcelain, false);
            K.Prim(t, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.86f, 0f), new Vector3(0.2f, 0.26f, 0.21f), K.Mats.Porcelain, false);
            var armL = K.Box(t, "ArmL", new Vector3(-0.25f, 1.25f, 0.02f), new Vector3(0.07f, 0.6f, 0.07f), K.Mats.Porcelain, false);
            armL.transform.localRotation = Quaternion.Euler(0f, 0f, -6f);
            var armR = K.Box(t, "ArmR", new Vector3(0.27f, 1.38f, 0.12f), new Vector3(0.07f, 0.6f, 0.07f), K.Mats.Porcelain, false);
            armR.transform.localRotation = Quaternion.Euler(-35f, 0f, 8f); // reaching, slightly
            var col = m.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1f, 0f);
            col.height = 2f;
            col.radius = 0.25f;
            m.AddComponent<ShiftWhenUnseen>().Configure(poses);
        }

        // ============================================================== reason

        private static void BuildReasonProps(Transform r)
        {
            // Chalkboard on the west wall.
            var b = K.Group(r, "Chalkboard", new Vector3(-4.03f, 2.0f, 14f)).transform;
            K.Box(b, "Board", Vector3.zero, new Vector3(0.04f, 1.3f, 2.6f), K.Mats.Chalkboard, false);
            K.Box(b, "Frame_T", new Vector3(0.01f, 0.67f, 0f), new Vector3(0.06f, 0.05f, 2.7f), K.Mats.Wood, false);
            K.Box(b, "Frame_B", new Vector3(0.01f, -0.67f, 0f), new Vector3(0.06f, 0.05f, 2.7f), K.Mats.Wood, false);
            K.Box(b, "Tray", new Vector3(0.06f, -0.7f, 0f), new Vector3(0.1f, 0.03f, 2.4f), K.Mats.Wood, false);
            K.Text3D(b, "Notes", "1. WEIGH WHAT YOU ARE SHOWN.\n2. WEIGH WHAT YOU ASSUME.\n3. ONLY ONE OF THEM HAS WEIGHT.\n\n7 SECONDS = ?",
                new Vector3(0.03f, 0.05f, 0f), Vector3.right, 0.085f, Chalk, TextAnchor.MiddleCenter, FontStyle.Normal, 48);
            var col = b.gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(0.12f, 1.3f, 2.6f);
            var insp = b.gameObject.AddComponent<InspectableObject>();
            insp.Prompt = "Read board";
            insp.Note = "Chalk dust on the floor. Someone kept rewriting the last line.";

            // Table in the south-east corner: a stopwatch and an abandoned game of chess.
            var t = K.Group(r, "SideTable", new Vector3(3.0f, 0f, 13.0f)).transform;
            Table(t, Vector3.zero, new Vector2(1.2f, 0.7f), 0.76f);
            var sw = K.Group(t, "Stopwatch", new Vector3(-0.3f, 0.78f, 0f)).transform;
            K.Prim(sw, "Case", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.1f, 0.01f, 0.1f), K.Mats.Brass, false);
            K.Box(sw, "Crown", new Vector3(0f, 0f, 0.06f), new Vector3(0.015f, 0.012f, 0.025f), K.Mats.Brass, false);
            K.Text3D(sw, "Front", "00:07", new Vector3(0f, 0.012f, 0f), Vector3.up, 0.02f, Ink, TextAnchor.MiddleCenter, FontStyle.Bold, 48, Vector3.forward);
            var swc = sw.gameObject.AddComponent<BoxCollider>();
            swc.size = new Vector3(0.14f, 0.06f, 0.16f);
            var swi = sw.gameObject.AddComponent<InspectableObject>();
            swi.Prompt = "Inspect stopwatch";
            swi.Note = "Stopped at seven seconds. As long as the blackout lasted.";

            var chess = K.Group(t, "Chessboard", new Vector3(0.25f, 0.78f, 0f)).transform;
            K.Box(chess, "Board", Vector3.zero, new Vector3(0.36f, 0.02f, 0.36f), K.Mats.Porcelain, false);
            for (int x = 0; x < 8; x++)
                for (int z = 0; z < 8; z++)
                    if ((x + z) % 2 == 1)
                        K.Box(chess, "Sq", new Vector3(-0.1575f + x * 0.045f, 0.011f, -0.1575f + z * 0.045f), new Vector3(0.045f, 0.002f, 0.045f), K.Mats.Silhouette, false);
            var pieces = new[] { (1, 1, true), (3, 2, true), (5, 6, false), (6, 5, false), (2, 6, false), (4, 4, true) };
            foreach (var (x, z, white) in pieces)
                K.Prim(chess, "Piece", PrimitiveType.Capsule, new Vector3(-0.1575f + x * 0.045f, 0.035f, -0.1575f + z * 0.045f), new Vector3(0.025f, 0.025f, 0.025f), white ? K.Mats.Porcelain : K.Mats.Silhouette, false);
            var fallen = K.Prim(chess, "FallenPiece", PrimitiveType.Capsule, new Vector3(0.2f, 0.012f, 0.12f), new Vector3(0.025f, 0.025f, 0.025f), K.Mats.Porcelain, false);
            fallen.transform.localRotation = Quaternion.Euler(0f, 30f, 90f);
            var cc = chess.gameObject.AddComponent<BoxCollider>();
            cc.size = new Vector3(0.38f, 0.1f, 0.38f);
            var chi = chess.gameObject.AddComponent<InspectableObject>();
            chi.Prompt = "Inspect chessboard";
            chi.Note = "The game stopped mid-move. One piece is missing: the black king.";

            // Ticking wall clock on the east wall.
            WallClock(r, new Vector3(4.03f, 3.0f, 14f), Vector3.left, 0.5f, true, null);

            // Brass weights on a shelf by the entrance.
            var s = K.Group(r, "WeightShelf", new Vector3(-2.8f, 1.2f, 11.32f)).transform;
            K.Box(s, "Shelf", Vector3.zero, new Vector3(1.2f, 0.04f, 0.3f), K.Mats.Wood, false);
            float wx = -0.45f;
            foreach (float size in new[] { 0.16f, 0.13f, 0.1f, 0.08f, 0.06f, 0.045f })
            {
                K.Prim(s, "Weight", PrimitiveType.Cylinder, new Vector3(wx, 0.02f + size * 0.5f, 0f), new Vector3(size, size * 0.5f, size), K.Mats.Brass, false);
                wx += size + 0.04f;
            }
        }

        // ============================================================== memory

        private static void BuildMemoryProps(Transform m)
        {
            // The audience: a wall of faces. One seat is circled.
            var wall = K.Group(m, "PhotoWall", new Vector3(4.03f, 2.0f, 14.3f)).transform;
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    var ph = K.Group(wall, $"Photo_{row}_{col}", new Vector3(0f, 0.5f - row * 0.5f, -0.66f + col * 0.44f)).transform;
                    K.Box(ph, "Frame", Vector3.zero, new Vector3(0.02f, 0.42f, 0.34f), K.Mats.BlackMetal, false);
                    K.Box(ph, "Print", new Vector3(-0.012f, 0f, 0f), new Vector3(0.01f, 0.36f, 0.28f), K.Mats.Photo, false);
                    K.Prim(ph, "Head", PrimitiveType.Sphere, new Vector3(-0.02f, 0.04f, 0f), new Vector3(0.01f, 0.11f, 0.09f), K.Mats.Silhouette, false);
                    K.Box(ph, "Shoulders", new Vector3(-0.02f, -0.1f, 0f), new Vector3(0.01f, 0.1f, 0.2f), K.Mats.Silhouette, false);
                    if (row == 1 && col == 2)
                    {
                        foreach (var (pos, size) in new[] { (new Vector3(-0.03f, 0.24f, 0f), new Vector3(0.01f, 0.02f, 0.42f)), (new Vector3(-0.03f, -0.24f, 0f), new Vector3(0.01f, 0.02f, 0.42f)),
                                                           (new Vector3(-0.03f, 0f, 0.2f), new Vector3(0.01f, 0.5f, 0.02f)), (new Vector3(-0.03f, 0f, -0.2f), new Vector3(0.01f, 0.5f, 0.02f)) })
                            K.Box(ph, "RedCircle", pos, size, K.Mats.RedString, false);
                        K.Text3D(ph, "Front", "ROW 7 / SEAT 14", new Vector3(-0.022f, -0.15f, 0f), Vector3.left, 0.03f, new Color(0.7f, 0.08f, 0.06f), TextAnchor.MiddleCenter, FontStyle.Bold, 48);
                        var pc = ph.gameObject.AddComponent<BoxCollider>();
                        pc.size = new Vector3(0.08f, 0.5f, 0.42f);
                        var pi = ph.gameObject.AddComponent<InspectableObject>();
                        pi.Prompt = "Look at photograph";
                        pi.Note = "Row 7, seat 14. Someone circled it in red.";
                    }
                }
            }

            // Shelf of stopped clocks on the west wall.
            var shelf = K.Group(m, "ClockShelf", new Vector3(-3.92f, 1.45f, 20.4f)).transform;
            K.Box(shelf, "Shelf", Vector3.zero, new Vector3(0.26f, 0.04f, 1.6f), K.Mats.Wood, false);
            float[] sizes = { 0.22f, 0.3f, 0.18f };
            float[] zs = { -0.5f, 0f, 0.5f };
            for (int i = 0; i < 3; i++)
            {
                var c = WallClock(shelf, new Vector3(0f, 0.02f + sizes[i] * 0.5f, zs[i]), Vector3.right, sizes[i], false, "7:14");
                if (i == 1)
                {
                    var bc = c.AddComponent<BoxCollider>();
                    bc.size = new Vector3(sizes[i], sizes[i], 0.1f); // clock-local: z is the face normal
                    var ins = c.AddComponent<InspectableObject>();
                    ins.Prompt = "Inspect clock";
                    ins.Note = "Every clock in this room stopped at 7:14.";
                }
            }

            // Voss's desk by the exit: a diary page and a tape recorder.
            var d = K.Group(m, "VossDesk", new Vector3(-2.8f, 0f, 21.9f)).transform;
            Table(d, Vector3.zero, new Vector2(1.4f, 0.7f), 0.76f);
            K.Box(d, "Diary", new Vector3(-0.4f, 0.79f, 0.05f), new Vector3(0.18f, 0.03f, 0.24f), K.Mats.Leather, false);
            Paper(d, "DiaryPage", new Vector3(-0.38f, 0.81f, -0.12f), Quaternion.Euler(0f, -10f, 0f), new Vector2(0.2f, 0.27f),
                "DAY 41.\nRehearsal again.\n\nPeople do not remember\nwhat they saw.\nThey remember what they\nwere told they saw.\n\n- E.V.",
                0.013f, "Voss's handwriting. Day 41 of rehearsal.");

            var tape = K.Group(d, "TapeRecorder", new Vector3(0.25f, 0.78f, 0f)).transform;
            K.Box(tape, "Body", new Vector3(0f, 0.07f, 0f), new Vector3(0.44f, 0.14f, 0.3f), K.Mats.BlackMetal, false);
            K.Box(tape, "Plate", new Vector3(0f, 0.145f, 0f), new Vector3(0.42f, 0.01f, 0.28f), K.Mats.Brass, false);
            var reelA = K.Group(tape, "ReelA", new Vector3(-0.1f, 0.16f, 0.02f)).transform;
            var reelB = K.Group(tape, "ReelB", new Vector3(0.1f, 0.16f, 0.02f)).transform;
            foreach (var reel in new[] { reelA, reelB })
            {
                K.Prim(reel, "Disc", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.15f, 0.006f, 0.15f), K.Mats.Silhouette, false);
                K.Box(reel, "Spoke", new Vector3(0f, 0.008f, 0f), new Vector3(0.12f, 0.004f, 0.02f), K.Mats.Brass, false);
            }
            for (int i = 0; i < 4; i++) K.Box(tape, "Key_" + i, new Vector3(-0.09f + i * 0.06f, 0.15f, -0.11f), new Vector3(0.04f, 0.02f, 0.03f), K.Mats.Porcelain, false);
            var tc = tape.gameObject.AddComponent<BoxCollider>();
            tc.center = new Vector3(0f, 0.09f, 0f);
            tc.size = new Vector3(0.46f, 0.2f, 0.32f);
            tape.gameObject.AddComponent<TapeRecorder>().Configure(reelA, reelB);

            var lamp = K.PointLight(d, "DeskLamp", new Vector3(0.6f, 1.25f, 0.2f), new Color(1f, 0.72f, 0.42f), 0.8f, 3f);
            lamp.gameObject.AddComponent<LightFlicker>().Configure(0.08f, new Vector2(6f, 14f), 0.3f);
            K.Prim(d, "LampBase", PrimitiveType.Cylinder, new Vector3(0.6f, 0.79f, 0.2f), new Vector3(0.12f, 0.015f, 0.12f), K.Mats.Brass, false);
            K.Box(d, "LampArm", new Vector3(0.6f, 1.0f, 0.2f), new Vector3(0.02f, 0.4f, 0.02f), K.Mats.Brass, false);
        }

        // ========================================================== corridors

        /// <summary>Behind the hidden door: what is left of the show, kept around the edges of the final theatre.</summary>
        private static void BuildFinalRoomProps(Transform c)
        {
            // The show's poster on the south wall.
            var p = K.Group(c, "ShowPosterSmall", new Vector3(-21.5f, 1.7f, -6.83f)).transform;
            K.Box(p, "Sheet", Vector3.zero, new Vector3(0.8f, 1.1f, 0.02f), K.Mats.Velvet, false);
            K.Text3D(p, "Text", "THE\nIMPOSSIBLE\nEXIT\n\nSEVEN SECONDS.\nONE CHAMBER.\nNO VOSS.", new Vector3(0f, 0f, 0.02f), Vector3.forward, 0.065f, BrassText, TextAnchor.MiddleCenter, FontStyle.Bold, 48);

            // Stage equipment dumped in the corner beside the screens.
            K.Prim(c, "FallenSpotlight", PrimitiveType.Cylinder, new Vector3(-28.4f, 0.18f, 1.0f), new Vector3(0.3f, 0.25f, 0.3f), K.Mats.BlackMetal, false)
                .transform.localRotation = Quaternion.Euler(90f, 30f, 0f);
            K.Prim(c, "CableCoil", PrimitiveType.Cylinder, new Vector3(-27.8f, 0.04f, 1.2f), new Vector3(0.5f, 0.04f, 0.5f), K.Mats.Silhouette, false);

            // A gurney with something under a sheet, against the south wall.
            var g = K.Group(c, "Gurney", new Vector3(-27.0f, 0f, -6.2f)).transform;
            K.Box(g, "Frame", new Vector3(0f, 0.8f, 0f), new Vector3(2.0f, 0.05f, 0.55f), K.Mats.Rust);
            foreach (float x in new[] { -0.9f, 0.9f })
                foreach (float z in new[] { -0.22f, 0.22f })
                {
                    K.Box(g, "Leg", new Vector3(x, 0.4f, z), new Vector3(0.04f, 0.8f, 0.04f), K.Mats.BlackMetal, false);
                    K.Prim(g, "Wheel", PrimitiveType.Sphere, new Vector3(x, 0.05f, z), Vector3.one * 0.1f, K.Mats.Silhouette, false);
                }
            K.Box(g, "Sheet", new Vector3(0f, 0.85f, 0f), new Vector3(2.02f, 0.03f, 0.6f), K.Mats.Fabric, false);
            var body = K.Prim(g, "Shape", PrimitiveType.Capsule, new Vector3(0.1f, 0.98f, 0f), new Vector3(0.36f, 0.75f, 0.24f), K.Mats.Fabric, false);
            body.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            K.Prim(g, "ShapeHead", PrimitiveType.Sphere, new Vector3(-0.78f, 0.98f, 0f), new Vector3(0.24f, 0.22f, 0.24f), K.Mats.Fabric, false);
            var gi = g.gameObject.AddComponent<InspectableObject>();
            gi.Prompt = "Lift the sheet";
            gi.Note = "A mannequin. Only a mannequin. You check twice.";
            gi.InspectRoot = body.transform;

            // Leftovers from the theatre: stacked seats, his costume rack.
            var seats = K.Group(c, "StackedSeats", new Vector3(-24.6f, 0f, -6.4f)).transform;
            seats.localRotation = Quaternion.Euler(0f, 90f, 0f);
            for (int i = 0; i < 3; i++)
            {
                K.Box(seats, "Cushion_" + i, new Vector3(0f, 0.25f + i * 0.5f, -0.6f + i * 0.15f), new Vector3(0.5f, 0.45f, 0.9f), K.Mats.Velvet);
            }
            var rack = K.Group(c, "CostumeRack", new Vector3(-26.0f, 0f, 1.2f)).transform;
            rack.localRotation = Quaternion.Euler(0f, 90f, 0f); // along the north wall
            K.Box(rack, "Rail", new Vector3(0f, 1.7f, 0f), new Vector3(0.04f, 0.04f, 1.4f), K.Mats.BlackMetal, false);
            K.Box(rack, "PostA", new Vector3(0f, 0.85f, -0.7f), new Vector3(0.04f, 1.7f, 0.04f), K.Mats.BlackMetal);
            K.Box(rack, "PostB", new Vector3(0f, 0.85f, 0.7f), new Vector3(0.04f, 1.7f, 0.04f), K.Mats.BlackMetal);
            K.Box(rack, "Coat", new Vector3(0f, 1.2f, -0.2f), new Vector3(0.12f, 0.9f, 0.5f), K.Mats.Silhouette, false);
            K.Box(rack, "Cape", new Vector3(0f, 1.15f, 0.35f), new Vector3(0.08f, 1.0f, 0.45f), K.Mats.Velvet, false);
            var hat = K.Group(rack, "TopHat", new Vector3(0f, 1.78f, -0.2f)).transform;
            K.Prim(hat, "Crown", PrimitiveType.Cylinder, new Vector3(0f, 0.09f, 0f), new Vector3(0.18f, 0.09f, 0.18f), K.Mats.Silhouette, false);
            K.Prim(hat, "Brim", PrimitiveType.Cylinder, new Vector3(0f, 0.005f, 0f), new Vector3(0.3f, 0.005f, 0.3f), K.Mats.Silhouette, false);
            var hc = hat.gameObject.AddComponent<BoxCollider>();
            hc.center = new Vector3(0f, 0.09f, 0f);
            hc.size = new Vector3(0.32f, 0.2f, 0.32f);
            var hi = hat.gameObject.AddComponent<InspectableObject>();
            hi.Prompt = "Inspect hat";
            hi.Note = "His hat. The lining is still warm.";

            // Stage plan on the wall shared with the hall: the second projector, stage left.
            var plan = K.Group(c, "StagePlan", new Vector3(-16.17f, 1.6f, 0.4f)).transform;
            plan.localRotation = Quaternion.Euler(0f, 180f, 0f); // its front (local +x) faces into the room
            K.Box(plan, "Sheet", Vector3.zero, new Vector3(0.02f, 0.9f, 1.4f), K.Mats.Accent, false);
            K.Text3D(plan, "Front", "THE IMPOSSIBLE EXIT\nSTAGE PLAN\n\n[ CHAMBER ]\n\nPROJECTOR B . . . STAGE LEFT\nCAMERA A . . . AUDIENCE\nCAMERA B . . . WINGS",
                new Vector3(0.015f, 0f, 0f), Vector3.right, 0.045f, new Color(0.88f, 0.92f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold, 48);
            var plc = plan.gameObject.AddComponent<BoxCollider>();
            plc.size = new Vector3(0.1f, 0.9f, 1.4f);
            var pli = plan.gameObject.AddComponent<InspectableObject>();
            pli.Prompt = "Read stage plan";
            pli.Note = "A second projector, stage left. It is in none of the newspaper photographs.";
        }

        // ============================================================= helpers

        /// <summary>A flat sheet with readable text named "Front", which the inspection view turns toward the player.</summary>
        private static GameObject Paper(Transform parent, string name, Vector3 pos, Quaternion rot, Vector2 size, string text, float lineHeight, string note)
        {
            var go = K.Group(parent, name, pos);
            go.transform.localRotation = rot;
            K.Box(go.transform, "Sheet", Vector3.zero, new Vector3(size.x, 0.004f, size.y), K.Mats.OldPaper, false);
            K.Text3D(go.transform, "Front", text, new Vector3(0f, 0.0025f, 0f), Vector3.up, lineHeight, Ink, TextAnchor.MiddleCenter, FontStyle.Bold, 64, Vector3.forward);
            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(size.x, 0.04f, size.y);
            var insp = go.AddComponent<InspectableObject>();
            insp.Prompt = "Read";
            insp.Note = note;
            return go;
        }

        private static void Table(Transform parent, Vector3 pos, Vector2 size, float height)
        {
            K.Box(parent, "TableTop", pos + new Vector3(0f, height - 0.025f, 0f), new Vector3(size.x, 0.05f, size.y), K.Mats.Wood);
            foreach (float x in new[] { -size.x * 0.5f + 0.05f, size.x * 0.5f - 0.05f })
                foreach (float z in new[] { -size.y * 0.5f + 0.05f, size.y * 0.5f - 0.05f })
                    K.Box(parent, "TableLeg", pos + new Vector3(x, (height - 0.05f) * 0.5f, z), new Vector3(0.05f, height - 0.05f, 0.05f), K.Mats.Wood, false);
            var col = K.Group(parent, "TableCollider", pos + new Vector3(0f, height * 0.5f, 0f)).AddComponent<BoxCollider>();
            col.size = new Vector3(size.x, height, size.y);
        }

        /// <summary>Round clock facing <paramref name="facing"/>; ticking ones get a moving second hand, stopped ones a fixed time.</summary>
        private static GameObject WallClock(Transform parent, Vector3 pos, Vector3 facing, float diameter, bool ticking, string stoppedAt)
        {
            var clock = K.Group(parent, ticking ? "WallClock" : "StoppedClock", pos);
            clock.transform.localRotation = Quaternion.LookRotation(facing, Vector3.up); // local +z faces the room
            var t = clock.transform;
            var face = K.Prim(t, "Face", PrimitiveType.Cylinder, Vector3.zero, new Vector3(diameter, 0.015f, diameter), K.Mats.Paper, false);
            face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var rim = K.Prim(t, "Rim", PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.01f), new Vector3(diameter * 1.1f, 0.012f, diameter * 1.1f), K.Mats.Brass, false);
            rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            if (!string.IsNullOrEmpty(stoppedAt))
                K.Text3D(t, "Time", stoppedAt, new Vector3(0f, -diameter * 0.18f, 0.02f), Vector3.forward, diameter * 0.16f, Ink, TextAnchor.MiddleCenter, FontStyle.Bold, 48);
            // 7:14 on every hand in this building.
            var hour = K.Box(t, "HourHand", Vector3.zero, new Vector3(0.012f, diameter * 0.22f, 0.004f), K.Mats.BlackMetal, false);
            hour.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            hour.transform.localRotation = Quaternion.Euler(0f, 0f, 217f); // viewed from +z, positive turns clockwise
            hour.transform.localPosition += hour.transform.localRotation * new Vector3(0f, diameter * 0.11f, 0f);
            var minute = K.Box(t, "MinuteHand", Vector3.zero, new Vector3(0.008f, diameter * 0.36f, 0.004f), K.Mats.BlackMetal, false);
            minute.transform.localRotation = Quaternion.Euler(0f, 0f, 84f);
            minute.transform.localPosition = new Vector3(0f, 0f, 0.022f) + minute.transform.localRotation * new Vector3(0f, diameter * 0.18f, 0f);
            if (ticking)
            {
                var pivot = K.Group(t, "SecondPivot", new Vector3(0f, 0f, 0.025f)).transform;
                K.Box(pivot, "SecondHand", new Vector3(0f, diameter * 0.2f, 0f), new Vector3(0.004f, diameter * 0.4f, 0.003f), K.Mats.RedString, false);
                clock.AddComponent<TickingClock>().Configure(pivot);
            }
            return clock;
        }

        private static void SurveillanceCamera(Transform parent, Vector3 pos, Vector3 look)
        {
            var mount = K.Group(parent, "Camera", pos).transform;
            K.Box(mount, "Bracket", new Vector3(0f, 0.12f, 0f), new Vector3(0.05f, 0.25f, 0.05f), K.Mats.BlackMetal, false);
            var head = K.Group(mount, "Head", Vector3.zero).transform;
            head.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
            K.Box(head, "Body", new Vector3(0f, 0f, -0.05f), new Vector3(0.16f, 0.14f, 0.36f), K.Mats.Porcelain, false);
            K.Prim(head, "Lens", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.15f), new Vector3(0.09f, 0.03f, 0.09f), K.Mats.BlackMetal, false)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var led = K.Box(head, "Led", new Vector3(0.05f, 0.075f, 0.08f), new Vector3(0.02f, 0.02f, 0.02f), K.Mats.SignRed, false);
            mount.gameObject.AddComponent<WatchingCamera>().Configure(head, led.GetComponent<Renderer>());
        }
    }
}
