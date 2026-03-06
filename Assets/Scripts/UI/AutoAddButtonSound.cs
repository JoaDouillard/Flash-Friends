using UnityEngine;
using UnityEngine.UI;

namespace FlashFriends
{
    /// <summary>
    /// Add this script to the main Canvas of each scene.
    /// Automatically attaches <see cref="ButtonSound"/> to every Button on Start.
    /// Requires <see cref="AudioManager"/> to be present in the scene.
    /// </summary>
    public class AutoAddButtonSound : MonoBehaviour
    {
        [Tooltip("Automatically add ButtonSound to all buttons on Start?")]
        [SerializeField] private bool autoAddOnStart = true;

        [Tooltip("Include buttons on inactive GameObjects?")]
        [SerializeField] private bool includeInactive = true;

        private void Start()
        {
            if (autoAddOnStart)
                AddSoundToAllButtons();
        }

        /// <summary>Attaches <see cref="ButtonSound"/> to every Button that doesn't already have one.</summary>
        public void AddSoundToAllButtons()
        {
            var inactive = includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;
            Button[] buttons = FindObjectsByType<Button>(inactive, FindObjectsSortMode.None);

            int added = 0, skipped = 0;
            foreach (Button btn in buttons)
            {
                if (btn.GetComponent<ButtonSound>() == null)
                {
                    btn.gameObject.AddComponent<ButtonSound>();
                    added++;
                }
                else
                {
                    skipped++;
                }
            }

            Debug.Log($"[AutoAddButtonSound] {added} button(s) wired, {skipped} already had sound.");
        }

        /// <summary>Removes <see cref="ButtonSound"/> from every Button (useful for debugging).</summary>
        public void RemoveSoundFromAllButtons()
        {
            var sounds = FindObjectsByType<ButtonSound>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var s in sounds) DestroyImmediate(s);
            Debug.Log($"[AutoAddButtonSound] {sounds.Length} ButtonSound component(s) removed.");
        }
    }
}
