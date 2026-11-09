using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Voss.UI;

namespace Voss.Core
{
    /// <summary>Fade-out, load scene, fade-in. Guards against double loads.</summary>
    public class SceneTransitionManager : MonoBehaviour
    {
        [SerializeField] private float fadeDuration = 1.0f;

        public bool IsTransitioning { get; private set; }
        public event Action<string> SceneLoaded;

        public void LoadScene(string sceneName, float holdBlackSeconds = 0.4f)
        {
            if (IsTransitioning) return;
            StartCoroutine(LoadRoutine(sceneName, holdBlackSeconds));
        }

        private IEnumerator LoadRoutine(string sceneName, float hold)
        {
            IsTransitioning = true;
            var ui = UIManager.Instance;
            if (ui != null) yield return ui.FadeTo(1f, fadeDuration);

            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (op == null)
            {
                Debug.LogError($"[SceneTransition] Scene '{sceneName}' not found in build settings.");
                IsTransitioning = false;
                if (ui != null) yield return ui.FadeTo(0f, fadeDuration);
                yield break;
            }
            while (!op.isDone) yield return null;

            if (hold > 0f) yield return new WaitForSeconds(hold);
            SceneLoaded?.Invoke(sceneName);
            if (ui != null) yield return ui.FadeTo(0f, fadeDuration);
            IsTransitioning = false;
        }
    }
}
