using UnityEngine;

namespace FlashFriends
{
    /// <summary>
    /// Invisible festival boundary wall.
    /// Shows a HUD notification when the player enters the warning zone.
    ///
    /// PREFAB SETUP (two-object approach — works reliably with CharacterController):
    ///
    ///   InvisibleWall (root)
    ///   ├─ BoxCollider           [Is Trigger = OFF]  ← physically stops the player
    ///   └─ WarningZone  (child)
    ///       ├─ BoxCollider       [Is Trigger = ON]   ← extends 1-2 m in front of the solid wall
    ///       ├─ Rigidbody         [Is Kinematic = ON, Use Gravity = OFF]
    ///       └─ InvisibleWall.cs  ← this script
    ///
    /// The WarningZone collider MUST be larger than the solid wall and protrude toward the
    /// player so the trigger fires BEFORE the player hits the wall.
    ///
    /// Player detection uses PlayerInputHandler component — no tag required.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class InvisibleWall : MonoBehaviour
    {
        [Tooltip("Message shown in the HUD when the player hits the boundary.")]
        [SerializeField] private string warningMessage =
            "The festival is closed ahead — please turn back!";

        [Tooltip("Minimum real-time seconds between two notifications.")]
        [SerializeField] private float notificationCooldown = 3f;

        // ─── State ─────────────────────────────────────────────────────────

        private float _lastNotifTime = -999f;

        // ─── Lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true;
            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity  = false;
        }

        // ─── Trigger ───────────────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;
            TryNotify();
        }

        // ─── Player detection (tag OR component — no configuration required) ─

        private static bool IsPlayer(Collider other)
        {
            // Primary: tag check (fast)
            if (other.CompareTag("Player")) return true;
            // Fallback: look for PlayerInputHandler anywhere in the parent chain
            return other.GetComponentInParent<PlayerInputHandler>() != null;
        }

        // ─── Notification ──────────────────────────────────────────────────

        private void TryNotify()
        {
            if (Time.realtimeSinceStartup - _lastNotifTime < notificationCooldown) return;
            _lastNotifTime = Time.realtimeSinceStartup;
            HUDManager.Instance?.ShowNotification(warningMessage);
        }
    }
}
