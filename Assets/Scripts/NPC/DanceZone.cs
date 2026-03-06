using System.Collections.Generic;
using UnityEngine;

namespace FlashFriends
{
    /// <summary>
    /// Zone musicale : les NPCs à l'intérieur dansent quand ils sont à l'arrêt.
    ///
    /// SETUP :
    ///   1. Crée un Empty GameObject, ajoute ce script.
    ///   2. Ajoute un Collider (Sphere ou Box) → coche "Is Trigger".
    ///   3. Redimensionne le collider pour définir la zone musicale.
    ///   4. Dans l'Animator Controller des NPCs, ajoute :
    ///        - Paramètre Bool nommé "Dance"
    ///        - Transition Any State → DanceState (condition : Dance = true)
    ///        - Transition DanceState → Grounded  (condition : Dance = false)
    ///   5. Play — les NPCs qui s'arrêtent dans la zone dansent automatiquement.
    ///
    /// OPTIONNEL : assigne un AudioSource pour la musique de la zone.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DanceZone : MonoBehaviour
    {
        [Header("Audio (optionnel)")]
        [Tooltip("AudioSource avec la musique de la zone. Laisse vide si tu gères l'audio séparément.")]
        [SerializeField] private AudioSource zoneMusic;

        // ─── State ─────────────────────────────────────────────────────────

        private readonly List<NpcController> _npcsInside = new List<NpcController>();

        // ─── Lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            // S'assurer que le collider est bien un trigger
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var npc = other.GetComponentInParent<NpcController>();
            if (npc == null) return;
            if (_npcsInside.Contains(npc)) return;

            _npcsInside.Add(npc);
            npc.SetDancing(true);
        }

        private void OnTriggerExit(Collider other)
        {
            var npc = other.GetComponentInParent<NpcController>();
            if (npc == null) return;

            _npcsInside.Remove(npc);
            npc.SetDancing(false);
        }

        // Nettoyage si un NPC est détruit alors qu'il était dans la zone
        private void OnTriggerStay(Collider other)
        {
            // Retirer les NPCs null de la liste (détruits en cours de jeu)
            _npcsInside.RemoveAll(n => n == null);
        }

        // ─── Editor gizmos ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.8f, 0.15f);
            var col = GetComponent<SphereCollider>();
            if (col != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawSphere(col.center, col.radius);
                Gizmos.color = new Color(1f, 0.4f, 0.8f, 0.8f);
                Gizmos.DrawWireSphere(col.center, col.radius);
            }
        }
#endif
    }
}
