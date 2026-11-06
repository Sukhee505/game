using UnityEngine;
using SevenMinutes.Game;
using SevenMinutes.Player;
using SevenMinutes.Robot;

namespace SevenMinutes.Environment
{
    /// <summary>The robot comments when the player enters this area (warnings, Easter egg rooms...).</summary>
    public class RobotLineZone : PlayerZone
    {
        [TextArea] [SerializeField] private string line = "Dikkatli ol.";
        [SerializeField] private bool once = true;
        [SerializeField] private int minLevel;

        private bool used;

        public bool Visited => used;

        public void Configure(string text, bool onlyOnce, int requiredLevel)
        {
            line = text;
            once = onlyOnce;
            minLevel = requiredLevel;
        }

        public override void OnPlayerEnter(PlayerController player)
        {
            base.OnPlayerEnter(player);
            var gm = GameManager.Instance;
            if (gm == null || gm.CurrentLevel < minLevel) return;
            if (once && used) return;
            used = true;
            RobotDialogue.Say(line, 3.5f);
        }
    }
}
