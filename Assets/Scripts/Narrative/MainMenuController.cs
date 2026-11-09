using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.UI;

namespace Voss.Narrative
{
    /// <summary>Main menu scene: slow camera drift over a dark set, menu panel via UIManager.</summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Transform cameraRig;
        [SerializeField] private float driftDegreesPerSecond = 1.5f;

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
            if (cameraRig != null) cameraRig.Rotate(Vector3.up, driftDegreesPerSecond * Time.deltaTime, Space.World);
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
