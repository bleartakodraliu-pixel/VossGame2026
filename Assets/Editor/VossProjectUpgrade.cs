using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Voss.Player;
using Voss.Puzzles;
using Voss.UI;

namespace Voss.EditorTools
{
    /// <summary>Updates saved scenes in place, without rebuilding or replacing their contents.</summary>
    public static class VossProjectUpgrade
    {
        [MenuItem("Voss/Apply Text, Mirror and Player Fixes")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before upgrading scenes.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            EnsureLayer(FirstPersonHands.HandsLayer, "VossHands");
            EnsureLayer(PlanarMirror.MirrorLayer, "VossMirrors");
            EnsureLayer(PlanarMirror.ReflectionOnlyLayer, "VossReflectionOnly");
            VossBuildKit.Mats.Load();
            try
            {
                foreach (string scenePath in new[] { "00_MainMenu", "01_Invitation", "02_VossLaboratory", "03_EmptyTheatre" })
                {
                    var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + scenePath + ".unity", OpenSceneMode.Single);
                    foreach (var root in scene.GetRootGameObjects())
                    {
                        foreach (var text in root.GetComponentsInChildren<TextMesh>(true))
                        {
                            var lettering = text.GetComponent<WorldText>();
                            if (lettering == null) lettering = text.gameObject.AddComponent<WorldText>();
                            text.GetComponent<Renderer>().sharedMaterial = Resources.Load<Material>("WorldText");
                            EditorUtility.SetDirty(lettering);
                        }
                        foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                            camera.cullingMask &= ~(1 << PlanarMirror.ReflectionOnlyLayer);
                        foreach (var player in root.GetComponentsInChildren<FirstPersonController>(true))
                            if (player.GetComponent<FirstPersonHands>() == null) player.gameObject.AddComponent<FirstPersonHands>();
                        foreach (var mirror in root.GetComponentsInChildren<MirrorDisplay>(true))
                        {
                            mirror.gameObject.layer = PlanarMirror.MirrorLayer;
                            mirror.GetComponent<Renderer>().sharedMaterial = VossBuildKit.Mats.Mirror;
                            var copy = mirror.transform.parent.Find("Reflection");
                            if (copy == null) continue;
                            foreach (var child in copy.GetComponentsInChildren<Transform>(true))
                                child.gameObject.layer = PlanarMirror.ReflectionOnlyLayer;
                            // Runtime Configure activates only the deliberately impossible exhibit.
                            copy.gameObject.SetActive(false);
                        }
                    }
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                AssetDatabase.SaveAssets();
                Debug.Log("[Voss] Text, mirrors, sprint and hands upgrades applied to all four scenes.");
            }
            finally
            {
                if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        private static void EnsureLayer(int index, string name)
        {
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layer = tags.FindProperty("layers").GetArrayElementAtIndex(index);
            if (!string.IsNullOrEmpty(layer.stringValue) && layer.stringValue != name)
                throw new InvalidOperationException("Layer " + index + " is already used by " + layer.stringValue);
            layer.stringValue = name;
            tags.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
