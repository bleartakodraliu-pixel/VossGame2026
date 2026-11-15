// TEMPORARY verification script (Claude). Safe to delete.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Voss.Core;
using Voss.Interaction;
using Voss.Narrative;
using Voss.Player;
using Voss.Puzzles;

namespace Voss.EditorTools
{
    public static class ClaudeIntroTest
    {
        private static string Out => Environment.GetEnvironmentVariable("VOSS_VERIFY_OUT") ?? "/tmp";
        private static int fails;
        private static IEnumerator<float> routine;
        private static float nextAt;
        private static int lastFrame;
        private static double bootTime;

        private static void Log(string m) => Debug.Log("[ClaudeVerify] " + m);
        private static void Check(bool ok, string what) { if (!ok) fails++; Log((ok ? "PASS " : "FAIL ") + what); }

        public static void Play()
        {
            fails = 0;
            EditorSceneManager.OpenScene("Assets/Scenes/VossIntroduction.unity");
            routine = Routine();
            nextAt = 0f;
            bootTime = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup - bootTime > 240) { Log("TIMEOUT"); fails++; Finish(); return; }
            if (!EditorApplication.isPlaying || Time.frameCount == lastFrame) return;
            lastFrame = Time.frameCount;
            try
            {
                if (Time.time < nextAt) return;
                if (!routine.MoveNext()) { Finish(); return; }
                nextAt = Time.time + routine.Current;
            }
            catch (Exception e) { Log("EXCEPTION " + e); fails++; Finish(); }
        }

        private static void Finish()
        {
            EditorApplication.update -= Tick;
            Log("PLAY DONE fails=" + fails);
            EditorApplication.Exit(fails == 0 ? 0 : 1);
        }

        private static IEnumerator<float> Routine()
        {
            yield return 2f;
            var player = UnityEngine.Object.FindAnyObjectByType<FirstPersonController>();
            var interactor = UnityEngine.Object.FindAnyObjectByType<PlayerInteractor>();
            var preview = GameObject.Find("VossIntroduction").transform.Find("LabDoor/LabPreview").gameObject;
            Check(!preview.activeSelf, "hall preview hidden before the door opens");
            player.Teleport(new Vector3(0f, 1.05f, 8.3f), 0f);
            yield return 0.5f;
            Capture("door_01_closed", new Vector3(0f, 2.6f, 8.3f), new Vector3(0f, 2.3f, 11f));
            Capture("door_00_wide", new Vector3(0f, 2.2f, 2f), new Vector3(0f, 2.6f, 11f));

            UnityEngine.Object.FindAnyObjectByType<IntroductionManager>().OnLetterCollected(false);
            var entrance = UnityEngine.Object.FindAnyObjectByType<LaboratoryEntrance>();
            entrance.Interact(interactor);
            yield return 1.2f;
            Check(preview.activeSelf, "hall preview shown as the door opens");
            yield return 1.6f;
            Capture("door_02_open", new Vector3(0f, 2.6f, 8.3f), new Vector3(0f, 2.3f, 11f));
            Capture("door_03_open_side", new Vector3(-1.2f, 2.6f, 8.6f), new Vector3(0.3f, 2.2f, 12f));

            float t0 = Time.time;
            while (SceneManager.GetActiveScene().name != SceneNames.Laboratory && Time.time - t0 < 20f) yield return 0.25f;
            Check(SceneManager.GetActiveScene().name == SceneNames.Laboratory, "door leads to the laboratory");
            yield return 3f;

            // REASON opens at once after PERCEPTION.
            var root = GameObject.Find("VossLaboratory").transform;
            var rDoor = root.Find("REASONRoom/Door_REASON").GetComponent<DoorController>();
            Check(rDoor.State == DoorState.Locked, "REASON locked before PERCEPTION");
            var pp = UnityEngine.Object.FindAnyObjectByType<PerceptionPuzzle>();
            string impossible = null;
            foreach (var d in UnityEngine.Object.FindObjectsByType<MirrorDisplay>(FindObjectsInactive.Exclude)) if (d.IsImpossible) impossible = d.DisplayId;
            Check(pp.Submit(impossible), "PERCEPTION solved");
            t0 = Time.time;
            while (rDoor.State != DoorState.Open && Time.time - t0 < 10f) yield return 0.05f;
            Check(rDoor.State == DoorState.Open, $"REASON door fully open {Time.time - t0:0.00}s after the solve (no card, no return to the hall)");
        }

        private static void Capture(string name, Vector3 eye, Vector3 lookAt)
        {
            var go = new GameObject("ClaudeCaptureCam");
            var cam = go.AddComponent<Camera>();
            cam.CopyFrom(Camera.main);
            go.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(lookAt - eye, Vector3.up));
            cam.fieldOfView = 60f;
            cam.cullingMask &= ~(1 << FirstPersonHands.HandsLayer);
            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var req = new RenderPipeline.StandardRequest { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(cam, req)) RenderPipeline.SubmitRenderRequest(cam, req);
            else { cam.targetTexture = rt; cam.Render(); }
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(Out, name + ".png"), tex.EncodeToPNG());
            rt.Release();
            UnityEngine.Object.DestroyImmediate(go);
            Log("captured " + name);
        }
    }
}
