using UnityEngine;

namespace FlashFriends
{
    /// <summary>
    /// Day/night cycle for the festival — driven by GameTimer.
    /// 24 real minutes = 24 in-game hours → 1 real second ≈ 1 in-game minute.
    ///
    /// Sun rotation:
    ///   6 AM → X = 0°  (sunrise, light on horizon)
    ///  12 PM → X = 90° (noon, light straight down)
    ///  18 PM → X = 180° (sunset, light on horizon)
    ///   0 AM → X = 270° (midnight, sun underground)
    ///
    /// Skybox: switches automatically between 4 time-of-day materials.
    /// Sun intensity: fades to 0 at night (18 h – 6 h).
    ///
    /// SETUP :
    /// 1. Add this script to a dedicated always-active GameObject.
    /// 2. Assign sunLight (Directional Light) in the Inspector.
    /// 3. Assign skybox materials (morning, lateMorning, afternoon, evening, night).
    ///    All slots are optional — null slots keep the previous skybox.
    /// </summary>
    public class DayNightCycle : MonoBehaviour
    {
        public static DayNightCycle Instance { get; private set; }

        [Header("Sun")]
        [Tooltip("The Directional Light that acts as the sun. Auto-detected if left empty.")]
        [SerializeField] private Light sunLight;

        [Tooltip("Y-axis rotation of the sun (azimuth). 170° = roughly south-west.")]
        [SerializeField] private float sunYRotation = 170f;

        [Tooltip("Maximum sun intensity at noon.")]
        [SerializeField] private float maxSunIntensity = 1.2f;

        [Header("Festival Start Time")]
        [Tooltip("In-game hour when the festival begins (24h format). 10 = 10:00 AM.")]
        [SerializeField] private float festivalStartHour = 10f;

        [Header("Skyboxes")]
        [Tooltip("Morning skybox — active from 5 h to 9 h.")]
        [SerializeField] private Material skyboxMorning;

        [Tooltip("Late morning skybox — active from 9 h to 13 h.")]
        [SerializeField] private Material skyboxLateMorning;

        [Tooltip("Afternoon skybox — active from 13 h to 17 h.")]
        [SerializeField] private Material skyboxAfternoon;

        [Tooltip("Evening / sunset skybox — active from 17 h to 21 h.")]
        [SerializeField] private Material skyboxEvening;

        [Tooltip("Night skybox — active from 21 h to 5 h. If null, the evening skybox is kept.")]
        [SerializeField] private Material skyboxNight;

        [Header("Debug")]
        [Tooltip("Print current in-game time to Console every 60 frames.")]
        [SerializeField] private bool showDebug;

        // ─── State ─────────────────────────────────────────────────────────

        private float    _currentHour;
        private Material _currentSkybox;
        private float    _festivalDuration; // seconds (from GameTimer)

        // ─── Lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            // Auto-detect Directional Light
            if (sunLight == null)
            {
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (l.type == LightType.Directional) { sunLight = l; break; }
                }
            }

            if (sunLight == null)
                Debug.LogWarning("[DayNightCycle] No Directional Light assigned or found — sun will not rotate.");

            // Cache festival duration from GameTimer
            if (GameTimer.Instance != null)
                _festivalDuration = GameTimer.Instance.RemainingSeconds; // full duration at start
            else
                _festivalDuration = 24f * 60f; // fallback: 1440 s

            _currentHour = festivalStartHour;
            ApplySun();
            ApplySkybox();
        }

        private void Update()
        {
            // Pause when game is paused (timeScale = 0)
            if (Time.timeScale == 0f) return;

            // Derive in-game hour from elapsed real time (via GameTimer)
            if (GameTimer.Instance != null)
            {
                float elapsed    = _festivalDuration - GameTimer.Instance.RemainingSeconds;
                float elapsedHrs = elapsed / 60f; // 1 real second = 1 in-game minute → /60 = hours
                _currentHour     = (festivalStartHour + elapsedHrs) % 24f;
            }

            ApplySun();
            ApplySkybox();

            if (showDebug && Time.frameCount % 60 == 0)
                Debug.Log($"[DayNightCycle] In-game time: {GetFormattedTime()}");
        }

        // ─── Sun ───────────────────────────────────────────────────────────

        private void ApplySun()
        {
            if (sunLight == null) return;

            // Map hour 0–24 to X angle 0–360, with 6 AM = 0° (sunrise on horizon)
            float sunAngle = ((_currentHour - 6f) / 24f * 360f + 360f) % 360f;
            sunLight.transform.rotation = Quaternion.Euler(sunAngle, sunYRotation, 0f);

            // Intensity: 0 at sunrise/sunset (0°/180°), max at noon (90°), 0 at night
            float sinVal = Mathf.Sin(sunAngle * Mathf.Deg2Rad);
            sunLight.intensity = Mathf.Max(0f, sinVal) * maxSunIntensity;
        }

        // ─── Skybox ────────────────────────────────────────────────────────

        private void ApplySkybox()
        {
            Material target = SelectSkybox(_currentHour);
            if (target == null || target == _currentSkybox) return;

            RenderSettings.skybox = target;
            _currentSkybox        = target;
            DynamicGI.UpdateEnvironment();
        }

        private Material SelectSkybox(float hour)
        {
            if (hour >= 5f  && hour < 9f)  return skyboxMorning     ?? _currentSkybox;
            if (hour >= 9f  && hour < 13f) return skyboxLateMorning  ?? _currentSkybox;
            if (hour >= 13f && hour < 17f) return skyboxAfternoon    ?? _currentSkybox;
            if (hour >= 17f && hour < 21f) return skyboxEvening      ?? _currentSkybox;
            // Night: 21h–5h
            return skyboxNight ?? skyboxEvening ?? _currentSkybox;
        }

        // ─── Public API ────────────────────────────────────────────────────

        /// <summary>Returns the current in-game hour (0–24).</summary>
        public float GetCurrentHour() => _currentHour;

        /// <summary>Returns in-game time formatted as HH:MM.</summary>
        public string GetFormattedTime()
        {
            int h = Mathf.FloorToInt(_currentHour);
            int m = Mathf.FloorToInt((_currentHour - h) * 60f);
            return $"{h:D2}:{m:D2}";
        }

        public bool IsNight() => _currentHour < 6f || _currentHour >= 21f;
        public bool IsDay()   => !IsNight();
    }
}
