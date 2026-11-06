using UnityEngine;
using SevenMinutes.Audio;
using SevenMinutes.Core;

namespace SevenMinutes.Traps
{
    public enum LaserMode
    {
        Static,
        MovingVertical
    }

    /// <summary>Red laser beam across the corridor. Static (jump / crouch) or moving up and down.</summary>
    public class LaserTrap : MonoBehaviour, IHazard
    {
        [SerializeField] private Transform beam;
        [SerializeField] private LaserMode mode = LaserMode.Static;
        [SerializeField] private float minY = 0.25f;
        [SerializeField] private float maxY = 2.4f;
        [SerializeField] private float holdBottom = 0.7f;
        [SerializeField] private float moveTime = 1.1f;
        [SerializeField] private float holdTop = 1.8f;
        [SerializeField] private Light[] emitterLights;

        private float baseThickness;
        private float cycleTime;

        public bool IsDangerous => true;
        public string DeathMessage => "Lazer seni yaktı!";
        public FailKind Kind => FailKind.Laser;

        public void Configure(Transform beamT, LaserMode m, float bottom, float top, Light[] lights)
        {
            beam = beamT; mode = m; minY = bottom; maxY = top; emitterLights = lights;
        }

        private void Start()
        {
            baseThickness = beam.localScale.y;
            AudioManager.Instance?.CreateLoop(SoundId.LaserHum, beam, 0.25f, 9f, mode == LaserMode.Static ? 1f : 1.15f);
        }

        private void Update()
        {
            float flick = 1f + 0.25f * Mathf.Sin(Time.time * 40f + transform.position.z);
            var s = beam.localScale;
            s.y = baseThickness * flick;
            s.z = baseThickness * flick;
            beam.localScale = s;

            if (mode != LaserMode.MovingVertical) return;
            float total = holdBottom + moveTime + holdTop + moveTime;
            cycleTime = Mathf.Repeat(Time.time, total);
            float y;
            if (cycleTime < holdBottom) y = minY;
            else if (cycleTime < holdBottom + moveTime) y = Mathf.Lerp(minY, maxY, Mathf.SmoothStep(0f, 1f, (cycleTime - holdBottom) / moveTime));
            else if (cycleTime < holdBottom + moveTime + holdTop) y = maxY;
            else y = Mathf.Lerp(maxY, minY, Mathf.SmoothStep(0f, 1f, (cycleTime - holdBottom - moveTime - holdTop) / moveTime));

            var p = transform.localPosition;
            p.y = y;
            transform.localPosition = p;
            if (emitterLights != null)
                foreach (var l in emitterLights) if (l != null) l.intensity = cycleTime > holdBottom + moveTime && cycleTime < holdBottom + moveTime + holdTop ? 0.6f : 1.4f;
        }
    }
}
