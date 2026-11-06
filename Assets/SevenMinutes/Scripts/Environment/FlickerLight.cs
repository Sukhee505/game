using System.Collections;
using UnityEngine;

namespace SevenMinutes.Environment
{
    /// <summary>Dim lamp that occasionally flickers; can be forced into a burst (TV hack) or switched off.</summary>
    [RequireComponent(typeof(Light))]
    public class FlickerLight : MonoBehaviour
    {
        [SerializeField] private bool ambientFlicker = true;
        [SerializeField] private float flickerChancePerSecond = 0.08f;
        [SerializeField] private Renderer bulb;

        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private Light lamp;
        private float baseIntensity;
        private bool bursting;
        private bool forcedOff;
        private MaterialPropertyBlock block;
        private Color bulbColor;

        public void Configure(bool ambient, float chance, Renderer bulbRenderer)
        {
            ambientFlicker = ambient;
            flickerChancePerSecond = chance;
            bulb = bulbRenderer;
        }

        private void Awake()
        {
            lamp = GetComponent<Light>();
            baseIntensity = lamp.intensity;
            block = new MaterialPropertyBlock();
            bulbColor = lamp.color;
        }

        private void Update()
        {
            if (bursting || forcedOff || !ambientFlicker) return;
            if (Random.value < flickerChancePerSecond * Time.deltaTime) StartCoroutine(Flicker(Random.Range(0.15f, 0.6f)));
        }

        public void Burst(float duration)
        {
            StopAllCoroutines();
            StartCoroutine(Flicker(duration));
        }

        public void SetOff(bool off)
        {
            forcedOff = off;
            if (!bursting) Apply(off ? 0f : 1f);
        }

        private IEnumerator Flicker(float duration)
        {
            bursting = true;
            float end = Time.time + duration;
            while (Time.time < end)
            {
                Apply(Random.value < 0.5f ? Random.Range(0f, 0.25f) : Random.Range(0.7f, 1.2f));
                yield return new WaitForSeconds(Random.Range(0.03f, 0.12f));
            }
            Apply(forcedOff ? 0f : 1f);
            bursting = false;
        }

        private void Apply(float k)
        {
            lamp.intensity = baseIntensity * k;
            if (bulb != null)
            {
                bulb.GetPropertyBlock(block);
                block.SetColor(EmissionId, bulbColor * (2f * k));
                bulb.SetPropertyBlock(block);
            }
        }
    }
}
