using UnityEngine;
using SevenMinutes.Player;
using SevenMinutes.Robot;

namespace SevenMinutes.Game
{
    /// <summary>Reacts to level changes: objectives, robot lines and where the robot goes.</summary>
    public class LevelDirector : MonoBehaviour
    {
        [SerializeField] private RobotController robot;
        [SerializeField] private PlayerController player;
        [SerializeField] private Transform lavaWaitPoint;
        [SerializeField] private Transform finalWaitPoint;

        public void Configure(RobotController r, PlayerController p, Transform lava, Transform final)
        {
            robot = r; player = p; lavaWaitPoint = lava; finalWaitPoint = final;
        }

        private void Start()
        {
            GameManager.Instance.LevelChanged += OnLevel;
        }

        private void OnLevel(int level)
        {
            var gm = GameManager.Instance;
            switch (level)
            {
                case 2:
                    gm.SetObjective("Düğmeye bas ve güvenli karoları ezberle.");
                    RobotDialogue.Say("Bu zemin... bir tuzak. Dikkatli ol.", 3.5f, true);
                    break;
                case 3:
                    gm.SetObjective("Robottan yardım istemek için bilgisayarı kullan.");
                    RobotDialogue.Say("Bakım kanalından karşıya geçeceğim. Benimle konuşmak için o bilgisayarı kullan.", 5f, true);
                    robot.WaitAt(lavaWaitPoint.position, player.transform);
                    break;
                case 4:
                    gm.SetObjective("Tuzak koridorundan sağ çık ve son kapıya ulaş.");
                    RobotDialogue.Say("Kanaldan geçip seninle çıkışta buluşacağım. Tuzaklara dikkat et!", 4.5f, true);
                    robot.WaitAt(finalWaitPoint.position, player.transform);
                    break;
                case 5:
                    gm.CompleteChallenge();
                    gm.SetObjective("Kapıyı bekçi robot UNIT-6 koruyor. Önce robotla konuş (E), UNIT-6'ya güç versin; sonra UNIT-6 ile konuş.");
                    RobotDialogue.Say("Çıkış burası! Kapıdaki eski bekçi UNIT-6 kapalı. Ona güç verebilirim — benimle konuş.", 4f, true);
                    break;
            }
        }
    }
}
