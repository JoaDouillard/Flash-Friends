using UnityEngine;
using UnityEngine.UI;

namespace FlashFriends
{
    public class SaveMenuUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject saveMenuPanel;

        [Header("Buttons")]
        [SerializeField] private Button backButton;

        // ─── Slots (auto-detected from children) ──────────────────────────

        private SaveSlotUI[] _slots;

        // ─── Callback ─────────────────────────────────────────────────────

        private System.Action _onBackCallback;

        // ─── Lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            // Auto-detect SaveSlotUI instances placed as children (from prefabs)
            _slots = GetComponentsInChildren<SaveSlotUI>(includeInactive: true);

            if (_slots.Length == 0)
                Debug.LogWarning("[SaveMenuUI] No SaveSlotUI found in children. " +
                                 "Place 3 SaveSlotUI prefab instances inside the slot container.");
        }

        // ─── Public API ───────────────────────────────────────────────────

        public void OpenSaveMenu(System.Action onBack = null)
        {
            _onBackCallback = onBack;
            if (saveMenuPanel != null) saveMenuPanel.SetActive(true);
            RefreshAllSlots();
        }

        public void CloseSaveMenu()
        {
            if (saveMenuPanel != null) saveMenuPanel.SetActive(false);
            _onBackCallback?.Invoke();
        }

        // ─── Refresh ──────────────────────────────────────────────────────

        private void RefreshAllSlots()
        {
            if (SaveManager.Instance == null)
            {
                Debug.LogError("[SaveMenuUI] SaveManager not found!");
                return;
            }

            SaveData[] headers = SaveManager.Instance.LoadAllSlotHeaders();

            for (int i = 0; i < _slots.Length && i < SaveManager.SlotCount; i++)
            {
                if (_slots[i] == null) continue;
                _slots[i].Refresh(headers[i], RefreshAllSlots);
            }
        }

        // ─── Callbacks ────────────────────────────────────────────────────

        private void OnBackClicked() => CloseSaveMenu();
    }
}
