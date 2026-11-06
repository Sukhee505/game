using UnityEngine;
using SevenMinutes.Audio;
using SevenMinutes.Core;
using SevenMinutes.Game;

namespace SevenMinutes.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        private bool settings;

        private void OnGUI()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.MainMenu) return;
            UIStyles.Ensure();
            GUI.depth = -100;
            float s = UIStyles.Scale;
            float w = Screen.width, h = Screen.height;

            UIStyles.Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, 0.45f));

            if (settings)
            {
                if (SettingsPanel.Draw()) settings = false;
                return;
            }

            float flicker = Random.value < 0.03f ? 0.4f : 1f;
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, flicker);
            GUI.Label(new Rect(0, h * 0.16f, w, 100 * s), "5 DAKİKA", UIStyles.Title);
            GUI.color = old;
            GUI.Label(new Rect(0, h * 0.16f + 92 * s, w, 50 * s), "E V", UIStyles.Subtitle);
            GUI.Label(new Rect(0, h * 0.16f + 140 * s, w, 40 * s),
                "<i>Uyan. Çıkış yolunu bul. Sandığından daha az zamanın var.</i>", UIStyles.LabelCenter);

            float bw = 360 * s, bh = 62 * s;
            float x = (w - bw) * 0.5f;
            float y = h * 0.52f;
            if (GUI.Button(new Rect(x, y, bw, bh), "OYNA", UIStyles.Button))
            {
                AudioManager.Instance?.PlayUI(SoundId.Click);
                gm.StartNewGame();
            }
            if (GUI.Button(new Rect(x, y + bh + 14 * s, bw, bh), "AYARLAR", UIStyles.Button))
            {
                AudioManager.Instance?.PlayUI(SoundId.Click);
                settings = true;
            }
            if (GUI.Button(new Rect(x, y + (bh + 14 * s) * 2, bw, bh), "ÇIKIŞ", UIStyles.Button))
                gm.QuitGame();

            GUI.Label(new Rect(0, h - 60 * s, w, 40 * s),
                "WASD hareket · Shift koş · Space zıpla · Ctrl eğil · E etkileşim · Esc duraklat", UIStyles.LabelCenter);
        }
    }
}
