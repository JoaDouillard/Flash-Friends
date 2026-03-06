using UnityEngine;

namespace FlashFriends
{
    /// <summary>
    /// Self-contained street lamp controller.
    /// • Turns ALL child Lights ON at night and OFF during the day (driven by DayNightCycle).
    /// • Applies a synchronized Perlin-noise flicker to all child lights.
    ///
    /// Supports any number of child Lights (single spot, double point, etc.).
    /// Does NOT override the light type or settings already configured in the prefab.
    /// Only auto-creates a Point Light when no child Light exists at all.
    ///
    /// SETUP (automatic via Editor tool) :
    ///   Use FlashFriends > Setup Street Lamps from the Unity menu bar.
    ///
    /// MANUAL SETUP :
    ///   1. Add this script to the lamp root GameObject.
    ///   2. Configure the child Light(s) directly in the Inspector (type, range, color, etc.).
    ///      OR leave no child Light — one will be auto-created at runtime.
    /// </summary>
    public class StreetLamp : MonoBehaviour
    {
        [Header("Auto-create fallback (only used if no child Light exists)")]
        [Tooltip("Local Y offset where the auto-created light sits.")]
        [SerializeField] public float lightHeight = 5f;

        [Tooltip("Range of the auto-created Point Light.")]
        [SerializeField] public float lightRange = 14f;

        [Tooltip("Intensity of the auto-created Point Light.")]
        [SerializeField] public float lightIntensity = 1.5f;

        [Tooltip("Color of the auto-created Point Light.")]
        [SerializeField] public Color lightColor = new Color(1f, 0.88f, 0.6f);

        [Header("Day / Night")]
        [Tooltip("ON at night, OFF during day. Uncheck for always-on lights.")]
        [SerializeField] public bool onlyAtNight = true;

        [Header("Flicker")]
        [Tooltip("Intensity variation amplitude (±). 0 = perfectly steady.")]
        [SerializeField] public float flickerVariance = 0.12f;

        [Tooltip("Speed of the Perlin noise. Higher = faster flicker.")]
        [SerializeField] public float flickerSpeed = 1.8f;

        // kept for compatibility / inspector reference — not used in Update
        [HideInInspector] public Light lampLight;

        // ─── State ─────────────────────────────────────────────────────────

        private Light[] _allLights;
        private float[] _baseIntensities;
        private float   _noiseOffset;

        // ─── Lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            _allLights = GetComponentsInChildren<Light>();

            // No child light found → auto-create one Point Light
            if (_allLights.Length == 0)
            {
                var go = new GameObject("LampLight");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.up * lightHeight;

                var l = go.AddComponent<Light>();
                l.type = LightType.Point;

                _allLights = new[] { l };
                lampLight  = l;
            }
            else if (lampLight == null)
            {
                lampLight = _allLights[0];
            }

            // Apply StreetLamp settings to every child Light.
            // type and rotation are left untouched (Spot stays Spot, Point stays Point).
            foreach (var l in _allLights)
            {
                l.range     = lightRange;
                l.intensity = lightIntensity;
                l.color     = lightColor;
            }

            // lightIntensity is now the flicker base for every light
            _baseIntensities = new float[_allLights.Length];
            for (int i = 0; i < _allLights.Length; i++)
                _baseIntensities[i] = lightIntensity;

            _noiseOffset = Random.Range(0f, 100f);
        }

        private void Update()
        {
            if (_allLights == null || _allLights.Length == 0) return;

            // ── Day / Night gating ──────────────────────────────────────────
            bool shouldBeOn = true;
            if (onlyAtNight && DayNightCycle.Instance != null)
                shouldBeOn = DayNightCycle.Instance.IsNight();

            // ── Flicker (compute once, apply to all lights together) ────────
            float flickerOffset = 0f;
            if (shouldBeOn && flickerVariance > 0f)
            {
                float noise = Mathf.PerlinNoise(Time.time * flickerSpeed + _noiseOffset, 0f);
                flickerOffset = (noise * 2f - 1f) * flickerVariance;
            }

            // ── Apply to every child Light ──────────────────────────────────
            for (int i = 0; i < _allLights.Length; i++)
            {
                if (_allLights[i] == null) continue;
                _allLights[i].enabled = shouldBeOn;
                if (shouldBeOn && flickerVariance > 0f)
                    _allLights[i].intensity = _baseIntensities[i] + flickerOffset;
            }
        }
    }
}
