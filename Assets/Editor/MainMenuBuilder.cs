using UnityEngine;
using UnityEngine.SceneManagement;
using Voss.Narrative;
using K = Voss.EditorTools.VossBuildKit;

namespace Voss.EditorTools
{
    /// <summary>00_MainMenu: a dark stage with the glass chamber, slowly orbited by the camera.</summary>
    public static class MainMenuBuilder
    {
        public static void Build(Scene scene)
        {
            K.DarkAmbient(new Color(0.02f, 0.02f, 0.03f));
            var root = K.Group(null, "MainMenu");

            // Set
            K.Box(root.transform, "StageFloor", new Vector3(0f, -0.1f, 0f), new Vector3(14f, 0.2f, 10f), K.Mats.Wood);
            K.Box(root.transform, "BackCurtain", new Vector3(0f, 3f, 5f), new Vector3(14f, 6f, 0.3f), K.Mats.Velvet);
            K.Box(root.transform, "GlassChamber", new Vector3(0f, 1.3f, 0f), new Vector3(1.6f, 2.6f, 1.6f), K.Mats.Glass, false);
            K.Box(root.transform, "ChamberFrame", new Vector3(0f, 2.62f, 0f), new Vector3(1.7f, 0.08f, 1.7f), K.Mats.Brass, false);
            K.Box(root.transform, "ChamberBase", new Vector3(0f, 0.04f, 0f), new Vector3(1.8f, 0.08f, 1.8f), K.Mats.Brass, false);
            K.Text3D(root.transform, "Title", "THE VOSS EXPERIMENT", new Vector3(0f, 4.2f, 4.8f), Vector3.back, 0.35f, new Color(0.78f, 0.62f, 0.32f), TextAnchor.MiddleCenter, FontStyle.Bold);

            // Lighting
            K.SpotLight(root.transform, "KeyLight", new Vector3(2f, 5f, -4f), new Vector3(0f, 1f, 0f), new Color(0.85f, 0.9f, 1f), 14f, 20f, 40f);
            K.PointLight(root.transform, "Fill", new Vector3(-4f, 2f, -2f), new Color(0.9f, 0.6f, 0.3f), 1.2f, 10f);

            // Camera rig
            var rig = K.Group(root.transform, "CameraRig");
            var camGo = new GameObject("MenuCamera");
            camGo.transform.SetParent(rig.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 2.2f, -6.5f);
            camGo.transform.LookAt(new Vector3(0f, 1.4f, 0f));
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.fieldOfView = 50f;
            camGo.AddComponent<AudioListener>();

            var controller = root.AddComponent<MainMenuController>();
            controller.Configure(rig.transform);
        }
    }
}
