using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace FlashFriends
{
    /// <summary>
    /// Écran Game Over (scène dédiée).
    /// Boutons : Restart (recharge la scène de jeu), Menu Principal, Quitter.
    ///
    /// SETUP SCÈNE GameOver :
    /// 1. Créer une scène "GameOver" dans Build Settings.
    /// 2. Ajouter un AudioManager si pas déjà présent (DontDestroyOnLoad).
    /// 3. Canvas
    ///    └─ GameOverPanel  ← glisser dans "Game Over Panel"
    ///       ├─ TitleText    [TextMeshProUGUI]
    ///       ├─ MessageText  [TextMeshProUGUI]
    ///       ├─ RestartButton
    ///       ├─ MainMenuButton
    ///       └─ QuitButton
    ///
    /// Pour charger cette scène depuis le jeu :
    ///   SceneManager.LoadScene("GameOver");
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject gameOverPanel;

        [Header("Textes")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;

        [Header("Boutons")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;

        [Header("Scènes")]
        [SerializeField] private string gameSceneName     = "GameScene";
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        // ─── Cycle de vie ─────────────────────────────────────────────────

        private void Awake()
        {
            if (restartButton  != null) restartButton.onClick.AddListener(OnRestartClicked);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            if (quitButton     != null) quitButton.onClick.AddListener(OnQuitClicked);
        }

        private void Start()
        {
            ShowGameOver("The festival is over — try again!");

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayGameOverSequence();
        }

        // ─── API publique ─────────────────────────────────────────────────

        /// <summary>Affiche l'écran Game Over avec un message personnalisé.</summary>
        public void ShowGameOver(string message = "The festival is over — try again!")
        {
            if (titleText   != null) titleText.text   = "GAME OVER";
            if (messageText != null) messageText.text = message;
            if (gameOverPanel != null) gameOverPanel.SetActive(true);

            Time.timeScale   = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        // ─── Boutons ──────────────────────────────────────────────────────

        private void OnRestartClicked()
        {
            SceneManager.LoadScene(gameSceneName);
        }

        private void OnMainMenuClicked()
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
