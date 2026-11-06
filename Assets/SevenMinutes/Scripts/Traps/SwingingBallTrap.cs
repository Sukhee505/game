using UnityEngine;
using SevenMinutes.Audio;
using SevenMinutes.Core;

namespace SevenMinutes.Traps
{
    /// <summary>Huge spiked ball on a chain swinging across the corridor (pendulum motion).</summary>
    public class SwingingBallTrap : MonoBehaviour, IHazard
    {
        [SerializeField] private Transform pivot;
        [SerializeField] private Transform ball;
        [SerializeField] private float maxAngle = 62f;
        [SerializeField] private float period = 2.6f;
        [SerializeField] private float phaseOffset;

        private float lastAngle;
        private AudioSource chainLoop;

        public bool IsDangerous => true;
        public string DeathMessage => "Dikenli top sana çarptı!";
        public FailKind Kind => FailKind.Hazard;

        public void Configure(Transform pivotT, Transform ballT)
        {
            pivot = pivotT;
            ball = ballT;
        }

        private void Start()
        {
            chainLoop = AudioManager.Instance != null ? AudioManager.Instance.CreateLoop(SoundId.Grind, pivot, 0.15f, 14f, 1.6f) : null;
        }

        private void Update()
        {
            float t = (Time.time + phaseOffset) * Mathf.PI * 2f / period;
            float angle = maxAngle * Mathf.Sin(t);
            pivot.localRotation = Quaternion.Euler(0f, 0f, angle);
            if (ball != null) ball.localRotation = Quaternion.Euler(t * 20f, 0f, angle * 0.5f);

            if (Mathf.Sign(angle) != Mathf.Sign(lastAngle) && Mathf.Abs(lastAngle) < 30f)
                AudioManager.Instance?.Play(SoundId.Whoosh, ball != null ? ball.position : transform.position, 0.9f, 0.8f);
            lastAngle = angle;
        }
    }
}
