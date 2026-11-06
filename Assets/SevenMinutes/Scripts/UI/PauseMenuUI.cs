using UnityEngine;
using SevenMinutes.Audio;
using SevenMinutes.Core;
using SevenMinutes.Game;

namespace SevenMinutes.UI
{
    public class PauseMenuUI : MonoBehaviour
    {
        private bool settings;

        private void OnGUI()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Paused)
            {
                settings = false;
                return;
            }
            UIStyles.Ensure();
            GUI.depth = -100;
            float s = UIStyles.Scale;
            float w = Screen.width, h = Screen.height;
            UIStyles.Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, 0.7f));

            if (settings)
            {
                if (SettingsPanel.Draw()) settings = false;
                return;
            }

            GUI.Label(new Rect(0, h * 0.2f, w, 90 * s), "DURAKLATILDI", UIStyles.Title);
            float bw = 360 * s, bh = 60 * s, gap = 14 * s;
            float x = (w - bw) * 0.5f, y = h * 0.42f;
            if (GUI.Button(new Rect(x, y, bw, bh), "DEVAM ET", UIStyles.Button)) { Click(); gm.Resume(); }
            if (GUI.Button(new Rect(x, y + (bh + gap), bw, bh), "YENİDEN BAŞLA", UIStyles.Button)) { Click(); gm.Restart(); }
            if (GUI.Button(new Rect(x, y + (bh + gap) * 2, bw, bh), "AYARLAR", UIStyles.Button)) { Click(); settings = true; }
            if (GUI.Button(new Rect(x, y + (bh + gap) * 3, bw, bh), "ANA MENÜ", UIStyles.Button)) { Click(); gm.GoToMainMenu(); }
        }

        private static void Click() => AudioManager.Instance?.PlayUI(SoundId.Click);
    }
}
