using System.Collections;
using UnityEngine;
using SevenMinutes.Audio;
using SevenMinutes.Game;
using SevenMinutes.Player;
using SevenMinutes.Robot;
using SevenMinutes.UI;

namespace SevenMinutes.Intro
{
    /// <summary>
    /// Opening cinematic: sleeping in bed → eyes open → sits up → looks around → stands → gameplay camera.
    /// 30 seconds after waking up the TV sequence starts; when the video ends the 7:00 countdown begins.
    /// </summary>
    public class IntroSequence : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private RobotController robot;
        [SerializeField] private TelevisionController tv;
        [SerializeField] private Transform lyingPose;
        [SerializeField] private Transform sittingPose;
        [SerializeField] private Transform standingPose;
        [SerializeField] private Transform cameraFace;
        [SerializeField] private Transform cameraRoom;
        [SerializeField] private Transform cameraMenu;
        [SerializeField] private float tvDelayAfterWaking = 15f;

        public void Configure(PlayerController p, RobotController r, TelevisionController t, Transform lying, Transform sitting,
            Transform standing, Transform camFace, Transform camRoom, Transform camMenu)
        {
            player = p; robot = r; tv = t; lyingPose = lying; sittingPose = sitting; standingPose = standing;
            cameraFace = camFace; cameraRoom = camRoom; cameraMenu = camMenu;
        }

        private HumanoidRig Rig => player.Rig;
        private PlayerCamera Cam => player.CameraRig;

        private void PutInBed()
        {
            player.SetKinematic(true);
            player.transform.SetPositionAndRotation(lyingPose.position, lyingPose.rotation);
            Rig.Mode = BodyMode.Lying;
            Rig.EyesOpen = 0f;
            Rig.HeadYaw = 0f;
            Rig.SnapPose();
        }

        public void ShowMenuView()
        {
            PutInBed();
            Cam.SetCinematic(cameraMenu.position, cameraMenu.rotation, 0f);
            ScreenEffects.Instance?.SetFadeImmediate(0f);
            if (ScreenEffects.Instance != null) ScreenEffects.Instance.Eyelids = 1f;
        }

        public void Play() => StartCoroutine(Run());

        private IEnumerator Run()
        {
            var fx = ScreenEffects.Instance;
            fx?.SetFadeImmediate(1f);
            if (fx != null) fx.Eyelids = 0f;
            PutInBed();
            Cam.SetCinematic(cameraFace.position, cameraFace.rotation, 0f);
            AudioManager.Instance?.StartHouseAmbience();

            yield return new WaitForSeconds(0.8f);
            fx?.FadeTo(0f, 2.2f);
            if (fx != null) fx.Eyelids = 1f; // the cinematic shot shows the sleeping character; the bars are not needed
            yield return new WaitForSeconds(2.4f);

            // eyes slowly open (with a blink)
            yield return Eyes(0f, 0.45f, 0.6f);
            yield return Eyes(0.45f, 0.1f, 0.18f);
            yield return Eyes(0.1f, 1f, 0.7f);
            StartCoroutine(TvTimer());
            yield return new WaitForSeconds(0.6f);

            // sit up
            Rig.Mode = BodyMode.Sitting;
            Cam.SetCinematic(cameraRoom.position, cameraRoom.rotation, 1.6f);
            yield return MoveRoot(lyingPose, sittingPose, 1.8f);

            // look around the unfamiliar room
            yield return HeadTurn(-55f, 1.0f);
            yield return new WaitForSeconds(0.3f);
            yield return HeadTurn(50f, 1.3f);
            yield return new WaitForSeconds(0.3f);
            yield return HeadTurn(0f, 0.6f);

            // stand up
            Rig.Mode = BodyMode.Gameplay;
            yield return MoveRoot(sittingPose, standingPose, 1.0f);
            player.SetKinematic(false);
            player.Teleport(standingPose.position, standingPose.rotation);
            Cam.ClearCinematic(1.6f);

            var gm = GameManager.Instance;
            gm.OnIntroFinished();
            gm.SetObjective("Neredeyim? Odaya iyice bak.");

            yield return new WaitForSeconds(1.4f);
            robot.Boot(() =>
            {
                RobotDialogue.Say("...Sistem çevrimiçi. Merhaba? Korkma — ben UNIT-7. Sana yardım etmek istiyorum.", 5f, true);
                RobotDialogue.Say("Burada bir şeyler ters. Etrafına dikkatlice bak.", 4f);
            });
        }

        private IEnumerator TvTimer()
        {
            yield return new WaitForSeconds(tvDelayAfterWaking);
            if (robot != null) robot.LookAt(tv.transform.position, 25f);
            tv.PlaySequence(() =>
            {
                var gm = GameManager.Instance;
                gm.BeginCountdown();
                RobotDialogue.Say("On dakika mı...? Ona güvenmiyorum. Acele etmeliyiz.", 4.5f, true);
            });
        }

        private IEnumerator Eyes(float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                Rig.EyesOpen = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            Rig.EyesOpen = to;
        }

        private IEnumerator MoveRoot(Transform from, Transform to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                player.transform.SetPositionAndRotation(
                    Vector3.Lerp(from.position, to.position, k),
                    Quaternion.Slerp(from.rotation, to.rotation, k));
                yield return null;
            }
            player.transform.SetPositionAndRotation(to.position, to.rotation);
        }

        private IEnumerator HeadTurn(float yaw, float duration)
        {
            Rig.HeadYaw = yaw;
            yield return new WaitForSeconds(duration);
        }
    }
}
