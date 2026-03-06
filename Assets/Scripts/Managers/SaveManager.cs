using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlashFriends
{
    // Singleton save system — DontDestroyOnLoad depuis MainMenu.
    // Chemins : persistentDataPath/Saves/Slot{N}/save.json + /Photos/
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        // ─── Configuration ──────────────────────────────────────────────────

        public const int SlotCount = 3;

        [Tooltip("Nom de la scène de jeu chargée au démarrage d'une partie.")]
        [SerializeField] private string gameSceneName = "TestScene";

        // ─── État courant ────────────────────────────────────────────────────

        public int CurrentSlotIndex { get; private set; } = -1;
        public SaveData CurrentSave { get; private set; }

        public string CurrentPhotoFolder
        {
            get
            {
                if (CurrentSlotIndex < 0) return Path.Combine(Application.persistentDataPath, "Photos");
                return PhotoFolderForSlot(CurrentSlotIndex);
            }
        }

        // ─── Cycle de vie ─────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // ─── Chemins ──────────────────────────────────────────────────────────

        private static string SlotFolder(int slot)
            => Path.Combine(Application.persistentDataPath, "Saves", $"Slot{slot}");

        private static string SaveFilePath(int slot)
            => Path.Combine(SlotFolder(slot), "save.json");

        private static string PhotoFolderForSlot(int slot)
            => Path.Combine(SlotFolder(slot), "Photos");

        // ─── Lecture des en-têtes (pour l'UI SaveMenu) ────────────────────────

        /// <summary>
        /// Lit les données des 3 slots. Retourne null pour un slot vide.
        /// </summary>
        public SaveData[] LoadAllSlotHeaders()
        {
            var result = new SaveData[SlotCount];
            for (int i = 0; i < SlotCount; i++)
            {
                string path = SaveFilePath(i);
                if (!File.Exists(path)) continue;

                try
                {
                    string json = File.ReadAllText(path);
                    result[i] = JsonUtility.FromJson<SaveData>(json);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SaveManager] Impossible de lire Slot{i} : {e.Message}");
                }
            }
            return result;
        }

        // ─── Nouvelle partie ──────────────────────────────────────────────────

        /// <summary>
        /// Crée un nouveau slot et charge la scène de jeu.
        /// </summary>
        public void StartNewGame(int slot, string playerName)
        {
            if (slot < 0 || slot >= SlotCount)
            {
                Debug.LogError($"[SaveManager] Index de slot invalide : {slot}");
                return;
            }

            // Créer les dossiers
            Directory.CreateDirectory(SlotFolder(slot));
            Directory.CreateDirectory(PhotoFolderForSlot(slot));

            string now = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            CurrentSlotIndex = slot;
            CurrentSave = new SaveData
            {
                playerName      = string.IsNullOrWhiteSpace(playerName) ? "Photographe" : playerName.Trim(),
                totalScore      = 0,
                playTimeSeconds = 0f,
                creationDate    = now,
                lastSaveDate    = now
            };

            SaveCurrentGame();
            SceneManager.LoadScene(gameSceneName);
        }

        // ─── Charger une partie existante ─────────────────────────────────────

        /// <summary>
        /// Charge un slot existant et charge la scène de jeu.
        /// </summary>
        public void LoadGame(int slot)
        {
            if (slot < 0 || slot >= SlotCount)
            {
                Debug.LogError($"[SaveManager] Index de slot invalide : {slot}");
                return;
            }

            string path = SaveFilePath(slot);
            if (!File.Exists(path))
            {
                Debug.LogError($"[SaveManager] Fichier de save introuvable : {path}");
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                CurrentSave      = JsonUtility.FromJson<SaveData>(json);
                CurrentSlotIndex = slot;
                SceneManager.LoadScene(gameSceneName);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Erreur lors du chargement du Slot{slot} : {e.Message}");
            }
        }

        // ─── Sauvegarder ──────────────────────────────────────────────────────

        /// <summary>
        /// Sérialise CurrentSave en JSON sur le disque.
        /// Met à jour totalScore depuis PhotoScorer si disponible.
        /// </summary>
        public void SaveCurrentGame()
        {
            if (CurrentSave == null || CurrentSlotIndex < 0) return;

            // Synchroniser le score depuis PhotoScorer
            var scorer = FindFirstObjectByType<PhotoScorer>();
            if (scorer != null)
                CurrentSave.totalScore = scorer.TotalScore;

            CurrentSave.lastSaveDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            Directory.CreateDirectory(SlotFolder(CurrentSlotIndex));
            string json = JsonUtility.ToJson(CurrentSave, prettyPrint: true);
            File.WriteAllText(SaveFilePath(CurrentSlotIndex), json);

            Debug.Log($"[SaveManager] Slot{CurrentSlotIndex} sauvegardé — Score: {CurrentSave.totalScore} | Temps: {FormatPlayTime(CurrentSave.playTimeSeconds)}");
        }

        // ─── Supprimer un slot ────────────────────────────────────────────────

        /// <summary>
        /// Supprime le dossier complet d'un slot (save.json + Photos/).
        /// </summary>
        public void DeleteSlot(int slot)
        {
            if (slot < 0 || slot >= SlotCount) return;

            string folder = SlotFolder(slot);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
                Debug.Log($"[SaveManager] Slot{slot} supprimé.");
            }

            if (CurrentSlotIndex == slot)
            {
                CurrentSlotIndex = -1;
                CurrentSave      = null;
            }
        }

        // ─── Photos ───────────────────────────────────────────────────────────

        /// <summary>
        /// Enregistre le nom de fichier d'une photo dans les données du slot actif.
        /// </summary>
        public void AddPhoto(string fileName)
        {
            if (CurrentSave == null) return;
            CurrentSave.photoFileNames.Add(fileName);
        }

        // ─── Utilitaire ───────────────────────────────────────────────────────

        /// <summary>Formate un temps en secondes au format HH:MM:SS.</summary>
        public static string FormatPlayTime(float seconds)
        {
            int h = (int)(seconds / 3600);
            int m = (int)(seconds % 3600 / 60);
            int s = (int)(seconds % 60);
            return $"{h:00}:{m:00}:{s:00}";
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only: initializes a temporary save for testing directly from the game scene.
        /// Has no effect if a save is already active.
        /// </summary>
        public void InitializeTestSave(string playerName = "Test Player")
        {
            if (CurrentSave != null) return;

            CurrentSlotIndex = 0;
            CurrentSave = new SaveData
            {
                playerName   = playerName,
                creationDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                lastSaveDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
            };
            Directory.CreateDirectory(PhotoFolderForSlot(CurrentSlotIndex));
            Debug.LogWarning("[SaveManager] Editor test save created. Launch from MainMenu for real saves.");
        }
#endif
    }
}
