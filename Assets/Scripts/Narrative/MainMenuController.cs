using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.UI;

namespace Voss.Narrative
{
    /// <summary>Main menu scene: the camera breathes gently over the theatre set; menu panel via UIManager.</summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Transform cameraRig;
        [SerializeField] private float swayDegrees = 1.6f;

        public void Configure(Transform rig) { cameraRig = rig; }

        private void Start()
        {
            var gm = GameManager.Instance;
            var ui = UIManager.Instance;
            if (ui != null)
            {
                ui.ResetUI();
                ui.SetCursorFree(true);
                ui.ShowMainMenu(OnNewGame, OnQuit);
            }
            gm?.Audio.PlayMusic(MusicId.Drone, 3f);
        }

        private void Update()
        {
            if (cameraRig == null) return;
            float t = Time.time;
            cameraRig.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.11f) * swayDegrees * 0.35f, Mathf.Sin(t * 0.07f) * swayDegrees, 0f);
        }

        private void OnNewGame()
        {
            UIManager.Instance?.SetCursorFree(false);
            GameManager.Instance?.StartNewGame();
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
