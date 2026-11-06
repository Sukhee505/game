using System.Collections.Generic;
using UnityEngine;

namespace SevenMinutes.Robot
{
    public readonly struct SensorReading
    {
        public readonly float Angle;     // degrees, relative to forward (- left / + right)
        public readonly float Distance;  // metres (== range when nothing was hit)
        public readonly bool Hit;
        public readonly Collider Collider;

        public SensorReading(float angle, float distance, bool hit, Collider collider)
        {
            Angle = angle;
            Distance = distance;
            Hit = hit;
            Collider = collider;
        }
    }

    /// <summary>
    /// Raycast fan used by the companion to perceive distance and angle to surrounding obstacles
    /// and to verify line of sight to its interaction target.
    /// </summary>
    public class ObstacleSensor : MonoBehaviour
    {
        [SerializeField] private int rayCount = 9;
        [SerializeField] private float fieldOfView = 120f;
        [SerializeField] private float range = 5f;
        [SerializeField] private float eyeHeight = 0.5f;
        [SerializeField] private LayerMask mask = ~0;

        private readonly List<SensorReading> readings = new List<SensorReading>();

        public IReadOnlyList<SensorReading> Readings => readings;
        public SensorReading Nearest { get; private set; }
        public float FrontClearance { get; private set; }
        public float Range => range;
        private Vector3 Eye => transform.position + Vector3.up * eyeHeight;

        public void Scan()
        {
            readings.Clear();
            Nearest = new SensorReading(0f, range, false, null);
            FrontClearance = range;
            int count = Mathf.Max(1, rayCount);

            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : (float)i / (count - 1);
                float angle = Mathf.Lerp(-fieldOfView / 2f, fieldOfView / 2f, t);
                var dir = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;

                SensorReading r;
                if (Physics.Raycast(Eye, dir, out var hit, range, mask, QueryTriggerInteraction.Ignore))
                    r = new SensorReading(angle, hit.distance, true, hit.collider);
                else
                    r = new SensorReading(angle, range, false, null);

                readings.Add(r);
                if (r.Hit && r.Distance < Nearest.Distance) Nearest = r;
                if (Mathf.Abs(angle) <= 15f) FrontClearance = Mathf.Min(FrontClearance, r.Distance);
            }
        }

        /// <summary>True when an unobstructed ray reaches the target within maxDistance.</summary>
        public bool CanSee(Transform target, float maxDistance, out float distance, out float angle)
        {
            var to = target.position - Eye;
            distance = to.magnitude;
            var flat = new Vector3(to.x, 0f, to.z);
            angle = flat.sqrMagnitude > 0.0001f ? Vector3.SignedAngle(transform.forward, flat, Vector3.up) : 0f;
            if (distance > maxDistance) return false;

            if (Physics.Raycast(Eye, to.normalized, out var hit, maxDistance, mask, QueryTriggerInteraction.Ignore))
                return hit.transform == target || hit.transform.IsChildOf(target);
            return false;
        }

        public string Describe()
        {
            return Nearest.Hit
                ? $"en yakın engel {Nearest.Distance:0.0} m @ {Nearest.Angle:+0;-0;0}°, ön boşluk {FrontClearance:0.0} m"
                : $"{range:0} m içinde engel yok";
        }

        private void OnDrawGizmos()
        {
            foreach (var r in readings)
            {
                Gizmos.color = r.Hit ? Color.red : Color.green;
                var dir = Quaternion.AngleAxis(r.Angle, Vector3.up) * transform.forward;
                Gizmos.DrawLine(Eye, Eye + dir * r.Distance);
            }
        }
    }
}
