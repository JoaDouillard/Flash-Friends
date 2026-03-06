using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace FlashFriends
{
    /// <summary>
    /// Pause menu — toggled by Escape.
    /// Uses an InputAction created in code for reliable Escape detection in both
    /// the Unity Editor and standalone builds (avoids editor cursor-lock interception).
    ///
    /// SETUP :
    /// 1. Add this script to a dedicated "PauseManager" GameObject — must be ALWAYS ACTIVE.
    ///    (The pausePanel is what gets shown/hidden, not this GameObject.)
    /// 2. Assign pausePanel (disabled by default) and the buttons in the Inspector.
    /// 3. Optionally assign SettingsMenuUI; if left empty it is auto-detected.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject pausePanel;

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;

        [Header("References")]
        [Tooltip("SettingsMenuUI — auto-detected if left empty.")]
        [SerializeField] private SettingsMenuUI settingsMenu;

        [Header("Scenes")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        // ─── State ─────────────────────────────────────────────────────────

        private bool               _isPaused;
        private CameraManager      _cameraManager;
        private PlayerInputHandler _playerInput;

        private InputAction _pauseAction;
        private bool        _pauseRequested;

        // ─── Lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            if (resumeButton   != null) resumeButton.onClick.AddListener(ResumeGame);
            if (restartButton  != null) restartButton.onClick.AddListener(OnRestartClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            if (quitButton     != null) quitButton.onClick.AddListener(OnQuitClicked);

            if (settingsMenu == null)
                settingsMenu = FindFirstObjectByType<SettingsMenuUI>();

            _cameraManager = FindFirstObjectByType<CameraManager>();
            _playerInput   = FindFirstObjectByType<PlayerInputHandler>();

            if (pausePanel != null) pausePanel.SetActive(false);

            // InputAction created in code — reliable Escape detection in editor + builds
            _pauseAction = new InputAction("Pause", InputActionType.Button);
            _pauseAction.AddBinding("<Keyboard>/escape");
            _pauseAction.performed += _ => _pauseRequested = true;
            _pauseAction.Enable();
        }

        private void Update()
        {
            // Belt-and-suspenders: also poll Keyboard directly
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                _pauseRequested = true;

            if (_pauseRequested)
            {
                _pauseRequested = false;
                TryTogglePause();
            }
        }

        private void OnDestroy()
        {
            _pauseAction?.Disable();
            _pauseAction?.Dispose();
        }

        // ─── Input handler ─────────────────────────────────────────────────

        private void TryTogglePause()
        {
            // Block if gallery is open
            if (_playerInput != null && _playerInput.galleryOpen) return;

            // Block in PhoneCamera mode
            if (_cameraManager != null && _cameraManager.CurrentMode == CameraMode.PhoneCamera) return;

            TogglePause();
        }

        // ─── Pause / Resume ────────────────────────────────────────────────

        public void TogglePause()
        {
            if (_isPaused) ResumeGame();
            else           PauseGame();
        }

        public void PauseGame()
        {
            _isPaused = true;

            if (pausePanel != null) pausePanel.SetActive(true);
            Time.timeScale   = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            if (AudioManager.Instance != null) AudioManager.Instance.PauseMusic();
        }

        public void ResumeGame()
        {
            _isPaused = false;

            if (pausePanel != null) pausePanel.SetActive(false);
            Time.timeScale   = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;

            if (AudioManager.Instance != null) AudioManager.Instance.ResumeMusic();
        }

        public bool IsPaused => _isPaused;

        // ─── Button callbacks ──────────────────────────────────────────────

        private void OnRestartClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnSettingsClicked()
        {
            if (settingsMenu == null)
            {
                Debug.LogWarning("[PauseMenuUI] SettingsMenuUI not assigned!");
                return;
            }

            if (pausePanel != null) pausePanel.SetActive(false);
            settingsMenu.OpenSettings(() =>
            {
                if (pausePanel != null) pausePanel.SetActive(true);
            });
        }

        private void OnMainMenuClicked()
        {
            Time.timeScale   = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void OnQuitClicked()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
