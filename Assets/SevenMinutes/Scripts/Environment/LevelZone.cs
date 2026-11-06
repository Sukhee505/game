using UnityEngine;
using SevenMinutes.Game;
using SevenMinutes.Player;

namespace SevenMinutes.Environment
{
    /// <summary>Entering this volume advances the game to the given level (sets checkpoint + objective).</summary>
    public class LevelZone : PlayerZone
    {
        [SerializeField] private int level = 2;

        public void Configure(int levelIndex) => level = levelIndex;

        public override void OnPlayerEnter(PlayerController player)
        {
            base.OnPlayerEnter(player);
            GameManager.Instance?.EnterLevel(level);
        }
    }
}
