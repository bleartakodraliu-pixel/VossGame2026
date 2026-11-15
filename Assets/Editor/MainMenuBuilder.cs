using UnityEngine;
using UnityEngine.SceneManagement;
using Voss.Atmosphere;
using Voss.Narrative;
using K = Voss.EditorTools.VossBuildKit;

namespace Voss.EditorTools
{
    /// <summary>
    /// 00_MainMenu, after the title-screen concept: the Voss Theatre seen from the house, red curtains
    /// under a VOSS THEATRE arch, a steel door marked LABORATORY 7314 glowing at the back of the stage,
    /// and in the foreground a table with the black envelope, the ticket, his photograph, an hourglass
    /// and a desk lamp. The menu itself is drawn by the UI on the left.
    /// </summary>
    public static class MainMenuBuilder
    {
        private static readonly Color Ink = new Color(0.1f, 0.08f, 0.06f);

        /// <summary>Where the menu camera stands; the playable introduction starts the player here.</summary>
        public static readonly Vector3 ViewPoint = new Vector3(-1.8f, 1.25f, -6f);

        /// <summary>References into the shared theatre set, for the playable introduction.</summary>
        public struct Environment
        {
            public Transform Root;
            public Transform Table;
            public Transform LabDoor;
        }

        public static void Build(Scene scene)
        {
            var root = BuildEnvironment("MainMenu").Root;

            // Camera rig: the rig only sways, the camera holds the composition.
            // Low and turned a little left: the stage door sits right of centre, the table bottom right.
            var rig = K.Group(root, "CameraRig", ViewPoint).transform;
            var camGo = new GameObject("MenuCamera");
            camGo.transform.SetParent(rig, false);
            camGo.transform.LookAt(new Vector3(-3.83f, -0.27f, 8f));
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();
            K.EnablePostProcessing(cam);

            root.gameObject.AddComponent<MainMenuController>().Configure(rig);
        }

        /// <summary>
        /// The theatre set behind the menu, shared with the playable introduction (VossIntroduction) so
        /// that stepping in from the menu shows exactly the same place.
        /// </summary>
        public static Environment BuildEnvironment(string rootName)
        {
            K.DarkAmbient(new Color(0.03f, 0.022f, 0.02f));
            K.Fog(new Color(0.03f, 0.014f, 0.012f), 0.03f);
            var root = K.Group(null, rootName).transform;
            K.GlobalVolume(root, K.HorrorProfile("Menu", 0.95f, 0.45f, 0.35f, new Color(1f, 0.9f, 0.86f)));

            var house = TheatreBuilder.BuildHouse(root, seats: false);
            house.Chamber.SetActive(false);
            house.Plaque.SetActive(false);

            var door = BuildLabDoor(root);
            BuildInnerArch(root);
            var table = BuildTable(root, new Vector3(-1.22f, 0f, -4.76f));

            // Red wash on the stage front, warm footlights and a faint house light.
            foreach (float x in new[] { -4.5f, 4.5f })
                K.PointLight(root, "StageRed", new Vector3(x, 1.6f, 2.6f), new Color(1f, 0.12f, 0.08f), 2f, 8f);
            for (int i = 0; i < 5; i++)
                K.PointLight(root, "Footlight_" + i, new Vector3(-4f + i * 2f, 1.15f, 2.2f), new Color(1f, 0.6f, 0.35f), 0.7f, 3.5f);
            K.PointLight(root, "House", new Vector3(0f, 7f, -6f), new Color(1f, 0.7f, 0.5f), 0.4f, 18f);
            K.Dust(root, "Dust", new Vector3(0f, 3f, 0f), new Vector3(16f, 6f, 16f), 10f, new Color(1f, 0.9f, 0.8f, 0.2f));

            return new Environment { Root = root, Table = table, LabDoor = door };
        }

        /// <summary>Steel door at the back of the stage, LABORATORY 7314 above it, cyan light and two red lamps.</summary>
        private static Transform BuildLabDoor(Transform root)
        {
            var d = K.Group(root, "LabDoor", new Vector3(0f, 1f, 10.85f)).transform;
            K.Box(d, "Door", new Vector3(0f, 1.5f, 0f), new Vector3(2.2f, 3f, 0.15f), K.Mats.DoorPaint);
            K.Box(d, "Seam", new Vector3(0f, 1.5f, -0.08f), new Vector3(0.02f, 3f, 0.01f), K.Mats.Silhouette, false);
            K.Box(d, "Frame", new Vector3(0f, 1.6f, 0.02f), new Vector3(2.6f, 3.3f, 0.12f), K.Mats.Steel, false);
            var sign = K.Group(d, "Sign", new Vector3(0f, 3.75f, -0.1f)).transform;
            K.Box(sign, "Plate", Vector3.zero, new Vector3(1.9f, 0.75f, 0.05f), K.Mats.Steel, false);
            K.Text3D(sign, "Text", "LABORATORY\n7314", new Vector3(0f, 0f, -0.035f), Vector3.back, 0.24f, new Color(0.06f, 0.12f, 0.13f), TextAnchor.MiddleCenter, FontStyle.Bold);
            K.Box(d, "Tube", new Vector3(0f, 4.5f, -0.25f), new Vector3(1.2f, 0.06f, 0.08f), K.Mats.Tube, false);
            K.PointLight(d, "Cyan", new Vector3(0f, 4.2f, -1.2f), new Color(0.35f, 0.85f, 1f), 4f, 9f);
            K.SpotLight(d, "CyanPool", new Vector3(0f, 4.6f, -1.6f), new Vector3(0f, 0f, -2.2f), new Color(0.35f, 0.85f, 1f), 10f, 7f, 60f);
            foreach (float x in new[] { -1.8f, 1.8f })
            {
                K.Prim(d, "RedLamp", PrimitiveType.Sphere, new Vector3(x, 2.1f, -0.1f), new Vector3(0.12f, 0.16f, 0.12f), K.Mats.SignRed, false);
                var l = K.PointLight(d, "RedLight", new Vector3(x, 2.1f, -0.5f), new Color(1f, 0.1f, 0.06f), 1.2f, 4f);
                l.gameObject.AddComponent<LightFlicker>().Configure(0.15f, new Vector2(3f, 8f), 0.2f);
            }
            // A cage wall of steel bars either side of the door.
            for (int i = 0; i < 12; i++)
            {
                float x = 1.5f + i * 0.25f;
                foreach (float s in new[] { -1f, 1f })
                    K.Box(d, "Bar", new Vector3(s * x, 2f, 0.05f), new Vector3(0.04f, 4f, 0.04f), K.Mats.BlackMetal, false);
            }
            K.Box(d, "Rail", new Vector3(0f, 4f, 0.05f), new Vector3(9f, 0.08f, 0.08f), K.Mats.Rust, false);
            return d;
        }

        /// <summary>An ornate arch on the stage framing the laboratory door: pillars, VOSS THEATRE, red drapes, sconces.</summary>
        private static void BuildInnerArch(Transform root)
        {
            var a = K.Group(root, "InnerArch", new Vector3(0f, 1f, 8.6f)).transform;
            foreach (float x in new[] { -3.5f, 3.5f })
            {
                K.Box(a, "Pillar", new Vector3(x, 2.6f, 0f), new Vector3(0.5f, 5.2f, 0.5f), K.Mats.OldWood);
                K.Box(a, "Capital", new Vector3(x, 5.15f, 0f), new Vector3(0.65f, 0.15f, 0.6f), K.Mats.Brass, false);
                K.Box(a, "Plinth", new Vector3(x, 0.15f, 0f), new Vector3(0.65f, 0.3f, 0.6f), K.Mats.Brass, false);
                foreach (float dy in new[] { 0f, 0.35f })
                {
                    var at = new Vector3(x, 3.3f + dy, -0.3f);
                    K.Box(a, "SconceArm", at + new Vector3(0f, -0.1f, 0.05f), new Vector3(0.04f, 0.2f, 0.12f), K.Mats.Brass, false);
                    K.Prim(a, "SconceGlass", PrimitiveType.Cylinder, at + new Vector3(0f, 0.06f, 0f), new Vector3(0.13f, 0.11f, 0.13f), K.Mats.AmberGlass, false);
                }
                var l = K.PointLight(a, "Sconce", new Vector3(x, 3.5f, -0.7f), new Color(1f, 0.66f, 0.36f), 2f, 7f);
                l.gameObject.AddComponent<LightFlicker>().Configure(0.06f, new Vector2(6f, 14f), 0.1f);
                // Drapes hanging inside the arch, drawn back toward the pillars.
                K.SurfaceBox(a, "Drape", new Vector3(x * 0.8f, 2.6f, 0.15f), new Vector3(0.9f, 5.2f, 0.15f), K.Mats.Curtain, false);
                K.Box(a, "Tieback", new Vector3(x * 0.8f, 1.6f, 0.05f), new Vector3(0.95f, 0.06f, 0.2f), K.Mats.Brass, false);
                K.SpotLight(root, "DrapeRed", new Vector3(x * 0.4f, 6.5f, 4f), new Vector3(x * 0.8f, 2.5f, 8.7f), new Color(1f, 0.12f, 0.08f), 14f, 10f, 30f);
            }
            K.Box(a, "Beam", new Vector3(0f, 5.45f, 0f), new Vector3(7.6f, 0.5f, 0.55f), K.Mats.OldWood);
            K.SurfaceBox(a, "Pelmet", new Vector3(0f, 5.05f, 0.1f), new Vector3(6.5f, 0.45f, 0.15f), K.Mats.Curtain, false);
            var sign = K.Group(a, "TheatreName", new Vector3(0f, 5.5f, -0.3f)).transform;
            K.Box(sign, "Frame", Vector3.zero, new Vector3(4.2f, 0.62f, 0.05f), K.Mats.Brass, false);
            K.Box(sign, "Plate", new Vector3(0f, 0f, -0.02f), new Vector3(4.05f, 0.5f, 0.04f), K.Mats.Navy, false);
            K.Text3D(sign, "Text", "VOSS THEATRE", new Vector3(0f, 0f, -0.05f), Vector3.back, 0.34f, new Color(0.85f, 0.68f, 0.38f), TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        /// <summary>Round table: envelope with its red seal, the ticket, a photograph, an hourglass, a lamp.</summary>
        private static Transform BuildTable(Transform root, Vector3 at)
        {
            var t = K.Group(root, "Table", at).transform;
            t.localRotation = Quaternion.Euler(0f, -8f, 0f); // local x = camera right, z = away from camera
            K.Prim(t, "Top", PrimitiveType.Cylinder, new Vector3(0f, 0.78f, 0f), new Vector3(1.3f, 0.025f, 1.3f), K.Mats.OldWood, false);
            K.Prim(t, "Edge", PrimitiveType.Cylinder, new Vector3(0f, 0.75f, 0f), new Vector3(1.34f, 0.02f, 1.34f), K.Mats.Wood, false);
            K.Prim(t, "Stem", PrimitiveType.Cylinder, new Vector3(0f, 0.38f, 0f), new Vector3(0.12f, 0.36f, 0.12f), K.Mats.OldWood, false);
            const float top = 0.806f;

            // Envelope and seal
            var env = K.Group(t, "Envelope", new Vector3(0f, top, 0.06f)).transform;
            env.localRotation = Quaternion.Euler(0f, -18f, 0f);
            K.Box(env, "Body", Vector3.zero, new Vector3(0.36f, 0.008f, 0.25f), K.Mats.Silhouette, false);
            K.Box(env, "FlapA", new Vector3(-0.09f, 0.005f, 0.03f), new Vector3(0.21f, 0.002f, 0.006f), K.Mats.Silhouette, false).transform.localRotation = Quaternion.Euler(0f, 30f, 0f);
            K.Box(env, "FlapB", new Vector3(0.09f, 0.005f, 0.03f), new Vector3(0.21f, 0.002f, 0.006f), K.Mats.Silhouette, false).transform.localRotation = Quaternion.Euler(0f, -30f, 0f);
            K.Prim(env, "Seal", PrimitiveType.Cylinder, new Vector3(0f, 0.008f, -0.02f), new Vector3(0.075f, 0.006f, 0.075f), K.Mats.RedString, false);
            K.Text3D(env, "SealV", "V", new Vector3(0f, 0.0145f, -0.02f), Vector3.up, 0.04f, new Color(0.3f, 0.02f, 0.02f), TextAnchor.MiddleCenter, FontStyle.Bold, 64, Vector3.forward);

            // Ticket
            var ticket = K.Group(t, "Ticket", new Vector3(0.04f, top + 0.004f, -0.24f)).transform;
            ticket.localRotation = Quaternion.Euler(0f, -6f, 0f);
            K.Box(ticket, "Card", Vector3.zero, new Vector3(0.24f, 0.004f, 0.11f), K.Mats.OldPaper, false);
            K.Text3D(ticket, "Number", "7314", new Vector3(0.02f, 0.003f, -0.005f), Vector3.up, 0.05f, Ink, TextAnchor.MiddleCenter, FontStyle.Bold, 64, Vector3.forward);
            K.Text3D(ticket, "Admit", "ADMIT ONE", new Vector3(0.02f, 0.003f, 0.038f), Vector3.up, 0.012f, Ink, TextAnchor.MiddleCenter, FontStyle.Bold, 64, Vector3.forward);

            // His photograph
            var photo = K.Group(t, "Photograph", new Vector3(-0.3f, top, -0.15f)).transform;
            photo.localRotation = Quaternion.Euler(0f, 22f, 0f);
            K.Box(photo, "Print", Vector3.zero, new Vector3(0.18f, 0.003f, 0.23f), K.Mats.Paper, false);
            K.Box(photo, "Image", new Vector3(0f, 0.002f, 0.01f), new Vector3(0.15f, 0.002f, 0.17f), K.Mats.Photo, false);
            K.Prim(photo, "Head", PrimitiveType.Sphere, new Vector3(0f, 0.004f, 0.03f), new Vector3(0.045f, 0.002f, 0.055f), K.Mats.Silhouette, false);
            K.Box(photo, "Hat", new Vector3(0f, 0.004f, 0.075f), new Vector3(0.042f, 0.002f, 0.045f), K.Mats.Silhouette, false);
            K.Box(photo, "Brim", new Vector3(0f, 0.004f, 0.054f), new Vector3(0.07f, 0.002f, 0.008f), K.Mats.Silhouette, false);
            K.Box(photo, "Shoulders", new Vector3(0f, 0.004f, -0.035f), new Vector3(0.11f, 0.002f, 0.06f), K.Mats.Silhouette, false);

            // Hourglass
            var hg = K.Group(t, "Hourglass", new Vector3(0.45f, top, 0.22f)).transform;
            foreach (float y in new[] { 0.01f, 0.31f })
                K.Prim(hg, "Plate", PrimitiveType.Cylinder, new Vector3(0f, y, 0f), new Vector3(0.17f, 0.012f, 0.17f), K.Mats.Brass, false);
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.PI * 2f / 3f;
                K.Prim(hg, "Post", PrimitiveType.Cylinder, new Vector3(Mathf.Cos(a) * 0.07f, 0.16f, Mathf.Sin(a) * 0.07f), new Vector3(0.012f, 0.15f, 0.012f), K.Mats.Brass, false);
            }
            K.Prim(hg, "BulbTop", PrimitiveType.Sphere, new Vector3(0f, 0.235f, 0f), new Vector3(0.1f, 0.12f, 0.1f), K.Mats.Glass, false);
            K.Prim(hg, "BulbBottom", PrimitiveType.Sphere, new Vector3(0f, 0.085f, 0f), new Vector3(0.1f, 0.12f, 0.1f), K.Mats.Glass, false);
            K.Prim(hg, "SandTop", PrimitiveType.Sphere, new Vector3(0f, 0.215f, 0f), new Vector3(0.06f, 0.04f, 0.06f), K.Mats.OldPaper, false);
            K.Prim(hg, "SandBottom", PrimitiveType.Sphere, new Vector3(0f, 0.05f, 0f), new Vector3(0.08f, 0.05f, 0.08f), K.Mats.OldPaper, false);
            K.Box(hg, "Stream", new Vector3(0f, 0.15f, 0f), new Vector3(0.004f, 0.1f, 0.004f), K.Mats.OldPaper, false);

            // Desk lamp, its pool of light on the envelope
            var lamp = K.Group(t, "DeskLamp", new Vector3(0.55f, top, 0.5f)).transform;
            K.Prim(lamp, "Base", PrimitiveType.Cylinder, new Vector3(0f, 0.015f, 0f), new Vector3(0.18f, 0.015f, 0.18f), K.Mats.BlackMetal, false);
            var arm = K.Box(lamp, "Arm", new Vector3(-0.05f, 0.22f, -0.04f), new Vector3(0.025f, 0.45f, 0.025f), K.Mats.BlackMetal, false);
            arm.transform.localRotation = Quaternion.Euler(-12f, 50f, 0f);
            var arm2 = K.Box(lamp, "Arm2", new Vector3(-0.12f, 0.46f, -0.1f), new Vector3(0.022f, 0.22f, 0.022f), K.Mats.BlackMetal, false);
            arm2.transform.localRotation = Quaternion.Euler(-55f, 50f, 0f);
            var head = K.Group(lamp, "Head", new Vector3(-0.2f, 0.5f, -0.16f)).transform;
            head.localRotation = Quaternion.Euler(28f, -130f, 0f);
            K.Prim(head, "Dome", PrimitiveType.Sphere, new Vector3(0f, 0.03f, 0f), new Vector3(0.25f, 0.14f, 0.25f), K.Mats.BlackMetal, false);
            K.Prim(head, "Rim", PrimitiveType.Cylinder, new Vector3(0f, -0.035f, 0f), new Vector3(0.26f, 0.01f, 0.26f), K.Mats.Brass, false);
            K.Prim(head, "Glow", PrimitiveType.Cylinder, new Vector3(0f, -0.041f, 0f), new Vector3(0.22f, 0.004f, 0.22f), K.Mats.WarmBulb, false);
            K.SpotLight(t, "LampLight", new Vector3(0.32f, top + 0.5f, 0.32f), new Vector3(0f, top, -0.02f), new Color(1f, 0.74f, 0.45f), 5.5f, 3f, 85f);
            return t;
        }
    }
}
