using System.Collections;
using UnityEngine;
using SevenMinutes.Audio;
using SevenMinutes.Player;
using SevenMinutes.Robot;

namespace SevenMinutes.Game
{
    /// <summary>Escape cinematic in the garden: player and robot together, slow camera orbit, then the victory screen.</summary>
    public class VictoryManager : MonoBehaviour
    {
        [SerializeField] private Transform robotGardenPoint;
        [SerializeField] private Light sun;
        [SerializeField] private float cinematicDuration = 6f;

        public bool ShowScreen { get; private set; }
        public float RemainingAtEscape { get; private set; }
        public float CompletionTime { get; private set; }

        public void Configure(Transform robotPoint, Light sunLight)
        {
            robotGardenPoint = robotPoint;
            sun = sunLight;
        }

        public void Play() => StartCoroutine(Run());

        private IEnumerator Run()
        {
            var gm = GameManager.Instance;
            RemainingAtEscape = gm.Timer.Remaining;
            CompletionTime = gm.Timer.Elapsed;

            PlayerController player = gm.Player;
            RobotController robot = gm.Robot;
            player.SetFrozen(true);

            var audio = AudioManager.Instance;
            if (audio != null)
            {
                audio.StopGameplayLoops();
                audio.Play(SoundId.Victory, null, 0.9f);
                audio.StartGardenAmbience();
            }

            Vector3 robotTarget = robotGardenPoint != null
                ? robotGardenPoint.position
                : player.transform.position + player.transform.right * 1.4f;
            robot.MoveTo(robotTarget, () => robot.TurnToward(player.transform));

            var cam = player.CameraRig;
            float t = 0f;
            float startYaw = player.transform.eulerAngles.y + 160f;
            while (t < cinematicDuration)
            {
                t += Time.deltaTime;
                Vector3 center = (player.transform.position + robot.transform.position) * 0.5f + Vector3.up * 1.1f;
                float yaw = startYaw + t * 12f;
                float dist = Mathf.Lerp(3.2f, 5.5f, t / cinematicDuration);
                Vector3 offset = Quaternion.Euler(12f + t * 1.5f, yaw, 0f) * new Vector3(0f, 0f, -dist);
                Vector3 pos = center + offset;
                cam.SetCinematic(pos, Quaternion.LookRotation(center - pos), 0.6f);
                if (sun != null) sun.intensity = Mathf.MoveTowards(sun.intensity, 1.8f, Time.deltaTime * 0.3f);
                yield return null;
            }
            ShowScreen = true;
        }
    }
}
