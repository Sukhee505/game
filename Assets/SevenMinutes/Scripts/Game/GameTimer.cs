using System;
using UnityEngine;

namespace SevenMinutes.Game
{
    /// <summary>
    /// The real 7:00 countdown. It only starts when <see cref="BeginCountdown"/> is called (after the TV video),
    /// it is driven by scaled time (so it freezes while paused) and never resets during a run.
    /// </summary>
    public class GameTimer : MonoBehaviour
    {
        [Tooltip("Total time in seconds (5 minutes = 300).")]
        [SerializeField] private float durationSeconds = 300f;

        private static readonly int[] WarningThresholds = { 180, 60, 30, 10 };
        private int nextWarning;

        public float Duration => durationSeconds;
        public float Remaining { get; private set; }
        public bool Started { get; private set; }
        public bool Running { get; private set; }
        public float Elapsed => Started ? durationSeconds - Remaining : 0f;

        /// <summary>Raised once for 180, 60, 30 and 10 seconds remaining.</summary>
        public event Action<int> WarningReached;
        /// <summary>Raised every whole second while 10 seconds or less remain.</summary>
        public event Action<int> FinalSecondTick;
        public event Action Expired;

        private int lastWholeSecond = -1;

        private void Awake() => Remaining = durationSeconds;

        public void BeginCountdown()
        {
            if (Started) return;
            Started = true;
            Running = true;
            Remaining = durationSeconds;
            nextWarning = 0;
            while (nextWarning < WarningThresholds.Length && WarningThresholds[nextWarning] >= durationSeconds) nextWarning++;
        }

        public void Stop() => Running = false;

        /// <summary>Freezes the countdown (used only while automated tests run).</summary>
        public bool Frozen { get; set; }

        private void Update()
        {
            if (!Running || Frozen) return;

            Remaining -= Time.deltaTime;

            while (nextWarning < WarningThresholds.Length && Remaining <= WarningThresholds[nextWarning])
            {
                WarningReached?.Invoke(WarningThresholds[nextWarning]);
                nextWarning++;
            }

            int whole = Mathf.CeilToInt(Remaining);
            if (whole <= 10 && whole != lastWholeSecond && whole > 0)
            {
                lastWholeSecond = whole;
                FinalSecondTick?.Invoke(whole);
            }

            if (Remaining <= 0f)
            {
                Remaining = 0f;
                Running = false;
                Expired?.Invoke();
            }
        }

        public static string Format(float seconds)
        {
            int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{s / 60:00}:{s % 60:00}";
        }

        public string Display => Format(Started ? Remaining : durationSeconds);
    }
}
