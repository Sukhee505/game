using System.Collections;
using UnityEngine;
using SevenMinutes.Audio;
using SevenMinutes.UI;

namespace SevenMinutes.Game
{
    /// <summary>Time is up: freeze everything, horror sting, fade to black, then the defeat screen.</summary>
    public class DefeatManager : MonoBehaviour
    {
        public bool ShowScreen { get; private set; }

        public void Play() => StartCoroutine(Run());

        private IEnumerator Run()
        {
            var gm = GameManager.Instance;
            gm.Player.SetFrozen(true);
            var audio = AudioManager.Instance;
            if (audio != null)
            {
                audio.StopGameplayLoops();
                audio.Play(SoundId.Stinger, null, 1f);
                audio.Play(SoundId.Impact, null, 0.9f, 0.7f);
            }
            var fx = ScreenEffects.Instance;
            if (fx != null)
            {
                fx.FlashRed(1f);
                yield return fx.FadeTo(1f, 1.6f, true);
            }
            yield return new WaitForSeconds(0.4f);
            ShowScreen = true;
        }
    }
}
