using System;
using UnityEngine;
using SevenMinutes.Core;
using SevenMinutes.Player;

namespace SevenMinutes.Environment
{
    /// <summary>Generic trigger volume that raises events when the player enters / leaves.</summary>
    [RequireComponent(typeof(Collider))]
    public class PlayerZone : MonoBehaviour, IPlayerTrigger
    {
        public event Action<PlayerController> Entered;
        public event Action<PlayerController> Exited;
        public bool PlayerInside { get; private set; }

        protected virtual void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        public virtual void OnPlayerEnter(PlayerController player)
        {
            PlayerInside = true;
            Entered?.Invoke(player);
        }

        public virtual void OnPlayerExit(PlayerController player)
        {
            PlayerInside = false;
            Exited?.Invoke(player);
        }
    }
}
