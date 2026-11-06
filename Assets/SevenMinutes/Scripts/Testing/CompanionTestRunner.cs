using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using SevenMinutes.Environment;
using SevenMinutes.Game;
using SevenMinutes.Robot;
using SevenMinutes.UI;

namespace SevenMinutes.Testing
{
    /// <summary>
    /// [F5] Automated path-finding test for the autonomous robot. The robot is placed at 10 different start
    /// positions on both floors and has to reach the red button on the far side of the lava pit (through the
    /// robot-only maintenance duct). Success rate, completion time and path length are written to the Console
    /// and to companion_tests.md / .csv in Application.persistentDataPath.
    /// NOTE: the test opens the doors on the route, so restart the game afterwards.
    /// </summary>
    public class CompanionTestRunner : MonoBehaviour
    {
        [SerializeField] private RobotController robot;
        [SerializeField] private Transform target;
        [SerializeField] private Transform[] startPoints;
        [SerializeField] private DoorController[] doorsToOpen;
        [SerializeField] private float trialTimeout = 60f;

        private bool running;

        public void Configure(RobotController r, Transform goal, Transform[] points, DoorController[] doors)
        {
            robot = r; target = goal; startPoints = points; doorsToOpen = doors;
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            bool pressed = kb != null && kb.f5Key.wasPressedThisFrame;
#else
            bool pressed = Input.GetKeyDown(KeyCode.F5);
#endif
            var gm = GameManager.Instance;
            if (pressed && !running && gm != null && gm.State == Core.GameState.Playing && !gm.IsModalOpen)
                StartCoroutine(RunAll());
        }

        private struct Trial
        {
            public int Index;
            public string Start;
            public bool Success;
            public float Seconds;
            public float PathLength;
            public float Straight;
            public string Note;
        }

        private IEnumerator RunAll()
        {
            running = true;
            GameHUD.Toast("Robot testi başladı (F5)... Süre test boyunca durduruldu.", 3f);
            GameManager.Instance.Timer.Frozen = true;
            foreach (var d in doorsToOpen) if (d != null) d.Unlock(true);
            yield return new WaitForSeconds(1.6f); // let door obstacles update

            var results = new List<Trial>();
            int idx = 0;
            foreach (var start in startPoints.Where(p => p != null))
            {
                idx++;
                robot.PlaceForTest(start.position);
                yield return null;
                yield return null;

                bool arrived = false;
                int warpsBefore = robot.WarpCount;
                float t0 = Time.time, path = 0f;
                Vector3 last = robot.transform.position;
                float straight = Vector3.Distance(start.position, target.position);
                robot.MoveTo(target.position, () => arrived = true);

                while (!arrived && Time.time - t0 < trialTimeout)
                {
                    path += Vector3.Distance(last, robot.transform.position);
                    last = robot.transform.position;
                    yield return null;
                }

                bool warped = robot.WarpCount != warpsBefore;
                bool ok = arrived && !warped && Vector3.Distance(robot.transform.position, target.position) < 1.2f;
                var tr = new Trial
                {
                    Index = idx, Start = start.name, Success = ok, Seconds = Time.time - t0, PathLength = path, Straight = straight,
                    Note = !arrived ? "zaman aşımı" : warped ? "takıldı (ışınlandı)" : "ok"
                };
                results.Add(tr);
                Debug.Log($"[RobotTest] #{idx} {start.name}: {(ok ? "OK" : "FAIL")} {tr.Seconds:0.00}s yol={path:0.0}m ({tr.Note})");
            }

            Report(results);
            GameManager.Instance.Timer.Frozen = false;
            robot.Follow();
            running = false;
        }

        private static void Report(List<Trial> results)
        {
            var ci = CultureInfo.InvariantCulture;
            int ok = results.Count(r => r.Success);
            float rate = results.Count == 0 ? 0f : 100f * ok / results.Count;
            var times = results.Where(r => r.Success).Select(r => r.Seconds).ToList();
            float avg = times.Count > 0 ? times.Average() : 0f;

            var md = new StringBuilder();
            md.AppendLine("| # | Başlangıç | Başarı | Süre (sn) | Yol (m) | Düz mesafe (m) | Not |");
            md.AppendLine("|---|---|---|---|---|---|---|");
            foreach (var r in results)
                md.AppendLine(string.Format(ci, "| {0} | {1} | {2} | {3:0.00} | {4:0.0} | {5:0.0} | {6} |",
                    r.Index, r.Start, r.Success ? "✅" : "❌", r.Seconds, r.PathLength, r.Straight, r.Note));
            md.AppendLine();
            md.AppendLine(string.Format(ci, "**Başarı oranı:** {0}/{1} (%{2:0}) — **ortalama süre:** {3:0.00} sn", ok, results.Count, rate, avg));

            var csv = new StringBuilder("deneme,baslangic,basari,sure_sn,yol_m,duz_m,not\n");
            foreach (var r in results)
                csv.AppendLine(string.Format(ci, "{0},{1},{2},{3:0.00},{4:0.0},{5:0.0},{6}", r.Index, r.Start, r.Success, r.Seconds, r.PathLength, r.Straight, r.Note));

            Debug.Log("[RobotTest] Sonuçlar:\n" + md);
            try
            {
                string dir = Application.persistentDataPath;
                File.WriteAllText(Path.Combine(dir, "companion_tests.md"), md.ToString());
                File.WriteAllText(Path.Combine(dir, "companion_tests.csv"), csv.ToString());
                GameHUD.Toast(string.Format(ci, "Robot testi: {0}/{1} başarılı (%{2:0}). Kayıt: {3}", ok, results.Count, rate, dir), 8f);
            }
            catch (IOException e)
            {
                Debug.LogWarning("[RobotTest] Kaydedilemedi: " + e.Message);
            }
        }
    }
}
