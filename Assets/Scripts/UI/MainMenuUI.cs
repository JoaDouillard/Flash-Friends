using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace FlashFriends
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;

        [Header("Boutons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button tutorialButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Références")]
        [Tooltip("SaveMenuUI pour la sélection / création de slot.")]
        [SerializeField] private SaveMenuUI saveMenu;

        [Tooltip("SettingsMenuUI pour ouvrir les paramètres.")]
        [SerializeField] private SettingsMenuUI settingsMenu;

        [Tooltip("TutorialUI pour ouvrir le tutoriel.")]
        [SerializeField] private TutorialUI tutorialUI;

        // ─── Cycle de vie ──────────────────────────────────────────────────

        private void Awake()
        {
            if (playButton     != null) playButton.onClick.AddListener(OnPlayClicked);
            if (tutorialButton != null) tutorialButton.onClick.AddListener(OnTutorialClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
            if (quitButton     != null) quitButton.onClick.AddListener(OnQuitClicked);

            if (saveMenu    == null) saveMenu    = FindFirstObjectByType<SaveMenuUI>();
            if (settingsMenu == null) settingsMenu = FindFirstObjectByType<SettingsMenuUI>();
            if (tutorialUI  == null) tutorialUI  = FindFirstObjectByType<TutorialUI>();
        }

        private void Start()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            Time.timeScale   = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayMainMenuMusic();
        }

        // ─── Boutons ────────────────────────────────────────────────────────

        private void OnPlayClicked()
        {
            if (saveMenu == null)
            {
                Debug.LogWarning("[MainMenuUI] SaveMenuUI non assignée !");
                return;
            }

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            saveMenu.OpenSaveMenu(() =>
            {
                if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            });
        }

        private void OnTutorialClicked()
        {
            if (tutorialUI == null)
            {
                Debug.LogWarning("[MainMenuUI] TutorialUI non assignée !");
                return;
            }

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            tutorialUI.OpenTutorial(() =>
            {
                if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            });
        }

        private void OnSettingsClicked()
        {
            if (settingsMenu == null)
            {
                Debug.LogWarning("[MainMenuUI] SettingsMenuUI non assignée !");
                return;
            }

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            settingsMenu.OpenSettings(() =>
            {
                if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            });
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
