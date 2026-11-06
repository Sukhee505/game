using System.Collections;
using UnityEngine;

namespace SevenMinutes.UI
{
    /// <summary>Full-screen overlays: fade to black, red damage flash, waking-up eyelids and the low-time danger pulse.</summary>
    public class ScreenEffects : MonoBehaviour
    {
        public static ScreenEffects Instance { get; private set; }

        [Range(0f, 1f)] public float Fade = 1f;
        /// <summary>0 = eyes closed (black bars cover the screen), 1 = fully open.</summary>
        [Range(0f, 1f)] public float Eyelids = 1f;

        private float red;
        private bool danger;
        private bool locked;
        private Coroutine fadeRoutine;
        private Texture2D edgeTex;

        private void Awake()
        {
            Instance = this;
            edgeTex = BuildEdgeTexture();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Animate the black overlay. A "final" fade (defeat) cannot be overridden afterwards.</summary>
        public Coroutine FadeTo(float target, float duration, bool final = false)
        {
            if (locked && !final) return null;
            if (final) locked = true;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeRoutine(target, duration));
            return fadeRoutine;
        }

        private IEnumerator FadeRoutine(float target, float duration)
        {
            float start = Fade;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                Fade = Mathf.Lerp(start, target, Mathf.Clamp01(t / duration));
                yield return null;
            }
            Fade = target;
            fadeRoutine = null;
        }

        public void SetFadeImmediate(float value)
        {
            if (locked) return;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = null;
            Fade = value;
        }

        public void FlashRed(float strength) => red = Mathf.Max(red, strength);

        public void SetDanger(bool value) => danger = value;

        private void Update()
        {
            red = Mathf.MoveTowards(red, 0f, Time.unscaledDeltaTime * 1.4f);
        }

        private void OnGUI()
        {
            GUI.depth = -50;
            UIStyles.Ensure();
            float w = Screen.width, h = Screen.height;

            if (danger)
            {
                float pulse = 0.25f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f);
                var old = GUI.color;
                GUI.color = new Color(0.8f, 0f, 0f, pulse);
                GUI.DrawTexture(new Rect(0, 0, w, h), edgeTex);
                GUI.color = old;
            }

            if (red > 0.01f) UIStyles.Fill(new Rect(0, 0, w, h), new Color(0.6f, 0f, 0f, red * 0.55f));

            if (Eyelids < 0.999f)
            {
                float cover = (1f - Eyelids) * h * 0.5f;
                UIStyles.Fill(new Rect(0, 0, w, cover + 2f), Color.black);
                UIStyles.Fill(new Rect(0, h - cover - 2f, w, cover + 2f), Color.black);
            }

            if (Fade > 0.001f) UIStyles.Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, Fade));
        }

        private static Texture2D BuildEdgeTexture()
        {
            const int size = 64;
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x / (size - 1f) - 0.5f) * 2f;
                float dy = Mathf.Abs(y / (size - 1f) - 0.5f) * 2f;
                float d = Mathf.Max(dx, dy);
                float a = Mathf.Clamp01((d - 0.55f) / 0.45f);
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
            t.Apply();
            return t;
        }
    }
}
