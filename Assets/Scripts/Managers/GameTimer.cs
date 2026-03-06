using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace FlashFriends
{
    /// <summary>
    /// Festival countdown timer — 24 real minutes = 24 in-game hours.
    /// Fires events every frame (for HUD) and when time runs out (Game Over).
    /// Pauses automatically when Time.timeScale == 0 (pause menu).
    ///
    /// SETUP (GameScene / TestScene) :
    /// 1. Add this script to a dedicated "GameTimer" GameObject.
    /// 2. In the Inspector:
    ///    • Connect onTimeChanged  → HUDManager.UpdateTimer
    ///    • Connect onTimerExpired → (leave empty, script handles scene load)
    /// 3. Set gameOverSceneName to your Game Over scene name.
    /// </summary>
    public class GameTimer : MonoBehaviour
    {
        public static GameTimer Instance { get; private set; }

        [Header("Duration")]
        [Tooltip("Total festival duration in real seconds. Default = 24 min (1 real min = 1 in-game hour).")]
        [SerializeField] private float festivalDurationSeconds = 24f * 60f; // 1440 s

        [Header("Scenes")]
        [SerializeField] private string gameOverSceneName = "GameOver";

        [Header("Events")]
        [Tooltip("Fired every frame with remaining seconds. Connect to HUDManager.UpdateTimer.")]
        public UnityEvent<float> onTimeChanged;

        [Tooltip("Fired once when the timer reaches 0, just before scene load.")]
        public UnityEvent onTimerExpired;

        // ─── State ─────────────────────────────────────────────────────────

        public float RemainingSeconds { get; private set; }
        public bool  IsRunning        { get; private set; }

        // ─── Lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            RemainingSeconds = festivalDurationSeconds;
        }

        private void Start()
        {
            IsRunning = true;
            onTimeChanged?.Invoke(RemainingSeconds);
        }

        private void Update()
        {
            if (!IsRunning || Time.timeScale == 0f) return;

            RemainingSeconds -= Time.deltaTime;
            onTimeChanged?.Invoke(RemainingSeconds);

            if (RemainingSeconds <= 0f)
            {
                RemainingSeconds = 0f;
                IsRunning = false;
                TriggerGameOver();
            }
        }

        // ─── Public API ────────────────────────────────────────────────────

        public void PauseTimer()  => IsRunning = false;
        public void ResumeTimer() => IsRunning = true;

        // ─── Game Over ─────────────────────────────────────────────────────

        private void TriggerGameOver()
        {
            onTimerExpired?.Invoke();
            SaveManager.Instance?.SaveCurrentGame();
            Debug.Log("[GameTimer] Festival over — loading Game Over screen.");
            SceneManager.LoadScene(gameOverSceneName);
        }
    }
}
