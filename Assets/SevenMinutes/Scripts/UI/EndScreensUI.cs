using UnityEngine;
using SevenMinutes.Core;
using SevenMinutes.Game;

namespace SevenMinutes.UI
{
    /// <summary>Victory and defeat screens.</summary>
    public class EndScreensUI : MonoBehaviour
    {
        [SerializeField] private VictoryManager victory;
        [SerializeField] private DefeatManager defeat;
        [SerializeField] private int totalChallenges = 5;

        public void Configure(VictoryManager v, DefeatManager d)
        {
            victory = v;
            defeat = d;
        }

        private void OnGUI()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (gm.State == GameState.Victory && victory != null && victory.ShowScreen) DrawVictory(gm);
            else if (gm.State == GameState.Defeat && defeat != null && defeat.ShowScreen) DrawDefeat(gm);
        }

        private void DrawVictory(GameManager gm)
        {
            UIStyles.Ensure();
            GUI.depth = -100;
            float s = UIStyles.Scale;
            float w = Screen.width, h = Screen.height;
            UIStyles.Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, 0.25f));

            var title = new GUIStyle(UIStyles.Title) { fontSize = UIStyles.Px(140) };
            title.normal.textColor = new Color(1f, 0.85f, 0.3f);
            var shadow = new GUIStyle(title);
            shadow.normal.textColor = new Color(0f, 0f, 0f, 0.6f);
            float pulse = 1f + 0.03f * Mathf.Sin(Time.unscaledTime * 3f);
            var m = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(pulse, pulse), new Vector2(w * 0.5f, h * 0.06f + 80 * s));
            GUI.Label(new Rect(4 * s, h * 0.06f + 4 * s, w, 160 * s), "TEBRİKLER!", shadow);
            GUI.Label(new Rect(0, h * 0.06f, w, 160 * s), "TEBRİKLER!", title);
            GUI.matrix = m;
            GUI.Label(new Rect(0, h * 0.06f + 160 * s, w, 50 * s),
                "KAÇTIN!   KALAN SÜRE: " + GameTimer.Format(victory.RemainingAtEscape), UIStyles.Subtitle);

            var panel = UIStyles.Centered(560 * s, 230 * s);
            panel.y = h * 0.38f;
            GUILayout.BeginArea(panel, UIStyles.Panel);
            Stat("Toplam tamamlama süresi", GameTimer.Format(victory.CompletionTime));
            Stat("Kalan süre", GameTimer.Format(victory.RemainingAtEscape));
            Stat("Başarısız denemeler", gm.FailedAttempts.ToString());
            Stat("Tamamlanan görevler", Mathf.Min(gm.ChallengesCompleted, totalChallenges) + " / " + totalChallenges);
            GUILayout.EndArea();

            float bw = 300 * s, bh = 60 * s;
            float y = panel.yMax + 30 * s;
            if (GUI.Button(new Rect(w * 0.5f - bw - 10 * s, y, bw, bh), "TEKRAR OYNA", UIStyles.Button)) gm.Restart();
            if (GUI.Button(new Rect(w * 0.5f + 10 * s, y, bw, bh), "ANA MENÜ", UIStyles.Button)) gm.GoToMainMenu();
        }

        private void DrawDefeat(GameManager gm)
        {
            UIStyles.Ensure();
            GUI.depth = -100;
            float s = UIStyles.Scale;
            float w = Screen.width, h = Screen.height;
            UIStyles.Fill(new Rect(0, 0, w, h), Color.black);

            GUI.Label(new Rect(0, h * 0.28f, w, 100 * s), "SÜRE DOLDU", UIStyles.Title);
            GUI.Label(new Rect(0, h * 0.28f + 100 * s, w, 50 * s), "KAÇMAYI BAŞARAMADIN.", UIStyles.Subtitle);
            GUI.Label(new Rect(0, h * 0.28f + 150 * s, w, 40 * s),
                $"Tamamlanan görevler: {Mathf.Min(gm.ChallengesCompleted, totalChallenges)} / {totalChallenges}   ·   Başarısız denemeler: {gm.FailedAttempts}",
                UIStyles.LabelCenter);

            float bw = 300 * s, bh = 60 * s;
            if (GUI.Button(new Rect(w * 0.5f - bw * 0.5f, h * 0.62f, bw, bh), "YENİDEN BAŞLA", UIStyles.Button)) gm.Restart();
        }

        private static void Stat(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, UIStyles.Label);
            GUILayout.FlexibleSpace();
            GUILayout.Label("<b>" + value + "</b>", UIStyles.Label, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }
    }
}
