using System.Collections.Generic;
using UnityEngine;
using SevenMinutes.Audio;

namespace SevenMinutes.Robot
{
    /// <summary>Queue of short robot text messages shown by the HUD (with a chirp). Avoids repeating itself.</summary>
    public class RobotDialogue : MonoBehaviour
    {
        private static RobotDialogue instance;

        private readonly Queue<(string text, float duration)> queue = new Queue<(string, float)>();
        private readonly Dictionary<string, float> lastSaid = new Dictionary<string, float>();
        private string current;
        private float remaining;

        public static string CurrentLine => instance != null ? instance.current : null;

        private void Awake() => instance = this;

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        /// <summary>Show a line. interrupt = replace whatever is shown now (important story beats).</summary>
        public static void Say(string text, float duration = 3.5f, bool interrupt = false)
        {
            if (instance == null || string.IsNullOrEmpty(text)) return;
            instance.Enqueue(text, duration, interrupt);
        }

        private void Enqueue(string text, float duration, bool interrupt)
        {
            if (!interrupt && lastSaid.TryGetValue(text, out float t) && Time.time - t < 20f) return;
            if (!interrupt && current == text) return;
            lastSaid[text] = Time.time;
            if (interrupt)
            {
                queue.Clear();
                Show(text, duration);
            }
            else if (current == null) Show(text, duration);
            else if (queue.Count < 3) queue.Enqueue((text, duration));
        }

        private void Show(string text, float duration)
        {
            current = text;
            remaining = Mathf.Max(duration, 1.5f + text.Length * 0.035f);
            AudioManager.Instance?.Play(SoundId.RobotChirp, null, 0.35f, Random.Range(0.95f, 1.1f));
        }

        private void Update()
        {
            if (current == null) return;
            remaining -= Time.deltaTime;
            if (remaining > 0f) return;
            current = null;
            if (queue.Count > 0)
            {
                var next = queue.Dequeue();
                Show(next.text, next.duration);
            }
        }
    }
}
