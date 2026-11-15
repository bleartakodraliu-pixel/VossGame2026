using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Voss.Atmosphere;
using Voss.Audio;
using Voss.Core;
using Voss.Interaction;
using Voss.Narrative;
using Voss.Player;
using K = Voss.EditorTools.VossBuildKit;

namespace Voss.EditorTools
{
    /// <summary>03_EmptyTheatre: rows of empty seats, curtains, the original glass chamber, a single projector.</summary>
    public static class TheatreBuilder
    {
        private const float H = 10f;

        public static void Build(Scene scene)
        {
            K.DarkAmbient(new Color(0.025f, 0.022f, 0.02f));
            K.Fog(new Color(0.02f, 0.018f, 0.016f), 0.03f);
            var root = K.Group(null, "EmptyTheatre").transform;
            K.GlobalVolume(root, K.HorrorProfile("Theatre", 0.8f, 0.38f, 0.35f, new Color(1f, 0.95f, 0.88f)));
            K.Group(root, "Soundscape").AddComponent<AmbientSoundscape>().Configure(
                new[] { SfxId.Creak, SfxId.Creak, SfxId.DistantSteps }, new Vector2(12f, 25f), new Vector2(5f, 10f), 1f, true, 0.25f);

            var house = BuildHouse(root);
            var sil = house.Silhouette;

            // Projector on a stand at the back of the house
            var proj = K.Group(root, "Projector", new Vector3(0f, 0f, -19.5f));
            K.Box(proj.transform, "Stand", new Vector3(0f, 0.75f, 0f), new Vector3(0.3f, 1.5f, 0.3f), K.Mats.BlackMetal);
            K.Box(proj.transform, "Body", new Vector3(0f, 1.7f, 0f), new Vector3(0.6f, 0.4f, 0.8f), K.Mats.BlackMetal);
            K.Prim(proj.transform, "Lens", PrimitiveType.Cylinder, new Vector3(0f, 1.7f, 0.45f), new Vector3(0.2f, 0.06f, 0.2f), K.Mats.Brass, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var projectorLight = K.SpotLight(proj.transform, "ProjectorBeam", new Vector3(0f, 1.75f, 0.5f), new Vector3(0f, 3.5f, 11.4f), new Color(0.9f, 0.95f, 1f), 40f, 40f, 22f);
            var screenText = K.Text3D(root, "ProjectionScreen", "", new Vector3(0f, 5.2f, 11.6f), Vector3.back, 0.5f, new Color(0.9f, 0.9f, 0.95f), TextAnchor.MiddleCenter, FontStyle.Bold);
            screenText.text = string.Empty;
            var completeSign = K.Text3D(root, "CompleteSign", "", new Vector3(0f, 8.9f, 1.9f), Vector3.back, 0.45f, new Color(0.78f, 0.62f, 0.32f), TextAnchor.MiddleCenter, FontStyle.Bold);

            // Exit door on the east wall
            var exit = K.Group(root, "TheatreExit", new Vector3(14f, 0f, -19f));
            var panel = K.Box(exit.transform, "Panel", new Vector3(0f, 1.5f, 0f), new Vector3(0.25f, 3f, 2f), K.Mats.BlackMetal);
            K.Box(exit.transform, "Frame_A", new Vector3(0f, 1.6f, -1.1f), new Vector3(0.4f, 3.2f, 0.2f), K.Mats.Brass);
            K.Box(exit.transform, "Frame_B", new Vector3(0f, 1.6f, 1.1f), new Vector3(0.4f, 3.2f, 0.2f), K.Mats.Brass);
            var exitDoor = exit.AddComponent<DoorController>();
            exitDoor.Configure(panel.transform, GameFlag.None, "The theatre is not finished with you.", false, GameFlag.None, true, "THEATRE EXIT");
            var exitSign = K.Group(exit.transform, "ExitSign", new Vector3(-0.3f, 3.5f, 0f));
            K.Box(exitSign.transform, "SignBody", Vector3.zero, new Vector3(0.08f, 0.3f, 0.9f), K.Mats.SignRed, false);
            K.Text3D(exitSign.transform, "Text", "EXIT", new Vector3(-0.05f, 0f, 0f), Vector3.left, 0.1f, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, 48);
            K.FloorAndCeiling(root, "ExitVestibule", new Vector2(14f, -20f), new Vector2(17f, -18f), 3.2f, K.Mats.Floor, K.Mats.Ceiling);
            K.Wall(root, "ExitVestibule_N", true, -18f, 14f, 17f, 3.2f, K.Mats.Wall);
            K.Wall(root, "ExitVestibule_S", true, -20f, 14f, 17f, 3.2f, K.Mats.Wall);
            K.Wall(root, "ExitVestibule_E", false, 17f, -20f, -18f, 3.2f, K.Mats.Wall);

            // Lights
            var stageLights = new List<Light>
            {
                K.SpotLight(root, "StageSpot_C", new Vector3(0f, 9.5f, -2f), new Vector3(0f, 1.5f, 6f), new Color(1f, 0.93f, 0.8f), 35f, 25f, 40f),
                K.SpotLight(root, "StageSpot_L", new Vector3(-6f, 9.5f, -1f), new Vector3(-3f, 1.5f, 7f), new Color(1f, 0.9f, 0.75f), 18f, 25f, 45f),
                K.SpotLight(root, "StageSpot_R", new Vector3(6f, 9.5f, -1f), new Vector3(3f, 1.5f, 7f), new Color(1f, 0.9f, 0.75f), 18f, 25f, 45f),
                K.PointLight(root, "ChamberGlow", new Vector3(0f, 3.2f, 6f), new Color(0.75f, 0.85f, 1f), 1.2f, 5f),
            };
            K.PointLight(root, "HouseLight_A", new Vector3(-8f, 6f, -10f), new Color(1f, 0.8f, 0.6f), 0.45f, 16f);
            K.PointLight(root, "HouseLight_B", new Vector3(8f, 6f, -10f), new Color(1f, 0.8f, 0.6f), 0.45f, 16f);
            K.PointLight(root, "HouseLight_Rear", new Vector3(0f, 4f, -19f), new Color(0.6f, 0.7f, 0.9f), 0.6f, 10f);
            K.PointLight(root, "ExitLight", new Vector3(13.2f, 3f, -19f), new Color(1f, 0.3f, 0.2f), 0.5f, 5f);

            BuildDust(root);

            // Zones
            var approach = K.TriggerBox(root, "ApproachZone", new Vector3(0f, 1.2f, -8f), new Vector3(8f, 2.4f, 2f)).gameObject.AddComponent<PlayerTriggerZone>();
            var chamberZone = K.TriggerBox(root, "ChamberZone", new Vector3(0f, 2.2f, 3.4f), new Vector3(6f, 2.4f, 1.6f)).gameObject.AddComponent<PlayerTriggerZone>();
            var exitZone = K.TriggerBox(root, "ExitZone", new Vector3(16f, 1.2f, -19f), new Vector3(1.4f, 2.4f, 1.8f)).gameObject.AddComponent<PlayerTriggerZone>();

            // Player
            var player = K.Player(root, new Vector3(0f, 0f, -20.4f), 0f);

            var ctrl = root.gameObject.AddComponent<TheatreEndingController>();
            ctrl.Configure(player.GetComponent<FirstPersonController>(), approach, chamberZone, exitZone, stageLights,
                sil, projectorLight, exitSign, exitDoor, completeSign);
            root.gameObject.AddComponent<GameRestartController>();
        }

        public struct House
        {
            public GameObject Chamber;
            public GameObject Silhouette;
            public GameObject Plaque;
        }

        /// <summary>
        /// The Voss Theatre auditorium, shared by the vanishing (prologue), the arrival and the ending:
        /// shell, stage with stairs, curtains and proscenium sign, the glass chamber and, optionally, seats.
        /// With lobbyDoorway the back wall has an opening onto the foyer instead of the laboratory door.
        /// </summary>
        public static House BuildHouse(Transform root, bool seats = true, bool lobbyDoorway = false)
        {
            var house = new House();
            // Shell
            K.FloorAndCeiling(root, "House", new Vector2(-14f, -22f), new Vector2(14f, 12f), H, K.Mats.Wood, K.Mats.Ceiling);
            if (lobbyDoorway) K.Wall(root, "Wall_South", true, -22f, -14f, 14f, H, K.Mats.Wall, 0f, 2.4f, 3.2f);
            else K.Wall(root, "Wall_South", true, -22f, -14f, 14f, H, K.Mats.Wall);
            K.Wall(root, "Wall_North", true, 12f, -14f, 14f, H, K.Mats.Velvet);
            K.Wall(root, "Wall_West", false, -14f, -22f, 12f, H, K.Mats.Wall);
            K.Wall(root, "Wall_East", false, 14f, -22f, 12f, H, K.Mats.Wall, -19f, 2f, 3f);
            if (!lobbyDoorway)
            {
                K.Box(root, "LabDoorBehind", new Vector3(0f, 1.5f, -21.8f), new Vector3(2f, 3f, 0.3f), K.Mats.BlackMetal);
                K.Text3D(root, "LabDoorLabel", "VOSS LABORATORY", new Vector3(0f, 3.4f, -21.6f), Vector3.forward, 0.25f, new Color(0.78f, 0.62f, 0.32f), TextAnchor.MiddleCenter, FontStyle.Bold);
            }

            // Stage, with stairs of 20 cm risers so it can be climbed on foot
            K.Box(root, "Stage", new Vector3(0f, 0.5f, 6.5f), new Vector3(20f, 1f, 9f), K.Mats.Wood);
            for (int i = 0; i < 5; i++)
            {
                float top = 0.2f * (i + 1);
                K.Box(root, "StageStep", new Vector3(0f, top * 0.5f, 2f - 0.15f - (4 - i) * 0.3f), new Vector3(4f, top, 0.3f), K.Mats.Wood);
            }
            K.Box(root, "Curtain_L", new Vector3(-9.6f, 5.5f, 10f), new Vector3(1.6f, 9f, 0.7f), K.Mats.Velvet);
            K.Box(root, "Curtain_R", new Vector3(9.6f, 5.5f, 10f), new Vector3(1.6f, 9f, 0.7f), K.Mats.Velvet);
            K.Box(root, "Curtain_Top", new Vector3(0f, 9f, 2.2f), new Vector3(22f, 2f, 0.5f), K.Mats.Velvet);
            K.Box(root, "ProsceniumTrim", new Vector3(0f, 7.95f, 2.2f), new Vector3(22f, 0.1f, 0.55f), K.Mats.Brass, false);
            // Velvet house curtains drawn back at the proscenium, and the theatre's name above the stage.
            K.SurfaceBox(root, "HouseCurtain_L", new Vector3(-8.4f, 4f, 2.35f), new Vector3(2.6f, 7.9f, 0.25f), K.Mats.Curtain, false);
            K.SurfaceBox(root, "HouseCurtain_R", new Vector3(8.4f, 4f, 2.35f), new Vector3(2.6f, 7.9f, 0.25f), K.Mats.Curtain, false);
            var sign = K.Group(root, "TheatreSign", new Vector3(0f, 8.35f, 1.92f)).transform;
            K.Box(sign, "Plate", Vector3.zero, new Vector3(5.2f, 0.55f, 0.06f), K.Mats.Silhouette, false);
            K.Box(sign, "Frame", new Vector3(0f, 0f, 0.005f), new Vector3(5.35f, 0.66f, 0.05f), K.Mats.Brass, false);
            K.Text3D(sign, "Text", "VOSS THEATRE", new Vector3(0f, 0f, -0.04f), Vector3.back, 0.36f, new Color(0.82f, 0.66f, 0.36f), TextAnchor.MiddleCenter, FontStyle.Bold);

            // The original chamber
            var chamber = K.Group(root, "GlassChamber", new Vector3(0f, 1f, 6f));
            K.Box(chamber.transform, "Glass", new Vector3(0f, 1.2f, 0f), new Vector3(1.6f, 2.4f, 1.6f), K.Mats.Glass, false);
            K.Box(chamber.transform, "Top", new Vector3(0f, 2.42f, 0f), new Vector3(1.7f, 0.06f, 1.7f), K.Mats.Brass);
            K.Box(chamber.transform, "Base", new Vector3(0f, 0.03f, 0f), new Vector3(1.8f, 0.06f, 1.8f), K.Mats.Brass);
            for (int c = 0; c < 4; c++)
            {
                float sx = (c % 2 == 0) ? -0.8f : 0.8f;
                float sz = (c < 2) ? -0.8f : 0.8f;
                K.Box(chamber.transform, "Post", new Vector3(sx, 1.2f, sz), new Vector3(0.06f, 2.4f, 0.06f), K.Mats.Brass);
            }
            var plaque = K.Group(root, "ChamberPlaque", new Vector3(0f, 1.3f, 4.9f));
            K.Box(plaque.transform, "Plate", Vector3.zero, new Vector3(0.9f, 0.3f, 0.03f), K.Mats.Brass);
            K.Text3D(plaque.transform, "Text", "THE IMPOSSIBLE EXIT", new Vector3(0f, 0f, -0.02f), Vector3.back, 0.08f, Color.black, TextAnchor.MiddleCenter, FontStyle.Bold, 48);
            var pi = plaque.AddComponent<InspectableObject>(); pi.Prompt = "Read plaque"; pi.Note = "The glass has never been opened. It was never closed on anyone.";

            // Silhouette inside the glass
            var sil = K.Group(chamber.transform, "VossSilhouette", new Vector3(0f, 0.06f, 0f));
            K.Prim(sil.transform, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.9f, 0f), new Vector3(0.45f, 0.9f, 0.45f), K.Mats.Silhouette, false);
            K.Prim(sil.transform, "Hat", PrimitiveType.Cylinder, new Vector3(0f, 1.9f, 0f), new Vector3(0.36f, 0.12f, 0.36f), K.Mats.Silhouette, false);
            K.Prim(sil.transform, "Brim", PrimitiveType.Cylinder, new Vector3(0f, 1.8f, 0f), new Vector3(0.5f, 0.02f, 0.5f), K.Mats.Silhouette, false);
            K.Box(sil.transform, "Hand", new Vector3(0f, 1.3f, -0.72f), new Vector3(0.12f, 0.16f, 0.04f), K.Mats.Silhouette, false);

            if (seats)
            {
                var seatGroup = K.Group(root, "Seats").transform;
                for (float z = -18f; z <= -2f; z += 1.6f)
                {
                    for (float x = -9f; x <= 9f; x += 1.2f)
                    {
                        if (Mathf.Abs(x) < 1.3f) continue; // centre aisle
                        var seat = K.Group(seatGroup, "Seat", new Vector3(x, 0f, z));
                        K.Box(seat.transform, "Cushion", new Vector3(0f, 0.25f, 0f), new Vector3(0.95f, 0.5f, 0.8f), K.Mats.Velvet);
                        K.Box(seat.transform, "Back", new Vector3(0f, 0.65f, -0.38f), new Vector3(0.95f, 0.8f, 0.12f), K.Mats.Velvet);
                    }
                }
            }

            house.Chamber = chamber;
            house.Silhouette = sil;
            house.Plaque = plaque;
            return house;
        }

        private static void BuildDust(Transform root)
        {
            var go = new GameObject("DustParticles");
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(0f, 5f, -5f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 14f;
            main.startSpeed = 0.05f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.04f);
            main.startColor = new Color(0.9f, 0.85f, 0.7f, 0.35f);
            main.maxParticles = 900;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.002f;
            var emission = ps.emission;
            emission.rateOverTime = 50f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(26f, 8f, 30f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.08f;
            noise.frequency = 0.2f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            string path = $"{K.MaterialFolder}/M_Dust.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetColor("_BaseColor", new Color(1f, 0.95f, 0.85f, 0.5f));
            mat.SetColor("_Color", new Color(1f, 0.95f, 0.85f, 0.5f));
            if (mat.HasProperty("_Surface")) { mat.SetFloat("_Surface", 1f); mat.SetOverrideTag("RenderType", "Transparent"); mat.renderQueue = 3000; mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); }
            EditorUtility.SetDirty(mat);
            renderer.sharedMaterial = mat;
        }
    }
}
