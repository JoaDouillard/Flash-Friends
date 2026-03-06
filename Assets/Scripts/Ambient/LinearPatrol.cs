using UnityEngine;

namespace FlashFriends
{
    /// <summary>
    /// Moves a GameObject back and forth between two points at a constant speed.
    ///
    /// Two modes:
    ///   • PingPong  — reaches B, reverses toward A, repeats.       (tram, boat)
    ///   • Teleport  — reaches B, teleports back to A silently.     (parade float loop)
    ///
    /// Initial delay: each object waits at its start position for
    ///   waitAtEndpoint + Random(0, randomExtraInitialDelay)  seconds
    /// before moving for the first time.
    /// Set a non-zero randomExtraInitialDelay to stagger multiple vehicles
    /// that share the same waitAtEndpoint value.
    ///
    /// SETUP :
    /// 1. Create two empty GameObjects as waypoints (e.g. "PatrolA", "PatrolB").
    /// 2. Assign them to pointA and pointB in the Inspector.
    /// 3. The patrolling object is placed at pointA on Play, then waits before moving.
    /// </summary>
    public class LinearPatrol : MonoBehaviour
    {
        public enum PatrolMode { PingPong, Teleport }

        [Header("Waypoints")]
        [Tooltip("Start point — object is placed here on Play.")]
        [SerializeField] private Transform pointA;

        [Tooltip("End point.")]
        [SerializeField] private Transform pointB;

        [Header("Movement")]
        [Tooltip("Movement speed in units per second.")]
        [SerializeField] private float speed = 3f;

        [Tooltip("PingPong: reverses at each end.  Teleport: snaps back to A after reaching B.")]
        [SerializeField] private PatrolMode mode = PatrolMode.PingPong;

        [Header("Wait times")]
        [Tooltip("Seconds to wait at each endpoint (including the very first departure).")]
        [SerializeField] private float waitAtEndpoint = 2f;

        [Tooltip("Extra random seconds added to the initial wait only. " +
                 "Use this to stagger multiple vehicles that share the same waitAtEndpoint. " +
                 "Each instance picks a random value in [0, this].")]
        [SerializeField] private float randomExtraInitialDelay = 5f;

        [Header("Rotation")]
        [Tooltip("If true, the object smoothly faces the direction of movement.")]
        [SerializeField] private bool faceDirection = true;

        [Tooltip("Rotation speed in degrees/sec. 0 = instant snap.")]
        [SerializeField] private float rotationSpeed = 180f;

        // ─── State ─────────────────────────────────────────────────────────

        private bool  _movingToB = true;
        private float _waitTimer;

        // ─── Lifecycle ─────────────────────────────────────────────────────

        private void Start()
        {
            if (pointA == null || pointB == null)
            {
                Debug.LogWarning($"[LinearPatrol] '{name}' — pointA or pointB not assigned. Disabling.");
                enabled = false;
                return;
            }

            transform.position = pointA.position;

            // Initial wait = standard endpoint wait + random stagger
            _waitTimer = waitAtEndpoint + Random.Range(0f, randomExtraInitialDelay);
        }

        private void Update()
        {
            // ── Wait phase ──────────────────────────────────────────────────
            if (_waitTimer > 0f)
            {
                _waitTimer -= Time.deltaTime;
                return;
            }

            Transform target = _movingToB ? pointB : pointA;

            // ── Move ────────────────────────────────────────────────────────
            transform.position = Vector3.MoveTowards(
                transform.position, target.position, speed * Time.deltaTime);

            // ── Face direction ──────────────────────────────────────────────
            if (faceDirection)
            {
                Vector3 dir = target.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir);
                    transform.rotation = (rotationSpeed <= 0f)
                        ? targetRot
                        : Quaternion.RotateTowards(transform.rotation, targetRot,
                              rotationSpeed * Time.deltaTime);
                }
            }

            // ── Reached target ──────────────────────────────────────────────
            if (Vector3.Distance(transform.position, target.position) < 0.02f)
            {
                _waitTimer = waitAtEndpoint;

                switch (mode)
                {
                    case PatrolMode.PingPong:
                        _movingToB = !_movingToB;
                        break;

                    case PatrolMode.Teleport:
                        transform.position = pointA.position;
                        _movingToB = true;
                        break;
                }
            }
        }

        // ─── Editor gizmos ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (pointA == null || pointB == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pointA.position, pointB.position);
            Gizmos.DrawWireSphere(pointA.position, 0.3f);
            Gizmos.DrawWireSphere(pointB.position, 0.3f);
        }
#endif
    }
}
