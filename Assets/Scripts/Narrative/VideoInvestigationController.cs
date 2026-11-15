using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Voss.Audio;
using Voss.Core;
using Voss.Interaction;
using Voss.Player;
using Voss.UI;

namespace Voss.Narrative
{
    /// <summary>
    /// Frame-by-frame reconstruction of The Impossible Exit, rendered from a miniature stage onto the
    /// two screens of the final theatre: Camera A (audience) sells the illusion, Camera B (stage left)
    /// shows Voss sliding into darkness while a projected silhouette fills the chamber. The recording
    /// loops on its own; the player can step through it frame by frame. The feeds are stills, so each
    /// camera renders once per frame change instead of every display frame.
    /// </summary>
    public class VideoInvestigationController : MonoBehaviour, IInteractable, IVideoControlTarget
    {
        [System.Serializable]
        public class Frame
        {
            public Vector3 vossLocalPosition;
            public bool vossVisible = true;
            public float projectionAlpha;
            public bool stageLit = true;
            public string captionA;
            public string captionB;
        }

        private static readonly Color CaptionFocus = new Color(0.9f, 0.74f, 0.4f);
        private static readonly Color CaptionDim = new Color(0.55f, 0.45f, 0.26f);

        [SerializeField] private Transform vossFigure;
        [SerializeField] private Renderer projectionQuad;
        [SerializeField] private List<Light> stageLights = new List<Light>();
        [SerializeField] private Camera cameraA;
        [SerializeField] private Camera cameraB;
        [SerializeField] private Renderer screenA;
        [SerializeField] private Renderer screenB;
        [SerializeField] private TextMesh captionA;
        [SerializeField] private TextMesh captionB;
        [SerializeField] private float secondsPerFrame = 1.1f;
        [SerializeField] private float holdSeconds = 3f;
        [SerializeField] private List<Frame> frames = new List<Frame>();

        private RenderTexture rtA, rtB;
        private RenderPipeline.StandardRequest requestA, requestB;
        private Material projectionMaterial;
        private int frameIndex;
        private bool playing;
        private bool usingCameraB;
        private bool available;
        private bool viewedCameraB;
        private bool controlsOpen;
        private bool frozen;
        private Coroutine playRoutine;
        private Coroutine loopRoutine;

        public bool ViewedCameraB => viewedCameraB;

        public void Configure(Transform voss, Renderer projection, List<Light> lights, Camera a, Camera b,
            Renderer screenARenderer, Renderer screenBRenderer, TextMesh captionAMesh, TextMesh captionBMesh)
        {
            vossFigure = voss;
            projectionQuad = projection;
            stageLights = lights;
            cameraA = a;
            cameraB = b;
            screenA = screenARenderer;
            screenB = screenBRenderer;
            captionA = captionAMesh;
            captionB = captionBMesh;
            frames = BuildDefaultFrames();
        }

        /// <summary>The choreography of the illusion. Local positions are relative to the diorama root.</summary>
        public static List<Frame> BuildDefaultFrames()
        {
            return new List<Frame>
            {
                new Frame { vossLocalPosition = new Vector3(-3.0f, 0f, 0.5f), captionA = "00  Voss walks on stage.", captionB = "00  Voss walks on stage." },
                new Frame { vossLocalPosition = new Vector3(-2.0f, 0f, 0.5f), captionA = "01  He approaches the glass chamber.", captionB = "01  He approaches the glass chamber." },
                new Frame { vossLocalPosition = new Vector3(-1.2f, 0f, 0.5f), captionA = "02  He stops beside the chamber.", captionB = "02  He stops beside the chamber." },
                new Frame { vossLocalPosition = new Vector3(-0.7f, 0f, 0.2f), projectionAlpha = 0.35f, captionA = "03  He steps toward the door.", captionB = "03  A faint shape appears inside the glass. Voss is still outside." },
                new Frame { vossLocalPosition = new Vector3(-0.7f, 0f, -0.6f), projectionAlpha = 0.8f, captionA = "04  Voss enters the chamber.", captionB = "04  Voss moves sideways, behind the light. The shape inside is not him." },
                new Frame { vossLocalPosition = new Vector3(-0.9f, 0f, -1.6f), projectionAlpha = 1f, captionA = "05  The chamber locks.", captionB = "05  Voss is in darkness beside the chamber. The chamber locks on a silhouette." },
                new Frame { vossLocalPosition = new Vector3(-1.4f, 0f, -2.6f), projectionAlpha = 1f, captionA = "06  His hand touches the glass. 'Seven seconds.'", captionB = "06  The projected hand touches the glass. Voss is already gone." },
                new Frame { vossLocalPosition = new Vector3(-2.2f, 0f, -3.4f), vossVisible = false, projectionAlpha = 0f, stageLit = false, captionA = "07  Blackout.", captionB = "07  Blackout. Nothing left to hide." },
                new Frame { vossLocalPosition = new Vector3(-2.2f, 0f, -3.4f), vossVisible = false, projectionAlpha = 0f, stageLit = false, captionA = "08  Seven seconds pass.", captionB = "08  Seven seconds pass. The projector is switched off." },
                new Frame { vossLocalPosition = new Vector3(-2.2f, 0f, -3.4f), vossVisible = false, projectionAlpha = 0f, captionA = "09  Lights. The chamber is empty.", captionB = "09  Lights. The chamber was always empty." },
            };
        }

        private void Awake()
        {
            if (frames == null || frames.Count == 0) frames = BuildDefaultFrames();
        }

        private void Start()
        {
            rtA = new RenderTexture(768, 432, 16) { name = "PerformanceCameraA" };
            rtB = new RenderTexture(768, 432, 16) { name = "PerformanceCameraB" };
            requestA = new RenderPipeline.StandardRequest { destination = rtA };
            requestB = new RenderPipeline.StandardRequest { destination = rtB };
            // The cameras never render on their own; RenderFeeds draws them when the picture changes.
            if (cameraA != null) cameraA.enabled = false;
            if (cameraB != null) cameraB.enabled = false;
            if (projectionQuad != null) projectionMaterial = projectionQuad.material;
            ApplyFrame(0);
            SetScreensActive(false);
        }

        private void OnDestroy()
        {
            if (rtA != null) { rtA.Release(); Destroy(rtA); }
            if (rtB != null) { rtB.Release(); Destroy(rtB); }
        }

        /// <summary>Called by the final investigation once the real test begins.</summary>
        public void MakeAvailable()
        {
            if (available) return;
            available = true;
            SetScreensActive(true);
            ApplyFrame(0);
            loopRoutine = StartCoroutine(Loop());
            GameManager.Instance?.Audio.PlayAt(SfxId.Projector, transform.position, 0.8f);
        }

        /// <summary>Stops the loop on the last frame: the chamber, empty, from both sides.</summary>
        public void ShowFinalFrame()
        {
            frozen = true;
            Pause();
            if (loopRoutine != null) StopCoroutine(loopRoutine);
            loopRoutine = null;
            ApplyFrame(frames.Count - 1);
        }

        private IEnumerator Loop()
        {
            while (!frozen)
            {
                bool atEnd = frameIndex >= frames.Count - 1;
                yield return new WaitForSeconds(atEnd ? holdSeconds : secondsPerFrame);
                if (controlsOpen || frozen) continue;
                ApplyFrame(atEnd ? 0 : frameIndex + 1);
            }
        }

        private void SetScreensActive(bool on)
        {
            SetScreen(screenA, rtA, on);
            SetScreen(screenB, rtB, on);
            if (!on)
            {
                if (captionA != null) captionA.text = string.Empty;
                if (captionB != null) captionB.text = string.Empty;
            }
        }

        private static void SetScreen(Renderer screen, RenderTexture rt, bool on)
        {
            if (screen == null) return;
            var m = screen.material;
            if (on)
            {
                m.SetTexture("_BaseMap", rt);
                m.SetTexture("_MainTex", rt);
                m.SetColor("_BaseColor", Color.white);
                m.SetColor("_Color", Color.white);
                m.EnableKeyword("_EMISSION");
                m.SetTexture("_EmissionMap", rt);
                m.SetColor("_EmissionColor", Color.white * 0.9f);
            }
            else
            {
                m.SetColor("_BaseColor", new Color(0.02f, 0.02f, 0.025f));
                m.SetColor("_Color", new Color(0.02f, 0.02f, 0.025f));
                m.SetColor("_EmissionColor", Color.black);
            }
        }

        private void RenderFeeds()
        {
            if (!available) return;
            Render(cameraA, rtA, requestA);
            Render(cameraB, rtB, requestB);
        }

        private static void Render(Camera cam, RenderTexture rt, RenderPipeline.StandardRequest request)
        {
            if (cam == null || rt == null) return;
            if (RenderPipeline.SupportsRenderRequest(cam, request)) RenderPipeline.SubmitRenderRequest(cam, request);
            else { cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; }
        }

        // ------------------------------------------------------- interaction

        public string GetInteractionPrompt() => available ? "Review Performance" : "Screen offline";
        public bool CanInteract() => true;

        public void Interact(PlayerInteractor player)
        {
            var gm = GameManager.Instance;
            var ui = UIManager.Instance;
            if (!available)
            {
                gm?.Subtitles.Show("The screens are dark. Nothing to watch yet.", 2.2f);
                return;
            }
            if (ui == null || frozen) return;
            controlsOpen = true;
            gm?.Audio.PlayAt(SfxId.TerminalActivate, transform.position, 0.6f);
            ui.VideoControls.Open(this, OnControlsClosed);
        }

        private void OnControlsClosed()
        {
            Pause();
            controlsOpen = false;
        }

        // ----------------------------------------------------- video control

        public void Play()
        {
            if (playing) return;
            playing = true;
            if (frameIndex >= frames.Count - 1) frameIndex = 0;
            playRoutine = StartCoroutine(PlayRoutine());
        }

        public void Pause()
        {
            playing = false;
            if (playRoutine != null) StopCoroutine(playRoutine);
            playRoutine = null;
        }

        public void PreviousFrame()
        {
            Pause();
            ApplyFrame(Mathf.Max(0, frameIndex - 1));
            GameManager.Instance?.Audio.Play(SfxId.Click, 0.5f);
        }

        public void NextFrame()
        {
            Pause();
            ApplyFrame(Mathf.Min(frames.Count - 1, frameIndex + 1));
            GameManager.Instance?.Audio.Play(SfxId.Click, 0.5f);
        }

        /// <summary>Both screens always play; this moves the focus (status line, bright caption) between them.</summary>
        public void ChangeCamera()
        {
            usingCameraB = !usingCameraB;
            if (usingCameraB) viewedCameraB = true;
            GameManager.Instance?.Audio.Play(SfxId.MetalMechanism, 0.4f);
            ApplyFrame(frameIndex);
        }

        public string GetStatusLine()
        {
            string cam = usingCameraB ? "FOCUS: CAMERA B — STAGE LEFT" : "FOCUS: CAMERA A — AUDIENCE";
            string state = playing ? "PLAYING" : "PAUSED";
            return $"{cam}    |    FRAME {frameIndex:00} / {frames.Count - 1:00}    |    {state}";
        }

        private IEnumerator PlayRoutine()
        {
            while (playing && frameIndex < frames.Count - 1)
            {
                yield return new WaitForSeconds(secondsPerFrame);
                ApplyFrame(frameIndex + 1);
                UIManager.Instance?.VideoControls.Refresh();
            }
            playing = false;
            playRoutine = null;
            UIManager.Instance?.VideoControls.Refresh();
        }

        /// <summary>Breaks a caption into lines of at most <paramref name="width"/> characters (3D text does not wrap).</summary>
        private static string Wrap(string text, int width)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= width) return text;
            var sb = new System.Text.StringBuilder(text.Length + 4);
            int lineLength = 0;
            foreach (var word in text.Split(' '))
            {
                if (lineLength > 0 && lineLength + 1 + word.Length > width) { sb.Append('\n'); lineLength = 0; }
                else if (lineLength > 0) { sb.Append(' '); lineLength++; }
                sb.Append(word);
                lineLength += word.Length;
            }
            return sb.ToString();
        }

        private void ApplyFrame(int index)
        {
            if (frames.Count == 0) return;
            frameIndex = Mathf.Clamp(index, 0, frames.Count - 1);
            var f = frames[frameIndex];
            // Both screens are always in view: reaching the moment of the switch counts as having seen B.
            if (available && frameIndex >= 4) viewedCameraB = true;

            if (vossFigure != null)
            {
                vossFigure.localPosition = f.vossLocalPosition;
                vossFigure.gameObject.SetActive(f.vossVisible);
            }
            if (projectionMaterial != null)
            {
                var c = projectionMaterial.GetColor("_BaseColor");
                c.a = f.projectionAlpha;
                projectionMaterial.SetColor("_BaseColor", c);
                projectionMaterial.SetColor("_Color", c);
                if (projectionQuad != null) projectionQuad.enabled = f.projectionAlpha > 0.01f;
            }
            foreach (var l in stageLights) if (l != null) l.enabled = f.stageLit;
            if (available)
            {
                if (captionA != null) { captionA.text = Wrap(f.captionA, 44); captionA.color = usingCameraB ? CaptionDim : CaptionFocus; }
                if (captionB != null) { captionB.text = Wrap(f.captionB, 44); captionB.color = usingCameraB ? CaptionFocus : CaptionDim; }
            }
            RenderFeeds();
        }
    }
}
