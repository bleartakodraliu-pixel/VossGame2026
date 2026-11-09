using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.Interaction;
using Voss.Player;
using Voss.UI;

namespace Voss.Narrative
{
    /// <summary>
    /// Frame-by-frame reconstruction of The Impossible Exit, rendered from a miniature stage
    /// into a render texture on the performance screen. Camera A (audience) sells the illusion;
    /// Camera B (side) shows Voss sliding into darkness while a projected silhouette fills the chamber.
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

        [SerializeField] private Transform vossFigure;
        [SerializeField] private Renderer projectionQuad;
        [SerializeField] private List<Light> stageLights = new List<Light>();
        [SerializeField] private Camera cameraA;
        [SerializeField] private Camera cameraB;
        [SerializeField] private Renderer screenRenderer;
        [SerializeField] private TextMesh caption;
        [SerializeField] private float secondsPerFrame = 0.9f;
        [SerializeField] private List<Frame> frames = new List<Frame>();

        private RenderTexture rt;
        private Material projectionMaterial;
        private int frameIndex;
        private bool playing;
        private bool usingCameraB;
        private bool available;
        private bool viewedCameraB;
        private Coroutine playRoutine;

        public bool ViewedCameraB => viewedCameraB;

        public void Configure(Transform voss, Renderer projection, List<Light> lights, Camera a, Camera b, Renderer screen, TextMesh captionMesh)
        {
            vossFigure = voss;
            projectionQuad = projection;
            stageLights = lights;
            cameraA = a;
            cameraB = b;
            screenRenderer = screen;
            caption = captionMesh;
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
            rt = new RenderTexture(1024, 576, 16);
            rt.name = "PerformanceScreenRT";
            if (cameraA != null) { cameraA.targetTexture = rt; cameraA.enabled = false; }
            if (cameraB != null) { cameraB.targetTexture = rt; cameraB.enabled = false; }
            if (projectionQuad != null) projectionMaterial = projectionQuad.material;
            ApplyFrame(0);
            SetScreenActive(false);
        }

        private void OnDestroy()
        {
            if (rt != null) { rt.Release(); Destroy(rt); }
        }

        /// <summary>Called by the final investigation once the real test begins.</summary>
        public void MakeAvailable()
        {
            available = true;
            SetScreenActive(true);
            SelectCamera(false);
            ApplyFrame(0);
            GameManager.Instance?.Audio.PlayAt(SfxId.Projector, transform.position, 0.8f);
        }

        private void SetScreenActive(bool on)
        {
            if (screenRenderer == null) return;
            var m = screenRenderer.material;
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
            if (caption != null) caption.text = on ? "PERFORMANCE RECONSTRUCTION — READY" : string.Empty;
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
                gm?.Subtitles.Show("The screen is dark. Nothing to watch yet.", 2.2f);
                return;
            }
            if (ui == null) return;
            gm?.Audio.PlayAt(SfxId.TerminalActivate, transform.position, 0.6f);
            ui.VideoControls.Open(this, OnControlsClosed);
        }

        private void OnControlsClosed()
        {
            Pause();
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

        public void ChangeCamera()
        {
            SelectCamera(!usingCameraB);
            GameManager.Instance?.Audio.Play(SfxId.MetalMechanism, 0.4f);
            ApplyFrame(frameIndex);
        }

        public string GetStatusLine()
        {
            string cam = usingCameraB ? "CAMERA B — SECONDARY ANGLE" : "CAMERA A — AUDIENCE VIEW";
            string state = playing ? "PLAYING" : "PAUSED";
            return $"{cam}    |    FRAME {frameIndex:00} / {frames.Count - 1:00}    |    {state}";
        }

        private void SelectCamera(bool b)
        {
            usingCameraB = b;
            if (cameraA != null) cameraA.enabled = !b;
            if (cameraB != null) cameraB.enabled = b;
            if (b) viewedCameraB = true;
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

        private void ApplyFrame(int index)
        {
            if (frames.Count == 0) return;
            frameIndex = Mathf.Clamp(index, 0, frames.Count - 1);
            var f = frames[frameIndex];

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
            if (caption != null) caption.text = usingCameraB ? f.captionB : f.captionA;
        }
    }
}
