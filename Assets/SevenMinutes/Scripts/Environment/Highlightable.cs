using UnityEngine;

namespace SevenMinutes.Environment
{
    /// <summary>Brightens an interactive object while the player is aiming at it.</summary>
    public class Highlightable : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private Renderer[] renderers;
        private Color[] originals;
        private MaterialPropertyBlock block;
        private bool on;

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            originals = new Color[renderers.Length];
            block = new MaterialPropertyBlock();
            for (int i = 0; i < renderers.Length; i++)
            {
                var m = renderers[i].sharedMaterial;
                if (m == null) originals[i] = Color.white;
                else if (m.HasProperty(BaseColorId)) originals[i] = m.GetColor(BaseColorId);
                else if (m.HasProperty(ColorId)) originals[i] = m.GetColor(ColorId);
                else originals[i] = Color.white;
            }
        }

        private static bool IsPaper(Renderer r)
        {
            var m = r.sharedMaterial;
            if (m == null) return false;
            string n = m.name;
            return n.Contains("Paper") || n.Contains("Photo") || n.Contains("Newspaper") || n.Contains("Workstation") || n.Contains("Symbol");
        }

        public void SetHighlighted(bool value)
        {
            if (value == on || renderers == null) return;
            on = value;
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null || r is ParticleSystemRenderer || IsPaper(r)) continue;
                if (!value)
                {
                    r.SetPropertyBlock(null);
                    continue;
                }
                Color c = originals[i] * 1.25f + new Color(0.05f, 0.04f, 0.02f, 0f);
                c.a = originals[i].a;
                r.GetPropertyBlock(block);
                block.SetColor(BaseColorId, c);
                block.SetColor(ColorId, c);
                r.SetPropertyBlock(block);
            }
        }
    }
}
