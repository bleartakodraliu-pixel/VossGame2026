using UnityEngine;
using UnityEngine.SceneManagement;
using Voss.Audio;
using Voss.Narrative;
using Voss.UI;

namespace Voss.Core
{
    /// <summary>
    /// Persistent root for all global services. Self-bootstraps before the first scene loads,
    /// so every scene (including ones opened directly in the editor) has a complete runtime.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private GameState state = new GameState();
        private AttemptManager attempts;
        private SceneTransitionManager transitions;
        private EvidenceManager evidence;
        private AudioManager audioManager;
        private SubtitleController subtitles;

        public GameState State => state;
        public AttemptManager Attempts => attempts;
        public SceneTransitionManager Transitions => transitions;
        public EvidenceManager Evidence => evidence;
        public AudioManager Audio => audioManager;
        public SubtitleController Subtitles => subtitles;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[VossRuntime]");
            go.AddComponent<GameManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            GameSettings.Load();

            attempts = gameObject.AddComponent<AttemptManager>();
            attempts.Bind(state);
            transitions = gameObject.AddComponent<SceneTransitionManager>();
            evidence = gameObject.AddComponent<EvidenceManager>();
            audioManager = gameObject.AddComponent<AudioManager>();
            subtitles = gameObject.AddComponent<SubtitleController>();

            // UI is a separate persistent object so it can sit on its own canvas sort order.
            if (UIManager.Instance == null)
            {
                var uiGo = new GameObject("[VossUI]");
                DontDestroyOnLoad(uiGo);
                uiGo.AddComponent<UIManager>();
            }

            state.StatusChanged += OnStatusChanged;
            state.AttemptsChanged += OnAttemptsChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            state.StatusChanged -= OnStatusChanged;
            state.AttemptsChanged -= OnAttemptsChanged;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Opening a gameplay scene directly in the editor still gets a valid "Playing" status.
            if (scene.name != SceneNames.MainMenu && state.Status == GameStatus.NotStarted)
            {
                state.SetStatus(GameStatus.Playing);
            }
            if (scene.name == SceneNames.MainMenu)
            {
                UIManager.Instance?.SetCursorFree(true);
            }
            // Scenes opened directly (editor play from any scene, or first launch) still fade in.
            if (!transitions.IsTransitioning)
            {
                UIManager.Instance?.FadeTo(0f, 1.2f);
            }
        }

        // ---------------------------------------------------------------- flow

        /// <summary>From the menu: step into the theatre shown behind the menu.</summary>
        public void StartNewGame()
        {
            ResetAll();
            state.SetStatus(GameStatus.Playing);
            state.SetStage(GameStage.Introduction);
            transitions.LoadScene(SceneNames.Introduction);
        }

        public void RestartExperiment()
        {
            ResetAll();
            state.SetStatus(GameStatus.Playing);
            state.SetStage(GameStage.Introduction);
            transitions.LoadScene(SceneNames.Introduction);
        }

        public void ReturnToMainMenu()
        {
            ResetAll();
            transitions.LoadScene(SceneNames.MainMenu);
        }

        public void CompleteGame()
        {
            state.SetStatus(GameStatus.Completed);
        }

        private void ResetAll()
        {
            Time.timeScale = 1f;
            state.Reset();
            evidence.ResetEvidence();
            audioManager.StopMusic();
            UIManager.Instance?.ResetUI();
        }

        // ------------------------------------------------------------ handlers

        private void OnAttemptsChanged(int remaining)
        {
            UIManager.Instance?.ShowAttempts(remaining);
        }

        private void OnStatusChanged(GameStatus status)
        {
            if (status == GameStatus.Lost)
            {
                audioManager.StopMusic();
                audioManager.Play(SfxId.Reject);
                UIManager.Instance?.ShowFailure("EXPERIMENT FAILED", "You accepted the illusion.",
                    RestartExperiment, ReturnToMainMenu);
            }
        }

        /// <summary>Convenience for Voss voice lines: subtitle + low voice cue.</summary>
        public void VossSays(string line, float duration = 3.5f)
        {
            audioManager.Play(SfxId.VossVoice);
            subtitles.Show(line, duration);
        }
    }
}
