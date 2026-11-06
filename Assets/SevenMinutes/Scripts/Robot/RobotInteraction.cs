using UnityEngine;
using SevenMinutes.Core;
using SevenMinutes.Game;
using SevenMinutes.Player;
using SevenMinutes.Puzzles;

namespace SevenMinutes.Robot
{
    /// <summary>[E] Talk to Robot — gives a hint, or asks it to power the final door.</summary>
    public class RobotInteraction : MonoBehaviour, IInteractable
    {
        [SerializeField] private RobotController robot;
        [SerializeField] private RobotHintSystem hints;
        [SerializeField] private FinalDoorPuzzle finalDoor;

        public void Configure(RobotController r, RobotHintSystem h, FinalDoorPuzzle f)
        {
            robot = r; hints = h; finalDoor = f;
        }

        private bool AskForPower =>
            finalDoor != null && finalDoor.WaitingForRobot && GameManager.Instance != null && GameManager.Instance.CurrentLevel >= 5;

        public string Prompt => AskForPower ? "Robottan UNIT-6'ya güç vermesini iste" : "Robotla Konuş";
        public bool CanInteract => robot != null && robot.Mode != RobotMode.Inactive;

        public void Interact(PlayerController player)
        {
            if (AskForPower)
            {
                finalDoor.RequestRobotPower();
                return;
            }
            robot.LookAt(player.transform.position + Vector3.up * 1.6f, 2f);
            hints.RequestHint();
        }
    }
}
