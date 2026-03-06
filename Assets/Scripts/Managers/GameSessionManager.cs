using System.Collections;
using UnityEngine;

namespace FlashFriends
{
    /// <summary>
    /// Game session manager: increments play time, runs auto-save, saves on quit/pause.
    /// In the Unity Editor, if SaveManager doesn't exist (launched directly from the game
    /// scene instead of MainMenu), creates a temporary test save automatically.
    ///
    /// SETUP (GameScene / TestScene):
    /// 1. Create a "GameSessionManager" GameObject and attach this script.
    /// </summary>
    public class GameSessionManager : MonoBehaviour
    {
        [Tooltip("Seconds between auto-saves.")]
        [SerializeField] private float autoSaveInterval = 60f;

        // ─── Lifecycle ──────────────────────────────────────────────────────

        private void Start()
        {
            EnsureSaveManager();

            if (SaveManager.Instance?.CurrentSave == null)
            {
                Debug.LogError("[GameSessionManager] No active save — cannot start session.");
                return;
            }

            // Lance la musique du jeu (AudioManager est DontDestroyOnLoad depuis MainMenu)
            AudioManager.Instance?.PlayGameMusic();

            StartCoroutine(AutoSaveRoutine());
        }

        private void Update()
        {
            if (SaveManager.Instance?.CurrentSave == null) return;
            SaveManager.Instance.CurrentSave.playTimeSeconds += Time.deltaTime;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveManager.Instance?.SaveCurrentGame();
        }

        private void OnApplicationQuit()
        {
            SaveManager.Instance?.SaveCurrentGame();
        }

        // ─── Auto-save ──────────────────────────────────────────────────────

        private IEnumerator AutoSaveRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(autoSaveInterval);
                if (SaveManager.Instance?.CurrentSave != null)
                {
                    SaveManager.Instance.SaveCurrentGame();
                    Debug.Log("[GameSessionManager] Auto-save done.");
                }
            }
        }

        // ─── Editor bootstrap ───────────────────────────────────────────────

        private static void EnsureSaveManager()
        {
            if (SaveManager.Instance != null) return;

#if UNITY_EDITOR
            // Launched directly from game scene — create a temporary SaveManager
            var go = new GameObject("[SaveManager - EditorTest]");
            go.AddComponent<SaveManager>();
            // Awake() fires synchronously → Instance is now set
            SaveManager.Instance?.InitializeTestSave();
            Debug.LogWarning("[GameSessionManager] Created editor test SaveManager. " +
                             "Launch from MainMenu for real save slots.");
#else
            Debug.LogError("[GameSessionManager] SaveManager not found! Launch from MainMenu.");
#endif
        }
    }
}
