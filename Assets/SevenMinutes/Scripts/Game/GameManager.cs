using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using SevenMinutes.Audio;
using SevenMinutes.Core;
using SevenMinutes.Intro;
using SevenMinutes.Player;
using SevenMinutes.Robot;
using SevenMinutes.UI;

namespace SevenMinutes.Game
{
    /// <summary>
    /// Central state machine: MainMenu → Introduction → Playing ⇄ Paused → Victory / Defeat.
    /// Owns progression (current level, failures, completed challenges) and restart logic.
    /// </summary>
    [DefaultExecutionOrder(-300)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        /// <summary>Set before reloading the scene so Restart skips the main menu.</summary>
        public static bool SkipMenuOnNextLoad;

        [SerializeField] private PlayerController player;
        [SerializeField] private RobotController robot;
        [SerializeField] private GameTimer timer;
        [SerializeField] private CheckpointManager checkpoints;
        [SerializeField] private IntroSequence intro;
        [SerializeField] private VictoryManager victory;
        [SerializeField] private DefeatManager defeat;

        public GameState State { get; private set; } = GameState.MainMenu;
        public int CurrentLevel { get; private set; } = 1;
        public int FailedAttempts { get; private set; }
        public int ChallengesCompleted { get; private set; }
        public string Objective { get; private set; } = "";
        public bool IsModalOpen => modalOwner != null;
        public bool IsRespawning { get; private set; }

        public PlayerController Player => player;
        public RobotController Robot => robot;
        public GameTimer Timer => timer;

        public bool CanControlPlayer =>
            State == GameState.Playing && modalOwner == null && !IsRespawning && Time.frameCount > modalClosedFrame + 1;

        public event Action<GameState> StateChanged;
        public event Action<int> LevelChanged;
        public event Action<string> ObjectiveChanged;
        public event Action<int> Respawned;

        private object modalOwner;
        private int modalClosedFrame = -10;
        private GameState stateBeforePause;

        public void Configure(PlayerController p, RobotController r, GameTimer t, CheckpointManager c,
            IntroSequence i, VictoryManager v, DefeatManager d)
        {
            player = p; robot = r; timer = t; checkpoints = c; intro = i; victory = v; defeat = d;
        }

        private void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            GameSettings.Load();
            GameSettings.Apply();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        private void Start()
        {
            timer.Expired += OnTimeExpired;
            if (SkipMenuOnNextLoad)
            {
                SkipMenuOnNextLoad = false;
                StartNewGame();
            }
            else
            {
                SetState(GameState.MainMenu);
                intro.ShowMenuView();
            }
        }

        private void Update()
        {
            if (InputReader.Down(GameKey.Pause) && modalOwner == null)
            {
                if (State == GameState.Playing || State == GameState.Introduction) Pause();
                else if (State == GameState.Paused) Resume();
            }
            UpdateCursor();
        }

        private void UpdateCursor()
        {
            bool locked = State == GameState.Playing && modalOwner == null;
            var mode = locked ? CursorLockMode.Locked : CursorLockMode.None;
            if (Cursor.lockState != mode) Cursor.lockState = mode;
            if (Cursor.visible == locked) Cursor.visible = !locked;
        }

        // ---------------- flow ----------------

        public void StartNewGame()
        {
            SetState(GameState.Introduction);
            intro.Play();
        }

        /// <summary>Called by the intro when the character has stood up and the player gets control.</summary>
        public void OnIntroFinished()
        {
            if (State == GameState.Introduction) SetState(GameState.Playing);
            else if (State == GameState.Paused) stateBeforePause = GameState.Playing;
        }

        /// <summary>Called when the TV video ends. The timer cannot start before this.</summary>
        public void BeginCountdown()
        {
            timer.BeginCountdown();
            SetObjective("Bir çıkış yolu bul. Kapı 4 haneli bir şifre istiyor — odayı ara.");
        }

        public void EnterLevel(int level)
        {
            if (level <= CurrentLevel) return;
            CurrentLevel = level;
            LevelChanged?.Invoke(level);
        }

        public void CompleteChallenge() => ChallengesCompleted++;

        public void SetObjective(string objective)
        {
            Objective = objective;
            ObjectiveChanged?.Invoke(objective);
        }

        public void OpenModal(object owner)
        {
            modalOwner = owner;
        }

        public void CloseModal(object owner)
        {
            if (modalOwner != owner) return;
            modalOwner = null;
            modalClosedFrame = Time.frameCount;
        }

        // ---------------- failure / respawn ----------------

        public void ReportFailure(string message, FailKind kind)
        {
            if (State != GameState.Playing || IsRespawning) return;
            IsRespawning = true;
            FailedAttempts++;
            AudioManager.Instance?.PlayFail(kind);
            GameHUD.Toast(message, 2.5f);
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            int level = CurrentLevel;
            yield return checkpoints.Respawn(player, level);
            IsRespawning = false;
            Respawned?.Invoke(level);
        }

        // ---------------- pause ----------------

        public void Pause()
        {
            if (State != GameState.Playing && State != GameState.Introduction) return;
            stateBeforePause = State;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SetState(stateBeforePause);
        }

        // ---------------- end states ----------------

        public void TriggerVictory()
        {
            if (State != GameState.Playing) return;
            timer.Stop();
            modalOwner = null;
            SetState(GameState.Victory);
            victory.Play();
        }

        private void OnTimeExpired()
        {
            if (State == GameState.Victory || State == GameState.Defeat) return;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            modalOwner = null;
            SetState(GameState.Defeat);
            defeat.Play();
        }

        public void Restart()
        {
            SkipMenuOnNextLoad = true;
            Reload();
        }

        public void GoToMainMenu()
        {
            SkipMenuOnNextLoad = false;
            Reload();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void Reload()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void SetState(GameState s)
        {
            State = s;
            StateChanged?.Invoke(s);
        }

        // ---------------- stats ----------------

        public float CompletionTime => timer.Elapsed;
    }
}
