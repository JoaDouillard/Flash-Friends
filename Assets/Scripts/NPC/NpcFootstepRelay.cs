using UnityEngine;

namespace FlashFriends
{
    /// <summary>
    /// Relay d'Animation Events pour les PNJs dont l'Animator est sur un enfant (cas Mixamo typique).
    ///
    /// POURQUOI :
    ///   Unity envoie les Animation Events uniquement aux scripts sur le MÊME GameObject que l'Animator.
    ///   Si l'Animator est sur un enfant (mesh/armature) et NpcController sur la racine,
    ///   les événements "OnFootstep" ne trouvent pas NpcController → aucun son.
    ///
    /// SETUP :
    ///   Ajoute ce script sur le même GameObject que l'Animator du PNJ
    ///   (ex : l'objet "Ch_NPC" ou "Armature" qui possède le composant Animator).
    ///   NpcController sera trouvé automatiquement via GetComponentInParent.
    ///
    ///   Si l'Animator est déjà sur la racine avec NpcController, ce script est inutile.
    /// </summary>
    public class NpcFootstepRelay : MonoBehaviour
    {
        private NpcController _npc;

        private void Awake()
        {
            _npc = GetComponentInParent<NpcController>();
            if (_npc == null)
                Debug.LogWarning($"[NpcFootstepRelay] Aucun NpcController trouvé dans les parents de '{name}'.");
        }

        // Ces méthodes sont appelées par les Animation Events dans les clips de marche/course
        private void OnFootstep(AnimationEvent animationEvent) => _npc?.ForwardFootstep(animationEvent);
        private void OnLand(AnimationEvent animationEvent)     => _npc?.ForwardLand(animationEvent);
    }
}
