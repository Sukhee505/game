using UnityEngine;
using SevenMinutes.Core;
using SevenMinutes.Game;

namespace SevenMinutes.Player
{
    /// <summary>
    /// Third-person orbit camera with wall collision, crouch height, camera shake and a cinematic mode
    /// (used by the intro and the victory sequence).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PlayerCamera : MonoBehaviour
    {
        [SerializeField] private PlayerController target;
        [SerializeField] private float distance = 3.1f;
        [SerializeField] private float standPivotHeight = 1.6f;
        [SerializeField] private float crouchPivotHeight = 1.05f;
        [SerializeField] private float shoulderOffset = 0.45f;
        [SerializeField] private float minPitch = -35f;
        [SerializeField] private float maxPitch = 70f;
        [SerializeField] private float collisionRadius = 0.22f;

        private float yaw;
        private float pitch = 12f;
        private float pivotHeight;
        private float currentDistance;

        private bool cinematic;
        private Vector3 cinePos;
        private Quaternion cineRot;
        private float cineSharpness = 4f;
        private float blendBack;     // 1 → 0 when returning from cinematic
        private float blendBackSpeed = 1f;
        private Vector3 blendFromPos;
        private Quaternion blendFromRot;

        private Vector3 smoothTarget;
        private Vector3 smoothVelocity;
        private Vector2 smoothLook;
        private bool hasSmoothTarget;

        private float shakeAmplitude;
        private float shakeTime;

        public Camera Cam { get; private set; }
        public float Yaw => yaw;
        public Quaternion PlanarRotation => Quaternion.Euler(0f, yaw, 0f);
        public bool IsCinematic => cinematic;

        public void Configure(PlayerController player) => target = player;

        private void Awake()
        {
            Cam = GetComponent<Camera>();
            pivotHeight = standPivotHeight;
            currentDistance = distance;
            if (target != null) yaw = target.transform.eulerAngles.y;
        }

        public void SnapBehind()
        {
            if (target == null) return;
            yaw = target.transform.eulerAngles.y;
            pitch = 12f;
            hasSmoothTarget = false;
        }

        /// <summary>Move the camera smoothly toward a fixed pose (higher blend time = slower).</summary>
        public void SetCinematic(Vector3 position, Quaternion rotation, float blendTime)
        {
            if (!cinematic && blendTime <= 0f)
            {
                transform.SetPositionAndRotation(position, rotation);
            }
            cinematic = true;
            cinePos = position;
            cineRot = rotation;
            cineSharpness = blendTime <= 0.01f ? 1000f : 3f / blendTime;
            if (blendTime <= 0.01f) transform.SetPositionAndRotation(position, rotation);
        }

        /// <summary>Return to the gameplay camera, blending over the given time.</summary>
        public void ClearCinematic(float blendTime)
        {
            if (!cinematic) return;
            cinematic = false;
            blendFromPos = transform.position;
            blendFromRot = transform.rotation;
            blendBack = 1f;
            blendBackSpeed = blendTime <= 0.01f ? 1000f : 1f / blendTime;
        }

        public void Shake(float amplitude, float duration)
        {
            shakeAmplitude = Mathf.Max(shakeAmplitude, amplitude);
            shakeTime = Mathf.Max(shakeTime, duration);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;

            if (cinematic)
            {
                float k = 1f - Mathf.Exp(-cineSharpness * dt);
                transform.position = Vector3.Lerp(transform.position, cinePos, k);
                transform.rotation = Quaternion.Slerp(transform.rotation, cineRot, k);
                ApplyShake(dt);
                return;
            }
            if (target == null) return;

            var gm = GameManager.Instance;
            if (gm != null && gm.CanControlPlayer)
            {
                Vector2 raw = InputReader.Look * GameSettings.MouseSensitivity;
                smoothLook = Vector2.Lerp(smoothLook, raw, 1f - Mathf.Exp(-35f * dt));
                Vector2 look = smoothLook;
                yaw += look.x;
                pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
            }

            float targetPivot = target.IsCrouching ? crouchPivotHeight : standPivotHeight;
            pivotHeight = Mathf.Lerp(pivotHeight, targetPivot, 1f - Mathf.Exp(-10f * dt));

            if (!hasSmoothTarget || (smoothTarget - target.transform.position).sqrMagnitude > 9f)
            {
                smoothTarget = target.transform.position;
                smoothVelocity = Vector3.zero;
                hasSmoothTarget = true;
            }
            smoothTarget = Vector3.SmoothDamp(smoothTarget, target.transform.position, ref smoothVelocity, 0.06f);
            Vector3 pivot = smoothTarget + Vector3.up * pivotHeight;
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 shoulder = rot * new Vector3(shoulderOffset, 0f, 0f);
            Vector3 back = rot * Vector3.back;

            // keep the shoulder offset inside walls
            Vector3 shoulderPivot = pivot;
            if (Physics.SphereCast(pivot, collisionRadius, shoulder.normalized, out var sh, shoulderOffset,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                shoulderPivot = pivot + shoulder.normalized * Mathf.Max(0f, sh.distance - 0.05f);
            else
                shoulderPivot = pivot + shoulder;

            float wanted = distance;
            if (Physics.SphereCast(shoulderPivot, collisionRadius, back, out var hit, distance,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                wanted = Mathf.Max(0.3f, hit.distance - 0.05f);

            currentDistance = wanted < currentDistance
                ? wanted
                : Mathf.Lerp(currentDistance, wanted, 1f - Mathf.Exp(-6f * dt));

            Vector3 pos = shoulderPivot + back * currentDistance;

            if (blendBack > 0f)
            {
                blendBack = Mathf.Max(0f, blendBack - dt * blendBackSpeed);
                float s = Mathf.SmoothStep(0f, 1f, blendBack);
                pos = Vector3.Lerp(pos, blendFromPos, s);
                rot = Quaternion.Slerp(rot, blendFromRot, s);
            }

            transform.SetPositionAndRotation(pos, rot);
            ApplyShake(dt);
        }

        private void ApplyShake(float dt)
        {
            if (shakeTime <= 0f) return;
            shakeTime -= dt;
            float a = shakeAmplitude * Mathf.Clamp01(shakeTime * 3f);
            transform.position += new Vector3(
                (Mathf.PerlinNoise(Time.time * 25f, 0f) - 0.5f) * a,
                (Mathf.PerlinNoise(0f, Time.time * 25f) - 0.5f) * a,
                0f);
            if (shakeTime <= 0f) shakeAmplitude = 0f;
        }
    }
}
