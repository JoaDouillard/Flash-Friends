using System.Collections;
using UnityEngine;

namespace FlashFriends
{
    /// <summary>
    /// Makes a Light flicker randomly.
    /// Suitable for faulty streetlights, candles, neon signs, fire effects.
    ///
    /// Two modes:
    ///   • Smooth  — continuously varies intensity with Perlin noise (candle, fire)
    ///   • Stutter — occasional random on/off bursts  (faulty neon, broken bulb)
    ///
    /// SETUP : attach to a GameObject that has a Light component.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class LightFlicker : MonoBehaviour
    {
        public enum FlickerMode { Smooth, Stutter }

        [Header("Mode")]
        [SerializeField] private FlickerMode mode = FlickerMode.Smooth;

        [Header("Smooth flicker (candle / fire)")]
        [Tooltip("Centre intensity around which the light fluctuates.")]
        [SerializeField] private float baseIntensity = 1f;

        [Tooltip("Max deviation from base intensity (±).")]
        [SerializeField] private float intensityVariance = 0.4f;

        [Tooltip("Speed of the Perlin noise. Higher = faster flicker.")]
        [SerializeField] private float noiseSpeed = 3f;

        [Header("Stutter flicker (broken neon / bulb)")]
        [Tooltip("Minimum seconds between stutter events.")]
        [SerializeField] private float stutterIntervalMin = 2f;

        [Tooltip("Maximum seconds between stutter events.")]
        [SerializeField] private float stutterIntervalMax = 8f;

        [Tooltip("Number of rapid on/off flashes per stutter event.")]
        [SerializeField] private int flashesPerStutter = 4;

        [Tooltip("Duration of each individual flash (seconds).")]
        [SerializeField] private float flashDuration = 0.05f;

        // ─── State ─────────────────────────────────────────────────────────

        private Light _light;
        private float _noiseOffset;

        // ─── Lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            _light       = GetComponent<Light>();
            _noiseOffset = Random.Range(0f, 100f);
        }

        private void Start()
        {
            _light.intensity = baseIntensity;
            if (mode == FlickerMode.Stutter)
                StartCoroutine(StutterLoop());
        }

        private void Update()
        {
            if (mode != FlickerMode.Smooth) return;
            if (Time.timeScale == 0f) return;

            float noise = Mathf.PerlinNoise(Time.time * noiseSpeed + _noiseOffset, 0f);
            // noise is 0..1, remap to base ± variance
            _light.intensity = baseIntensity + (noise * 2f - 1f) * intensityVariance;
        }

        // ─── Stutter ───────────────────────────────────────────────────────

        private IEnumerator StutterLoop()
        {
            while (true)
            {
                float wait = Random.Range(stutterIntervalMin, stutterIntervalMax);
                yield return new WaitForSeconds(wait);

                // Rapid flashes
                for (int i = 0; i < flashesPerStutter; i++)
                {
                    _light.intensity = 0f;
                    yield return new WaitForSeconds(flashDuration);
                    _light.intensity = baseIntensity;
                    yield return new WaitForSeconds(flashDuration * Random.Range(0.5f, 2f));
                }

                // Optionally go dark for a longer moment
                if (Random.value < 0.3f)
                {
                    _light.intensity = 0f;
                    yield return new WaitForSeconds(Random.Range(0.2f, 1f));
                    _light.intensity = baseIntensity;
                }
            }
        }
    }
}
