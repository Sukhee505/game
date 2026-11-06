using System.Collections;
using UnityEngine;
using SevenMinutes.Player;
using SevenMinutes.UI;

namespace SevenMinutes.Game
{
    /// <summary>One respawn point per level. Failing only sends the player back to the current level's start.</summary>
    public class CheckpointManager : MonoBehaviour
    {
        [Tooltip("Index 0 = Level 1 (bedroom), 1 = tile room, 2 = lava room, 3 = trap corridor, 4 = final hall.")]
        [SerializeField] private Transform[] checkpoints;

        public void Configure(Transform[] points) => checkpoints = points;

        public Transform Get(int level)
        {
            if (checkpoints == null || checkpoints.Length == 0) return null;
            int i = Mathf.Clamp(level - 1, 0, checkpoints.Length - 1);
            return checkpoints[i];
        }

        public IEnumerator Respawn(PlayerController player, int level)
        {
            player.SetFrozen(true);
            var fx = ScreenEffects.Instance;
            if (fx != null)
            {
                fx.FlashRed(0.6f);
                yield return fx.FadeTo(1f, 0.45f);
            }
            else yield return new WaitForSeconds(0.45f);

            var cp = Get(level);
            if (cp != null) player.Teleport(cp.position, cp.rotation);
            yield return new WaitForSeconds(0.3f);

            if (fx != null) yield return fx.FadeTo(0f, 0.45f);
            player.SetFrozen(false);
        }
    }
}
