using UnityEngine;

namespace FlashFriends
{
    /// <summary>
    /// Makes a GameObject sway like it's caught in the wind.
    /// Combines a primary sinusoidal tilt with a secondary noise-driven wobble.
    /// Ideal for flags, banners, tree branches, festival decorations.
    ///
    /// SETUP : attach to a flag/banner root. The pivot should be at the attachment point
    ///         (top or bottom of the object) so the tip swings while the root stays fixed.
    /// </summary>
    public class WindSwayer : MonoBehaviour
    {
        [Header("Primary sway")]
        [Tooltip("Maximum tilt angle in degrees.")]
        [SerializeField] private float swayAngle = 12f;

        [Tooltip("Swings per second (0.3–0.8 looks natural).")]
        [SerializeField] private float swayFrequency = 0.4f;

        [Tooltip("Axis to sway on (local). Z = tilts forward/back, X = tilts side to side.")]
        [SerializeField] private Vector3 swayAxis = Vector3.forward;

        [Header("Secondary noise")]
        [Tooltip("Extra random wobble amplitude (degrees). Adds organic feel.")]
        [SerializeField] private float noiseAmplitude = 4f;

        [Tooltip("Speed of the random noise.")]
        [SerializeField] private float noiseSpeed = 1.2f;

        // ─── State ─────────────────────────────────────────────────────────

        private Quaternion _startLocalRot;
        private float      _noiseOffset;

        // ─── Lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            _startLocalRot = transform.localRotation;
            _noiseOffset   = Random.Range(0f, 100f); // unique per instance
        }

        private void Update()
        {
            if (Time.timeScale == 0f) return;

            float primary = Mathf.Sin(Time.time * swayFrequency * Mathf.PI * 2f) * swayAngle;
            float noise   = (Mathf.PerlinNoise(Time.time * noiseSpeed + _noiseOffset, 0f) * 2f - 1f)
                            * noiseAmplitude;

            transform.localRotation = _startLocalRot *
                Quaternion.AngleAxis(primary + noise, swayAxis.normalized);
        }
    }
}
