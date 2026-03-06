using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FlashFriends
{
    // Gère les quêtes photo du festival. Se connecte automatiquement à PhotoSystem et PhotoScorer.
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Header("Quests")]
        [SerializeField] private List<PhotoQuest> quests = new List<PhotoQuest>();

        [Header("Events")]
        public UnityEvent<PhotoQuest> onActiveQuestChanged;
        public UnityEvent<PhotoQuest> onQuestCompleted;

        // ─── Runtime state ─────────────────────────────────────────────────

        private readonly HashSet<int> _completedIndices = new HashSet<int>();
        private PhotoScorer _scorer;

        // Last PhotoScore received — used for MinScore quest check
        private PhotoScore _lastPhotoScore;

        // ─── Lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            _scorer = FindFirstObjectByType<PhotoScorer>();

            // Auto-connect to PhotoSystem so Inspector wiring is not required.
            // RemoveListener first prevents double-firing if also wired in Inspector.
            var photoSystem = FindFirstObjectByType<PhotoSystem>();
            if (photoSystem != null)
            {
                photoSystem.onPhotoTaken.RemoveListener(OnPhotoTaken);
                photoSystem.onPhotoTaken.AddListener(OnPhotoTaken);
                Debug.Log("[QuestManager] Auto-connected to PhotoSystem.onPhotoTaken.");
            }
            else
            {
                Debug.LogWarning("[QuestManager] PhotoSystem not found in scene — quests will not complete. " +
                                 "Make sure a PhotoSystem component exists on the Player.");
            }

            // Auto-connect to PhotoScorer for MinScore quest evaluation
            if (_scorer != null)
            {
                _scorer.onScoreCalculated.RemoveListener(OnScoreCalculated);
                _scorer.onScoreCalculated.AddListener(OnScoreCalculated);
                Debug.Log("[QuestManager] Auto-connected to PhotoScorer.onScoreCalculated.");
            }

            RefreshActiveQuest();
        }

        private void OnDestroy()
        {
            // Clean up auto-connected listeners to avoid stale references
            var photoSystem = FindFirstObjectByType<PhotoSystem>();
            photoSystem?.onPhotoTaken.RemoveListener(OnPhotoTaken);

            if (_scorer != null)
                _scorer.onScoreCalculated.RemoveListener(OnScoreCalculated);
        }

        // ─── Public API ────────────────────────────────────────────────────

        public void OnPhotoTaken(PhotoResult result)
        {
            if (result == null) return;

            for (int i = 0; i < quests.Count; i++)
            {
                if (_completedIndices.Contains(i)) continue;
                if (quests[i] == null) continue;
                if (EvaluateCondition(quests[i], result, _lastPhotoScore))
                    CompleteQuest(i);
            }
        }

        public void OnScoreCalculated(PhotoScore score)
        {
            _lastPhotoScore = score;
        }

        public PhotoQuest ActiveQuest
        {
            get
            {
                for (int i = 0; i < quests.Count; i++)
                    if (!_completedIndices.Contains(i) && quests[i] != null)
                        return quests[i];
                return null;
            }
        }

        public int TotalQuests    => quests.Count;
        public int CompletedCount => _completedIndices.Count;

        // ─── Condition evaluation ──────────────────────────────────────────

        private bool EvaluateCondition(PhotoQuest quest, PhotoResult result, PhotoScore score)
        {
            // For MinScore: if no score was passed via event, fall back to PhotoScorer's last entry.
            // This makes the check order-independent (works regardless of Inspector event order).
            PhotoScore effectiveScore = score;
            if (effectiveScore == null && _scorer != null && _scorer.History.Count > 0)
                effectiveScore = _scorer.History[^1];

            return quest.conditionType switch
            {
                QuestConditionType.AnySubject  => result.subjects.Count >= 1,

                // conditionValue = minimum number of subjects of requiredSubjectType (default 1)
                QuestConditionType.SubjectType => result.subjects.Count(
                    s => s.subject != null && s.subject.subjectType == quest.requiredSubjectType)
                    >= Mathf.Max(1, quest.conditionValue),

                QuestConditionType.MinSubjects => result.subjects.Count >= quest.conditionValue,

                QuestConditionType.MinScore    => effectiveScore != null
                                                  && effectiveScore.totalScore >= quest.conditionValue,

                QuestConditionType.SubjectTag  => result.subjects.Exists(
                    s => s.subject != null && s.subject.HasTag(quest.requiredTag)),

                // Photo must include at least 2 distinct PhotoSubjectType values
                QuestConditionType.MultipleTypes => HasMultipleTypes(result),

                _ => false
            };
        }

        private static bool HasMultipleTypes(PhotoResult result)
        {
            var types = new HashSet<PhotoSubjectType>();
            foreach (var entry in result.subjects)
                if (entry.subject != null)
                    types.Add(entry.subject.subjectType);
            return types.Count >= 2;
        }

        // ─── Quest completion ──────────────────────────────────────────────

        private void CompleteQuest(int index)
        {
            _completedIndices.Add(index);
            PhotoQuest quest = quests[index];

            Debug.Log($"[QuestManager] Quest completed: \"{quest.title}\" (+{quest.scoreReward} GV)");

            // Award score bonus
            if (_scorer != null && quest.scoreReward > 0)
                _scorer.AddBonusScore(quest.scoreReward);

            // Notify HUD
            onQuestCompleted?.Invoke(quest);
            HUDManager.Instance?.ShowQuestCompleteNotification(quest);

            // Update active quest display
            RefreshActiveQuest();
        }

        private void RefreshActiveQuest()
        {
            onActiveQuestChanged?.Invoke(ActiveQuest);
            HUDManager.Instance?.UpdateQuestDisplay(ActiveQuest, CompletedCount, TotalQuests);

            // When all quests are complete, direct the player to the festival exit
            if (ActiveQuest == null && TotalQuests > 0)
                HUDManager.Instance?.ShowNotification(
                    "All quests complete! Head to the festival exit!");
        }
    }
}
