using UnityEngine;
using SevenMinutes.Core;

namespace SevenMinutes.UI
{
    /// <summary>Settings shared by the main menu and the pause menu.</summary>
    public static class SettingsPanel
    {
        /// <summary>Draws the panel. Returns true when the player pressed Back.</summary>
        public static bool Draw()
        {
            float s = UIStyles.Scale;
            var r = UIStyles.Centered(680 * s, 640 * s);
            GUILayout.BeginArea(r, UIStyles.Panel);
            GUILayout.Label("AYARLAR", UIStyles.Subtitle);
            GUILayout.Space(14 * s);

            GameSettings.MouseSensitivity = Row("Fare hassasiyeti", GameSettings.MouseSensitivity, 0.3f, 6f, GameSettings.MouseSensitivity.ToString("0.0"));
            GameSettings.MasterVolume = Row("Ana ses", GameSettings.MasterVolume, 0f, 1f, Pct(GameSettings.MasterVolume));
            GameSettings.MusicVolume = Row("Müzik sesi", GameSettings.MusicVolume, 0f, 1f, Pct(GameSettings.MusicVolume));
            GameSettings.SfxVolume = Row("Efekt sesi", GameSettings.SfxVolume, 0f, 1f, Pct(GameSettings.SfxVolume));

            GUILayout.Space(8 * s);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Tam ekran", UIStyles.Label, GUILayout.Width(300 * s));
            if (GUILayout.Button(GameSettings.Fullscreen ? "AÇIK" : "KAPALI", UIStyles.Button, GUILayout.Height(44 * s)))
                GameSettings.Fullscreen = !GameSettings.Fullscreen;
            GUILayout.EndHorizontal();

            GUILayout.Space(8 * s);
            var names = QualitySettings.names;
            if (names.Length > 0)
            {
                int q = Mathf.Clamp(GameSettings.Quality < 0 ? QualitySettings.GetQualityLevel() : GameSettings.Quality, 0, names.Length - 1);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Grafik kalitesi", UIStyles.Label, GUILayout.Width(300 * s));
                if (GUILayout.Button("<", UIStyles.Button, GUILayout.Width(50 * s), GUILayout.Height(44 * s))) q = Mathf.Max(0, q - 1);
                GUILayout.Label(names[q], UIStyles.LabelCenter, GUILayout.Height(44 * s));
                if (GUILayout.Button(">", UIStyles.Button, GUILayout.Width(50 * s), GUILayout.Height(44 * s))) q = Mathf.Min(names.Length - 1, q + 1);
                GUILayout.EndHorizontal();
                GameSettings.Quality = q;
            }

            GUILayout.FlexibleSpace();
            bool back = GUILayout.Button("GERİ", UIStyles.Button, GUILayout.Height(58 * s));
            GUILayout.EndArea();

            if (GUI.changed || back)
            {
                GameSettings.Apply();
                GameSettings.Save();
            }
            return back;
        }

        private static float Row(string label, float value, float min, float max, string display)
        {
            float s = UIStyles.Scale;
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, UIStyles.Label, GUILayout.Width(300 * s));
            GUILayout.BeginVertical();
            GUILayout.Space(14 * s);
            value = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(220 * s));
            GUILayout.EndVertical();
            GUILayout.Label(display, UIStyles.Label, GUILayout.Width(80 * s));
            GUILayout.EndHorizontal();
            GUILayout.Space(6 * s);
            return value;
        }

        private static string Pct(float v) => Mathf.RoundToInt(v * 100f) + "%";
    }
}
