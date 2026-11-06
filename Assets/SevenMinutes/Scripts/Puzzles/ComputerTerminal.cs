using System;
using System.Collections.Generic;
using UnityEngine;
using SevenMinutes.Audio;
using SevenMinutes.Core;
using SevenMinutes.Player;
using SevenMinutes.UI;

namespace SevenMinutes.Puzzles
{
    /// <summary>Computer used to send text messages to the robot.</summary>
    public class ComputerTerminal : MonoBehaviour, IInteractable
    {
        [SerializeField] private TMPro.TMP_Text screenText;

        public readonly List<string> Log = new List<string>();
        public event Action<string> MessageSent;

        public string Prompt => "Bilgisayarı Kullan";
        public bool CanInteract => true;

        public void Configure(TMPro.TMP_Text text) => screenText = text;

        private void Start()
        {
            Log.Add("SİSTEM: UNIT-7 bağlantısı aktif.");
            Log.Add("SİSTEM: robot konumu — karşı platform.");
            RefreshScreen();
        }

        public void Interact(PlayerController player)
        {
            AudioManager.Instance?.Play(SoundId.Beep, transform.position, 0.5f, 0.8f);
            ComputerTerminalUI.Instance?.Open(this);
        }

        public void Send(string message)
        {
            Log.Add("> SEN: " + message);
            RefreshScreen();
            MessageSent?.Invoke(message);
        }

        public void Reply(string message)
        {
            Log.Add(message);
            RefreshScreen();
        }

        private void RefreshScreen()
        {
            if (screenText == null) return;
            int start = Mathf.Max(0, Log.Count - 4);
            var lines = new List<string>();
            for (int i = start; i < Log.Count; i++)
            {
                string l = Log[i];
                lines.Add(l.Length > 28 ? l.Substring(0, 28) : l);
            }
            screenText.text = string.Join("\n", lines);
        }
    }
}
