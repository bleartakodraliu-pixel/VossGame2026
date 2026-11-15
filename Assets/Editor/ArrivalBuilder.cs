using UnityEngine;
using UnityEngine.SceneManagement;
using Voss.Atmosphere;
using Voss.Audio;
using Voss.Interaction;
using Voss.Narrative;
using Voss.Player;
using K = Voss.EditorTools.VossBuildKit;

namespace Voss.EditorTools
{
    /// <summary>
    /// 01b_TheatreArrival: the Voss Theatre twenty years on. A small foyer, padded doors that open by
    /// themselves, an empty house lit by a single ghost light, the glass chamber still on stage, and a
    /// caged stage lift that a spotlight finds once the player is inside. Driven by TheatreArrivalSequence.
    /// </summary>
    public static class ArrivalBuilder
    {
        public static void Build(Scene scene)
        {
            K.DarkAmbient(new Color(0.022f, 0.022f, 0.028f));
            K.Fog(new Color(0.012f, 0.012f, 0.016f), 0.035f);
            var root = K.Group(null, "TheatreArrival").transform;
            K.GlobalVolume(root, K.HorrorProfile("Arrival", 0.9f, 0.42f, 0.42f, new Color(0.92f, 0.95f, 1f)));
            K.Group(root, "Soundscape").AddComponent<AmbientSoundscape>().Configure(
                new[] { SfxId.Creak, SfxId.Creak, SfxId.Drip }, new Vector2(10f, 22f), new Vector2(5f, 10f), 1f, true, 0.25f);

            var house = TheatreBuilder.BuildHouse(root, seats: true, lobbyDoorway: true);
            house.Silhouette.SetActive(false); // the chamber has been empty for twenty years

            // The side exit is chained shut.
            K.Box(root, "SideExitClosed", new Vector3(14f, 1.5f, -19f), new Vector3(0.3f, 3f, 2f), K.Mats.BlackMetal);
            K.Box(root, "Chain", new Vector3(13.82f, 1.3f, -19f), new Vector3(0.04f, 0.06f, 1.8f), K.Mats.Steel, false);
            K.PointLight(root, "ExitLight", new Vector3(13.4f, 3f, -19f), new Color(1f, 0.3f, 0.2f), 0.5f, 5f);

            // Foyer behind the auditorium doors.
            K.FloorAndCeiling(root, "Foyer", new Vector2(-3f, -26f), new Vector2(3f, -22f), 3.5f, K.Mats.Wood, K.Mats.Ceiling);
            K.Wall(root, "Foyer_W", false, -3f, -26f, -22f, 3.5f, K.Mats.Wall);
            K.Wall(root, "Foyer_E", false, 3f, -26f, -22f, 3.5f, K.Mats.Wall);
            K.Wall(root, "Foyer_S", true, -26f, -3f, 3f, 3.5f, K.Mats.Wall);
            var poster = K.Group(root, "OldPoster", new Vector3(-2.83f, 1.8f, -24f)).transform;
            K.Box(poster, "Sheet", Vector3.zero, new Vector3(0.02f, 1.4f, 1f), K.Mats.Velvet, false);
            K.Text3D(poster, "Text", "E. VOSS\n\nTHE\nIMPOSSIBLE\nEXIT\n\nONE NIGHT ONLY", new Vector3(0.02f, 0f, 0f), Vector3.right, 0.09f, new Color(0.7f, 0.55f, 0.3f), TextAnchor.MiddleCenter, FontStyle.Bold, 48);
            foreach (float x in new[] { -2.85f, 2.85f })
            {
                var sconce = K.PointLight(root, "FoyerSconce", new Vector3(x * 0.95f, 2.6f, -22.8f), new Color(1f, 0.7f, 0.42f), 0.7f, 5f);
                sconce.gameObject.AddComponent<LightFlicker>().Configure(0.1f, new Vector2(4f, 10f), 0.3f);
                K.Prim(root, "SconceGlass", PrimitiveType.Sphere, new Vector3(x, 2.6f, -22.8f), Vector3.one * 0.14f, K.Mats.AmberGlass, false);
            }

            // Padded double doors, hinged at the jambs; they open inward by themselves.
            var left = DoorLeaf(root, "DoorLeft", new Vector3(-1.2f, 0f, -22f), 1f);
            var right = DoorLeaf(root, "DoorRight", new Vector3(1.2f, 0f, -22f), -1f);

            // A ghost light: the single bulb theatres leave burning on an empty stage.
            var ghost = K.Group(root, "GhostLight", new Vector3(-2.4f, 1f, 3.6f)).transform;
            K.Prim(ghost, "Base", PrimitiveType.Cylinder, new Vector3(0f, 0.03f, 0f), new Vector3(0.4f, 0.03f, 0.4f), K.Mats.BlackMetal);
            K.Prim(ghost, "Pole", PrimitiveType.Cylinder, new Vector3(0f, 0.8f, 0f), new Vector3(0.04f, 0.8f, 0.04f), K.Mats.Steel, false);
            K.Prim(ghost, "Bulb", PrimitiveType.Sphere, new Vector3(0f, 1.68f, 0f), Vector3.one * 0.12f, K.Mats.WarmBulb, false);
            K.Box(ghost, "CageA", new Vector3(0f, 1.68f, 0f), new Vector3(0.16f, 0.2f, 0.01f), K.Mats.BlackMetal, false);
            K.Box(ghost, "CageB", new Vector3(0f, 1.68f, 0f), new Vector3(0.01f, 0.2f, 0.16f), K.Mats.BlackMetal, false);
            var ghostLight = K.PointLight(ghost, "Light", new Vector3(0f, 1.68f, 0f), new Color(1f, 0.8f, 0.55f), 1.1f, 10f, true);
            ghostLight.gameObject.AddComponent<LightFlicker>().Configure(0.06f, new Vector2(8f, 16f), 0.15f);
            K.PointLight(root, "Moonlight", new Vector3(0f, 9f, -9f), new Color(0.55f, 0.65f, 1f), 0.35f, 22f);

            // The hidden stage lift.
            var car = BuildLift(root, new Vector3(5.5f, 1f, 6.5f), out var gate, out var liftZone);
            var spot = K.SpotLight(root, "LiftSpot", new Vector3(5.5f, 8.2f, 1.6f), new Vector3(5.5f, 1.6f, 6.5f), new Color(0.88f, 0.94f, 1f), 26f, 14f, 26f);

            K.Dust(root, "Dust", new Vector3(0f, 4f, -5f), new Vector3(24f, 7f, 24f), 20f, new Color(0.9f, 0.9f, 0.95f, 0.2f));

            var inside = K.TriggerBox(root, "InsideZone", new Vector3(0f, 1.2f, -19.6f), new Vector3(6f, 2.4f, 1.6f)).gameObject.AddComponent<PlayerTriggerZone>();
            var steps = K.Group(root, "FootstepsOnStage", new Vector3(-5f, 1.6f, 7f));

            var player = K.Player(root, new Vector3(0f, 0f, -24.6f), 0f);
            root.gameObject.AddComponent<TheatreArrivalSequence>().Configure(left, right, inside, liftZone, spot, steps.transform,
                car, gate, new Vector3(-1.85f, 0f, 0f), player.GetComponent<FirstPersonController>());
            root.gameObject.AddComponent<GameRestartController>();
        }

        /// <summary>One padded leaf; side +1 means it extends toward +x from its hinge.</summary>
        private static Transform DoorLeaf(Transform parent, string name, Vector3 hinge, float side)
        {
            var pivot = K.Group(parent, name, hinge).transform;
            K.Box(pivot, "Leaf", new Vector3(side * 0.6f, 1.6f, 0f), new Vector3(1.2f, 3.2f, 0.08f), K.Mats.Velvet);
            for (int i = 0; i < 3; i++)
                K.Box(pivot, "Button", new Vector3(side * 0.6f, 0.8f + i * 0.8f, -0.05f), new Vector3(0.9f, 0.03f, 0.02f), K.Mats.Brass, false);
            K.Box(pivot, "PushPlate", new Vector3(side * 1.0f, 1.2f, -0.05f), new Vector3(0.1f, 0.4f, 0.02f), K.Mats.Brass, false);
            K.Prim(pivot, "Porthole", PrimitiveType.Cylinder, new Vector3(side * 0.6f, 2.3f, 0f), new Vector3(0.32f, 0.045f, 0.32f), K.Mats.DarkGlass, false)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            return pivot;
        }

        /// <summary>Open-sided cage lift standing on the stage; its gate starts slid open beside it.</summary>
        private static Transform BuildLift(Transform parent, Vector3 at, out Transform gate, out PlayerTriggerZone zone)
        {
            var car = K.Group(parent, "StageLift", at).transform;
            K.Box(car, "Floor", new Vector3(0f, 0.04f, 0f), new Vector3(2f, 0.08f, 2f), K.Mats.Steel);
            foreach (float x in new[] { -0.96f, 0.96f })
                foreach (float z in new[] { -0.96f, 0.96f })
                    K.Box(car, "Post", new Vector3(x, 1.35f, z), new Vector3(0.08f, 2.7f, 0.08f), K.Mats.Rust);
            for (int i = 0; i < 12; i++)
            {
                float u = -0.88f + i * 0.16f;
                K.Box(car, "BarBack", new Vector3(u, 1.35f, 0.96f), new Vector3(0.025f, 2.6f, 0.025f), K.Mats.BlackMetal, false);
                K.Box(car, "BarL", new Vector3(-0.96f, 1.35f, u), new Vector3(0.025f, 2.6f, 0.025f), K.Mats.BlackMetal, false);
                K.Box(car, "BarR", new Vector3(0.96f, 1.35f, u), new Vector3(0.025f, 2.6f, 0.025f), K.Mats.BlackMetal, false);
            }
            foreach (float y in new[] { 0.1f, 1.1f, 2.65f })
            {
                K.Box(car, "RailBack", new Vector3(0f, y, 0.96f), new Vector3(2f, 0.05f, 0.05f), K.Mats.Rust, false);
                K.Box(car, "RailL", new Vector3(-0.96f, y, 0f), new Vector3(0.05f, 0.05f, 2f), K.Mats.Rust, false);
                K.Box(car, "RailR", new Vector3(0.96f, y, 0f), new Vector3(0.05f, 0.05f, 2f), K.Mats.Rust, false);
            }
            // Invisible walls so nobody squeezes between the bars.
            foreach (var (c, s) in new[] { (new Vector3(-0.97f, 1.35f, 0f), new Vector3(0.05f, 2.6f, 2f)), (new Vector3(0.97f, 1.35f, 0f), new Vector3(0.05f, 2.6f, 2f)), (new Vector3(0f, 1.35f, 0.97f), new Vector3(2f, 2.6f, 0.05f)) })
                K.Group(car, "CageWall", c).AddComponent<BoxCollider>().size = s;
            K.Box(car, "Roof", new Vector3(0f, 2.75f, 0f), new Vector3(2.1f, 0.1f, 2.1f), K.Mats.Rust, false);
            foreach (float x in new[] { -0.5f, 0.5f })
                K.Box(car, "Cable", new Vector3(x, 6.2f, 0f), new Vector3(0.03f, 6.8f, 0.03f), K.Mats.Steel, false);
            var plate = K.Group(car, "LiftPlate", new Vector3(0f, 2.62f, -1.0f)).transform;
            K.Box(plate, "Plate", Vector3.zero, new Vector3(0.9f, 0.18f, 0.03f), K.Mats.Brass, false);
            K.Text3D(plate, "Text", "STAGE LIFT", new Vector3(0f, 0f, -0.02f), Vector3.back, 0.1f, new Color(0.08f, 0.06f, 0.04f), TextAnchor.MiddleCenter, FontStyle.Bold, 48);
            K.Box(car, "ControlBox", new Vector3(-0.8f, 1.3f, 0.75f), new Vector3(0.18f, 0.3f, 0.12f), K.Mats.DoorPaint, false);
            K.Prim(car, "CallLamp", PrimitiveType.Sphere, new Vector3(-0.8f, 1.5f, 0.68f), Vector3.one * 0.06f, K.Mats.SignRed, false);

            // Front gate, starting slid out to the right of the cage.
            gate = K.Group(car, "Gate", new Vector3(1.85f, 0f, -0.98f)).transform;
            for (int i = 0; i < 12; i++)
                K.Box(gate, "Bar", new Vector3(-0.88f + i * 0.16f, 1.3f, 0f), new Vector3(0.025f, 2.5f, 0.025f), K.Mats.BlackMetal, false);
            foreach (float y in new[] { 0.1f, 1.3f, 2.5f })
                K.Box(gate, "Rail", new Vector3(0f, y, 0f), new Vector3(1.9f, 0.05f, 0.04f), K.Mats.Rust, false);

            zone = K.TriggerBox(car, "LiftZone", new Vector3(0f, 1.2f, 0.1f), new Vector3(1.4f, 2.2f, 1.4f)).gameObject.AddComponent<PlayerTriggerZone>();
            return car;
        }
    }
}
