using System;
using System.Collections;
using UnityEngine;
using SevenMinutes.Audio;
using SevenMinutes.Core;
using SevenMinutes.Player;
using SevenMinutes.UI;

namespace SevenMinutes.Environment
{
    /// <summary>A physical button (player-pressable and/or pressed by the robot).</summary>
    public class PushButton : MonoBehaviour, IInteractable
    {
        [SerializeField] private string label = "Düğmeye Bas";
        [SerializeField] private Transform cap;
        [SerializeField] private bool playerCanPress = true;
        [SerializeField] private string playerRefusal = "";
        [SerializeField] private float cooldown = 1f;

        private float lastPress = -10f;
        private Vector3 capRest;

        public event Action Pressed;
        public bool Enabled = true;

        public string Prompt => label;
        public bool CanInteract => Enabled && Time.time - lastPress > cooldown;

        public void Configure(string text, Transform capT, bool byPlayer, string refusal)
        {
            label = text;
            cap = capT;
            playerCanPress = byPlayer;
            playerRefusal = refusal;
        }

        private void Awake()
        {
            if (cap != null) capRest = cap.localPosition;
        }

        public void Interact(PlayerController player)
        {
            if (!playerCanPress)
            {
                if (!string.IsNullOrEmpty(playerRefusal)) GameHUD.Toast(playerRefusal, 2.2f);
                return;
            }
            Press();
        }

        public void Press()
        {
            if (Time.time - lastPress < 0.2f) return;
            lastPress = Time.time;
            AudioManager.Instance?.Play(SoundId.Click, transform.position, 1f);
            if (cap != null) StartCoroutine(Animate());
            Pressed?.Invoke();
        }

        private IEnumerator Animate()
        {
            Vector3 down = capRest + Vector3.down * 0.04f;
            float t = 0f;
            while (t < 0.1f) { t += Time.deltaTime; cap.localPosition = Vector3.Lerp(capRest, down, t / 0.1f); yield return null; }
            yield return new WaitForSeconds(0.15f);
            t = 0f;
            while (t < 0.15f) { t += Time.deltaTime; cap.localPosition = Vector3.Lerp(down, capRest, t / 0.15f); yield return null; }
            cap.localPosition = capRest;
        }
    }
}
