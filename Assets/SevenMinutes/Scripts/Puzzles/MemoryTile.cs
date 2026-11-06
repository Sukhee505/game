using System.Collections;
using UnityEngine;
using SevenMinutes.Core;
using SevenMinutes.Player;

namespace SevenMinutes.Puzzles
{
    /// <summary>One floor tile of the 4 × 10 memory floor.</summary>
    public class MemoryTile : MonoBehaviour, ISteppable
    {
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Renderer surface;

        private TileMemoryPuzzle puzzle;
        private Collider col;
        private Vector3 homePos;
        private Quaternion homeRot;
        private MaterialPropertyBlock block;
        private Color baseColor;
        private Coroutine anim;

        public int Column { get; private set; }
        public int Row { get; private set; }
        public bool Safe { get; private set; }
        public bool Collapsed { get; private set; }

        public void Configure(Renderer r) => surface = r;

        public void Setup(TileMemoryPuzzle owner, int column, int row, bool safe)
        {
            puzzle = owner;
            Column = column;
            Row = row;
            Safe = safe;
            col = GetComponent<Collider>();
            homePos = transform.localPosition;
            homeRot = transform.localRotation;
            block = new MaterialPropertyBlock();
            if (surface == null) surface = GetComponent<Renderer>();
            baseColor = surface != null && surface.sharedMaterial != null && surface.sharedMaterial.HasProperty(BaseColorId)
                ? surface.sharedMaterial.GetColor(BaseColorId) : Color.gray;
            SetGlow(Color.black);
        }

        public void OnPlayerStep(PlayerController player)
        {
            if (!Collapsed) puzzle.OnTileStepped(this, player);
        }

        public void SetGlow(Color emission)
        {
            if (surface == null) return;
            surface.GetPropertyBlock(block);
            block.SetColor(EmissionId, emission);
            block.SetColor(BaseColorId, emission.maxColorComponent > 0.01f ? Color.Lerp(baseColor, emission, 0.6f) : baseColor);
            surface.SetPropertyBlock(block);
        }

        public void Collapse()
        {
            if (Collapsed) return;
            Collapsed = true;
            if (col != null) col.enabled = false;
            if (anim != null) StopCoroutine(anim);
            anim = StartCoroutine(Fall());
        }

        private IEnumerator Fall()
        {
            SetGlow(new Color(0.8f, 0.05f, 0.02f));
            float t = 0f;
            Vector3 axis = Random.value < 0.5f ? Vector3.right : Vector3.forward;
            while (t < 1.2f)
            {
                t += Time.deltaTime;
                transform.localPosition = homePos + Vector3.down * (t * t * 4f);
                transform.localRotation = homeRot * Quaternion.AngleAxis(t * 70f, axis);
                yield return null;
            }
        }

        public void Restore()
        {
            if (anim != null) StopCoroutine(anim);
            anim = null;
            Collapsed = false;
            transform.localPosition = homePos;
            transform.localRotation = homeRot;
            if (col != null) col.enabled = true;
            SetGlow(Color.black);
        }

        /// <summary>Short confirmation glow when stepping on a safe tile.</summary>
        public void Confirm()
        {
            if (anim != null) StopCoroutine(anim);
            anim = StartCoroutine(Pulse());
        }

        private IEnumerator Pulse()
        {
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                SetGlow(new Color(0.05f, 0.4f, 0.12f) * (1f - t / 0.5f));
                yield return null;
            }
            SetGlow(Color.black);
        }
    }
}
