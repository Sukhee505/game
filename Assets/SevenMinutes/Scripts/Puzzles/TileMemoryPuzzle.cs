using System.Collections;
using UnityEngine;
using SevenMinutes.Audio;
using SevenMinutes.Environment;
using SevenMinutes.Game;
using SevenMinutes.Player;
using SevenMinutes.Robot;

namespace SevenMinutes.Puzzles
{
    /// <summary>
    /// Level 2. A 4 × 10 tile floor over a pit. The start button shows the continuous safe route in green
    /// for a few seconds; stepping on any other tile makes it collapse (the player falls and restarts the room).
    /// </summary>
    public class TileMemoryPuzzle : MonoBehaviour
    {
        public const int Columns = 4;
        public const int Rows = 10;

        /// <summary>Continuous safe route from row 0 (entrance) to row 9 (exit). (column, row)</summary>
        public static readonly Vector2Int[] SafePath =
        {
            new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3),
            new Vector2Int(1, 3), new Vector2Int(1, 4), new Vector2Int(2, 4), new Vector2Int(2, 5), new Vector2Int(2, 6),
            new Vector2Int(3, 6), new Vector2Int(3, 7), new Vector2Int(3, 8), new Vector2Int(2, 8), new Vector2Int(2, 9)
        };

        [SerializeField] private MemoryTile[] tiles;           // index = row * Columns + column
        [SerializeField] private PushButton startButton;
        [SerializeField] private PlayerZone endZone;
        [SerializeField] private LeverController lever;
        [SerializeField] private DoorController exitDoor;
        [SerializeField] private DoorController[] robotGates;
        [SerializeField] private Transform robotMeetPoint;
        [SerializeField] private float previewSeconds = 2f;

        private bool previewing;
        private bool previewedOnce;
        private bool completed;
        private int failures;

        public bool Completed => completed;
        public bool PreviewedOnce => previewedOnce;
        public int Failures => failures;
        public bool LeverPulled { get; private set; }

        public void Configure(MemoryTile[] tileArray, PushButton button, PlayerZone end, LeverController l,
            DoorController exit, DoorController[] gates, Transform meet)
        {
            tiles = tileArray; startButton = button; endZone = end; lever = l; exitDoor = exit; robotGates = gates; robotMeetPoint = meet;
        }

        private void Start()
        {
            var safe = new bool[Columns * Rows];
            foreach (var p in SafePath) safe[p.y * Columns + p.x] = true;
            for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Columns; c++)
                tiles[r * Columns + c].Setup(this, c, r, safe[r * Columns + c]);

            startButton.Pressed += () => { if (!previewing) StartCoroutine(Preview()); };
            endZone.Entered += _ => Complete();
            lever.Pulled += OnLever;
            if (GameManager.Instance != null) GameManager.Instance.Respawned += OnRespawned;
        }

        private IEnumerator Preview()
        {
            previewing = true;
            AudioManager.Instance?.Play(SoundId.PowerUp, startButton.transform.position, 0.6f, 1.4f);
            yield return new WaitForSeconds(0.3f);
            // light the path tile by tile, then hold
            foreach (var p in SafePath)
            {
                tiles[p.y * Columns + p.x].SetGlow(new Color(0.1f, 1.6f, 0.35f));
                AudioManager.Instance?.Play(SoundId.KeyPress, tiles[p.y * Columns + p.x].transform.position, 0.35f, 0.8f + p.y * 0.05f);
                yield return new WaitForSeconds(0.06f);
            }
            yield return new WaitForSeconds(previewSeconds);
            foreach (var t in tiles) if (!t.Collapsed) t.SetGlow(Color.black);
            AudioManager.Instance?.Play(SoundId.Beep, startButton.transform.position, 0.5f, 0.6f);
            if (!previewedOnce) RobotDialogue.Say("Ezberledin mi? Sadece o karolara bas.", 3.5f);
            previewedOnce = true;
            previewing = false;
        }

        public void OnTileStepped(MemoryTile tile, PlayerController player)
        {
            if (completed) return;
            if (tile.Safe)
            {
                if (!previewing) tile.Confirm();
                AudioManager.Instance?.Play(SoundId.Click, tile.transform.position, 0.25f, 0.7f);
                return;
            }
            failures++;
            tile.Collapse();
            AudioManager.Instance?.Play(SoundId.Slam, tile.transform.position, 0.8f, 1.4f);
            if (failures == 1 && !previewedOnce) RobotDialogue.Say("Önce düğmeye bas. Güvenli yolu gösteriyor.", 4f, true);
            else if (failures == 3) RobotDialogue.Say("Soldan ikinci karodan başla. Yol yılan gibi sağa kıvrılıyor.", 5f, true);
        }

        private void OnRespawned(int level)
        {
            if (level != 2) return;
            foreach (var t in tiles) if (t.Collapsed) t.Restore();
        }

        private void Complete()
        {
            if (completed) return;
            completed = true;
            AudioManager.Instance?.Play(SoundId.Success, endZone.transform.position, 0.9f);
            lever.SetUsable(true);
            RobotDialogue.Say("Başardın! Kolu çek.", 3f, true);
            GameManager.Instance?.SetObjective("Sonraki kapıyı açmak için kolu çek.");
        }

        private void OnLever()
        {
            LeverPulled = true;
            exitDoor.Unlock(true);
            foreach (var g in robotGates) if (g != null) g.Unlock(true);
            GameManager.Instance?.CompleteChallenge();
            GameManager.Instance?.SetObjective("Kapıdan geç.");
            RobotDialogue.Say("Etrafından geliyorum. Beni bekle.", 3f);
            var robot = GameManager.Instance != null ? GameManager.Instance.Robot : null;
            if (robot != null) robot.MoveTo(robotMeetPoint.position, robot.Follow);
        }
    }
}
