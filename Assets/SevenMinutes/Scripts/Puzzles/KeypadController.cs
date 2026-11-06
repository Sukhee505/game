using System;
using System.Collections;
using UnityEngine;
using SevenMinutes.Audio;
using SevenMinutes.Core;
using SevenMinutes.Environment;
using SevenMinutes.Player;
using SevenMinutes.UI;

namespace SevenMinutes.Puzzles
{
    /// <summary>Four-digit keypad next to the bedroom door.</summary>
    public class KeypadController : MonoBehaviour, IInteractable
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private DoorController door;
        [SerializeField] private Renderer statusLight;
        [SerializeField] private string code = "6674";

        private bool unlocked;
        private MaterialPropertyBlock block;

        public event Action Unlocked;
        public event Action WrongCode;
        public bool IsUnlocked => unlocked;

        [SerializeField] private string prompt = "Tuş Takımını Kullan";

        public string Prompt => prompt;
        public void SetPrompt(string value) => prompt = value;
        public bool CanInteract => !unlocked;

        public void Configure(DoorController d, Renderer light) { door = d; statusLight = light; }

        public void SetCode(string value) => code = value;

        private void Start()
        {
            block = new MaterialPropertyBlock();
            SetLight(new Color(1f, 0.15f, 0.1f));
        }

        public void Interact(PlayerController player)
        {
            AudioManager.Instance?.Play(SoundId.Beep, transform.position, 0.5f);
            KeypadUI.Instance?.Open(this);
        }

        public bool Submit(string entry)
        {
            if (unlocked) return true;
            if (entry == code)
            {
                unlocked = true;
                SetLight(new Color(0.1f, 1f, 0.25f));
                AudioManager.Instance?.Play(SoundId.Success, transform.position, 0.8f);
                StartCoroutine(OpenDoorDelayed());
                Unlocked?.Invoke();
                return true;
            }
            AudioManager.Instance?.Play(SoundId.Error, transform.position, 0.9f);
            StartCoroutine(ErrorFlash());
            WrongCode?.Invoke();
            return false;
        }

        private IEnumerator OpenDoorDelayed()
        {
            yield return new WaitForSeconds(0.5f);
            AudioManager.Instance?.Play(SoundId.Unlock, transform.position, 1f);
            yield return new WaitForSeconds(0.6f);
            if (door != null) door.Unlock(true);
        }

        private IEnumerator ErrorFlash()
        {
            for (int i = 0; i < 3; i++)
            {
                SetLight(Color.black);
                yield return new WaitForSeconds(0.1f);
                SetLight(new Color(1f, 0.15f, 0.1f));
                yield return new WaitForSeconds(0.1f);
            }
        }

        private void SetLight(Color c)
        {
            if (statusLight == null || block == null) return;
            statusLight.GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            block.SetColor(EmissionId, c * 3f);
            statusLight.SetPropertyBlock(block);
        }
    }
}
