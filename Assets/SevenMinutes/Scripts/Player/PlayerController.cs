using System.Collections.Generic;
using UnityEngine;
using SevenMinutes.Audio;
using SevenMinutes.Core;
using SevenMinutes.Environment;
using SevenMinutes.Game;

namespace SevenMinutes.Player
{
    /// <summary>
    /// Third-person character movement: walk / run / crouch / jump with gravity (CharacterController).
    /// Also detects hazards, trigger zones and the floor piece under the player.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerCamera cameraRig;
        [SerializeField] private HumanoidRig rig;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 3.2f;
        [SerializeField] private float runSpeed = 5.8f;
        [SerializeField] private float crouchSpeed = 1.9f;
        [SerializeField] private float acceleration = 8f;
        [SerializeField] private float turnSpeed = 540f;
        [Tooltip("ON: the character always faces where the mouse/camera looks. OFF: faces the walking direction.")]
        [SerializeField] private bool faceMouseDirection = true;
        [SerializeField] private float mouseTurnSharpness = 15f;
        [SerializeField] private float jumpHeight = 1.15f;
        [SerializeField] private float gravity = -20f;

        [Header("Capsule")]
        [SerializeField] private float standHeight = 1.8f;
        [SerializeField] private float crouchHeight = 1.1f;

        private CharacterController cc;
        private Vector3 planarVelocity;
        private float verticalVelocity;
        private bool frozen;
        private bool kinematic;
        private float stepDistance;
        private Collider lastGroundCollider;

        private readonly Collider[] overlapBuffer = new Collider[48];
        private readonly HashSet<IPlayerTrigger> insideTriggers = new HashSet<IPlayerTrigger>();
        private readonly HashSet<IPlayerTrigger> currentTriggers = new HashSet<IPlayerTrigger>();
        private readonly List<IPlayerTrigger> exitBuffer = new List<IPlayerTrigger>();

        public PlayerCamera CameraRig => cameraRig;
        public HumanoidRig Rig => rig;
        public CharacterController Controller => cc;
        public bool IsGrounded { get; private set; } = true;
        public bool IsCrouching { get; private set; }
        public bool IsRunning { get; private set; }
        public float PlanarSpeed { get; private set; }
        public float VerticalVelocity => verticalVelocity;
        public float RunSpeed => runSpeed;
        public Collider GroundCollider { get; private set; }

        public bool ControlEnabled =>
            !frozen && !kinematic && GameManager.Instance != null && GameManager.Instance.CanControlPlayer;

        public Vector3 Chest => transform.position + Vector3.up * (IsCrouching ? 0.8f : 1.3f);

        public void Configure(PlayerCamera cam, HumanoidRig humanoid)
        {
            cameraRig = cam;
            rig = humanoid;
        }

        private void Awake()
        {
            cc = GetComponent<CharacterController>();
            ApplyCapsule(standHeight);
        }

        /// <summary>No input, but gravity still applies (used during respawns and cinematics on the floor).</summary>
        public void SetFrozen(bool value)
        {
            frozen = value;
            if (value) planarVelocity = Vector3.zero;
        }

        /// <summary>Fully script-driven (lying in bed, sitting): no physics, no collisions.</summary>
        public void SetKinematic(bool value)
        {
            kinematic = value;
            cc.enabled = !value;
            planarVelocity = Vector3.zero;
            verticalVelocity = 0f;
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            bool was = cc.enabled;
            cc.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            cc.enabled = was && !kinematic;
            planarVelocity = Vector3.zero;
            verticalVelocity = 0f;
            if (IsCrouching && CanStand()) SetCrouch(false);
            lastGroundCollider = null;
            if (cameraRig != null) cameraRig.SnapBehind();
        }

        private void Update()
        {
            if (kinematic)
            {
                PlanarSpeed = 0f;
                return;
            }

            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            bool control = ControlEnabled;

            Vector2 input = control ? InputReader.Move : Vector2.zero;

            // Crouch (hold Left Ctrl)
            bool wantCrouch = control && InputReader.Held(GameKey.Crouch);
            if (wantCrouch && !IsCrouching) SetCrouch(true);
            else if (!wantCrouch && IsCrouching && CanStand()) SetCrouch(false);

            IsRunning = control && InputReader.Held(GameKey.Run) && !IsCrouching && input.sqrMagnitude > 0.01f;
            float targetSpeed = input.sqrMagnitude < 0.01f ? 0f : IsCrouching ? crouchSpeed : IsRunning ? runSpeed : walkSpeed;

            Quaternion yaw = cameraRig != null ? cameraRig.PlanarRotation : transform.rotation;
            Vector3 desired = yaw * new Vector3(input.x, 0f, input.y);
            if (desired.sqrMagnitude > 1f) desired.Normalize();
            desired *= targetSpeed;

            float accel = IsGrounded ? acceleration : acceleration * 0.35f;
            planarVelocity = Vector3.Lerp(planarVelocity, desired, 1f - Mathf.Exp(-accel * dt));

            if (faceMouseDirection && control && cameraRig != null)
            {
                // always turn toward where the mouse (camera) is looking
                var targetRot = cameraRig.PlanarRotation;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-mouseTurnSharpness * dt));
            }
            else if (desired.sqrMagnitude > 0.01f)
            {
                var targetRot = Quaternion.LookRotation(desired.normalized, Vector3.up);
                var stepRot = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * dt);
                transform.rotation = Quaternion.Slerp(transform.rotation, stepRot, 1f - Mathf.Exp(-18f * dt));
            }

            // Gravity & jump
            if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            if (control && cc.isGrounded && InputReader.Down(GameKey.Jump))
            {
                if (IsCrouching && CanStand()) SetCrouch(false);
                if (!IsCrouching)
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    AudioManager.Instance?.Play(SoundId.Footstep, transform.position, 0.5f, 0.8f);
                }
            }
            verticalVelocity += gravity * dt;
            verticalVelocity = Mathf.Max(verticalVelocity, -40f);

            bool wasGrounded = IsGrounded;
            cc.Move((planarVelocity + Vector3.up * verticalVelocity) * dt);
            IsGrounded = cc.isGrounded;
            var flat = cc.velocity;
            flat.y = 0f;
            PlanarSpeed = flat.magnitude;

            if (IsGrounded && !wasGrounded && verticalVelocity < -6f)
                AudioManager.Instance?.Play(SoundId.Footstep, transform.position, 0.9f, 0.7f);

            UpdateGround();
            UpdateTriggers();
            UpdateFootsteps(dt);
        }

        private void SetCrouch(bool crouch)
        {
            IsCrouching = crouch;
            ApplyCapsule(crouch ? crouchHeight : standHeight);
        }

        private void ApplyCapsule(float height)
        {
            cc.height = height;
            cc.center = new Vector3(0f, height * 0.5f, 0f);
        }

        private bool CanStand()
        {
            float r = cc.radius * 0.9f;
            Vector3 origin = transform.position + Vector3.up * (crouchHeight - r);
            return !Physics.SphereCast(origin, r, Vector3.up, out _, standHeight - crouchHeight + 0.05f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        private void UpdateGround()
        {
            GroundCollider = null;
            if (!IsGrounded) return;
            if (Physics.Raycast(transform.position + Vector3.up * 0.3f, Vector3.down, out var hit, 0.7f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                GroundCollider = hit.collider;
                if (hit.collider != lastGroundCollider)
                {
                    lastGroundCollider = hit.collider;
                    var step = hit.collider.GetComponentInParent<ISteppable>();
                    step?.OnPlayerStep(this);
                }
            }
        }

        private void UpdateTriggers()
        {
            float r = cc.radius + 0.04f;
            Vector3 p0 = transform.position + Vector3.up * r;
            Vector3 p1 = transform.position + Vector3.up * Mathf.Max(r, cc.height - r);
            int n = Physics.OverlapCapsuleNonAlloc(p0, p1, r, overlapBuffer, ~0, QueryTriggerInteraction.Collide);

            currentTriggers.Clear();
            for (int i = 0; i < n; i++)
            {
                var col = overlapBuffer[i];
                if (col == null || col == cc) continue;

                var hazard = col.GetComponentInParent<IHazard>();
                if (hazard != null && hazard.IsDangerous && !kinematic)
                {
                    GameManager.Instance?.ReportFailure(hazard.DeathMessage, hazard.Kind);
                }

                var trig = col.GetComponent<IPlayerTrigger>();
                if (trig != null) currentTriggers.Add(trig);
            }

            foreach (var t in currentTriggers)
                if (insideTriggers.Add(t)) t.OnPlayerEnter(this);

            exitBuffer.Clear();
            foreach (var t in insideTriggers)
                if (!currentTriggers.Contains(t)) exitBuffer.Add(t);
            foreach (var t in exitBuffer)
            {
                insideTriggers.Remove(t);
                if (t is Object o && o != null) t.OnPlayerExit(this);
            }
        }

        private void UpdateFootsteps(float dt)
        {
            if (!IsGrounded || PlanarSpeed < 0.6f) return;
            stepDistance += PlanarSpeed * dt;
            float stride = IsRunning ? 1.7f : IsCrouching ? 0.8f : 1.15f;
            if (stepDistance < stride) return;
            stepDistance = 0f;

            var surface = SurfaceType.Wood;
            if (GroundCollider != null)
            {
                var fs = GroundCollider.GetComponentInParent<FootstepSurface>();
                if (fs != null) surface = fs.Surface;
            }
            float vol = IsCrouching ? 0.25f : IsRunning ? 0.75f : 0.5f;
            AudioManager.Instance?.PlayFootstep(surface, transform.position, vol);
        }
    }
}
