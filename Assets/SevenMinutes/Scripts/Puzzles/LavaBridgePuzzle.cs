using System.Collections;
using UnityEngine;
using SevenMinutes.Audio;
using SevenMinutes.Environment;
using SevenMinutes.Game;
using SevenMinutes.Robot;
using SevenMinutes.UI;

namespace SevenMinutes.Puzzles
{
    /// <summary>
    /// Level 3. The player types a request on the computer; the robot (on the far platform) answers "Okay.",
    /// walks to the big red button and presses it with its arm; bridge segments extend over the lava.
    /// </summary>
    public class LavaBridgePuzzle : MonoBehaviour
    {
        [SerializeField] private ComputerTerminal terminal;
        [SerializeField] private Transform buttonStandPoint;
        [SerializeField] private PushButton redButton;
        [SerializeField] private Transform[] segments;
        [SerializeField] private LeverController lever;
        [SerializeField] private DoorController exitDoor;

        private Vector3[] extended;
        private bool requested;
        private int unknownMessages;

        public bool BridgeExtended { get; private set; }
        public bool Requested => requested;
        public bool MessagesSent { get; private set; }

        public void Configure(ComputerTerminal t, Transform stand, PushButton button, Transform[] bridgeSegments,
            LeverController l, DoorController exit)
        {
            terminal = t; buttonStandPoint = stand; redButton = button; segments = bridgeSegments; lever = l; exitDoor = exit;
        }

        private void Awake()
        {
            extended = new Vector3[segments.Length];
            for (int i = 0; i < segments.Length; i++)
            {
                extended[i] = segments[i].localPosition;
                segments[i].localPosition = Retracted(i);
                foreach (var c in segments[i].GetComponentsInChildren<Collider>()) c.enabled = false;
            }
        }

        private Vector3 Retracted(int i) => extended[i] + new Vector3(0f, -1.6f, -2f * (i + 1));

        private void Start()
        {
            terminal.MessageSent += OnMessage;
            lever.Pulled += OnLever;
        }

        private void OnMessage(string msg)
        {
            MessagesSent = true;
            string m = msg.ToLowerInvariant();
            if (BridgeExtended)
            {
                terminal.Reply("UNIT-7: Köprü aktif. Geç!");
                return;
            }
            if (requested)
            {
                terminal.Reply("UNIT-7: Üzerinde çalışıyorum...");
                return;
            }

            bool help = m.Contains("help") || m.Contains("yardım") || m.Contains("yardim") || m.Contains("köprü") || m.Contains("kopru") || m.Contains("düğme") || m.Contains("dugme") || m.Contains("tuş");
            if (!help)
            {
                unknownMessages++;
                terminal.Reply(unknownMessages < 2
                    ? "UNIT-7: Seni duyuyorum. Neye ihtiyacın var?"
                    : "UNIT-7: Bana ihtiyacın varsa YARDIM yaz.");
                return;
            }

            requested = true;
            terminal.Reply("UNIT-7: Tamam.");
            RobotDialogue.Say("Tamam.", 2.5f, true);
            var gm = GameManager.Instance;
            var robot = gm.Robot;
            robot.TurnToward(gm.Player.transform);
            ComputerTerminalUI.Instance?.CloseAfter(1.6f);
            StartCoroutine(RobotSequence(robot));
        }

        private IEnumerator RobotSequence(RobotController robot)
        {
            yield return new WaitForSeconds(1.2f);
            robot.PressButton(buttonStandPoint, redButton.transform, () =>
            {
                redButton.Press();
                StartCoroutine(Extend());
            });
        }

        private IEnumerator Extend()
        {
            var audio = AudioManager.Instance;
            audio?.Play(SoundId.PowerUp, transform.position, 0.8f, 0.8f);
            yield return new WaitForSeconds(0.4f);
            for (int i = 0; i < segments.Length; i++)
            {
                var seg = segments[i];
                var src = audio != null ? audio.CreateLoop(SoundId.Grind, seg, 0.6f, 25f) : null;
                Vector3 from = seg.localPosition;
                float t = 0f;
                while (t < 0.7f)
                {
                    t += Time.deltaTime;
                    seg.localPosition = Vector3.Lerp(from, extended[i], Mathf.SmoothStep(0f, 1f, t / 0.7f));
                    yield return null;
                }
                seg.localPosition = extended[i];
                foreach (var c in seg.GetComponentsInChildren<Collider>()) c.enabled = true;
                if (src != null) Destroy(src.gameObject);
                audio?.Play(SoundId.Clank, seg.position, 0.8f, 0.8f);
                audio?.Play(SoundId.Slam, seg.position, 0.4f, 1.5f);
                yield return new WaitForSeconds(0.2f);
            }
            BridgeExtended = true;
            RobotDialogue.Say("Köprü yerine kilitlendi. Dikkatlice geç!", 3.5f, true);
            GameManager.Instance?.SetObjective("Köprüden geç ve kolu çek.");
        }

        private void OnLever()
        {
            exitDoor.Unlock(true);
            GameManager.Instance?.CompleteChallenge();
            GameManager.Instance?.Robot.Follow();
            RobotDialogue.Say("Hemen arkandayım.", 2.5f);
            GameManager.Instance?.SetObjective("Kapıdan geç.");
        }
    }
}
