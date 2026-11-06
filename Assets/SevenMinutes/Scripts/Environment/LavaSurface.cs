using UnityEngine;

namespace SevenMinutes.Environment
{
    /// <summary>Scrolls and pulses the lava material and makes the lava lights breathe.</summary>
    public class LavaSurface : MonoBehaviour
    {
        [SerializeField] private Renderer surface;
        [SerializeField] private Light[] lights;
        [SerializeField] private Vector2 scroll = new Vector2(0.02f, 0.035f);

        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private Material mat;
        private Color baseEmission;
        private float[] baseIntensity;

        public void Configure(Renderer r, Light[] l)
        {
            surface = r;
            lights = l;
        }

        private void Start()
        {
            if (surface != null)
            {
                mat = surface.material;
                baseEmission = mat.HasProperty(EmissionId) ? mat.GetColor(EmissionId) : Color.black;
            }
            baseIntensity = new float[lights != null ? lights.Length : 0];
            for (int i = 0; i < baseIntensity.Length; i++) baseIntensity[i] = lights[i].intensity;
        }

        private void Update()
        {
            float t = Time.time;
            if (mat != null)
            {
                mat.SetTextureOffset(BaseMapId, scroll * t);
                float pulse = 0.85f + 0.15f * Mathf.Sin(t * 1.3f) + 0.05f * Mathf.Sin(t * 4.1f);
                mat.SetColor(EmissionId, baseEmission * pulse);
            }
            for (int i = 0; i < baseIntensity.Length; i++)
            {
                if (lights[i] == null) continue;
                lights[i].intensity = baseIntensity[i] * (0.85f + 0.15f * Mathf.PerlinNoise(t * 0.8f, i * 3.1f) * 2f);
            }
        }
    }
}
