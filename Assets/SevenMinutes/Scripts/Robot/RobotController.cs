using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using SevenMinutes.Audio;
using SevenMinutes.Core;
using SevenMinutes.Game;
using SevenMinutes.Player;

namespace SevenMinutes.Robot
{
    public enum RobotMode
    {
        Inactive,
        Follow,
        MoveTo,
        Waiting,
        Busy
    }

    /// <summary>
    /// Robot companion brain. Movement always comes from NavMesh path finding (NavMeshAgent):
    /// it follows the player at a comfortable distance, walks to places it is sent to, presses buttons,
    /// stands on plates, and recovers when stuck (re-path, then a discreet warp only when off-screen).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(ObstacleSensor))]
    public class RobotController : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private RobotRig rig;
        [SerializeField] private float followDistance = 2.2f;
        [SerializeField] private float startFollowDistance = 3.2f;
        [SerializeField] private float repathInterval = 0.3f;
        [SerializeField] private float farWarpDistance = 22f;

        private NavMeshAgent agent;
        private ObstacleSensor sensor;
        private float sensorTimer;
        private float baseSpeed;
        private Action onArrive;
        private Vector3 destination;
        private float repathTimer;
        private float stuckTimer;
        private float moveTimer;
        private bool followingMoving;
        private Transform faceTarget;
        private Vector3? interestPoint;
        private float interestUntil;

        public RobotMode Mode { get; private set; } = RobotMode.Inactive;
        public RobotRig Rig => rig;
        public ObstacleSensor Sensor => sensor;
        /// <summary>How many times the robot had to be teleported (stuck recovery). Used by the automated tests.</summary>
        public int WarpCount { get; private set; }
        public float Speed => agent != null && agent.enabled ? agent.velocity.magnitude : 0f;
        public bool IsBusy => Mode == RobotMode.MoveTo || Mode == RobotMode.Busy;
        public Vector3 Position => transform.position;

        public void Configure(PlayerController p, RobotRig r)
        {
            player = p;
            rig = r;
        }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            sensor = GetComponent<ObstacleSensor>();
            baseSpeed = agent.speed;
        }

        private void Start()
        {
            // NavMesh is baked at runtime (RuntimeNavMeshBaker, Awake) – enable the agent afterwards.
            if (!agent.enabled) agent.enabled = true;
            if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out var hit, 3f, NavMesh.AllAreas))
                agent.Warp(hit.position);
            agent.updateRotation = true;
        }

        // ---------------- commands ----------------

        /// <summary>Wake the robot up (intro).</summary>
        public void Boot(Action onBooted = null) => StartCoroutine(BootRoutine(onBooted));

        private IEnumerator BootRoutine(Action onBooted)
        {
            AudioManager.Instance?.Play(SoundId.PowerUp, transform.position, 0.6f);
            rig.SetPower(true);
            yield return new WaitForSeconds(1.4f);
            AudioManager.Instance?.Play(SoundId.RobotChirp, transform.position, 0.6f);
            Mode = RobotMode.Follow;
            onBooted?.Invoke();
        }

        public void Follow()
        {
            onArrive = null;
            faceTarget = null;
            Mode = RobotMode.Follow;
            followingMoving = true;
            repathTimer = 0f;
        }

        public void MoveTo(Vector3 position, Action arrived = null)
        {
            if (!agent.isOnNavMesh) TryRecoverOntoNavMesh();
            destination = Sample(position);
            onArrive = arrived;
            faceTarget = null;
            Mode = RobotMode.MoveTo;
            stuckTimer = 0f;
            moveTimer = 0f;
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.stoppingDistance = 0.15f;
                agent.SetDestination(destination);
            }
        }

        /// <summary>Walk somewhere and wait there, facing the given target.</summary>
        public void WaitAt(Vector3 position, Transform face)
        {
            MoveTo(position, () => TurnToward(face));
        }

        public void PressButton(Transform standPoint, Transform button, Action onPressed)
        {
            MoveTo(standPoint.position, () => StartCoroutine(PressRoutine(button, onPressed)));
        }

        private IEnumerator PressRoutine(Transform button, Action onPressed)
        {
            Mode = RobotMode.Busy;
            yield return TurnRoutine(button.position, 0.4f);
            yield return rig.PressRoutine(onPressed);
            Mode = RobotMode.Waiting;
        }

        public void TurnToward(Transform target)
        {
            faceTarget = target;
            if (Mode != RobotMode.Busy && Mode != RobotMode.MoveTo) Mode = RobotMode.Waiting;
        }

        /// <summary>Instant placement on the NavMesh (used only by the automated tests). Does not count as a warp.</summary>
        public void PlaceForTest(Vector3 position)
        {
            if (!agent.isOnNavMesh) TryRecoverOntoNavMesh();
            agent.Warp(Sample(position));
            agent.ResetPath();
            onArrive = null;
            Mode = RobotMode.Waiting;
        }

        /// <summary>Briefly look at something interesting (head only).</summary>
        public void LookAt(Vector3 point, float seconds)
        {
            interestPoint = point;
            interestUntil = Time.time + seconds;
        }

        // ---------------- update ----------------

        private void Update()
        {
            if (Mode == RobotMode.Inactive || player == null) return;
            if (!agent.isOnNavMesh)
            {
                TryRecoverOntoNavMesh();
                return;
            }

            float dt = Time.deltaTime;

            // Raycast sensor: perceive distance/angle to obstacles, slow down when something is right ahead.
            sensorTimer -= dt;
            if (sensorTimer <= 0f)
            {
                sensorTimer = 0.1f;
                sensor.Scan();
                agent.speed = sensor.FrontClearance < 0.7f ? baseSpeed * 0.55f : baseSpeed;
            }

            switch (Mode)
            {
                case RobotMode.Follow: UpdateFollow(dt); break;
                case RobotMode.MoveTo: UpdateMoveTo(dt); break;
                case RobotMode.Waiting:
                case RobotMode.Busy:
                    if (faceTarget != null && Mode == RobotMode.Waiting) FaceSmooth(faceTarget.position, dt);
                    break;
            }

            UpdateLook();
        }

        private void UpdateFollow(float dt)
        {
            Vector3 p = player.transform.position;
            float dist = FlatDistance(transform.position, p);
            float vertical = Mathf.Abs(transform.position.y - p.y);

            // Far away and not visible → discreet catch-up (e.g. the player fell back to a checkpoint).
            if ((dist > farWarpDistance || vertical > 3.5f && dist > 10f) && !VisibleToCamera() && player.IsGrounded)
            {
                Vector3 behind = p - player.transform.forward * 1.8f;
                if (NavMesh.SamplePosition(behind, out var hit, 2.5f, NavMesh.AllAreas) && !PointVisible(hit.position))
                {
                    agent.Warp(hit.position);
                    WarpCount++;
                    return;
                }
            }

            if (dist > startFollowDistance) followingMoving = true;
            else if (dist < followDistance) followingMoving = false;

            if (followingMoving)
            {
                repathTimer -= dt;
                if (repathTimer <= 0f)
                {
                    repathTimer = repathInterval;
                    agent.isStopped = false;
                    agent.stoppingDistance = followDistance * 0.8f;
                    agent.SetDestination(FollowPoint());
                }
            }
            else
            {
                if (agent.hasPath) agent.ResetPath();
                if (dist < 6f) FaceSmooth(p, dt);
            }
        }

        private void UpdateMoveTo(float dt)
        {
            moveTimer += dt;
            if (!agent.pathPending)
            {
                float remaining = agent.hasPath ? agent.remainingDistance : FlatDistance(transform.position, destination);
                if (remaining <= 0.35f || FlatDistance(transform.position, destination) <= 0.3f)
                {
                    Arrive();
                    return;
                }
            }

            // stuck handling
            if (agent.velocity.sqrMagnitude < 0.01f && !agent.pathPending) stuckTimer += dt;
            else stuckTimer = Mathf.Max(0f, stuckTimer - dt);

            if (stuckTimer > 2.5f && stuckTimer - dt <= 2.5f) agent.SetDestination(destination);
            bool longStuck = stuckTimer > 6f && !VisibleToCamera();
            bool softlock = moveTimer > 25f && stuckTimer > 4f; // never block progression
            if (longStuck || softlock)
            {
                agent.Warp(destination);
                WarpCount++;
                Arrive();
            }
        }

        private void Arrive()
        {
            agent.ResetPath();
            Mode = RobotMode.Waiting;
            var cb = onArrive;
            onArrive = null;
            cb?.Invoke();
        }

        private void UpdateLook()
        {
            if (rig == null) return;
            if (interestPoint.HasValue && Time.time < interestUntil) rig.LookPoint = interestPoint;
            else if (FlatDistance(transform.position, player.transform.position) < 9f)
                rig.LookPoint = player.transform.position + Vector3.up * (player.IsCrouching ? 1.0f : 1.6f);
            else rig.LookPoint = null;
        }

        private Vector3 FollowPoint()
        {
            Vector3 p = player.transform.position;
            Vector3 offset = -player.transform.forward * 1.3f + player.transform.right * 0.9f;
            if (NavMesh.SamplePosition(p + offset, out var hit, 1.5f, NavMesh.AllAreas)) return hit.position;
            if (NavMesh.SamplePosition(p, out hit, 2.5f, NavMesh.AllAreas)) return hit.position;
            return p;
        }

        private void FaceSmooth(Vector3 point, float dt)
        {
            Vector3 dir = point - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            var target = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, 240f * dt);
        }

        private IEnumerator TurnRoutine(Vector3 point, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                FaceSmooth(point, Time.deltaTime * 2f);
                yield return null;
            }
        }

        private void TryRecoverOntoNavMesh()
        {
            if (!agent.enabled) agent.enabled = true;
            if (NavMesh.SamplePosition(transform.position, out var hit, 4f, NavMesh.AllAreas)) agent.Warp(hit.position);
        }

        private static Vector3 Sample(Vector3 p)
        {
            return NavMesh.SamplePosition(p, out var hit, 2f, NavMesh.AllAreas) ? hit.position : p;
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private bool VisibleToCamera() => PointVisible(transform.position + Vector3.up * 0.5f);

        private bool PointVisible(Vector3 point)
        {
            var cam = player != null && player.CameraRig != null ? player.CameraRig.Cam : Camera.main;
            if (cam == null) return false;
            Vector3 vp = cam.WorldToViewportPoint(point);
            if (vp.z <= 0f || vp.x < -0.05f || vp.x > 1.05f || vp.y < -0.05f || vp.y > 1.05f) return false;
            return !Physics.Linecast(cam.transform.position, point, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }
    }
}
