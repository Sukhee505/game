using UnityEngine;
using SevenMinutes.Core;
using SevenMinutes.Game;
using SevenMinutes.Puzzles;

namespace SevenMinutes.Robot
{
    /// <summary>
    /// Context-aware hints. The robot offers a hint automatically when the player seems stuck,
    /// and gives an immediate (escalating) hint when the player talks to it. It never solves puzzles itself.
    /// </summary>
    public class RobotHintSystem : MonoBehaviour
    {
        [SerializeField] private RobotController robot;
        [SerializeField] private MathPuzzle math;
        [SerializeField] private TileMemoryPuzzle tiles;
        [SerializeField] private LavaBridgePuzzle lava;
        [SerializeField] private FinalDoorPuzzle finalDoor;
        [SerializeField] private float firstAutoHint = 35f;

        private float idle;
        private float nextAuto;
        private int lastLevel = -1;
        private int noteAsks;

        public void Configure(RobotController r, MathPuzzle m, TileMemoryPuzzle t, LavaBridgePuzzle l, FinalDoorPuzzle f)
        {
            robot = r; math = m; tiles = t; lava = l; finalDoor = f;
        }

        private void Start() => nextAuto = firstAutoHint;

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing || !gm.Timer.Started || robot.Mode == RobotMode.Inactive) return;
            if (gm.CurrentLevel != lastLevel)
            {
                lastLevel = gm.CurrentLevel;
                idle = 0f;
                nextAuto = firstAutoHint;
            }
            idle += Time.deltaTime;
            if (idle < nextAuto) return;
            idle = 0f;
            nextAuto = Mathf.Min(nextAuto + 10f, 60f);
            string h = Hint(false);
            if (h != null) RobotDialogue.Say(h, 4.5f);
        }

        public void RequestHint()
        {
            idle = 0f;
            string h = Hint(true) ?? "Fazla zamanımız yok. Devam edelim.";
            RobotDialogue.Say(h, 4.5f, true);
        }

        private string Hint(bool asked)
        {
            var gm = GameManager.Instance;
            int level = gm.CurrentLevel;

            if (level <= 1)
            {
                if (!gm.Timer.Started) return "Burada bir şeyler ters. Etrafına dikkatlice bak.";
                if (math.Solved) return "Merdivenleri bulmalıyız. Koridorun en sonunda.";
                if (math.SecretFound) return "Nottaki dört işlemi çöz. Her cevap şifrenin bir rakamı.";
                if (asked) noteAsks++;
                switch (noteAsks)
                {
                    case 0:
                    case 1: return "Sanırım çekmecelerden birinin içinde bir şey var.";
                    case 2: return "Gardırobu, masayı ve komodini kontrol et. Sahte notlara aldırma.";
                    default:
                        if (math.SecretLocation != null) robot.LookAt(math.SecretLocation.transform.position, 5f);
                        return "Taranıyor... " + (math.SecretLocation != null ? math.SecretLocation.LocationName : "çekmece") + " içindeki kâğıt farklı görünüyor.";
                }
            }

            if (level == 2)
            {
                if (tiles.LeverPulled) return "Kapı açık. Hadi gidelim.";
                if (tiles.Completed) return "Kolu çek!";
                if (!tiles.PreviewedOnce) return "Girişteki düğmeye bas. Güvenli yolu gösterecek.";
                if (tiles.Failures >= 2) return "Soldan ikinci karodan başla. Yol yılan gibi sağa kıvrılıyor.";
                return "Yeşil karoları ezberle. Tek yanlış adım ve zemin çöker.";
            }

            if (level == 3)
            {
                if (lava.BridgeExtended) return "Köprüden geç ve kolu çek.";
                if (lava.Requested) return "Hallediyorum!";
                if (!lava.MessagesSent) return "Sağındaki bilgisayarı kullan. Oradan mesajları okuyabilirim.";
                return "Sadece YARDIM yaz. Gerisini ben hallederim.";
            }

            if (level == 4)
            {
                float z = gm.Player.transform.position.z;
                if (z < 71f) return "Topu izle. Kenara savrulduğunda koşarak geç.";
                if (z < 81f) return "Duvarların açılmasını bekle, sonra dümdüz koş.";
                return "Alçak lazerin üstünden atla. Yüksek olanın altından Ctrl ile eğilerek geç. Hareketli olan yükselince geç.";
            }

            if (finalDoor.Solved) return "Git! Kapıdan geç!";
            if (finalDoor.WaitingForRobot) return asked ? null : "UNIT-6'nın güce ihtiyacı var. Benden iste — plakanın üstünde dururum.";
            if (!finalDoor.Powered) return "Plakaya yürüyorum...";
            return finalDoor.Hint();
        }
    }
}
