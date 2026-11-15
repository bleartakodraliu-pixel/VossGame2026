using UnityEngine;

namespace Voss.Core
{
    /// <summary>Player options from the main menu, stored in PlayerPrefs and applied on start.</summary>
    public static class GameSettings
    {
        private const string VolumeKey = "voss.volume";
        private const string SensitivityKey = "voss.sensitivity";
        private const string FullscreenKey = "voss.fullscreen";

        public static float MasterVolume { get; private set; } = 0.9f;
        public static float MouseSensitivity { get; private set; } = 1f;   // multiplier on the controller's base sensitivity
        public static bool Fullscreen { get; private set; } = true;

        public static void Load()
        {
            MasterVolume = PlayerPrefs.GetFloat(VolumeKey, 0.9f);
            MouseSensitivity = PlayerPrefs.GetFloat(SensitivityKey, 1f);
            Fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
            Apply();
        }

        public static void SetVolume(float value) { MasterVolume = Mathf.Clamp01(value); Save(); }
        public static void SetSensitivity(float value) { MouseSensitivity = Mathf.Clamp(value, 0.2f, 3f); Save(); }
        public static void SetFullscreen(bool value) { Fullscreen = value; Save(); }

        private static void Save()
        {
            PlayerPrefs.SetFloat(VolumeKey, MasterVolume);
            PlayerPrefs.SetFloat(SensitivityKey, MouseSensitivity);
            PlayerPrefs.SetInt(FullscreenKey, Fullscreen ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
        }

        private static void Apply()
        {
            AudioListener.volume = MasterVolume;
            if (!Application.isEditor && Screen.fullScreen != Fullscreen) Screen.fullScreen = Fullscreen;
        }
    }
}
