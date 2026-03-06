using System.Collections;
using TMPro;
using UnityEngine;

namespace FlashFriends
{
    /// <summary>
    /// Always-visible HUD: Good Vibes score, festival countdown timer,
    /// photo count, and transient notifications.
    ///
    /// SETUP (GameScene) :
    /// 1. Canvas (Screen Space Overlay, Sort Order = 10).
    /// 2. Add HUDManager to a child object.
    /// 3. Assign all TextMeshProUGUI references in the Inspector.
    /// 4. Wire up events:
    ///    • PhotoScorer.onTotalScoreChanged  → HUDManager.UpdateScore
    ///    • PhotoScorer.onScoreCalculated    → HUDManager.ShowPhotoNotification
    ///    • GameTimer.onTimeChanged          → HUDManager.UpdateTimer
    ///    • CameraManager.onHUDNotification  → HUDManager.ShowNotification
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        public static HUDManager Instance { get; private set; }

        // ── Score ──────────────────────────────────────────────────────────

        [Header("Score")]
        [Tooltip("Displays the total Good Vibes score.")]
        public TextMeshProUGUI scoreText;

        [Tooltip("Score display format. {0} = numeric value.")]
        public string scoreFormat = "Good Vibes: {0}";

        // ── Timer ──────────────────────────────────────────────────────────

        [Header("Festival Timer")]
        [Tooltip("Displays remaining festival time (HH:MM).")]
        public TextMeshProUGUI timerText;

        [Tooltip("Normal timer color.")]
        public Color timerColorNormal  = Color.white;

        [Tooltip("Warning color when less than 5 minutes remain.")]
        public Color timerColorWarning = new Color(1f, 0.6f, 0f); // orange

        [Tooltip("Critical color when less than 2 minutes remain.")]
        public Color timerColorCritical = Color.red;

        // ── Photo count ────────────────────────────────────────────────────

        [Header("Photo Count")]
        [Tooltip("Displays current / max photo count (e.g. '12 / 25').")]
        public TextMeshProUGUI photoCountText;

        [Tooltip("Format. {0} = current count, {1} = max.")]
        public string photoCountFormat = "{0} / {1}";

        // ── Quest display ──────────────────────────────────────────────────

        [Header("Active Quest")]
        [Tooltip("Panel showing the current active quest (always visible in HUD).")]
        public GameObject questPanel;

        [Tooltip("Quest title text.")]
        public TextMeshProUGUI questTitleText;

        [Tooltip("Quest description text.")]
        public TextMeshProUGUI questDescriptionText;

        [Tooltip("Quest progress text, e.g. '2 / 5 completed'.")]
        public TextMeshProUGUI questProgressText;

        // ── Notifications ──────────────────────────────────────────────────

        [Header("Notifications")]
        [Tooltip("Parent panel of the notification (activated / deactivated).")]
        public GameObject notificationPanel;

        [Tooltip("TextMeshProUGUI inside the notification panel.")]
        public TextMeshProUGUI notificationText;

        [Tooltip("Optional timestamp label.")]
        public TextMeshProUGUI notificationTimeText;

        [Tooltip("How long a notification stays visible (seconds).")]
        [Range(0.5f, 5f)]
        public float notificationDuration = 2.5f;

        // ─── State ──────────────────────────────────────────────────────────

        private Coroutine _hideNotifCoroutine;

        // ─── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            if (notificationPanel != null) notificationPanel.SetActive(false);
            UpdateScore(0);
            UpdatePhotoCount(0, PhotoSystem.MaxPhotos);
        }

        // ─── Public methods (wire via UnityEvents or code) ──────────────────

        /// <summary>Updates the Good Vibes score display. Wire to PhotoScorer.onTotalScoreChanged.</summary>
        public void UpdateScore(int total)
        {
            if (scoreText != null)
                scoreText.text = string.Format(scoreFormat, total);
        }

        /// <summary>
        /// Updates the countdown timer display. Wire to GameTimer.onTimeChanged.
        /// Displayed as "Time remaining: MM:SS".
        /// </summary>
        public void UpdateTimer(float remainingSeconds)
        {
            if (timerText == null) return;

            remainingSeconds = Mathf.Max(0f, remainingSeconds);
            int m = (int)(remainingSeconds / 60);
            int s = (int)(remainingSeconds % 60);

            timerText.text = $"Time remaining: {m:00}:{s:00}";

            // Color feedback
            timerText.color = remainingSeconds switch
            {
                <= 0f   => timerColorCritical,
                < 120f  => timerColorCritical,
                < 300f  => timerColorWarning,
                _       => timerColorNormal
            };
        }

        /// <summary>Updates the photo count display. Call after every photo taken or deleted.</summary>
        public void UpdatePhotoCount(int current, int max)
        {
            if (photoCountText != null)
                photoCountText.text = string.Format(photoCountFormat, current, max);
        }

        /// <summary>Shows a generic text notification. Wire to CameraManager.onHUDNotification.</summary>
        public void ShowNotification(string message)
        {
            if (notificationPanel == null || notificationText == null) return;

            notificationText.text = message;
            if (notificationTimeText != null)
                notificationTimeText.text = System.DateTime.Now.ToString("HH:mm");
            notificationPanel.SetActive(true);

            if (_hideNotifCoroutine != null) StopCoroutine(_hideNotifCoroutine);
            _hideNotifCoroutine = StartCoroutine(HideNotifAfterDelay());
        }

        /// <summary>
        /// Updates the active quest display. Called by QuestManager when the active quest changes.
        /// Pass null quest to show "All quests completed!".
        /// </summary>
        public void UpdateQuestDisplay(PhotoQuest quest, int completed, int total)
        {
            if (questProgressText != null)
                questProgressText.text = $"{completed} / {total}";

            if (quest == null)
            {
                // All done
                if (questTitleText      != null) questTitleText.text      = "All quests complete!";
                if (questDescriptionText != null) questDescriptionText.text = "Amazing work, photographer!";
                return;
            }

            if (questTitleText       != null) questTitleText.text       = quest.title;
            if (questDescriptionText != null) questDescriptionText.text = quest.description;
        }

        /// <summary>Shows a "Quest Complete" notification in the notification area.</summary>
        public void ShowQuestCompleteNotification(PhotoQuest quest)
        {
            if (quest == null) return;
            ShowNotification($"Quest complete: {quest.title}  +{quest.scoreReward} Good Vibes!");
        }

        /// <summary>Displays the score detail of a photo (+X Good Vibes!). Wire to PhotoScorer.onScoreCalculated.</summary>
        public void ShowPhotoNotification(PhotoScore score)
        {
            if (score == null) return;

            if (score.totalScore <= 0)
            {
                ShowNotification("No subject in frame...");
                return;
            }

            string msg = $"+{score.totalScore} Good Vibes!";

            int    bestVal   = 0;
            string bestLabel = "";
            foreach (var b in score.bonuses)
            {
                if (!b.label.EndsWith("base") && b.value > bestVal)
                {
                    bestVal   = b.value;
                    bestLabel = b.label;
                }
            }
            if (bestVal > 0)
                msg += $"  ✦ {bestLabel} +{bestVal}";

            ShowNotification(msg);
        }

        // ─── Coroutine ──────────────────────────────────────────────────────

        private IEnumerator HideNotifAfterDelay()
        {
            // WaitForSecondsRealtime so notifications hide correctly during pause (timeScale = 0)
            yield return new WaitForSecondsRealtime(notificationDuration);
            if (notificationPanel != null) notificationPanel.SetActive(false);
        }
    }
}
