using System;
using System.Collections;
using UnityEngine;
using SevenMinutes.Audio;
using SevenMinutes.Core;
using SevenMinutes.Player;
using SevenMinutes.UI;

namespace SevenMinutes.Environment
{
    /// <summary>Large mechanical lever. Can be locked until a puzzle enables it.</summary>
    public class LeverController : MonoBehaviour, IInteractable
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Transform handle;
        [SerializeField] private Renderer indicator;
        [SerializeField] private bool usable;
        [SerializeField] private string lockedMessage = "Kol yerine kilitli.";

        private bool pulled;
        private MaterialPropertyBlock block;

        public bool IsPulled => pulled;
        public event Action Pulled;

        public string Prompt => "Kolu Çek";
        public bool CanInteract => !pulled;

        public void Configure(Transform handleT, Renderer light, bool startUsable, string lockedText)
        {
            handle = handleT;
            indicator = light;
            usable = startUsable;
            lockedMessage = lockedText;
        }

        private void Start()
        {
            block = new MaterialPropertyBlock();
            SetHandle(0f);
            UpdateIndicator();
        }

        public void SetUsable(bool value)
        {
            usable = value;
            UpdateIndicator();
        }

        public void Interact(PlayerController player)
        {
            if (!usable)
            {
                AudioManager.Instance?.Play(SoundId.Rattle, transform.position, 0.7f, 0.7f);
                GameHUD.Toast(lockedMessage, 2.2f);
                return;
            }
            if (pulled) return;
            pulled = true;
            StartCoroutine(PullRoutine());
        }

        private IEnumerator PullRoutine()
        {
            AudioManager.Instance?.Play(SoundId.Lever, transform.position, 1f);
            float t = 0f;
            while (t < 0.55f)
            {
                t += Time.deltaTime;
                SetHandle(Mathf.SmoothStep(0f, 1f, t / 0.55f));
                yield return null;
            }
            SetHandle(1f);
            UpdateIndicator();
            Pulled?.Invoke();
        }

        private void SetHandle(float k)
        {
            if (handle != null) handle.localRotation = Quaternion.Euler(Mathf.Lerp(-40f, 40f, k), 0f, 0f);
        }

        private void UpdateIndicator()
        {
            if (indicator == null || block == null) return;
            Color c = pulled ? new Color(0.2f, 0.6f, 1f) : usable ? new Color(0.1f, 1f, 0.25f) : new Color(1f, 0.1f, 0.05f);
            indicator.GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            block.SetColor(EmissionId, c * 2.5f);
            indicator.SetPropertyBlock(block);
        }
    }
}
