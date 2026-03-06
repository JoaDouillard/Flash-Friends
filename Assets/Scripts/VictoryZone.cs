using UnityEngine;

namespace FlashFriends
{
    /// <summary>
    /// Festival exit zone — triggers the victory screen when the player has completed
    /// all quests and walks through this collider.
    /// If quests are not yet finished, a reminder notification is shown instead.
    ///
    /// SETUP :
    ///   ExitZone (any position)
    ///   ├─ BoxCollider       [Is Trigger = ON]
    ///   ├─ Rigidbody         [Is Kinematic = ON, Use Gravity = OFF]
    ///   └─ VictoryZone.cs    ← this script
    ///
    /// Assign the VictoryPanel reference in the Inspector (or it is auto-detected).
    /// Tag the Player GameObject as "Player".
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class VictoryZone : MonoBehaviour
    {
        [Tooltip("VictoryPanel script — auto-detected if left empty.")]
        [SerializeField] private VictoryPanel victoryPanel;

        [Tooltip("Message shown when the player tries to leave before finishing all quests.")]
        [SerializeField] private string notDoneMessage =
            "You still have quests to complete before leaving the festival!";

        // ─── Lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            // Ensure correct collider/rigidbody setup
            GetComponent<Collider>().isTrigger = true;
            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity  = false;

            if (victoryPanel == null)
                victoryPanel = FindFirstObjectByType<VictoryPanel>();
        }

        // ─── Trigger ───────────────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            var qm = QuestManager.Instance;
            bool allDone = qm == null || qm.CompletedCount >= qm.TotalQuests;

            if (!allDone)
            {
                HUDManager.Instance?.ShowNotification(notDoneMessage);
                return;
            }

            if (victoryPanel != null)
                victoryPanel.Show();
            else
                Debug.LogWarning("[VictoryZone] VictoryPanel not found — cannot show victory screen.");
        }
    }
}
