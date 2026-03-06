using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FlashFriends
{
    /// <summary>
    /// UI for a single save slot.
    /// Two states: empty (New Game flow) and occupied (Continue / Delete).
    ///
    /// SETUP (on the prefab) :
    ///   • Set slotIndex (0, 1 or 2) in the Inspector.
    ///   • Assign emptyPanel and occupiedPanel.
    ///   • Assign all child UI elements in their respective fields.
    ///
    /// The slot is refreshed by SaveMenuUI after every action.
    /// </summary>
    public class SaveSlotUI : MonoBehaviour
    {
        [Header("Slot Index (0, 1 or 2)")]
        [SerializeField] private int slotIndex = 0;

        [Header("State Panels")]
        [Tooltip("Shown when the slot is empty.")]
        [SerializeField] private GameObject emptyPanel;

        [Tooltip("Shown when the slot contains a save.")]
        [SerializeField] private GameObject occupiedPanel;

        [Tooltip("Confirmation panel shown before deletion (disabled by default).")]
        [SerializeField] private GameObject confirmDeletePanel;

        // ── Empty state ────────────────────────────────────────────────────

        [Header("Empty State")]
        [Tooltip("'New Game' button.")]
        [SerializeField] private Button newGameButton;

        [Tooltip("Name input panel (InputField + Confirm) — shown after clicking New Game.")]
        [SerializeField] private GameObject nameInputPanel;

        [Tooltip("Player name input field.")]
        [SerializeField] private TMP_InputField playerNameInput;

        [Tooltip("Confirm creation button.")]
        [SerializeField] private Button confirmNewGameButton;

        [Tooltip("Cancel name entry button.")]
        [SerializeField] private Button cancelNameButton;

        // ── Occupied state ─────────────────────────────────────────────────

        [Header("Occupied State")]
        [Tooltip("Player name label.")]
        [SerializeField] private TextMeshProUGUI playerNameText;

        [Tooltip("Total Good Vibes score.")]
        [SerializeField] private TextMeshProUGUI scoreText;

        [Tooltip("Play time (HH:MM:SS).")]
        [SerializeField] private TextMeshProUGUI playTimeText;

        [Tooltip("Slot creation date.")]
        [SerializeField] private TextMeshProUGUI creationDateText;

        [Tooltip("'Continue' button.")]
        [SerializeField] private Button continueButton;

        [Tooltip("'Delete' button.")]
        [SerializeField] private Button deleteButton;

        // ── Delete confirmation ────────────────────────────────────────────

        [Header("Delete Confirmation")]
        [Tooltip("Confirm deletion button.")]
        [SerializeField] private Button confirmDeleteButton;

        [Tooltip("Cancel deletion button.")]
        [SerializeField] private Button cancelDeleteButton;

        // ─── Refresh callback ──────────────────────────────────────────────

        private System.Action _onRefresh;

        // ─── Lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            if (newGameButton        != null) newGameButton.onClick.AddListener(OnNewGameClicked);
            if (confirmNewGameButton != null) confirmNewGameButton.onClick.AddListener(OnConfirmNewGame);
            if (cancelNameButton     != null) cancelNameButton.onClick.AddListener(OnCancelName);
            if (continueButton       != null) continueButton.onClick.AddListener(OnContinueClicked);
            if (deleteButton         != null) deleteButton.onClick.AddListener(OnDeleteClicked);
            if (confirmDeleteButton  != null) confirmDeleteButton.onClick.AddListener(OnConfirmDelete);
            if (cancelDeleteButton   != null) cancelDeleteButton.onClick.AddListener(OnCancelDelete);
        }

        // ─── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Refreshes the slot display from save data.
        /// Called by SaveMenuUI after every action.
        /// Pass null for data to display the empty state.
        /// </summary>
        public void Refresh(SaveData data, System.Action onRefresh)
        {
            _onRefresh = onRefresh;

            // Reset transient panels
            if (nameInputPanel     != null) nameInputPanel.SetActive(false);
            if (confirmDeletePanel != null) confirmDeletePanel.SetActive(false);

            if (data == null) ShowEmptyState();
            else              ShowOccupiedState(data);
        }

        // ─── States ────────────────────────────────────────────────────────

        private void ShowEmptyState()
        {
            if (emptyPanel    != null) emptyPanel.SetActive(true);
            if (occupiedPanel != null) occupiedPanel.SetActive(false);
        }

        private void ShowOccupiedState(SaveData data)
        {
            if (emptyPanel    != null) emptyPanel.SetActive(false);
            if (occupiedPanel != null) occupiedPanel.SetActive(true);

            if (playerNameText   != null) playerNameText.text   = data.playerName;
            if (scoreText        != null) scoreText.text        = $"{data.totalScore} Good Vibes";
            if (playTimeText     != null) playTimeText.text     = SaveManager.FormatPlayTime(data.playTimeSeconds);
            if (creationDateText != null) creationDateText.text = data.creationDate;
        }

        // ─── Button callbacks ──────────────────────────────────────────────

        private void OnNewGameClicked()
        {
            if (nameInputPanel  != null) nameInputPanel.SetActive(true);
            if (playerNameInput != null)
            {
                playerNameInput.text = string.Empty;
                playerNameInput.Select();
            }
        }

        private void OnConfirmNewGame()
        {
            string name = playerNameInput != null ? playerNameInput.text : string.Empty;
            SaveManager.Instance.StartNewGame(slotIndex, name);
            // Scene will load — no refresh needed
        }

        private void OnCancelName()
        {
            if (nameInputPanel != null) nameInputPanel.SetActive(false);
        }

        private void OnContinueClicked()
        {
            SaveManager.Instance.LoadGame(slotIndex);
            // Scene will load
        }

        private void OnDeleteClicked()
        {
            if (confirmDeletePanel != null) confirmDeletePanel.SetActive(true);
        }

        private void OnConfirmDelete()
        {
            SaveManager.Instance.DeleteSlot(slotIndex);
            _onRefresh?.Invoke(); // Slot becomes empty again
        }

        private void OnCancelDelete()
        {
            if (confirmDeletePanel != null) confirmDeletePanel.SetActive(false);
        }
    }
}
