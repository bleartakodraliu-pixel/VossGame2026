using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.UI;

namespace Voss.Narrative
{
    /// <summary>
    /// Scene 00b: the vanishing, twenty years ago, seen from the audience. Applause over black and the
    /// narration; the stage, Voss in the glass chamber under a spotlight; seven seconds of darkness;
    /// the chamber is empty. "E. VOSS WAS NEVER SEEN AGAIN." Then the invitation. SPACE skips.
    /// How he did it is never shown here.
    /// </summary>
    public class PrologueCinematic : MonoBehaviour
    {
        [SerializeField] private Transform cameraRig;
        [SerializeField] private Vector3 cameraEnd;
        [SerializeField] private List<Light> lights = new List<Light>();
        [SerializeField] private GameObject voss;

        private bool leaving;
        private Color ambient;

        public void Configure(Transform rig, Vector3 pushTo, List<Light> sceneLights, GameObject vossFigure)
        {
            cameraRig = rig;
            cameraEnd = pushTo;
            lights = sceneLights;
            voss = vossFigure;
        }

        private void Start()
        {
            var ui = UIManager.Instance;
            if (ui != null) { ui.EvidenceEnabled = false; ui.SetCursorFree(false); }
            ambient = RenderSettings.ambientLight;
            StartCoroutine(Run());
        }

        private void Update()
        {
            if (leaving || !VossInput.SkipPressed()) return;
            StopAllCoroutines();
            StartCoroutine(Leave(0.4f));
        }

        private IEnumerator Run()
        {
            var gm = GameManager.Instance;
            var ui = UIManager.Instance;
            if (ui == null) yield break;

            ui.Story.Open(StoryCards.SkipHint);
            while (gm != null && gm.Transitions.IsTransitioning) yield return null;
            yield return new WaitForSeconds(0.6f);

            // Black. Applause, and the voice.
            gm?.Audio.Play(SfxId.Applause, 0.8f);
            yield return StoryCards.Play(new[] { "20 years ago, the world's greatest illusionist performed his final trick." }, false);

            // The stage.
            gm?.Audio.Play(SfxId.Projector, 0.5f);
            yield return ui.FadeTo(1f, 0.3f);
            ui.Story.Close();
            yield return ui.FadeTo(0f, 1.8f);

            // A slow push toward the chamber while he stands in the light.
            Vector3 start = cameraRig != null ? cameraRig.position : Vector3.zero;
            for (float t = 0f; t < 5.5f; t += Time.deltaTime)
            {
                if (cameraRig != null) cameraRig.position = Vector3.Lerp(start, cameraEnd, Mathf.SmoothStep(0f, 1f, t / 7f));
                yield return null;
            }

            // Seven seconds of darkness.
            gm?.Audio.Play(SfxId.PowerDown, 0.9f);
            SetLights(false);
            RenderSettings.ambientLight = Color.black;
            if (voss != null) voss.SetActive(false);
            for (int i = 0; i < 7; i++)
            {
                gm?.Audio.Play(SfxId.ClockTick, 0.9f);
                yield return new WaitForSeconds(1f);
            }

            // Lights. Nobody in the glass.
            gm?.Audio.Play(SfxId.DoorLock, 0.8f);
            RenderSettings.ambientLight = ambient;
            SetLights(true);
            yield return new WaitForSeconds(2.2f);
            ui.ShowMessage("E. VOSS WAS NEVER SEEN AGAIN.", "", 0f, 0f, UIFactory.Ink);
            yield return new WaitForSeconds(3.8f);
            yield return Leave(1.2f);
        }

        private void SetLights(bool on)
        {
            foreach (var l in lights) if (l != null) l.enabled = on;
        }

        private IEnumerator Leave(float fade)
        {
            if (leaving) yield break;
            leaving = true;
            var gm = GameManager.Instance;
            var ui = UIManager.Instance;
            if (ui != null)
            {
                yield return ui.FadeTo(1f, fade);
                ui.HideMessage();
                ui.Story.Close();
            }
            RenderSettings.ambientLight = ambient;
            gm?.Transitions.LoadScene(SceneNames.Invitation, 0.6f);
        }
    }
}
