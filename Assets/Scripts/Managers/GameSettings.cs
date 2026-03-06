using UnityEngine;

namespace FlashFriends
{
    // Paramètres persistants (sensibilité, volumes) — singleton DontDestroyOnLoad, sauvegarde via PlayerPrefs.
    public class GameSettings : MonoBehaviour
    {
        public static GameSettings Instance { get; private set; }

        [Header("Valeurs par défaut")]
        [Range(1f, 10f)]
        [SerializeField] private float defaultMouseSensitivity = 5f;

        [Range(0f, 1f)]
        [SerializeField] private float defaultSFXVolume = 0.8f;

        [Range(0f, 1f)]
        [SerializeField] private float defaultMusicVolume = 0.6f;

        // ─── Clés PlayerPrefs ──────────────────────────────────────────────

        private const string KEY_MOUSE = "MouseSensitivity";
        private const string KEY_SFX   = "SFXVolume";
        private const string KEY_MUSIC = "MusicVolume";

        // ─── État courant ──────────────────────────────────────────────────

        private float _mouseSensitivity;
        private float _sfxVolume;
        private float _musicVolume;

        // ─── Cycle de vie ──────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadSettings();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Re-appliquer une fois AudioManager disponible
            ApplySettings();
        }

        // ─── Chargement / Sauvegarde ───────────────────────────────────────

        private void LoadSettings()
        {
            _mouseSensitivity = PlayerPrefs.GetFloat(KEY_MOUSE, defaultMouseSensitivity);
            _sfxVolume        = PlayerPrefs.GetFloat(KEY_SFX,   defaultSFXVolume);
            _musicVolume      = PlayerPrefs.GetFloat(KEY_MUSIC, defaultMusicVolume);
            CameraSettings.MouseSensitivityMultiplier = _mouseSensitivity / 5f; // synchroniser dès le démarrage
            ApplySettings();
        }

        public void SaveSettings()
        {
            PlayerPrefs.SetFloat(KEY_MOUSE, _mouseSensitivity);
            PlayerPrefs.SetFloat(KEY_SFX,   _sfxVolume);
            PlayerPrefs.SetFloat(KEY_MUSIC, _musicVolume);
            PlayerPrefs.Save();
        }

        private void ApplySettings()
        {
            if (AudioManager.Instance == null) return;
            AudioManager.Instance.SetSFXVolume(_sfxVolume);
            AudioManager.Instance.SetMusicVolume(_musicVolume);
        }

        // ─── Sensibilité souris ────────────────────────────────────────────

        public float GetMouseSensitivity()           => _mouseSensitivity;
        public float GetMouseSensitivityMultiplier() => _mouseSensitivity / 5f;

        public void SetMouseSensitivity(float value)
        {
            _mouseSensitivity = Mathf.Clamp(value, 1f, 10f);
            CameraSettings.MouseSensitivityMultiplier = _mouseSensitivity / 5f; // mise à jour du pont
            SaveSettings(); // auto-save immédiat
        }

        // ─── Volumes ───────────────────────────────────────────────────────

        public float GetSFXVolume()   => _sfxVolume;
        public float GetMusicVolume() => _musicVolume;

        public void SetSFXVolume(float value)
        {
            _sfxVolume = Mathf.Clamp01(value);
            ApplySettings();
            SaveSettings(); // auto-save immédiat
        }

        public void SetMusicVolume(float value)
        {
            _musicVolume = Mathf.Clamp01(value);
            ApplySettings();
            SaveSettings(); // auto-save immédiat
        }

        // ─── Reset ─────────────────────────────────────────────────────────

        public void ResetToDefaults()
        {
            _mouseSensitivity = defaultMouseSensitivity;
            _sfxVolume        = defaultSFXVolume;
            _musicVolume      = defaultMusicVolume;
            ApplySettings();
            SaveSettings();
        }
    }
}
