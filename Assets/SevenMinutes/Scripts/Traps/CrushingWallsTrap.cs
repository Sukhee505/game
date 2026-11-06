using System.Collections;
using UnityEngine;
using SevenMinutes.Audio;
using SevenMinutes.Core;
using SevenMinutes.Game;

namespace SevenMinutes.Traps
{
    /// <summary>
    /// Two massive walls that slam together: open 4 s → close 0.8 s → closed 0.8 s → open 0.8 s → repeat.
    /// Touching a wall while it moves or is closed sends the player back to the start of Level 4.
    /// </summary>
    public class CrushingWallsTrap : MonoBehaviour, IHazard
    {
        [SerializeField] private Transform leftWall;
        [SerializeField] private Transform rightWall;
        [SerializeField] private float travel = 1.4f;
        [SerializeField] private float openTime = 4f;
        [SerializeField] private float closeTime = 0.8f;
        [SerializeField] private float closedTime = 0.8f;
        [SerializeField] private float openingTime = 0.8f;
        [SerializeField] private Light[] warningLights;
        [SerializeField] private ParticleSystem dust;

        private Vector3 leftOpen, rightOpen;
        private bool dangerous;
        private AudioSource grind;

        public bool IsDangerous => dangerous;
        public string DeathMessage => "Duvarlar seni ezdi!";
        public FailKind Kind => FailKind.Crush;

        public void Configure(Transform left, Transform right, float travelDistance, Light[] warnings, ParticleSystem dustFx)
        {
            leftWall = left; rightWall = right; travel = travelDistance; warningLights = warnings; dust = dustFx;
        }

        private void Start()
        {
            leftOpen = leftWall.localPosition;
            rightOpen = rightWall.localPosition;
            SetWarning(0f);
            StartCoroutine(Cycle());
        }

        private IEnumerator Cycle()
        {
            var audio = AudioManager.Instance;
            while (true)
            {
                dangerous = false;
                SetWarning(0f);
                yield return new WaitForSeconds(openTime - 0.6f);
                // pre-warning blink
                float w = 0f;
                while (w < 0.6f) { w += Time.deltaTime; SetWarning(Mathf.Repeat(w * 8f, 1f) < 0.5f ? 1f : 0.2f); yield return null; }

                dangerous = true;
                if (audio != null) grind = audio.CreateLoop(SoundId.Grind, transform, 0.7f, 22f, 0.8f);
                yield return Move(0f, 1f, closeTime);
                if (grind != null) Destroy(grind.gameObject);
                audio?.Play(SoundId.Slam, transform.position, 1f);
                if (dust != null) dust.Play();
                ShakeIfNear(0.25f);

                float c = 0f;
                while (c < closedTime) { c += Time.deltaTime; SetWarning(1f); yield return null; }

                if (audio != null) grind = audio.CreateLoop(SoundId.Grind, transform, 0.5f, 22f, 0.7f);
                yield return Move(1f, 0f, openingTime);
                if (grind != null) Destroy(grind.gameObject);
            }
        }

        private IEnumerator Move(float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                k = to > from ? k * k : k; // accelerate when closing
                leftWall.localPosition = leftOpen + Vector3.right * travel * k;
                rightWall.localPosition = rightOpen + Vector3.left * travel * k;
                SetWarning(1f);
                yield return null;
            }
            leftWall.localPosition = leftOpen + Vector3.right * travel * to;
            rightWall.localPosition = rightOpen + Vector3.left * travel * to;
        }

        private void ShakeIfNear(float amplitude)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Player == null) return;
            float d = Vector3.Distance(gm.Player.transform.position, transform.position);
            if (d < 14f) gm.Player.CameraRig.Shake(amplitude * (1f - d / 14f) + 0.05f, 0.45f);
        }

        private void SetWarning(float k)
        {
            if (warningLights == null) return;
            foreach (var l in warningLights) if (l != null) l.intensity = 2.5f * k;
        }
    }
}
