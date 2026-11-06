using System;
using System.Collections.Generic;
using UnityEngine;
using SevenMinutes.Audio;
using SevenMinutes.Core;
using SevenMinutes.Game;

namespace SevenMinutes.UI
{
    public enum Speaker { Player, Npc, System }

    /// <summary>View abstraction so the NPC logic does not depend on IMGUI (Dependency Inversion).</summary>
    public interface IDialogueView
    {
        event Action<string> Submitted;
        event Action CloseRequested;
        bool IsOpen { get; }
        void Open(string npcName);
        void Close();
        void AddLine(Speaker speaker, string text);
        void SetThinking(bool thinking);
        void SetTrust(int trust);
        void SetStatus(string status);
        void ClearLines();
    }

    /// <summary>Free-text chat window for the LLM guard (modal, does not block the game loop).</summary>
    public class DialogueUI : MonoBehaviour, IDialogueView
    {
        private const string InputName = "DialogueInput";

        private readonly List<(Speaker who, string text)> lines = new List<(Speaker, string)>();
        private string npcName = "NPC";
        private string input = "";
        private bool thinking;
        private float thinkingStart;
        private int trust = -1;
        private int lastDelta;
        private float deltaTime = -10f;
        public int Threshold { get; set; } = 60;
        private string status = "";
        private Vector2 scroll;
        private bool focusPending;
        private float openedAt;

        public event Action<string> Submitted;
        public event Action CloseRequested;
        public bool IsOpen { get; private set; }

        public void Open(string name)
        {
            npcName = name;
            IsOpen = true;
            input = "";
            focusPending = true;
            openedAt = Time.unscaledTime;
            GameManager.Instance?.OpenModal(this);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            GameManager.Instance?.CloseModal(this);
        }

        public void AddLine(Speaker speaker, string text)
        {
            lines.Add((speaker, text));
            if (lines.Count > 40) lines.RemoveAt(0);
            scroll.y = float.MaxValue;
        }

        public void SetThinking(bool value)
        {
            thinking = value;
            if (value) thinkingStart = Time.unscaledTime;
            else focusPending = true;
        }

        public void SetTrust(int value)
        {
            if (trust >= 0 && value != trust)
            {
                lastDelta = value - trust;
                deltaTime = Time.unscaledTime;
            }
            trust = value;
        }
        public void SetStatus(string value) => status = value;
        public void ClearLines() { lines.Clear(); trust = -1; }

        private void Update()
        {
            if (!IsOpen) return;
            var gm = GameManager.Instance;
            if (gm != null && (gm.State == GameState.Defeat || gm.State == GameState.Victory)) Close();
        }

        private void TrySubmit()
        {
            string text = (input ?? "").Trim();
            if (text.Length == 0 || thinking) return;
            input = "";
            AudioManager.Instance?.PlayUI(SoundId.KeyPress, 0.4f);
            Submitted?.Invoke(text);
        }

        private void OnGUI()
        {
            if (!IsOpen) return;
            UIStyles.Ensure();
            GUI.depth = -10;
            float s = UIStyles.Scale;

            var e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Escape && Time.unscaledTime - openedAt > 0.2f) { e.Use(); CloseRequested?.Invoke(); return; }
                if ((e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) && !thinking) { e.Use(); TrySubmit(); }
            }

            float w = Mathf.Min(Screen.width * 0.72f, 1100 * s), h = Screen.height * 0.46f;
            var area = new Rect((Screen.width - w) / 2f, Screen.height - h - 20f, w, h);
            GUILayout.BeginArea(area, UIStyles.Panel);

            GUILayout.BeginHorizontal();
            GUILayout.Label("<b>" + npcName + "</b>  <size=" + UIStyles.Px(15) + "><color=#888888>(yerel LLM)</color></size>", UIStyles.Subtitle, GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace();
            if (trust >= 0) DrawTrustSlider(s);
            GUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(status)) GUILayout.Label("<size=" + UIStyles.Px(15) + "><color=#888888>" + status + "</color></size>", UIStyles.LabelSmall);

            scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
            foreach (var (who, text) in lines) GUILayout.Label(Format(who, text), UIStyles.Label);
            if (thinking)
            {
                int dots = 1 + (int)(Time.unscaledTime * 3f) % 3;
                float secs = Time.unscaledTime - thinkingStart;
                GUILayout.Label($"<i><color=#ff8c6b>{npcName} düşünüyor{new string('.', dots)} ({secs:0.0} sn)</color></i>", UIStyles.Label);
            }
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            GUI.enabled = !thinking;
            GUI.SetNextControlName(InputName);
            input = GUILayout.TextField(input ?? "", 200, UIStyles.TerminalInput, GUILayout.ExpandWidth(true), GUILayout.Height(46 * s));
            if (GUILayout.Button("GÖNDER", UIStyles.Button, GUILayout.Width(150 * s), GUILayout.Height(46 * s))) TrySubmit();
            GUI.enabled = true;
            if (GUILayout.Button("KAPAT", UIStyles.Button, GUILayout.Width(130 * s), GUILayout.Height(46 * s))) CloseRequested?.Invoke();
            GUILayout.EndHorizontal();
            GUILayout.Label("<size=" + UIStyles.Px(14) + ">[Enter] gönder   [Esc] kapat</size>", UIStyles.LabelSmall);
            GUILayout.EndArea();

            if (focusPending && !thinking)
            {
                GUI.FocusControl(InputName);
                focusPending = false;
            }
        }

        /// <summary>"Güven" slider: red → yellow → green, white mark at the threshold, floating +/- change.</summary>
        private void DrawTrustSlider(float s)
        {
            GUILayout.Label("<b>GÜVEN</b>", UIStyles.Label, GUILayout.ExpandWidth(false));
            var bar = GUILayoutUtility.GetRect(320 * s, 26 * s, GUILayout.Width(320 * s), GUILayout.Height(26 * s));
            bar.y += 6 * s;
            float k = Mathf.Clamp01(trust / 100f);
            Color fill = k < 0.5f ? Color.Lerp(new Color(0.85f, 0.12f, 0.08f), new Color(0.95f, 0.8f, 0.15f), k * 2f)
                                  : Color.Lerp(new Color(0.95f, 0.8f, 0.15f), new Color(0.2f, 0.9f, 0.35f), (k - 0.5f) * 2f);
            UIStyles.Fill(bar, new Color(0f, 0f, 0f, 0.7f));
            UIStyles.Fill(new Rect(bar.x + 2, bar.y + 2, (bar.width - 4) * k, bar.height - 4), fill);
            float mark = bar.x + bar.width * Threshold / 100f;
            UIStyles.Fill(new Rect(mark - 1.5f * s, bar.y - 4 * s, 3 * s, bar.height + 8 * s), Color.white);
            GUILayout.Label($"<b>{trust}</b>/100", UIStyles.Label, GUILayout.ExpandWidth(false));

            float age = Time.unscaledTime - deltaTime;
            if (age < 2.5f && lastDelta != 0)
            {
                string col = lastDelta > 0 ? "#5dff7a" : "#ff5a4a";
                string txt = (lastDelta > 0 ? "+" : "") + lastDelta;
                GUILayout.Label($"<color={col}><b>{txt}</b></color>", UIStyles.Label, GUILayout.Width(60 * s));
            }
            else GUILayout.Space(60 * s);
        }

        private string Format(Speaker who, string text)
        {
            string safe = (text ?? "").Replace("<", "‹").Replace(">", "›");
            switch (who)
            {
                case Speaker.Player: return "<color=#8fd3ff><b>Sen:</b></color> " + safe;
                case Speaker.Npc: return "<color=#ff7a5c><b>" + npcName + ":</b></color> " + safe;
                default: return "<color=#ffcc55><i>[Sistem] " + safe + "</i></color>";
            }
        }
    }
}
