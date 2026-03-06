using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FlashFriends
{
    /// <summary>
    /// Victory screen shown when the player completes all quests and reaches the festival exit.
    /// Displays final stats, saves the game, then returns to Main Menu.
    ///
    /// SETUP (UI Canvas) :
    ///   UICanvas
    ///   └─ VictoryPanel   ← assign to victoryPanel (inactive by default)
    ///       ├─ PlayerNameText   [TMP]
    ///       ├─ TotalScoreText   [TMP]
    ///       ├─ TimePlayedText   [TMP]
    ///       ├─ QuestsText       [TMP]
    ///       ├─ PhotosText       [TMP]
    ///       └─ ReturnButton     [Button]
    ///
    /// Attach this script to the Canvas (not to the panel itself).
    /// </summary>
    public class VictoryPanel : MonoBehaviour
    {
        [Header("Panel")]
        [Tooltip("The victory panel (inactive by default).")]
        [SerializeField] private GameObject victoryPanel;

        [Header("Stats Texts")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI totalScoreText;
        [SerializeField] private TextMeshProUGUI timePlayedText;
        [SerializeField] private TextMeshProUGUI questsText;
        [SerializeField] private TextMeshProUGUI photosText;

        [Header("Button")]
        [Tooltip("Button to return to Main Menu immediately.")]
        [SerializeField] private Button returnButton;

        [Header("Settings")]
        [Tooltip("Seconds before auto-returning to Main Menu (real-time).")]
        [SerializeField] private float autoReturnDelay = 12f;

        [SerializeField] private string mainMenuSceneName = "MainMenu";

        // ─── Lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (returnButton != null) returnButton.onClick.AddListener(ReturnToMenu);
        }

        // ─── Public API ────────────────────────────────────────────────────

        /// <summary>Activates the victory screen and populates all stats.</summary>
        public void Show()
        {
            // Freeze game and stop timer
            GameTimer.Instance?.PauseTimer();
            Time.timeScale   = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            // Save game first so totalScore is synced
            SaveManager.Instance?.SaveCurrentGame();

            // Populate stats
            var save = SaveManager.Instance?.CurrentSave;

            if (playerNameText != null)
                playerNameText.text = save?.playerName ?? "Photographer";

            if (totalScoreText != null)
                totalScoreText.text = $"Good Vibes: {save?.totalScore ?? 0}";

            if (timePlayedText != null && save != null)
            {
                float secs = save.playTimeSeconds;
                int h = (int)(secs / 3600);
                int m = (int)((secs % 3600) / 60);
                int s = (int)(secs % 60);
                timePlayedText.text = h > 0
                    ? $"Time played: {h}h {m:00}m"
                    : $"Time played: {m}m {s:00}s";
            }

            var qm = QuestManager.Instance;
            if (questsText != null)
                questsText.text = $"Quests: {qm?.CompletedCount ?? 0} / {qm?.TotalQuests ?? 0}";

            int photoCount = save?.photoFileNames?.Count ?? 0;
            if (photosText != null)
                photosText.text = $"Photos taken: {photoCount}";

            // Show panel
            if (victoryPanel != null) victoryPanel.SetActive(true);

            StartCoroutine(AutoReturn());
        }

        // ─── Internal ──────────────────────────────────────────────────────

        private IEnumerator AutoReturn()
        {
            yield return new WaitForSecondsRealtime(autoReturnDelay);
            ReturnToMenu();
        }

        private void ReturnToMenu()
        {
            StopAllCoroutines();
            Time.timeScale   = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
