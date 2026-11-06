using UnityEngine;
using SevenMinutes.Core;
using SevenMinutes.Game;
using SevenMinutes.Player;
using SevenMinutes.Robot;

namespace SevenMinutes.UI
{
    /// <summary>Countdown (top centre), objective, interaction prompt, robot messages, toasts and crosshair.</summary>
    public class GameHUD : MonoBehaviour
    {
        [SerializeField] private PlayerInteraction interaction;

        private static bool debugPanel;
        private static string toastText;
        private static float toastUntil;
        private static float flashUntil;

        public void Configure(PlayerInteraction i) => interaction = i;

        public static void Toast(string text, float seconds = 2f)
        {
            toastText = text;
            toastUntil = Time.unscaledTime + seconds;
        }

        public static void FlashTimer(float seconds) => flashUntil = Time.unscaledTime + seconds;

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame) debugPanel = !debugPanel;
#else
            if (Input.GetKeyDown(KeyCode.F1)) debugPanel = !debugPanel;
#endif
        }

        private void DrawDebug(GameManager gm, float s, float h)
        {
            var robot = gm.Robot;
            if (robot == null) return;
            string text =
                "<b>F1 DEBUG</b>\n" +
                $"Robot durumu: <color=#7fe3ff>{robot.Mode}</color>   hız: {robot.Speed:0.0} m/s   ışınlanma: {robot.WarpCount}\n" +
                $"Sensör (Raycast): {(robot.Sensor != null ? robot.Sensor.Describe() : "-")}\n" +
                $"{SevenMinutes.NPC.GuardNpc.ServiceStatus}\n" +
                $"Seviye: {gm.CurrentLevel}   Başarısız: {gm.FailedAttempts}\n" +
                "<size=" + UIStyles.Px(14) + ">F5 robot testi · F6 LLM diyalog testi</size>";
            var r = new Rect(18 * s, h - 250 * s, 640 * s, 150 * s);
            UIStyles.Fill(r, new Color(0f, 0f, 0f, 0.65f));
            GUI.Label(new Rect(r.x + 10 * s, r.y + 6 * s, r.width - 20 * s, r.height - 12 * s), text, UIStyles.LabelSmall);
        }

        private void OnGUI()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            var state = gm.State;
            if (state == GameState.MainMenu || state == GameState.Victory || state == GameState.Defeat) return;

            UIStyles.Ensure();
            GUI.depth = 0;
            float s = UIStyles.Scale;
            float w = Screen.width, h = Screen.height;

            // ---- timer ----
            var timer = gm.Timer;
            if (timer.Started)
            {
                float rem = timer.Remaining;
                Color c = rem <= 30f ? new Color(1f, 0.15f, 0.1f) : rem <= 60f ? new Color(1f, 0.45f, 0.2f) : UIStyles.Bone;
                bool flashing = Time.unscaledTime < flashUntil && Mathf.Repeat(Time.unscaledTime * 3f, 1f) < 0.5f;
                if (flashing) c = Color.white;
                var r = new Rect(w * 0.5f - 130 * s, 14 * s, 260 * s, 70 * s);
                UIStyles.Fill(r, new Color(0f, 0f, 0f, 0.55f));
                var old = UIStyles.Timer.normal.textColor;
                UIStyles.Timer.normal.textColor = c;
                float pulse = rem <= 10f ? 1f + 0.08f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.PI)) : 1f;
                var m = GUI.matrix;
                GUIUtility.ScaleAroundPivot(new Vector2(pulse, pulse), r.center);
                GUI.Label(r, timer.Display, UIStyles.Timer);
                GUI.matrix = m;
                UIStyles.Timer.normal.textColor = old;
            }

            // ---- objective ----
            if (!string.IsNullOrEmpty(gm.Objective) && state != GameState.Introduction)
            {
                var r = new Rect(18 * s, 18 * s, 520 * s, 120 * s);
                GUILayout.BeginArea(r);
                GUILayout.BeginVertical(UIStyles.Panel);
                GUILayout.Label("<size=" + UIStyles.Px(15) + "><color=#b0302a>GÖREV</color></size>", UIStyles.LabelSmall);
                GUILayout.Label(gm.Objective, UIStyles.LabelSmall);
                GUILayout.EndVertical();
                GUILayout.EndArea();
            }

            if (state == GameState.Paused) return;
            if (debugPanel) DrawDebug(gm, s, h);

            // ---- crosshair ----
            if (gm.CanControlPlayer)
            {
                float d = 5f * s;
                UIStyles.Fill(new Rect(w * 0.5f - d * 0.5f, h * 0.5f - d * 0.5f, d, d), new Color(1f, 1f, 1f, 0.55f));
            }

            // ---- interaction prompt ----
            if (interaction != null && interaction.Current != null && gm.CanControlPlayer)
            {
                var content = new GUIContent("[E]  " + interaction.Current.Prompt);
                var size = UIStyles.Prompt.CalcSize(content);
                GUI.Label(new Rect(w * 0.5f - size.x * 0.5f, h * 0.5f + 34 * s, size.x, size.y), content, UIStyles.Prompt);
            }

            // ---- robot dialogue ----
            var line = RobotDialogue.CurrentLine;
            if (!string.IsNullOrEmpty(line))
            {
                float bw = Mathf.Min(900f * s, w - 40f);
                var r = new Rect(w * 0.5f - bw * 0.5f, h - 140 * s, bw, 86 * s);
                UIStyles.Fill(r, new Color(0.01f, 0.03f, 0.06f, 0.82f));
                UIStyles.Fill(new Rect(r.x, r.y, 5 * s, r.height), UIStyles.RobotBlue);
                GUI.Label(new Rect(r.x + 22 * s, r.y + 6 * s, r.width - 40 * s, 26 * s),
                    "<size=" + UIStyles.Px(15) + "><b>ROBOT  UNIT-7</b></size>", UIStyles.RobotLine);
                GUI.Label(new Rect(r.x + 22 * s, r.y + 30 * s, r.width - 40 * s, r.height - 34 * s),
                    "“" + line + "”", UIStyles.RobotLine);
            }

            // ---- toast ----
            if (Time.unscaledTime < toastUntil && !string.IsNullOrEmpty(toastText))
            {
                var content = new GUIContent(toastText);
                var size = UIStyles.Prompt.CalcSize(content);
                GUI.Label(new Rect(w * 0.5f - size.x * 0.5f, h * 0.30f, size.x, size.y), content, UIStyles.Prompt);
            }
        }
    }
}
