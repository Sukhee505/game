using UnityEngine;
using SevenMinutes.Audio;
using SevenMinutes.Game;
using SevenMinutes.Puzzles;
using SevenMinutes.UI;

namespace SevenMinutes.NPC
{
    public interface INpcActionHandler
    {
        /// <summary>Applies the game-state change requested by the NPC. Returns true if the dialogue should close.</summary>
        bool Handle(NpcDecision decision);
    }

    /// <summary>Maps validated NPC actions to game-state changes (open the great door, knock the player back).</summary>
    public class GuardActionHandler : MonoBehaviour, INpcActionHandler
    {
        [SerializeField] private FinalDoorPuzzle door;
        [SerializeField] private Transform knockbackPoint;

        public void Configure(FinalDoorPuzzle finalDoor, Transform knockback)
        {
            door = finalDoor;
            knockbackPoint = knockback;
        }

        public bool Handle(NpcDecision decision)
        {
            switch (decision.Action)
            {
                case NpcAction.OpenDoor:
                    door.OpenByGuard();
                    return true;

                case NpcAction.Attack:
                    var gm = GameManager.Instance;
                    if (gm != null && knockbackPoint != null)
                    {
                        gm.Player.Teleport(knockbackPoint.position, knockbackPoint.rotation);
                        gm.Player.CameraRig.Shake(0.35f, 0.5f);
                    }
                    AudioManager.Instance?.Play(SoundId.Impact, null, 1f, 0.8f);
                    ScreenEffects.Instance?.FlashRed(0.8f);
                    GameHUD.Toast("UNIT-6 sana saldırdı ve seni geri fırlattı!", 3f);
                    return true;

                default:
                    return false;
            }
        }
    }
}
