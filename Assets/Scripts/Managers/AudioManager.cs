using UnityEngine;
using UnityEngine.Audio;

namespace FlashFriends
{
    // Singleton audio — DontDestroyOnLoad depuis la scène MainMenu.
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Mixer (obligatoire)")]
        [SerializeField] private AudioMixer audioMixer;

        [Header("Mixer Groups (obligatoire)")]
        [SerializeField] private AudioMixerGroup sfxMixerGroup;
        [SerializeField] private AudioMixerGroup musicMixerGroup;

        [Header("Noms des paramètres exposés")]
        [SerializeField] private string masterVolumeParam = "MasterVolume";
        [SerializeField] private string sfxVolumeParam    = "SFXVolume";
        [SerializeField] private string musicVolumeParam  = "MusicVolume";

        [Header("Musiques")]
        [SerializeField] private AudioClip mainMenuMusic;
        [SerializeField] private AudioClip gameMusic;
        [SerializeField] private AudioClip gameOverMusic;

        [Header("Game Over SFX")]
        [SerializeField] private AudioClip gameOverSFX;

        [Header("UI Sounds")]
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip panelOpenSound;
        [SerializeField] private AudioClip panelCloseSound;

        // ─── Sources audio ────────────────────────────────────────────────

        private AudioSource musicSource;
        private AudioSource sfxSource;

        // ─── Cycle de vie ─────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                CreateAudioSources();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void CreateAudioSources()
        {
            AudioBridge.SFXMixerGroup = sfxMixerGroup;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop         = true;
            musicSource.playOnAwake  = false;
            if (musicMixerGroup != null)
                musicSource.outputAudioMixerGroup = musicMixerGroup;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop        = false;
            sfxSource.playOnAwake = false;
            if (sfxMixerGroup != null)
                sfxSource.outputAudioMixerGroup = sfxMixerGroup;

            if (audioMixer == null)
                Debug.LogError("[AudioManager] AudioMixer non assigné ! Les volumes ne fonctionneront pas.");
        }

        // ─── Volume (via AudioMixer) ──────────────────────────────────────

        public void SetMasterVolume(float volume) => SetMixerVolume(masterVolumeParam, volume);
        public void SetSFXVolume(float volume)    => SetMixerVolume(sfxVolumeParam,    volume);
        public void SetMusicVolume(float volume)  => SetMixerVolume(musicVolumeParam,  volume);

        public AudioMixerGroup GetSFXMixerGroup() => sfxMixerGroup;

        public float GetMasterVolume() => GetMixerVolume(masterVolumeParam);
        public float GetSFXVolume()    => GetMixerVolume(sfxVolumeParam);
        public float GetMusicVolume()  => GetMixerVolume(musicVolumeParam);

        private void SetMixerVolume(string param, float volume)
        {
            if (audioMixer == null) return;
            float db = volume > 0.001f ? Mathf.Log10(volume) * 20f : -80f;
            if (!audioMixer.SetFloat(param, db))
                Debug.LogError($"[AudioManager] Paramètre '{param}' non exposé dans le mixer !");
        }

        private float GetMixerVolume(string param)
        {
            if (audioMixer == null) return 1f;
            audioMixer.GetFloat(param, out float db);
            return Mathf.Pow(10f, db / 20f);
        }

        // ─── Musique ──────────────────────────────────────────────────────

        public void PlayMainMenuMusic() => PlayMusic(mainMenuMusic);
        public void PlayGameMusic()     => PlayMusic(gameMusic);
        public void PlayGameOverMusic() => PlayMusic(gameOverMusic);

        public void PlayGameOverSequence()
        {
            StopMusic();
            if (gameOverSFX != null)
            {
                PlaySFX(gameOverSFX);
                Invoke(nameof(PlayGameOverMusic), gameOverSFX.length + 0.5f);
            }
            else
            {
                PlayGameOverMusic();
            }
        }

        private void PlayMusic(AudioClip clip)
        {
            if (clip == null || musicSource == null) return;
            if (musicSource.clip == clip && musicSource.isPlaying) return;
            musicSource.clip = clip;
            musicSource.Play();
        }

        public void StopMusic()   { if (musicSource != null) musicSource.Stop(); }
        public void PauseMusic()  { if (musicSource != null) musicSource.Pause(); }
        public void ResumeMusic() { if (musicSource != null) musicSource.UnPause(); }

        // ─── UI Sounds ────────────────────────────────────────────────────

        public void PlayButtonClick() => PlaySFX(buttonClickSound);
        public void PlayPanelOpen()   => PlaySFX(panelOpenSound);
        public void PlayPanelClose()  => PlaySFX(panelCloseSound);

        // ─── Core ─────────────────────────────────────────────────────────

        private void PlaySFX(AudioClip clip, float volume = 1f)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, volume);
        }
    }
}
