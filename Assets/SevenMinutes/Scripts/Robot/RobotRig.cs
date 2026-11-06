using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using SevenMinutes.Audio;

namespace SevenMinutes.Robot
{
    /// <summary>
    /// Procedural animation for the robot companion: walking legs driven by the NavMeshAgent velocity,
    /// swinging arms, head tracking, glowing eyes (boot / blink) and the button-press arm motion.
    /// </summary>
    public class RobotRig : MonoBehaviour
    {
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Transform body;
        [SerializeField] private Transform head;
        [SerializeField] private Transform hipL, kneeL, hipR, kneeR;
        [SerializeField] private Transform shoulderL, elbowL, shoulderR, elbowR;
        [SerializeField] private Renderer[] glowRenderers;
        [SerializeField] private Light eyeLight;
        [SerializeField] private Color glowColor = new Color(0.25f, 0.65f, 1f);

        public Vector3? LookPoint;
        public float Power { get; private set; }

        private float powerTarget;
        private float phase;
        private float press;
        private float headYaw, headPitch;
        private float blinkTimer = 3f;
        private float blink = 1f;
        private Vector3 bodyBase;
        private float lastStepSign;
        private MaterialPropertyBlock block;

        public void Configure(NavMeshAgent a, Transform bodyT, Transform headT, Transform hL, Transform kL, Transform hR, Transform kR,
            Transform sL, Transform eL, Transform sR, Transform eR, Renderer[] glow, Light light)
        {
            agent = a; body = bodyT; head = headT; hipL = hL; kneeL = kL; hipR = hR; kneeR = kR;
            shoulderL = sL; elbowL = eL; shoulderR = sR; elbowR = eR; glowRenderers = glow; eyeLight = light;
        }

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            if (body != null) bodyBase = body.localPosition;
            ApplyGlow(0f);
        }

        public void SetPower(bool on, bool instant = false)
        {
            powerTarget = on ? 1f : 0f;
            if (instant) Power = powerTarget;
        }

        /// <summary>Raise the right arm to the button, call onContact, then lower it.</summary>
        public IEnumerator PressRoutine(Action onContact)
        {
            AudioManager.Instance?.Play(SoundId.Servo, transform.position, 0.6f);
            float t = 0f;
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                press = Mathf.SmoothStep(0f, 1f, t / 0.45f);
                yield return null;
            }
            press = 1f;
            onContact?.Invoke();
            yield return new WaitForSeconds(0.35f);
            AudioManager.Instance?.Play(SoundId.Servo, transform.position, 0.4f, 0.8f);
            t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                press = 1f - Mathf.SmoothStep(0f, 1f, t / 0.4f);
                yield return null;
            }
            press = 0f;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Power = Mathf.MoveTowards(Power, powerTarget, dt * 0.8f);

            Vector3 v = agent != null && agent.enabled ? agent.velocity : Vector3.zero;
            v.y = 0f;
            float speed = v.magnitude;
            float k = Mathf.Clamp01(speed / 1.8f);
            phase += dt * speed * 7.5f;
            float s = Mathf.Sin(phase), c = Mathf.Cos(phase);
            float amp = 32f * k;

            Rot(hipL, -s * amp, 0f);
            Rot(hipR, s * amp, 0f);
            Rot(kneeL, Mathf.Max(0f, c) * amp * 1.4f + 3f, 0f);
            Rot(kneeR, Mathf.Max(0f, -c) * amp * 1.4f + 3f, 0f);
            Rot(shoulderL, s * amp * 0.6f, -5f);
            float armR = -s * amp * 0.6f;
            Rot(shoulderR, Mathf.Lerp(armR, -88f, press), 5f);
            Rot(elbowL, -20f - 15f * k, 0f);
            Rot(elbowR, Mathf.Lerp(-20f - 15f * k, -4f, press), 0f);

            if (body != null)
            {
                float idle = Mathf.Sin(Time.time * 2f) * 0.006f * Power;
                body.localPosition = bodyBase + Vector3.up * (Mathf.Abs(c) * 0.025f * k + idle);
                body.localRotation = Quaternion.Euler(4f * k, 0f, s * 2.5f * k);
            }

            // footsteps
            float sign = Mathf.Sign(s);
            if (k > 0.15f && sign != lastStepSign)
            {
                AudioManager.Instance?.Play(SoundId.RobotStep, transform.position, 0.28f, UnityEngine.Random.Range(0.9f, 1.1f));
            }
            lastStepSign = sign;

            // head look
            float targetYaw = 0f, targetPitch = 0f;
            if (LookPoint.HasValue && head != null && Power > 0.2f)
            {
                Vector3 dir = LookPoint.Value - head.position;
                Vector3 local = Quaternion.Inverse(transform.rotation) * dir;
                targetYaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -75f, 75f);
                float flat = new Vector2(local.x, local.z).magnitude;
                targetPitch = Mathf.Clamp(-Mathf.Atan2(local.y, flat) * Mathf.Rad2Deg, -35f, 30f);
            }
            float hl = 1f - Mathf.Exp(-6f * dt);
            headYaw = Mathf.Lerp(headYaw, targetYaw, hl);
            headPitch = Mathf.Lerp(headPitch, targetPitch, hl);
            if (head != null) head.localRotation = Quaternion.Euler(headPitch, headYaw, 0f);

            // eyes
            blinkTimer -= dt;
            if (blinkTimer <= 0f)
            {
                blink = 0.05f;
                blinkTimer = UnityEngine.Random.Range(2.5f, 6f);
            }
            blink = Mathf.MoveTowards(blink, 1f, dt * 8f);
            float flicker = Power < 0.99f && Power > 0.01f ? (UnityEngine.Random.value < 0.3f ? 0.2f : 1f) : 1f;
            ApplyGlow(Power * blink * flicker);
        }

        private void ApplyGlow(float intensity)
        {
            if (glowRenderers != null)
            {
                foreach (var r in glowRenderers)
                {
                    if (r == null) continue;
                    r.GetPropertyBlock(block);
                    block.SetColor(EmissionId, glowColor * (intensity * 4f));
                    block.SetColor(BaseColorId, Color.Lerp(new Color(0.05f, 0.05f, 0.06f), glowColor, intensity));
                    r.SetPropertyBlock(block);
                }
            }
            if (eyeLight != null) eyeLight.intensity = intensity * 0.9f;
        }

        private static void Rot(Transform t, float x, float z)
        {
            if (t != null) t.localRotation = Quaternion.Euler(x, 0f, z);
        }
    }
}
