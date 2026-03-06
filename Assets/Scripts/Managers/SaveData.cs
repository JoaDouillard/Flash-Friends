using System;
using System.Collections.Generic;

namespace FlashFriends
{
    /// <summary>
    /// Données sérialisables d'un slot de sauvegarde.
    /// Sauvegardé en JSON dans : persistentDataPath/Saves/Slot{N}/save.json
    ///
    /// Pas de position de spawn (position fixe dans la scène).
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>Nom du joueur saisi à la création du slot.</summary>
        public string playerName = "Photographe";

        /// <summary>Score Good Vibes total accumulé.</summary>
        public int totalScore = 0;

        /// <summary>Temps de jeu cumulé en secondes.</summary>
        public float playTimeSeconds = 0f;

        /// <summary>Date/heure de création du slot (format dd/MM/yyyy HH:mm).</summary>
        public string creationDate = string.Empty;

        /// <summary>Date/heure de la dernière sauvegarde (format dd/MM/yyyy HH:mm).</summary>
        public string lastSaveDate = string.Empty;

        /// <summary>
        /// Noms de fichiers des photos sauvegardées dans ce slot.
        /// Chemin complet : persistentDataPath/Saves/Slot{N}/Photos/{fileName}
        /// </summary>
        public List<string> photoFileNames = new List<string>();
    }
}
