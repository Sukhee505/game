using System;
using UnityEngine;

namespace SevenMinutes.Core
{
    /// <summary>Player preferences (persisted with PlayerPrefs).</summary>
    public static class GameSettings
    {
        private const string Prefix = "7MIN_";

        public static float MouseSensitivity = 2f;
        public static float MasterVolume = 1f;
        public static float MusicVolume = 0.7f;
        public static float SfxVolume = 1f;
        public static bool Fullscreen = true;
        public static int Quality = -1;

        public static event Action Changed;

        private static bool loaded;

        public static void Load()
        {
            if (loaded) return;
            loaded = true;
            MouseSensitivity = PlayerPrefs.GetFloat(Prefix + "Sens", 2f);
            MasterVolume = PlayerPrefs.GetFloat(Prefix + "Master", 1f);
            MusicVolume = PlayerPrefs.GetFloat(Prefix + "Music", 0.7f);
            SfxVolume = PlayerPrefs.GetFloat(Prefix + "Sfx", 1f);
            Fullscreen = PlayerPrefs.GetInt(Prefix + "Full", Screen.fullScreen ? 1 : 0) == 1;
            Quality = PlayerPrefs.GetInt(Prefix + "Quality", QualitySettings.GetQualityLevel());
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat(Prefix + "Sens", MouseSensitivity);
            PlayerPrefs.SetFloat(Prefix + "Master", MasterVolume);
            PlayerPrefs.SetFloat(Prefix + "Music", MusicVolume);
            PlayerPrefs.SetFloat(Prefix + "Sfx", SfxVolume);
            PlayerPrefs.SetInt(Prefix + "Full", Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "Quality", Quality);
            PlayerPrefs.Save();
        }

        public static void Apply()
        {
            AudioListener.volume = Mathf.Clamp01(MasterVolume);
            int levels = QualitySettings.names.Length;
            if (Quality >= 0 && Quality < levels && Quality != QualitySettings.GetQualityLevel())
                QualitySettings.SetQualityLevel(Quality, true);
            if (!Application.isEditor && Screen.fullScreen != Fullscreen)
                Screen.fullScreen = Fullscreen;
            Changed?.Invoke();
        }
    }
}
