using System;
using UnityEngine;
using SevenMinutes.Audio;
using SevenMinutes.Core;
using SevenMinutes.Game;
using SevenMinutes.Puzzles;

namespace SevenMinutes.UI
{
    /// <summary>Reads notes, newspapers, diaries and screens found in the house.</summary>
    public class NoteUI : MonoBehaviour
    {
        public static NoteUI Instance { get; private set; }
        public static event Action<NoteData> NoteRead;

        private NoteData note;
        private float openedAt;

        public bool IsOpen => note != null;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            NoteRead = null;
        }

        public void Show(NoteData data)
        {
            if (data == null) return;
            note = data;
            openedAt = Time.unscaledTime;
            GameManager.Instance?.OpenModal(this);
            AudioManager.Instance?.Play(SoundId.Paper, null, 0.7f);
            NoteRead?.Invoke(data);
        }

        public void Close()
        {
            if (note == null) return;
            note = null;
            GameManager.Instance?.CloseModal(this);
        }

        private void Update()
        {
            if (note == null) return;
            var gm = GameManager.Instance;
            if (gm != null && (gm.State == GameState.Defeat || gm.State == GameState.Victory)) { Close(); return; }
            if (Time.unscaledTime - openedAt < 0.25f) return;
            if (InputReader.Down(GameKey.Interact) || InputReader.Down(GameKey.Pause)) Close();
        }

        private void OnGUI()
        {
            if (note == null) return;
            UIStyles.Ensure();
            GUI.depth = -10;
            float s = UIStyles.Scale;
            UIStyles.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.6f));

            var r = UIStyles.Centered(720 * s, 640 * s);

            // close with the mouse: click outside the paper, or on the close button
            var e = Event.current;
            if (e.type == EventType.MouseDown && !r.Contains(e.mousePosition) && Time.unscaledTime - openedAt > 0.25f)
            {
                e.Use();
                Close();
                return;
            }
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape && Time.unscaledTime - openedAt > 0.25f)
            {
                e.Use();
                Close();
                return;
            }
            GUILayout.BeginArea(r, UIStyles.Paper);
            var title = UIStyles.Lock(new GUIStyle(UIStyles.PaperText) { fontSize = UIStyles.Px(30), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter });
            GUILayout.Label(note.title, title);
            GUILayout.Space(16 * s);
            GUILayout.Label(note.body, UIStyles.PaperText);
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            bool clicked = GUILayout.Button("KAPAT  [Esc]", UIStyles.Button, GUILayout.Width(260 * s), GUILayout.Height(52 * s));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            if (clicked) Close();
        }
    }
}
