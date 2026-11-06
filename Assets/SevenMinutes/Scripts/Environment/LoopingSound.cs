using UnityEngine;
using SevenMinutes.Audio;

namespace SevenMinutes.Environment
{
    /// <summary>Starts a looping 3D ambience sound at this position (lava, machines...).</summary>
    public class LoopingSound : MonoBehaviour
    {
        [SerializeField] private SoundId sound = SoundId.Lava;
        [SerializeField] private float volume = 0.7f;
        [SerializeField] private float maxDistance = 20f;

        public void Configure(SoundId id, float vol, float distance)
        {
            sound = id;
            volume = vol;
            maxDistance = distance;
        }

        private void Start()
        {
            AudioManager.Instance?.CreateLoop(sound, transform, volume, maxDistance);
        }
    }
}
