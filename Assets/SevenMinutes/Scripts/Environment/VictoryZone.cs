using SevenMinutes.Game;
using SevenMinutes.Player;

namespace SevenMinutes.Environment
{
    /// <summary>Passing through the final door into the garden ends the game.</summary>
    public class VictoryZone : PlayerZone
    {
        public override void OnPlayerEnter(PlayerController player)
        {
            base.OnPlayerEnter(player);
            GameManager.Instance?.TriggerVictory();
        }
    }
}
