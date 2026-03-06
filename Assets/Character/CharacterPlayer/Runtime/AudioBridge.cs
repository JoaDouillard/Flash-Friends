using UnityEngine.Audio;

namespace FlashFriends
{
    /// <summary>
    /// Pont statique entre AudioManager (Assembly-CSharp) et ThirdPersonController
    /// (Unity.StarterAssets). Même principe que CameraSettings.
    ///
    /// AudioManager écrit SFXMixerGroup dans son Awake.
    /// ThirdPersonController le lit dans son Start pour router ses AudioSources.
    /// </summary>
    public static class AudioBridge
    {
        /// <summary>Groupe SFX du mixer — affecté par le slider SFX dans les Settings.</summary>
        public static AudioMixerGroup SFXMixerGroup;
    }
}
