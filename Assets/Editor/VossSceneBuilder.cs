using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Voss.Core;

namespace Voss.EditorTools
{
    /// <summary>
    /// Menu: Voss > Build All Scenes. Generates the six scenes (menu, the vanishing, the invitation, the
    /// theatre arrival, the laboratory, the ending), the materials
    /// they use, and registers them in Build Settings in play order. Safe to re-run: scenes are overwritten.
    /// </summary>
    public static class VossSceneBuilder
    {
        [MenuItem("Voss/Build All Scenes", priority = 0)]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            VossBuildKit.EnsureFolder(VossBuildKit.ScenesFolder);
            VossBuildKit.Mats.Load();
            AssetDatabase.SaveAssets();

            BuildScene(SceneNames.MainMenu, MainMenuBuilder.Build);
            BuildScene(SceneNames.Introduction, IntroductionBuilder.Build);
            BuildScene(SceneNames.Prologue, PrologueBuilder.Build);
            BuildScene(SceneNames.Invitation, InvitationBuilder.Build);
            BuildScene(SceneNames.Arrival, ArrivalBuilder.Build);
            BuildScene(SceneNames.Laboratory, LaboratoryBuilder.Build);
            BuildScene(SceneNames.Theatre, TheatreBuilder.Build);

            RegisterBuildSettings();
            EditorSceneManager.OpenScene($"{VossBuildKit.ScenesFolder}/{SceneNames.MainMenu}.unity");
            Debug.Log("[Voss] All scenes built. Press Play in 00_MainMenu.");
        }

        [MenuItem("Voss/Build Scene/00 Main Menu")] public static void BuildMenu() { Prep(); BuildScene(SceneNames.MainMenu, MainMenuBuilder.Build); RegisterBuildSettings(); }
        [MenuItem("Voss/Build Scene/00a Voss Introduction")] public static void BuildIntroduction() { Prep(); BuildScene(SceneNames.Introduction, IntroductionBuilder.Build); RegisterBuildSettings(); }
        [MenuItem("Voss/Build Scene/00b Prologue")] public static void BuildPrologue() { Prep(); BuildScene(SceneNames.Prologue, PrologueBuilder.Build); RegisterBuildSettings(); }
        [MenuItem("Voss/Build Scene/01b Theatre Arrival")] public static void BuildArrival() { Prep(); BuildScene(SceneNames.Arrival, ArrivalBuilder.Build); RegisterBuildSettings(); }
        [MenuItem("Voss/Build Scene/01 Invitation")] public static void BuildInvitation() { Prep(); BuildScene(SceneNames.Invitation, InvitationBuilder.Build); RegisterBuildSettings(); }
        [MenuItem("Voss/Build Scene/02 Laboratory")] public static void BuildLab() { Prep(); BuildScene(SceneNames.Laboratory, LaboratoryBuilder.Build); RegisterBuildSettings(); }
        [MenuItem("Voss/Build Scene/03 Theatre")] public static void BuildTheatre() { Prep(); BuildScene(SceneNames.Theatre, TheatreBuilder.Build); RegisterBuildSettings(); }

        [MenuItem("Voss/Create Audio Library Asset (Resources)")]
        public static void CreateAudioLibrary()
        {
            VossBuildKit.EnsureFolder("Assets/Resources");
            const string path = "Assets/Resources/VossAudioLibrary.asset";
            if (AssetDatabase.LoadAssetAtPath<Voss.Audio.AudioLibrary>(path) != null)
            {
                Debug.Log("[Voss] Audio library already exists at " + path);
                return;
            }
            var lib = ScriptableObject.CreateInstance<Voss.Audio.AudioLibrary>();
            foreach (Voss.Audio.SfxId id in System.Enum.GetValues(typeof(Voss.Audio.SfxId)))
                lib.sfx.Add(new Voss.Audio.AudioLibrary.SfxEntry { id = id });
            foreach (Voss.Audio.MusicId id in System.Enum.GetValues(typeof(Voss.Audio.MusicId)))
                if (id != Voss.Audio.MusicId.None) lib.music.Add(new Voss.Audio.AudioLibrary.MusicEntry { id = id });
            AssetDatabase.CreateAsset(lib, path);
            AssetDatabase.SaveAssets();
            Debug.Log("[Voss] Created " + path + ". Drop real clips into its slots; empty slots keep procedural placeholders.");
        }

        [MenuItem("Voss/Register Scenes In Build Settings")]
        public static void RegisterBuildSettings()
        {
            var list = new List<EditorBuildSettingsScene>();
            foreach (var name in new[] { SceneNames.MainMenu, SceneNames.Introduction, SceneNames.Prologue, SceneNames.Invitation, SceneNames.Arrival, SceneNames.Laboratory, SceneNames.Theatre })
            {
                string path = $"{VossBuildKit.ScenesFolder}/{name}.unity";
                if (System.IO.File.Exists(path)) list.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = list.ToArray();
        }

        private static void Prep()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            VossBuildKit.EnsureFolder(VossBuildKit.ScenesFolder);
            VossBuildKit.Mats.Load();
            AssetDatabase.SaveAssets();
        }

        private static void BuildScene(string name, System.Action<Scene> build)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            build(scene);
            string path = $"{VossBuildKit.ScenesFolder}/{name}.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[Voss] Built {path}");
        }
    }
}
