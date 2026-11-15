using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Voss.Atmosphere;
using Voss.Audio;
using Voss.Narrative;
using Voss.Player;
using K = Voss.EditorTools.VossBuildKit;

namespace Voss.EditorTools
{
    /// <summary>
    /// VossIntroduction: the theatre behind the main menu, made playable. Built from the very same set
    /// (MainMenuBuilder.BuildEnvironment), then given a first-person player standing where the menu
    /// camera stood, an envelope that opens, a laboratory door that only opens once the letter is taken,
    /// guidance lights that wake up afterwards, and a little more life in the dark.
    /// </summary>
    public static class IntroductionBuilder
    {
        private static readonly Color Red = new Color(1f, 0.1f, 0.06f);

        public static void Build(Scene scene)
        {
            var env = MainMenuBuilder.BuildEnvironment("VossIntroduction");
            var root = env.Root;

            // Here the laboratory door really leads somewhere: open the back wall behind it.
            var north = root.Find("Wall_North");
            if (north != null) Object.DestroyImmediate(north.gameObject);
            K.Wall(root, "Wall_North", true, 12f, -14f, 14f, 10f, K.Mats.Velvet, 0f, 2.4f, 4.15f);

            // The set has no vestibule behind the side exit: keep it shut.
            K.Box(root, "SideExitClosed", new Vector3(14f, 1.5f, -19f), new Vector3(0.3f, 3f, 2f), K.Mats.BlackMetal);
            K.PointLight(root, "ExitLight", new Vector3(13.4f, 3f, -19f), new Color(1f, 0.3f, 0.2f), 0.4f, 4f);

            // Player where the menu camera stood, eyes on the table.
            var view = MainMenuBuilder.ViewPoint;
            var interactor = K.Player(root, new Vector3(view.x, 0f, view.z), 18f);
            interactor.GetComponentInChildren<Camera>().transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
            var player = interactor.GetComponent<FirstPersonController>();

            var manager = root.gameObject.AddComponent<IntroductionManager>();

            // The table is solid here.
            var tableCol = K.Group(env.Table, "TableCollider", new Vector3(0f, 0.4f, 0f)).AddComponent<BoxCollider>();
            tableCol.size = new Vector3(1.3f, 0.8f, 1.3f);

            BuildEnvelope(env.Table, manager);
            BuildDoor(env.LabDoor, manager);

            // Guidance, dark until the letter is taken: a red lamp above the door, a light on the stairs, the hum.
            var alarm = K.Group(env.LabDoor, "DoorAlarm", new Vector3(0f, 4.95f, -0.15f));
            K.Box(alarm.transform, "Lamp", Vector3.zero, new Vector3(0.3f, 0.12f, 0.08f), K.Mats.SignRed, false);
            var alarmLight = K.PointLight(env.LabDoor, "DoorAlarmLight", new Vector3(0f, 4.8f, -0.6f), Red, 1.8f, 7f);
            alarmLight.gameObject.AddComponent<LightFlicker>().Configure(0.15f, new Vector2(4f, 9f), 0.25f);
            var stairs = K.SpotLight(root, "StairLight", new Vector3(0f, 6.2f, -2.2f), new Vector3(0f, 0.6f, 1.2f), new Color(1f, 0.78f, 0.5f), 9f, 10f, 45f);
            var hum = K.Group(env.LabDoor, "DoorHum", new Vector3(0f, 2.5f, -0.2f));
            hum.AddComponent<LoopingSound>().Configure(SfxId.Static, 0.35f, 8f);

            // A little more life in the dark: unreliable red lamps, steps and whispers somewhere in the house.
            foreach (Transform child in root)
                if (child.name == "StageRed") child.gameObject.AddComponent<LightFlicker>().Configure(0.2f, new Vector2(5f, 12f), 0.5f);
            K.Group(root, "Soundscape").AddComponent<AmbientSoundscape>().Configure(
                new[] { SfxId.DistantSteps, SfxId.Creak, SfxId.Creak, SfxId.Whisper, SfxId.Drip }, new Vector2(10f, 20f), new Vector2(5f, 10f), 1f, true, 0.25f);

            manager.Configure(player, new List<Light> { alarmLight, stairs }, new List<GameObject> { alarm, hum });
            root.gameObject.AddComponent<GameRestartController>();
        }

        /// <summary>Turns the menu's envelope into one that opens: a hinged flap carrying the seal, a letter and the ticket inside.</summary>
        private static void BuildEnvelope(Transform table, IntroductionManager manager)
        {
            var envelope = table.Find("Envelope");
            var ticket = table.Find("Ticket");
            envelope.Find("FlapA").gameObject.SetActive(false);
            envelope.Find("FlapB").gameObject.SetActive(false);

            var flap = K.Group(envelope, "FlapPivot", new Vector3(0f, 0.0045f, 0.125f)).transform;
            K.Box(flap, "Flap", new Vector3(0f, 0f, -0.07f), new Vector3(0.36f, 0.003f, 0.14f), K.Mats.Silhouette, false);
            var seal = envelope.Find("Seal");
            seal.SetParent(flap, true);
            envelope.Find("SealV").SetParent(flap, true);

            var letter = K.Group(envelope, "Letter", new Vector3(0f, 0.002f, 0f)).transform;
            K.Box(letter, "Sheet", Vector3.zero, new Vector3(0.3f, 0.003f, 0.2f), K.Mats.OldPaper, false);
            for (int i = 0; i < 6; i++)
                K.Box(letter, "Ink", new Vector3(-0.02f, 0.002f, 0.07f - i * 0.025f), new Vector3(0.2f - (i % 3) * 0.03f, 0.001f, 0.004f), K.Mats.Leather, false);

            var col = envelope.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.02f, 0f);
            col.size = new Vector3(0.42f, 0.06f, 0.32f);
            var glow = K.PointLight(envelope, "InspectGlow", new Vector3(0f, 0.18f, -0.05f), new Color(1f, 0.75f, 0.45f), 0f, 1.2f);

            envelope.gameObject.AddComponent<EnvelopeInteraction>().Configure(manager, flap, letter, ticket,
                envelope.Find("Body").GetComponent<Renderer>(), seal.GetComponent<Renderer>(), glow);
        }

        /// <summary>
        /// Two sliding leaves in place of the menu's single slab. The menu's solid steel frame becomes an
        /// open one (posts and lintel), and behind it lies the passage to the laboratory's Main Hall.
        /// </summary>
        private static LaboratoryEntrance BuildDoor(Transform door, IntroductionManager manager)
        {
            door.Find("Door").gameObject.SetActive(false);
            door.Find("Seam").gameObject.SetActive(false);
            door.Find("Frame").gameObject.SetActive(false);
            K.Box(door, "FramePost_L", new Vector3(-1.2f, 1.6f, 0.02f), new Vector3(0.2f, 3.3f, 0.12f), K.Mats.Steel, false);
            K.Box(door, "FramePost_R", new Vector3(1.2f, 1.6f, 0.02f), new Vector3(0.2f, 3.3f, 0.12f), K.Mats.Steel, false);
            K.Box(door, "FrameLintel", new Vector3(0f, 3.1f, 0.02f), new Vector3(2.6f, 0.24f, 0.12f), K.Mats.Steel, false);
            var left = Leaf(door, "LeafLeft", -0.55f);
            var right = Leaf(door, "LeafRight", 0.55f);
            var preview = BuildLabPreview(door);

            var entrance = door.gameObject.AddComponent<LaboratoryEntrance>();
            entrance.Configure(manager, left, right, preview);
            return entrance;
        }

        /// <summary>
        /// What the open door shows: a short dark passage and, beyond it, the laboratory's Main Hall as the
        /// player will find it - three chamber doors, PERCEPTION's lamp warm, the other two red. Inactive
        /// (and costing nothing) until the door opens; its lights sit deep in the hall or point into it, so
        /// none of them reaches back into the theatre. Door frame: floor y 0, +z behind the door.
        /// </summary>
        private static GameObject BuildLabPreview(Transform door)
        {
            var p = K.Group(door, "LabPreview").transform;

            // The passage through the theatre wall.
            K.FloorAndCeiling(p, "Passage", new Vector2(-1.2f, 0.08f), new Vector2(1.2f, 2.6f), 3.1f, K.Mats.BlackMetal, K.Mats.BlackMetal);
            K.Wall(p, "Passage_W", false, -1.2f, 0.08f, 2.6f, 3.1f, K.Mats.BlackMetal);
            K.Wall(p, "Passage_E", false, 1.2f, 0.08f, 2.6f, 3.1f, K.Mats.BlackMetal);

            // The Main Hall.
            const float h = 5f, x0 = -9f, x1 = 9f, z0 = 2.6f, z1 = 13.6f;
            var brass = new Color(0.78f, 0.62f, 0.32f);
            K.FloorAndCeiling(p, "Hall", new Vector2(x0, z0), new Vector2(x1, z1), h, K.Mats.WetFloor, K.Mats.ConcreteCeiling);
            K.Wall(p, "Hall_S", true, z0, x0, x1, h, K.Mats.Cracked, 0f, 2.4f, 3.1f);
            K.Wall(p, "Hall_W", false, x0, z0, z1, h, K.Mats.Cracked);
            K.Wall(p, "Hall_E", false, x1, z0, z1, h, K.Mats.Cracked);
            var doorsX = new[] { -6f, 0f, 6f };
            var names = new[] { "PERCEPTION", "REASON", "MEMORY" };
            K.WallWithGaps(p, "Hall_N", true, z1, x0, x1, h, K.Mats.Cracked,
                new Vector3(doorsX[0], 2f, 3f), new Vector3(doorsX[1], 2f, 3f), new Vector3(doorsX[2], 2f, 3f));
            for (int i = 0; i < 3; i++)
            {
                float x = doorsX[i];
                K.Box(p, "Door_" + names[i], new Vector3(x, 1.5f, z1), new Vector3(2f, 3f, 0.25f), K.Mats.DoorPaint, false);
                K.Box(p, "Window", new Vector3(x + 0.45f, 2.05f, z1 - 0.01f), new Vector3(0.42f, 0.55f, 0.26f), K.Mats.DarkGlass, false);
                K.Box(p, "KickPlate", new Vector3(x, 0.2f, z1 - 0.01f), new Vector3(1.9f, 0.32f, 0.26f), K.Mats.Steel, false);
                K.Box(p, "Frame_L", new Vector3(x - 1.1f, 1.6f, z1), new Vector3(0.2f, 3.2f, 0.4f), K.Mats.Steel, false);
                K.Box(p, "Frame_R", new Vector3(x + 1.1f, 1.6f, z1), new Vector3(0.2f, 3.2f, 0.4f), K.Mats.Steel, false);
                K.Text3D(p, "Label_" + names[i], names[i], new Vector3(x, 3.75f, z1 - 0.2f), Vector3.back, 0.4f, brass, TextAnchor.MiddleCenter, FontStyle.Bold);
                bool open = i == 0;
                K.Prim(p, "StatusLamp", PrimitiveType.Sphere, new Vector3(x + 1.45f, 2.85f, z1 - 0.2f), Vector3.one * 0.12f, open ? K.Mats.WarmBulb : K.Mats.SignRed, false);
                K.PointLight(p, "StatusLight", new Vector3(x + 1.45f, 2.85f, z1 - 0.5f), open ? new Color(1f, 0.72f, 0.38f) : new Color(1f, 0.1f, 0.06f), 0.9f, 3f);
            }

            // Columns, beams, fluorescent fittings, red cage lamps.
            foreach (float x in new[] { -3f, 3f, -8.6f, 8.6f })
                K.SurfaceBox(p, "Column", new Vector3(x, h * 0.5f, z1 - 0.25f), new Vector3(0.4f, h, 0.4f), K.Mats.Cracked, false);
            foreach (float z in new[] { 5.5f, 9.5f, 12.8f })
                K.SurfaceBox(p, "Beam", new Vector3(0f, 4.75f, z), new Vector3(18f, 0.5f, 0.45f), K.Mats.Cracked, false);
            foreach (var at in new[] { new Vector3(-4f, 4.3f, 7.5f), new Vector3(4f, 4.3f, 7.5f), new Vector3(0f, 4.3f, 11f) })
            {
                K.Box(p, "Fitting", at, new Vector3(1.4f, 0.08f, 0.35f), K.Mats.Steel, false);
                K.Box(p, "Tube", at + Vector3.down * 0.05f, new Vector3(1.3f, 0.03f, 0.25f), K.Mats.Tube, false);
            }
            foreach (float x in new[] { -3f, 3f })
            {
                K.Prim(p, "RedCageLamp", PrimitiveType.Sphere, new Vector3(x, 2.7f, z1 - 0.5f), new Vector3(0.11f, 0.14f, 0.11f), K.Mats.SignRed, false);
                K.PointLight(p, "RedLamp", new Vector3(x, 2.7f, z1 - 0.9f), new Color(1f, 0.12f, 0.08f), 1.4f, 5f);
            }

            // Cold hall light, deep enough in the hall not to reach the theatre, and a key light aimed into it.
            foreach (float x in new[] { -4f, 4f })
                K.PointLight(p, "HallLight", new Vector3(x, 4.2f, 9.5f), new Color(0.55f, 0.7f, 1f), 1.4f, 7.5f);
            var key = K.SpotLight(p, "HallKey", new Vector3(0f, 4.6f, 3.2f), new Vector3(0f, 0.5f, z1), new Color(0.55f, 0.7f, 1f), 9f, 16f, 80f);
            key.shadows = LightShadows.None;

            p.gameObject.SetActive(false);
            return p.gameObject;
        }

        private static Transform Leaf(Transform door, string name, float x)
        {
            var leaf = K.Group(door, name, new Vector3(x, 0f, 0f)).transform;
            K.Box(leaf, "Slab", new Vector3(0f, 1.5f, 0f), new Vector3(1.1f, 3f, 0.15f), K.Mats.DoorPaint);
            K.Box(leaf, "Window", new Vector3(0f, 2.15f, 0f), new Vector3(0.36f, 0.5f, 0.17f), K.Mats.DarkGlass, false);
            K.Box(leaf, "KickPlate", new Vector3(0f, 0.2f, 0f), new Vector3(1.05f, 0.3f, 0.17f), K.Mats.Steel, false);
            K.Box(leaf, "Handle", new Vector3(-Mathf.Sign(x) * 0.42f, 1.2f, -0.1f), new Vector3(0.04f, 0.35f, 0.04f), K.Mats.Steel, false);
            return leaf;
        }
    }
}
