using System.Collections.Generic;
using UnityEngine;

namespace FlashFriends
{
    /// <summary>
    /// Marque un GameObject comme sujet photographiable (PNJ, objet, décor...).
    /// Le PhotoSystem le détecte automatiquement lorsqu'il est dans le cadre.
    ///
    /// SETUP :
    /// - Placer ce composant sur chaque PNJ, objet d'intérêt, décor spécial, etc.
    /// - Renseigner les tags pour le futur système de quêtes
    ///   (ex : "PersonneAgee", "Enfant", "Chien", "MurGraffiti", "FoodTruck"...).
    /// - Optionnel : assigner "Detection Point" à la tête du PNJ pour une détection précise.
    /// </summary>
    public class PhotoSubject : MonoBehaviour
    {
        [Tooltip("Type de sujet (pour le scoring et les quêtes).")]
        public PhotoSubjectType subjectType = PhotoSubjectType.NPC;

        [Tooltip("Tags du sujet, utilisés par le système de quêtes.\n" +
                 "Ex : 'PersonneAgee', 'Enfant', 'Chien', 'MurGraffiti', 'FoodTruck'...")]
        public List<string> tags = new List<string>();

        [Tooltip("Point de référence pour la détection dans le cadre (ex : tête du PNJ). " +
                 "Si vide, utilise le centre du GameObject.")]
        public Transform detectionPoint;

        /// <summary>Retourne true si ce sujet possède le tag donné (insensible à la casse).</summary>
        public bool HasTag(string tag) =>
            tags.Exists(t => string.Equals(t, tag, System.StringComparison.OrdinalIgnoreCase));

        /// <summary>Position de référence en monde pour la détection.</summary>
        public Vector3 DetectionWorldPosition =>
            detectionPoint != null ? detectionPoint.position : transform.position;
    }

    public enum PhotoSubjectType
    {
        NPC,
        POI,        // Point d'intérêt (stand, scène de concert...)
        Object,     // Objet spécifique
        Landmark    // Décor remarquable (mur graffiti, fontaine...)
    }
}
