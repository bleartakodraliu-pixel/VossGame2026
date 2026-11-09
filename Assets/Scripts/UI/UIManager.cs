using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using Voss.Core;

namespace Voss.UI
{
    /// <summary>
    /// Persistent, code-built interface: fade, crosshair, prompt, subtitles, attempts, messages
    /// and every modal panel. Also owns cursor lock state based on whether a modal is open.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        private Canvas canvas;
        private Image fade;
        private Image crosshair;
        private Text prompt;
        private Text subtitle;
        private Text attempts;
        private Text message;
        private Text messageSub;
        private Image messageBackdrop;

        private KeypadPanel keypad;
        private EvidencePanel evidencePanel;
        private ChoicePanel choice;
        private ChoicePanel failure;
        private ChoicePanel mainMenu;
        private VideoControlsPanel videoControls;

        private Coroutine attemptsRoutine;
        private Coroutine messageRoutine;
        private bool cursorFree;
        private bool evidenceEnabled;

        public KeypadPanel Keypad => keypad;
        public ChoicePanel Choice => choice;
        public VideoControlsPanel VideoControls => videoControls;
        public bool EvidenceEnabled { get => evidenceEnabled; set => evidenceEnabled = value; }

        public bool IsModalOpen =>
            keypad.IsOpen || evidencePanel.IsOpen || choice.IsOpen || failure.IsOpen || mainMenu.IsOpen || videoControls.IsOpen;

        public bool IsFailureOpen => failure.IsOpen;

        /// <summary>True while gameplay input should be suppressed (any modal, fade, or free cursor).</summary>
        public bool BlocksGameplay => IsModalOpen || cursorFree || fade.color.a > 0.95f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Build();
        }

        private void Build()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            EnsureEventSystem();

            var t = transform;

            // HUD (non-blocking)
            crosshair = UIFactory.Panel(t, "Crosshair", new Color(1f, 1f, 1f, 0.55f), false).GetComponent<Image>();
            crosshair.raycastTarget = false;
            UIFactory.Center(crosshair.rectTransform, new Vector2(4f, 4f));

            prompt = UIFactory.Label(t, "Prompt", "", 18, UIFactory.Ink);
            UIFactory.Center(prompt.rectTransform, new Vector2(600f, 30f), new Vector2(0f, -60f));

            subtitle = UIFactory.Label(t, "Subtitle", "", 24, UIFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Italic);
            UIFactory.Place(subtitle.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(1200f, 80f));
            subtitle.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(1f, -1f);

            attempts = UIFactory.Label(t, "Attempts", "", 18, UIFactory.Brass, TextAnchor.UpperRight);
            UIFactory.Place(attempts.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -32f), new Vector2(400f, 60f));
            attempts.gameObject.SetActive(false);

            messageBackdrop = UIFactory.Panel(t, "MessageBackdrop", new Color(0f, 0f, 0f, 0f)).GetComponent<Image>();
            messageBackdrop.raycastTarget = false;
            message = UIFactory.Label(messageBackdrop.transform, "Message", "", 44, UIFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Center(message.rectTransform, new Vector2(1400f, 200f), new Vector2(0f, 20f));
            messageSub = UIFactory.Label(messageBackdrop.transform, "MessageSub", "", 22, UIFactory.Dim, TextAnchor.MiddleCenter);
            UIFactory.Center(messageSub.rectTransform, new Vector2(1200f, 80f), new Vector2(0f, -90f));
            messageBackdrop.gameObject.SetActive(false);

            // Modals
            keypad = new KeypadPanel(t);
            evidencePanel = new EvidencePanel(t);
            choice = new ChoicePanel(t, "ChoicePanel");
            failure = new ChoicePanel(t, "FailurePanel");
            mainMenu = new ChoicePanel(t, "MainMenuPanel");
            videoControls = new VideoControlsPanel(t);

            // Fade is last so it sits on top of everything.
            fade = UIFactory.Panel(t, "Fade", Color.black).GetComponent<Image>();
            fade.raycastTarget = true;
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var es = new GameObject("[EventSystem]");
            DontDestroyOnLoad(es);
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        private void Start()
        {
            StartCoroutine(InitialFadeSafety());
        }

        /// <summary>Guarantees the screen never stays black if no transition claims the first scene.</summary>
        private IEnumerator InitialFadeSafety()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            var gm = GameManager.Instance;
            bool transitioning = gm != null && gm.Transitions != null && gm.Transitions.IsTransitioning;
            if (!transitioning && fade.color.a > 0.99f) yield return FadeRoutine(0f, 1.2f);
        }

        private void Update()
        {
            // Back closes the top-most modal; Tab toggles evidence.
            if (VossInput.BackPressed())
            {
                if (keypad.IsOpen) keypad.HandleBack();
                else if (evidencePanel.IsOpen) evidencePanel.Close();
                else if (videoControls.IsOpen) videoControls.HandleBack();
                else if (choice.IsOpen) choice.HandleBack();
            }
            if (VossInput.EvidencePressed() && evidenceEnabled && !failure.IsOpen && !mainMenu.IsOpen)
            {
                if (evidencePanel.IsOpen) evidencePanel.Close();
                else if (!keypad.IsOpen && !choice.IsOpen && !videoControls.IsOpen) evidencePanel.Open();
            }

            bool free = IsModalOpen || cursorFree;
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = free;
            crosshair.enabled = !free && fade.color.a < 0.5f;
        }

        // ---------------------------------------------------------------- fade

        public void SetFade(float alpha)
        {
            var c = fade.color; c.a = Mathf.Clamp01(alpha); fade.color = c;
            fade.raycastTarget = c.a > 0.01f;
        }

        public Coroutine FadeTo(float alpha, float seconds)
        {
            return StartCoroutine(FadeRoutine(alpha, seconds));
        }

        private IEnumerator FadeRoutine(float target, float seconds)
        {
            float start = fade.color.a;
            float t = 0f;
            seconds = Mathf.Max(0.01f, seconds);
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                SetFade(Mathf.Lerp(start, target, t / seconds));
                yield return null;
            }
            SetFade(target);
        }

        // ----------------------------------------------------------------- HUD

        public void SetPrompt(string text) { prompt.text = text ?? string.Empty; }

        public void SetCrosshairHighlight(bool on)
        {
            crosshair.color = on ? new Color(UIFactory.Brass.r, UIFactory.Brass.g, UIFactory.Brass.b, 0.95f) : new Color(1f, 1f, 1f, 0.55f);
            crosshair.rectTransform.sizeDelta = on ? new Vector2(7f, 7f) : new Vector2(4f, 4f);
        }

        public void SetSubtitle(string text) { subtitle.text = text ?? string.Empty; }

        public void SetCursorFree(bool free) { cursorFree = free; }

        public void ShowAttempts(int remaining, float seconds = 3f)
        {
            attempts.text = $"ATTEMPTS REMAINING  {remaining} / {GameState.MaxAttempts}";
            attempts.color = remaining <= 1 ? UIFactory.Danger : UIFactory.Brass;
            attempts.gameObject.SetActive(true);
            if (attemptsRoutine != null) StopCoroutine(attemptsRoutine);
            attemptsRoutine = StartCoroutine(HideAfter(attempts.gameObject, seconds));
        }

        private IEnumerator HideAfter(GameObject go, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            go.SetActive(false);
        }

        // ------------------------------------------------------------ messages

        /// <summary>Large centred text, optionally over a black backdrop. Duration 0 keeps it on screen.</summary>
        public void ShowMessage(string main, string sub = "", float seconds = 3f, float backdropAlpha = 0f, Color? color = null)
        {
            if (messageRoutine != null) StopCoroutine(messageRoutine);
            message.text = main ?? string.Empty;
            message.color = color ?? UIFactory.Ink;
            messageSub.text = sub ?? string.Empty;
            messageBackdrop.color = new Color(0f, 0f, 0f, backdropAlpha);
            messageBackdrop.gameObject.SetActive(true);
            if (seconds > 0f) messageRoutine = StartCoroutine(HideMessageAfter(seconds));
        }

        public void HideMessage()
        {
            if (messageRoutine != null) StopCoroutine(messageRoutine);
            messageRoutine = null;
            messageBackdrop.gameObject.SetActive(false);
        }

        private IEnumerator HideMessageAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            messageBackdrop.gameObject.SetActive(false);
            messageRoutine = null;
        }

        /// <summary>Shows lines one after another. Caller can yield on the returned coroutine.</summary>
        public Coroutine ShowMessageSequence(IList<string> lines, float perLine, float backdropAlpha = 0f, Color? color = null)
        {
            return StartCoroutine(MessageSequence(lines, perLine, backdropAlpha, color));
        }

        private IEnumerator MessageSequence(IList<string> lines, float perLine, float backdropAlpha, Color? color)
        {
            foreach (var l in lines)
            {
                ShowMessage(l, "", 0f, backdropAlpha, color);
                yield return new WaitForSeconds(perLine);
            }
            HideMessage();
        }

        // ------------------------------------------------------------- modals

        public void ShowFailure(string title, string body, Action restart, Action menu)
        {
            CloseGameplayModals();
            failure.Open(title, body, new[] { "RESTART EXPERIMENT", "RETURN TO MAIN MENU" }, i =>
            {
                failure.Close();
                if (i == 0) restart?.Invoke(); else menu?.Invoke();
            }, false, null, new Color(0.12f, 0f, 0f, 0.92f));
        }

        public void ShowMainMenu(Action newGame, Action quit)
        {
            CloseGameplayModals();
            mainMenu.Open("THE VOSS EXPERIMENT", "You watched him disappear.",
                new[] { "BEGIN EXPERIMENT", "QUIT" }, i =>
                {
                    if (i == 0) { mainMenu.Close(); newGame?.Invoke(); }
                    else quit?.Invoke();
                }, false, null, new Color(0f, 0f, 0f, 0.55f));
        }

        public void HideMainMenu() { mainMenu.Close(); }

        public void OpenEvidence() { if (evidenceEnabled) evidencePanel.Open(); }

        public void CloseGameplayModals()
        {
            keypad.Close();
            evidencePanel.Close();
            choice.Close();
            videoControls.Close(false);
        }

        /// <summary>Clears every gameplay element. Fade coroutines are left alone so transitions complete.</summary>
        public void ResetUI()
        {
            if (attemptsRoutine != null) StopCoroutine(attemptsRoutine);
            if (messageRoutine != null) StopCoroutine(messageRoutine);
            attemptsRoutine = null;
            messageRoutine = null;
            CloseGameplayModals();
            failure.Close();
            mainMenu.Close();
            SetPrompt(string.Empty);
            SetSubtitle(string.Empty);
            HideMessage();
            attempts.gameObject.SetActive(false);
            cursorFree = false;
            evidenceEnabled = false;
        }
    }
}
