using UnityEngine;
using SevenMinutes.Audio;
using SevenMinutes.Core;
using SevenMinutes.Game;
using SevenMinutes.Puzzles;

namespace SevenMinutes.UI
{
    /// <summary>Four-digit door keypad (mouse or number keys).</summary>
    public class KeypadUI : MonoBehaviour
    {
        public static KeypadUI Instance { get; private set; }

        private KeypadController target;
        private string entry = "";
        private string status = "ŞİFREYİ GİR";
        private Color statusColor = new Color(0.4f, 1f, 0.5f);
        private float closeAt = -1f;
        private float openedAt;

        public bool IsOpen => target != null;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Open(KeypadController keypad)
        {
            target = keypad;
            entry = "";
            status = "ŞİFREYİ GİR";
            statusColor = new Color(0.4f, 1f, 0.5f);
            closeAt = -1f;
            openedAt = Time.unscaledTime;
            GameManager.Instance?.OpenModal(this);
        }

        public void Close()
        {
            if (target == null) return;
            target = null;
            GameManager.Instance?.CloseModal(this);
        }

        private void Update()
        {
            if (target == null) return;
            var gm = GameManager.Instance;
            if (gm != null && (gm.State == GameState.Defeat || gm.State == GameState.Victory)) { Close(); return; }
            if (closeAt > 0f && Time.unscaledTime >= closeAt) { Close(); return; }
            if (Time.unscaledTime - openedAt > 0.2f && InputReader.Down(GameKey.Pause)) Close();
        }

        private void Press(string key)
        {
            if (closeAt > 0f) return;
            AudioManager.Instance?.PlayUI(SoundId.KeyPress, 0.5f);
            if (key == "C")
            {
                entry = "";
                return;
            }
            if (key == "OK")
            {
                Submit();
                return;
            }
            if (entry.Length < 4) entry += key;
            if (entry.Length == 4) Submit();
        }

        private void Submit()
        {
            if (entry.Length < 4)
            {
                status = "4 RAKAM GEREKLİ";
                statusColor = new Color(1f, 0.7f, 0.2f);
                return;
            }
            if (target.Submit(entry))
            {
                status = "ERİŞİM ONAYLANDI";
                statusColor = new Color(0.3f, 1f, 0.4f);
                closeAt = Time.unscaledTime + 0.9f;
            }
            else
            {
                status = "HATA — YANLIŞ ŞİFRE";
                statusColor = new Color(1f, 0.2f, 0.15f);
                entry = "";
            }
        }

        private void OnGUI()
        {
            if (target == null) return;
            UIStyles.Ensure();
            GUI.depth = -10;
            float s = UIStyles.Scale;

            var e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode >= KeyCode.Alpha0 && e.keyCode <= KeyCode.Alpha9) { Press(((int)(e.keyCode - KeyCode.Alpha0)).ToString()); e.Use(); }
                else if (e.keyCode >= KeyCode.Keypad0 && e.keyCode <= KeyCode.Keypad9) { Press(((int)(e.keyCode - KeyCode.Keypad0)).ToString()); e.Use(); }
                else if (e.keyCode == KeyCode.Backspace) { if (entry.Length > 0) entry = entry.Substring(0, entry.Length - 1); e.Use(); }
                else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) { Press("OK"); e.Use(); }
            }

            UIStyles.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.55f));
            var r = UIStyles.Centered(420 * s, 600 * s);
            GUILayout.BeginArea(r, UIStyles.Panel);
            GUILayout.Label("GÜVENLİK TUŞ TAKIMI", UIStyles.Subtitle);
            GUILayout.Space(8 * s);

            string shown = entry.PadRight(4, '_');
            GUILayout.Label(string.Join(" ", shown.ToCharArray()), UIStyles.KeypadDisplay, GUILayout.Height(80 * s));
            var st = new GUIStyle(UIStyles.LabelCenter) { fontSize = UIStyles.Px(18) };
            st.normal.textColor = statusColor;
            GUILayout.Label(status, st);
            GUILayout.Space(10 * s);

            string[,] keys = { { "1", "2", "3" }, { "4", "5", "6" }, { "7", "8", "9" }, { "C", "0", "OK" } };
            for (int row = 0; row < 4; row++)
            {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < 3; col++)
                {
                    if (GUILayout.Button(keys[row, col], UIStyles.Keypad, GUILayout.Height(72 * s))) Press(keys[row, col]);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label("Rakam tuşları / Enter · Kapatmak için Esc", new GUIStyle(UIStyles.LabelCenter) { fontSize = UIStyles.Px(15) });
            GUILayout.EndArea();
        }
    }
}
