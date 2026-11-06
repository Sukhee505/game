using System;
using UnityEngine;
using SevenMinutes.Core;
using SevenMinutes.Environment;

namespace SevenMinutes.Player
{
    /// <summary>
    /// Universal [E] interaction: a ray from the screen centre finds the first usable IInteractable
    /// within reach of the character and highlights it.
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private float reach = 2.4f;

        private readonly RaycastHit[] hits = new RaycastHit[24];
        private Highlightable highlighted;

        public IInteractable Current { get; private set; }

        public void Configure(PlayerController p) => player = p;

        private void Update()
        {
            IInteractable found = null;
            if (player != null && player.ControlEnabled && player.CameraRig != null)
                found = FindTarget();

            SetCurrent(found);

            if (Current != null && InputReader.Down(GameKey.Interact))
            {
                var target = Current;
                player.Rig?.Reach();
                target.Interact(player);
            }
        }

        private IInteractable FindTarget()
        {
            var cam = player.CameraRig.Cam;
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            float camToPlayer = Vector3.Distance(cam.transform.position, player.Chest);
            int n = Physics.RaycastNonAlloc(ray, hits, camToPlayer + reach + 1.5f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            if (n > 1) Array.Sort(hits, 0, n, Comparer.Instance);

            for (int i = 0; i < n; i++)
            {
                var col = hits[i].collider;
                if (col == null || col.transform.IsChildOf(player.transform)) continue;
                var it = col.GetComponentInParent<IInteractable>();
                if (it != null)
                {
                    if (!it.CanInteract) { if (!col.isTrigger) return null; continue; }
                    Vector3 closest = col.ClosestPoint(player.Chest);
                    if (Vector3.Distance(closest, player.Chest) <= reach) return it;
                    if (!col.isTrigger) return null;
                    continue;
                }
                if (!col.isTrigger) return null;
            }
            return null;
        }

        private void SetCurrent(IInteractable it)
        {
            if (ReferenceEquals(it, Current)) return;
            Current = it;
            if (highlighted != null) highlighted.SetHighlighted(false);
            highlighted = it is Component c ? c.GetComponentInParent<Highlightable>() : null;
            if (highlighted != null) highlighted.SetHighlighted(true);
        }

        private sealed class Comparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly Comparer Instance = new Comparer();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }
}
