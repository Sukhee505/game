using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SevenMinutes.Core
{
    /// <summary>
    /// Bakes the NavMesh for the whole house when the scene loads (no extra package needed).
    /// The agent height is small (the robot is small) so it fits through the maintenance ducts the player cannot use.
    /// Doors use carving NavMeshObstacles, so opening a door updates paths automatically.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class RuntimeNavMeshBaker : MonoBehaviour
    {
        [SerializeField] private Vector3 boundsCenter = new Vector3(18f, -1f, 58f);
        [SerializeField] private Vector3 boundsSize = new Vector3(42f, 26f, 124f);
        [SerializeField] private float agentRadius = 0.3f;
        [SerializeField] private float agentHeight = 0.9f;
        [SerializeField] private float agentClimb = 0.35f;
        [SerializeField] private float agentSlope = 45f;
        [SerializeField] private float voxelSize = 0.12f;

        private NavMeshDataInstance instance;

        public static bool IsBaked { get; private set; }

        private void Awake()
        {
            IsBaked = false;
            var bounds = new Bounds(boundsCenter, boundsSize);
            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(bounds, ~0, NavMeshCollectGeometry.PhysicsColliders, 0,
                new List<NavMeshBuildMarkup>(), sources);
            sources.RemoveAll(Ignore);

            var settings = NavMesh.GetSettingsByID(0);
            settings.agentRadius = agentRadius;
            settings.agentHeight = agentHeight;
            settings.agentClimb = agentClimb;
            settings.agentSlope = agentSlope;
            settings.overrideVoxelSize = true;
            settings.voxelSize = voxelSize;
            settings.overrideTileSize = true;
            settings.tileSize = 128;

            var data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            if (data == null)
            {
                Debug.LogError("[NavMesh] Bake failed.");
                return;
            }
            instance = NavMesh.AddNavMeshData(data);
            IsBaked = true;
            Debug.Log($"[NavMesh] Baked from {sources.Count} sources.");
        }

        private static bool Ignore(NavMeshBuildSource s)
        {
            var c = s.component;
            if (c == null) return false;
            if (c is Collider col && col.isTrigger) return true;
            return c.GetComponentInParent<NavMeshIgnore>() != null;
        }

        private void OnDestroy()
        {
            if (instance.valid) NavMesh.RemoveNavMeshData(instance);
        }
    }
}
