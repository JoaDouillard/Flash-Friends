using UnityEngine;

namespace FlashFriends
{
    /// <summary>
    /// Maintient un objet toujours orienté de la même façon dans l'espace monde,
    /// quelle que soit la rotation de son parent.
    ///
    /// Cas d'usage typique : cabines de grande roue.
    ///   Le parent (grande roue) tourne, mais les cabines restent "à la verticale"
    ///   comme si elles étaient soumises à la gravité.
    ///
    /// SETUP :
    ///   1. Placer ce script sur la cabine (enfant de la grande roue).
    ///   2. Orienter la cabine correctement dans l'éditeur (sens voulu au démarrage).
    ///   3. Play — la cabine garde cette orientation monde quelle que soit la position sur la roue.
    /// </summary>
    public class GravityAligned : MonoBehaviour
    {
        [Tooltip("Si coché, l'axe Y local peut tourner librement (le haut reste fixe mais la cabine " +
                 "peut pivoter sur elle-même). Décoché = orientation monde totalement fixe.")]
        [SerializeField] private bool freeYRotation = false;

        // ─── State ─────────────────────────────────────────────────────────

        private Quaternion _fixedWorldRotation;

        // ─── Lifecycle ─────────────────────────────────────────────────────

        private void Start()
        {
            // Mémorise l'orientation monde au démarrage — c'est l'orientation "droite"
            _fixedWorldRotation = transform.rotation;
        }

        private void LateUpdate()
        {
            if (freeYRotation)
            {
                // Garde l'axe UP mondial mais laisse Y tourner
                float currentY = transform.eulerAngles.y;
                transform.rotation = Quaternion.Euler(
                    _fixedWorldRotation.eulerAngles.x,
                    currentY,
                    _fixedWorldRotation.eulerAngles.z);
            }
            else
            {
                // Orientation monde totalement fixe
                transform.rotation = _fixedWorldRotation;
            }
        }
    }
}
