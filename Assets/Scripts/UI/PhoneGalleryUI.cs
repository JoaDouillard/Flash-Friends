using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlashFriends
{
    // ─── Données d'entrée de galerie ───────────────────────────────────────────

    /// <summary>Associe un PhotoResult à son score calculé, pour affichage dans la galerie.</summary>
    public class GalleryEntry
    {
        public PhotoResult result;
        public PhotoScore  score;
        public Texture2D   thumbnail; // chargé en arrière-plan
    }

    // ─── PhoneGalleryUI ────────────────────────────────────────────────────────

    /// <summary>
    /// Interface téléphone style GTA : galerie de photos ouvertes par Tab.
    ///
    /// SETUP SCÈNE (dans le Canvas unique) :
    ///   UICanvas
    ///   └─ PhonePanel          ← phoneRoot (désactivé par défaut)
    ///      ├─ GridView          ← gridViewRoot
    ///      │  └─ ScrollView → Content [GridLayoutGroup + ContentSizeFitter] ← gridContent
    ///      └─ DetailView        ← detailViewRoot
    ///         ├─ FullImage  [RawImage]
    ///         ├─ ScoreText  [TextMeshProUGUI]
    ///         ├─ DeleteBtn  [Button]
    ///         └─ BackBtn    [Button]
    ///
    /// PREFAB CELLULE :
    ///   CellRoot [Button]
    ///   ├─ Thumbnail [RawImage]
    ///   └─ ScoreLabel [TextMeshProUGUI]
    ///
    /// SCROLLBAR HORIZONTALE :
    ///   Sur le ScrollRect : cocher "Horizontal", décocher "Vertical".
    ///   Glisser la Scrollbar horizontale dans "Horizontal Scrollbar".
    ///   Sur le Content : GridLayoutGroup → Start Axis = Vertical,
    ///   ContentSizeFitter → Horizontal Fit = Preferred Size, Vertical Fit = Unconstrained.
    /// </summary>
    public class PhoneGalleryUI : MonoBehaviour
    {
        [Header("Racine téléphone")]
        [Tooltip("Panel principal du téléphone (activé/désactivé par Tab).")]
        public GameObject phoneRoot;

        [Header("Vue grille")]
        [Tooltip("Root de la vue grille.")]
        public GameObject gridViewRoot;

        [Tooltip("Content du ScrollView (GridLayoutGroup + ContentSizeFitter).")]
        public RectTransform gridContent;

        [Tooltip("Prefab d'une cellule : CellRoot[Button] > Thumbnail[RawImage] + ScoreLabel[TMP].")]
        public GameObject thumbnailCellPrefab;

        [Header("Vue détail")]
        [Tooltip("Root de la vue détail.")]
        public GameObject detailViewRoot;

        [Tooltip("RawImage plein écran de la photo sélectionnée.")]
        public RawImage detailImage;

        [Tooltip("Texte du score de la photo sélectionnée.")]
        public TextMeshProUGUI detailScoreText;

        [Tooltip("Bouton de suppression.")]
        public Button deleteButton;

        [Tooltip("Bouton retour vers la grille.")]
        public Button backButton;

        [Header("Audio")]
        [Tooltip("Son joué à l'ouverture du téléphone.")]
        public AudioClip openSound;

        [Tooltip("Son joué à la fermeture du téléphone.")]
        public AudioClip closeSound;

        [Range(0f, 1f)]
        public float soundVolume = 0.8f;

        [Header("Références Player")]
        [Tooltip("PlayerInputHandler (auto-détecté si vide).")]
        public PlayerInputHandler playerInput;

        // ─── État ──────────────────────────────────────────────────────────

        private readonly List<GalleryEntry> _entries = new List<GalleryEntry>();
        private GalleryEntry                _selected;

        private bool          _isOpen;
        private CameraManager _cameraManager;

        /// <summary>True quand la galerie est ouverte. Utilisé par CameraManager.</summary>
        public bool IsOpen => _isOpen;

        // ─── Cycle de vie ──────────────────────────────────────────────────

        private void Awake()
        {
            if (playerInput == null)
                playerInput = FindFirstObjectByType<PlayerInputHandler>();

            _cameraManager = FindFirstObjectByType<CameraManager>();

            if (phoneRoot      != null) phoneRoot.SetActive(false);
            if (detailViewRoot != null) detailViewRoot.SetActive(false);
            if (gridViewRoot   != null) gridViewRoot.SetActive(true);

            if (deleteButton != null) deleteButton.onClick.AddListener(DeleteCurrentPhoto);
            if (backButton   != null) backButton.onClick.AddListener(CloseDetailView);
        }

        private void Update()
        {
            if (playerInput == null) return;

            if (playerInput.tab)
            {
                playerInput.tab = false;

                // Bloquer l'ouverture si on est en mode photo
                if (!_isOpen && _cameraManager != null && _cameraManager.CurrentMode == CameraMode.PhoneCamera)
                    return;

                TogglePhone();
            }
        }

        // ─── Entrée publique ───────────────────────────────────────────────

        /// <summary>
        /// Enregistre une nouvelle photo dans la galerie.
        /// Brancher sur PhotoScorer.onScoreCalculated.
        /// </summary>
        public void RegisterPhoto(PhotoScore score)
        {
            var entry = new GalleryEntry
            {
                result = score.photoResult,
                score  = score
            };
            _entries.Add(entry);

            // Charge la miniature en arrière-plan
            StartCoroutine(LoadThumbnail(entry));
        }

        // ─── Chargement miniature ──────────────────────────────────────────

        private IEnumerator LoadThumbnail(GalleryEntry entry)
        {
            // Attendre que le fichier soit écrit sur disque
            yield return null;

            if (!File.Exists(entry.result.filePath)) yield break;

            byte[] bytes = File.ReadAllBytes(entry.result.filePath);
            var    tex   = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            entry.thumbnail = tex;

            // Si la galerie est ouverte pendant le chargement, rafraîchir la grille
            if (_isOpen) RefreshGrid();
        }

        // ─── Ouverture / fermeture ─────────────────────────────────────────

        private void TogglePhone()
        {
            _isOpen = !_isOpen;
            if (phoneRoot != null) phoneRoot.SetActive(_isOpen);

            // Synchronise le flag lu par CameraManager
            if (playerInput != null) playerInput.galleryOpen = _isOpen;

            if (_isOpen)
            {
                if (openSound != null)
                    AudioSource.PlayClipAtPoint(openSound, Camera.main.transform.position, soundVolume);

                ShowGridView();
                Time.timeScale   = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;
            }
            else
            {
                if (closeSound != null)
                    AudioSource.PlayClipAtPoint(closeSound, Camera.main.transform.position, soundVolume);

                CloseDetailView();
                Time.timeScale   = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
            }
        }

        // ─── Vue grille ────────────────────────────────────────────────────

        private void ShowGridView()
        {
            if (gridViewRoot   != null) gridViewRoot.SetActive(true);
            if (detailViewRoot != null) detailViewRoot.SetActive(false);
            RefreshGrid();
        }

        /// <summary>
        /// Détruit toutes les cellules existantes et les recrée depuis _entries.
        /// Appelé à chaque ouverture de la grille pour garantir un layout correct.
        /// </summary>
        private void RefreshGrid()
        {
            if (thumbnailCellPrefab == null || gridContent == null) return;

            // Détruire les anciennes cellules
            foreach (Transform child in gridContent)
                Destroy(child.gameObject);

            // Recréer toutes les cellules
            foreach (var entry in _entries)
                CreateCell(entry);

            // Forcer le recalcul du layout une fois que tout est instancié
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(gridContent);
        }

        private void CreateCell(GalleryEntry entry)
        {
            GameObject cell = Instantiate(thumbnailCellPrefab, gridContent);

            var rawImage = cell.GetComponentInChildren<RawImage>();
            if (rawImage != null && entry.thumbnail != null)
                rawImage.texture = entry.thumbnail;

            var scoreLabel = cell.GetComponentInChildren<TextMeshProUGUI>();
            if (scoreLabel != null)
                scoreLabel.text = $"+{entry.score.totalScore}";

            var button = cell.GetComponentInChildren<Button>();
            if (button == null) button = cell.AddComponent<Button>();

            var captured = entry;
            button.onClick.AddListener(() => OpenDetailView(captured));
        }

        // ─── Vue détail ────────────────────────────────────────────────────

        private void OpenDetailView(GalleryEntry entry)
        {
            _selected = entry;

            if (gridViewRoot   != null) gridViewRoot.SetActive(false);
            if (detailViewRoot != null) detailViewRoot.SetActive(true);

            if (detailImage != null)
                detailImage.texture = entry.thumbnail;

            if (detailScoreText != null)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Score: +{entry.score.totalScore} Good Vibes");
                sb.AppendLine($"Subjects: {entry.result.subjects.Count}");
                sb.AppendLine($"Time: {entry.result.takenAt:HH:mm:ss}");
                sb.AppendLine();
                foreach (var b in entry.score.bonuses)
                    sb.AppendLine($"  {b.label}: +{b.value}");

                detailScoreText.text = sb.ToString();
            }
        }

        private void CloseDetailView()
        {
            _selected = null;
            ShowGridView();
        }

        // ─── Suppression ───────────────────────────────────────────────────

        private void DeleteCurrentPhoto()
        {
            if (_selected == null) return;

            // Delete file from disk
            if (File.Exists(_selected.result.filePath))
                File.Delete(_selected.result.filePath);

            // Remove from SaveManager tracking (so the photo limit check stays accurate)
            if (SaveManager.Instance?.CurrentSave != null)
            {
                string fileName = Path.GetFileName(_selected.result.filePath);
                SaveManager.Instance.CurrentSave.photoFileNames.Remove(fileName);
            }

            if (_selected.thumbnail != null)
                Destroy(_selected.thumbnail);

            _entries.Remove(_selected);
            _selected = null;

            // Update HUD photo counter
            int count = SaveManager.Instance?.CurrentSave?.photoFileNames.Count ?? _entries.Count;
            HUDManager.Instance?.UpdatePhotoCount(count, PhotoSystem.MaxPhotos);

            // Back to grid
            ShowGridView();
        }
    }
}
