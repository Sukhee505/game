using System;
using UnityEngine;
using SevenMinutes.Audio;
using SevenMinutes.Core;
using SevenMinutes.Player;
using SevenMinutes.Robot;
using SevenMinutes.UI;

namespace SevenMinutes.Puzzles
{
    /// <summary>Heavy pressure plate: only the (heavy) robot activates it. The player is too light.</summary>
    public class PressurePlate : MonoBehaviour, IPlayerTrigger
    {
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Transform plateTop;
        [SerializeField] private Renderer glow;
        [SerializeField] private float radius = 0.8f;

        private RobotController robot;
        private Vector3 topRest;
        private bool told;
        private MaterialPropertyBlock block;

        public bool Pressed { get; private set; }
        public event Action Activated;
        public event Action Released;

        public void Configure(Transform top, Renderer glowRenderer) { plateTop = top; glow = glowRenderer; }

        private void Start()
        {
            robot = Game.GameManager.Instance != null ? Game.GameManager.Instance.Robot : null;
            if (plateTop != null) topRest = plateTop.localPosition;
            block = new MaterialPropertyBlock();
            SetGlow(new Color(0.6f, 0.1f, 0.05f));
        }

        private void Update()
        {
            if (robot == null) return;
            Vector3 a = robot.Position, b = transform.position;
            a.y = 0f; b.y = 0f;
            bool on = Vector3.Distance(a, b) < radius && Mathf.Abs(robot.Position.y - transform.position.y) < 1f;
            if (on == Pressed) return;
            Pressed = on;
            if (plateTop != null) plateTop.localPosition = topRest + (on ? Vector3.down * 0.06f : Vector3.zero);
            AudioManager.Instance?.Play(SoundId.Clank, transform.position, 0.6f, 0.6f);
            SetGlow(on ? new Color(0.1f, 0.9f, 0.3f) : new Color(0.6f, 0.1f, 0.05f));
            if (on) Activated?.Invoke();
            else Released?.Invoke();
        }

        public void OnPlayerEnter(PlayerController player)
        {
            if (Pressed || told) return;
            told = true;
            AudioManager.Instance?.Play(SoundId.Click, transform.position, 0.5f, 0.6f);
            GameHUD.Toast("Plaka kıpırdamıyor. Çok hafifsin — ağır bir şey gerekiyor.", 3f);
        }

        public void OnPlayerExit(PlayerController player) { }

        private void SetGlow(Color c)
        {
            if (glow == null || block == null) return;
            glow.GetPropertyBlock(block);
            block.SetColor(EmissionId, c * 2f);
            glow.SetPropertyBlock(block);
        }
    }
}
