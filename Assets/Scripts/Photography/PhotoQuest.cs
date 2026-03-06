using UnityEngine;

namespace FlashFriends
{
    /// <summary>
    /// Condition type for a photo quest.
    /// </summary>
    public enum QuestConditionType
    {
        /// <summary>Photo must contain at least 1 subject of any type.</summary>
        AnySubject,

        /// <summary>Photo must contain at least conditionValue subjects of requiredSubjectType.</summary>
        SubjectType,

        /// <summary>Photo must contain N or more subjects of any type (conditionValue = N).</summary>
        MinSubjects,

        /// <summary>Photo must score N+ Good Vibes in a single shot (conditionValue = N).</summary>
        MinScore,

        /// <summary>Photo must contain a subject carrying a specific tag (requiredTag). Case-insensitive.</summary>
        SubjectTag,

        /// <summary>Photo must contain subjects of at least 2 different PhotoSubjectType values.</summary>
        MultipleTypes,
    }

    /// <summary>
    /// ScriptableObject defining a photo quest.
    ///
    /// CREATE: Right-click in Project → Create > FlashFriends > Photo Quest
    ///
    /// CONDITION EXAMPLES:
    ///   AnySubject    → Take any photo with a subject
    ///   SubjectType   → requiredSubjectType=NPC, conditionValue=2 → 2 NPCs in one photo
    ///   MinSubjects   → conditionValue=3 → get 3+ subjects in one frame
    ///   MinScore      → conditionValue=250 → score 250+ in one photo
    ///   SubjectTag    → requiredTag="ferriswheel" → photograph the ferris wheel
    ///   MultipleTypes → photo must include subjects of 2+ different types (e.g. NPC + Object)
    /// </summary>
    [CreateAssetMenu(fileName = "Quest_New", menuName = "FlashFriends/Photo Quest")]
    public class PhotoQuest : ScriptableObject
    {
        [Header("Display")]
        [Tooltip("Short quest title shown in the HUD.")]
        public string title = "New Quest";

        [TextArea(2, 4)]
        [Tooltip("Quest description shown to the player.")]
        public string description = "Take a photo...";

        [Tooltip("Optional icon displayed next to the quest.")]
        public Sprite icon;

        [Header("Condition")]
        public QuestConditionType conditionType = QuestConditionType.AnySubject;

        [Tooltip("Required subject type (SubjectType condition only).")]
        public PhotoSubjectType requiredSubjectType = PhotoSubjectType.NPC;

        [Tooltip("Required tag on the subject (SubjectTag condition only). Case-insensitive.")]
        public string requiredTag = "";

        [Tooltip("Numeric threshold (MinSubjects: count, MinScore: Good Vibes).")]
        public int conditionValue = 1;

        [Header("Reward")]
        [Tooltip("Good Vibes bonus awarded when this quest is completed.")]
        public int scoreReward = 150;
    }
}
