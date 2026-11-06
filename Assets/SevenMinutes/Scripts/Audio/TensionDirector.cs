using UnityEngine;
using SevenMinutes.Core;
using SevenMinutes.Game;
using SevenMinutes.UI;

namespace SevenMinutes.Audio
{
    /// <summary>
    /// Reacts to the countdown (3:00 / 1:00 / 0:30 / 0:10 warnings) and plays occasional distant
    /// metallic sounds around the house to keep the atmosphere tense.
    /// </summary>
    public class TensionDirector : MonoBehaviour
    {
        [SerializeField] private GameTimer timer;
        [SerializeField] private Vector2 ambientInterval = new Vector2(14f, 30f);

        private float nextAmbient;

        public void Configure(GameTimer t) => timer = t;

        private void Start()
        {
            timer.WarningReached += OnWarning;
            timer.FinalSecondTick += OnTick;
            nextAmbient = Time.time + Random.Range(ambientInterval.x, ambientInterval.y);
        }

        private void OnDestroy()
        {
            if (timer == null) return;
            timer.WarningReached -= OnWarning;
            timer.FinalSecondTick -= OnTick;
        }

        private void OnWarning(int seconds)
        {
            var audio = AudioManager.Instance;
            var fx = ScreenEffects.Instance;
            switch (seconds)
            {
                case 180:
                    audio?.Play(SoundId.Warning, null, 0.6f);
                    audio?.SetTension(1);
                    GameHUD.FlashTimer(3f);
                    break;
                case 60:
                    audio?.Play(SoundId.Warning, null, 0.8f, 0.9f);
                    audio?.SetTension(2);
                    GameHUD.FlashTimer(4f);
                    GameHUD.Toast("BİR DAKİKA KALDI", 2.5f);
                    break;
                case 30:
                    audio?.Play(SoundId.Stinger, null, 0.5f, 1.2f);
                    audio?.SetTension(3);
                    fx?.SetDanger(true);
                    GameHUD.FlashTimer(30f);
                    GameHUD.Toast("30 SANİYE!", 2.5f);
                    break;
            }
        }

        private void OnTick(int secondsLeft)
        {
            AudioManager.Instance?.Play(SoundId.Tick, null, 0.9f, secondsLeft <= 3 ? 1.2f : 1f);
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;
            if (Time.time < nextAmbient) return;
            nextAmbient = Time.time + Random.Range(ambientInterval.x, ambientInterval.y);

            var p = gm.Player.transform.position;
            Vector3 offset = Random.onUnitSphere;
            offset.y = Mathf.Abs(offset.y) * 0.3f;
            var pos = p + offset.normalized * Random.Range(10f, 18f);
            var audio = AudioManager.Instance;
            if (audio == null) return;
            float r = Random.value;
            if (r < 0.45f) audio.Play(SoundId.Clank, pos, 0.35f, Random.Range(0.6f, 0.9f));
            else if (r < 0.75f) audio.Play(SoundId.Impact, pos, 0.3f, Random.Range(0.5f, 0.7f));
            else audio.Play(SoundId.Creak, pos, 0.35f, Random.Range(0.7f, 1f));
        }
    }
}
