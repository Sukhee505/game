using UnityEngine;
using SevenMinutes.Core;

namespace SevenMinutes.Environment
{
    /// <summary>Touching this collider (lava, pits, spikes) sends the player back to the level checkpoint.</summary>
    public class HazardVolume : MonoBehaviour, IHazard
    {
        [SerializeField] private string message = "Öldün.";
        [SerializeField] private FailKind kind = FailKind.Hazard;
        public bool Active = true;

        public bool IsDangerous => Active && isActiveAndEnabled;
        public string DeathMessage => message;
        public FailKind Kind => kind;

        public void Configure(string text, FailKind failKind)
        {
            message = text;
            kind = failKind;
        }
    }
}
