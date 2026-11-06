using UnityEngine;
using SevenMinutes.Audio;
using SevenMinutes.Core;
using SevenMinutes.Game;
using SevenMinutes.Puzzles;

namespace SevenMinutes.UI
{
    /// <summary>Dark terminal screen used to type messages to the robot (Level 3).</summary>
    public class ComputerTerminalUI : MonoBehaviour
    {
        public static ComputerTerminalUI Instance { get; private set; }

        private const string InputName = "TerminalInput";

        private ComputerTerminal terminal;
        private string input = "";
        private float closeAt = -1f;
        private float openedAt;
        private Vector2 scroll;
        private bool focusPending;

        public bool IsOpen => terminal != null;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Open(ComputerTerminal t)
        {
            terminal = t;
            input = "";
            closeAt = -1f;
            openedAt = Time.unscaledTime;
            focusPending = true;
            GameManager.Instance?.OpenModal(this);
        }

        public void Close()
        {
            if (terminal == null) return;
            terminal = null;
            GameManager.Instance?.CloseModal(this);
        }

        public void CloseAfter(float seconds) => closeAt = Time.unscaledTime + seconds;

        private void Update()
        {
            if (terminal == null) return;
            var gm = GameManager.Instance;
            if (gm != null && (gm.State == GameState.Defeat || gm.State == GameState.Victory)) { Close(); return; }
            if (closeAt > 0f && Time.unscaledTime >= closeAt) Close();
        }

        private void Send()
        {
            string msg = (input ?? "").Trim();
            if (msg.Length == 0) return;
            input = "";
            AudioManager.Instance?.PlayUI(SoundId.Beep, 0.5f);
            terminal.Send(msg);
            scroll.y = float.MaxValue;
        }

        private void OnGUI()
        {
            if (terminal == null) return;
            UIStyles.Ensure();
            GUI.depth = -10;
            float s = UIStyles.Scale;

            var e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Escape && Time.unscaledTime - openedAt > 0.2f) { e.Use(); Close(); return; }
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) { e.Use(); Send(); }
            }

            UIStyles.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.5f));
            var r = UIStyles.Centered(860 * s, 560 * s);
            GUILayout.BeginArea(r, UIStyles.Terminal);

            var green = new GUIStyle(UIStyles.Label) { fontSize = UIStyles.Px(22), font = null };
            green.normal.textColor = new Color(0.35f, 1f, 0.45f);
            var dim = new GUIStyle(green);
            dim.normal.textColor = new Color(0.2f, 0.6f, 0.3f);

            GUILayout.Label("UNIT-7 UZAK BAĞLANTI  v2.31   ::   BAĞLANTI KURULDU", dim);
            GUILayout.Label("Robot için bir mesaj yaz ve ENTER'a bas.", dim);
            GUILayout.Space(10 * s);

            scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
            foreach (var line in terminal.Log) GUILayout.Label(line, green);
            if (Mathf.Repeat(Time.unscaledTime, 1f) < 0.5f) GUILayout.Label("_", green);
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            GUI.SetNextControlName(InputName);
            input = GUILayout.TextField(input ?? "", 60, UIStyles.TerminalInput, GUILayout.Height(48 * s));
            if (GUILayout.Button("GÖNDER", UIStyles.Button, GUILayout.Width(150 * s), GUILayout.Height(48 * s))) Send();
            GUILayout.EndHorizontal();
            GUILayout.Label("[Esc] kapat", dim);
            GUILayout.EndArea();

            if (focusPending)
            {
                GUI.FocusControl(InputName);
                focusPending = false;
            }
        }
    }
}
