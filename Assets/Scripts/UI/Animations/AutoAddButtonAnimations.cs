using UnityEngine;
using UnityEngine.UI;

namespace FlashFriends
{
    /// <summary>
    /// Ajouter ce script sur le Canvas principal de chaque scène.
    /// Il attache automatiquement <see cref="ButtonHoverAnimation"/> à tous les boutons au démarrage.
    /// </summary>
    public class AutoAddButtonAnimations : MonoBehaviour
    {
        [Tooltip("Ajouter automatiquement ButtonHoverAnimation à tous les boutons au Start?")]
        [SerializeField] private bool autoAddOnStart = true;

        [Tooltip("Inclure les boutons désactivés?")]
        [SerializeField] private bool includeInactive = true;

        private void Start()
        {
            if (autoAddOnStart)
                AddAnimationsToAllButtons();
        }

        /// <summary>
        /// Ajoute <see cref="ButtonHoverAnimation"/> à tous les boutons de la scène.
        /// </summary>
        public void AddAnimationsToAllButtons()
        {
            var inactive = includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;
            Button[] buttons = FindObjectsByType<Button>(inactive, FindObjectsSortMode.None);

            int added = 0, skipped = 0;
            foreach (Button btn in buttons)
            {
                if (btn.GetComponent<ButtonHoverAnimation>() == null)
                {
                    btn.gameObject.AddComponent<ButtonHoverAnimation>();
                    added++;
                }
                else
                {
                    skipped++;
                }
            }

            Debug.Log($"[AutoAddButtonAnimations] {added} bouton(s) animé(s), {skipped} déjà animé(s).");
        }

        /// <summary>
        /// Supprime <see cref="ButtonHoverAnimation"/> de tous les boutons (utile pour debug).
        /// </summary>
        public void RemoveAnimationsFromAllButtons()
        {
            var anims = FindObjectsByType<ButtonHoverAnimation>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var a in anims) DestroyImmediate(a);
            Debug.Log($"[AutoAddButtonAnimations] {anims.Length} animation(s) supprimée(s).");
        }
    }
}
