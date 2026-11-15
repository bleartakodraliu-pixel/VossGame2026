using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Voss.Narrative;
using K = Voss.EditorTools.VossBuildKit;

namespace Voss.EditorTools
{
    /// <summary>
    /// 00b_Prologue: the night of THE IMPOSSIBLE EXIT, twenty years ago. A full house in silhouette,
    /// Voss in the glass chamber under the spotlight, the camera in the aisle. Driven by PrologueCinematic.
    /// </summary>
    public static class PrologueBuilder
    {
        public static void Build(Scene scene)
        {
            K.DarkAmbient(new Color(0.035f, 0.028f, 0.022f));
            K.Fog(new Color(0.02f, 0.014f, 0.01f), 0.022f);
            var root = K.Group(null, "Prologue").transform;
            // Warm, grainy, like an old film of the night.
            K.GlobalVolume(root, K.HorrorProfile("Prologue", 0.95f, 0.48f, 0.55f, new Color(1f, 0.88f, 0.74f)));

            var house = TheatreBuilder.BuildHouse(root);

            // The audience: heads and shoulders in the rows nearest the stage.
            var crowd = K.Group(root, "Audience").transform;
            var rnd = new System.Random(20);
            for (float z = -10f; z <= -1.9f; z += 1.6f)
                for (float x = -9f; x <= 9f; x += 1.2f)
                {
                    if (Mathf.Abs(x) < 1.3f || rnd.NextDouble() < 0.18) continue;
                    var p = K.Group(crowd, "Spectator", new Vector3(x + (float)rnd.NextDouble() * 0.1f, 0f, z + 0.05f)).transform;
                    K.Box(p, "Shoulders", new Vector3(0f, 0.95f, 0f), new Vector3(0.46f, 0.32f, 0.26f), K.Mats.Silhouette, false);
                    K.Prim(p, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.27f, 0.02f), new Vector3(0.21f, 0.25f, 0.22f), K.Mats.Silhouette, false);
                }

            // Show lighting.
            var lights = new List<Light>
            {
                K.SpotLight(root, "Spot_Chamber", new Vector3(0f, 9.2f, 0f), new Vector3(0f, 2.2f, 6f), new Color(1f, 0.92f, 0.78f), 70f, 22f, 24f),
                // Backlight on the curtain behind the glass, so the black figure reads against it.
                K.SpotLight(root, "Spot_Backdrop", new Vector3(0f, 7f, 6.5f), new Vector3(0f, 3f, 11.8f), new Color(1f, 0.72f, 0.48f), 40f, 12f, 55f),
                K.PointLight(root, "Glow_Behind", new Vector3(0f, 2.6f, 8.2f), new Color(1f, 0.8f, 0.6f), 3.5f, 6f),
                K.SpotLight(root, "Spot_Left", new Vector3(-7f, 8.5f, -1f), new Vector3(-2f, 1.5f, 7f), new Color(1f, 0.78f, 0.55f), 14f, 22f, 45f),
                K.SpotLight(root, "Spot_Right", new Vector3(7f, 8.5f, -1f), new Vector3(2f, 1.5f, 7f), new Color(1f, 0.78f, 0.55f), 14f, 22f, 45f),
                K.PointLight(root, "ChamberGlow", new Vector3(0f, 3.4f, 6f), new Color(0.75f, 0.85f, 1f), 1.4f, 5f),
                K.PointLight(root, "HouseLight_A", new Vector3(-8f, 6f, -10f), new Color(1f, 0.75f, 0.5f), 0.6f, 16f),
                K.PointLight(root, "HouseLight_B", new Vector3(8f, 6f, -10f), new Color(1f, 0.75f, 0.5f), 0.6f, 16f),
            };
            for (int i = 0; i < 6; i++)
                lights.Add(K.PointLight(root, "Footlight_" + i, new Vector3(-6.25f + i * 2.5f, 1.15f, 2.2f), new Color(1f, 0.7f, 0.4f), 1.2f, 5f));
            K.Dust(root, "Dust", new Vector3(0f, 5f, -2f), new Vector3(24f, 8f, 20f), 18f, new Color(0.95f, 0.88f, 0.75f, 0.25f));

            // Camera in the aisle, pushing slowly toward the stage.
            var rig = K.Group(root, "CameraRig", new Vector3(0f, 2.1f, -12.4f)).transform;
            var camGo = new GameObject("PrologueCamera");
            camGo.transform.SetParent(rig, false);
            camGo.transform.LookAt(new Vector3(0f, 2.3f, 6f));
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.05f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGo.AddComponent<AudioListener>();
            K.EnablePostProcessing(cam);

            root.gameObject.AddComponent<PrologueCinematic>().Configure(rig, new Vector3(0f, 2f, -7.6f), lights, house.Silhouette);
        }
    }
}
