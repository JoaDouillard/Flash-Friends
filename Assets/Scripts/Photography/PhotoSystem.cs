using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FlashFriends
{
    // ─── Structures de données ─────────────────────────────────────────────────

    /// <summary>Données d'un sujet détecté dans le cadre au moment de la photo.</summary>
    [Serializable]
    public class PhotoSubjectData
    {
        /// <summary>Le composant PhotoSubject détecté.</summary>
        public PhotoSubject subject;

        /// <summary>Distance entre la caméra et le sujet au moment de la photo.</summary>
        public float distance;

        /// <summary>Position normalisée du sujet dans le cadre (0-1 en X et Y).</summary>
        public Vector2 viewportPosition;

        /// <summary>
        /// Taille approximative du sujet dans le cadre, exprimée en fraction de la hauteur de l'écran.
        /// Ex : 0.5 = le sujet occupe 50% de la hauteur du cadre.
        /// </summary>
        public float normalizedSize;
    }

    /// <summary>Résultat complet d'une prise de photo.</summary>
    [Serializable]
    public class PhotoResult
    {
        /// <summary>Sujets détectés dans le cadre au moment de la photo.</summary>
        public List<PhotoSubjectData> subjects = new List<PhotoSubjectData>();

        /// <summary>Chemin absolu du fichier PNG sauvegardé.</summary>
        public string filePath;

        /// <summary>Date et heure de la prise de vue.</summary>
        public DateTime takenAt;
    }

    // ─── PhotoSystem ───────────────────────────────────────────────────────────

    // Système de photo : détection des sujets dans le cadre, capture PNG, flash et son déclencheur.
    [RequireComponent(typeof(PlayerInputHandler))]
    [RequireComponent(typeof(CameraManager))]
    public class PhotoSystem : MonoBehaviour
    {
        [Header("Capture")]
        [Tooltip("Largeur de la photo sauvegardée en pixels.")]
        public int photoWidth = 1920;

        [Tooltip("Hauteur de la photo sauvegardée en pixels.")]
        public int photoHeight = 1080;

        [Tooltip("Délai minimum entre deux photos (empêche le spam de clic).")]
        [Range(0.1f, 3f)]
        public float shootCooldown = 0.5f;

        [Header("Flash visuel")]
        [Tooltip("Image UI blanche plein écran (Canvas Screen Space Overlay). Laisser vide pour désactiver.")]
        public Image flashImage;

        [Tooltip("Durée totale de l'effet de flash en secondes.")]
        [Range(0.05f, 1f)]
        public float flashDuration = 0.25f;

        [Header("Audio")]
        [Tooltip("Son de déclenchement de l'appareil photo.")]
        public AudioClip shutterSound;

        [Range(0f, 1f)]
        public float shutterVolume = 0.8f;

        [Header("Détection des sujets")]
        [Tooltip("Distance maximale de détection des sujets dans le cadre.")]
        public float maxDetectionDistance = 25f;

        [Tooltip("Layers utilisés pour la vérification d'occlusion. " +
                 "IMPORTANT : exclure le layer du joueur (PlayerBody, Player) pour éviter " +
                 "que le corps du personnage bloque la détection.")]
        public LayerMask occlusionMask = ~0;

        [Header("Photo Limit")]
        [Tooltip("Maximum number of photos the player can store. Shows a notification when full.")]
        public static int MaxPhotos = 25;

        [Header("Events")]
        public UnityEvent<PhotoResult> onPhotoTaken;
        public UnityEvent<int> onPhotoCountChanged;

        // ─── Internal state ────────────────────────────────────────────────

        private PlayerInputHandler _input;
        private CameraManager      _cameraManager;
        private bool               _isTakingPhoto;
        private float              _cooldownTimer;
        private AudioSource        _shutterSource;

        // ─── Cycle de vie ──────────────────────────────────────────────────

        private void Awake()
        {
            _input         = GetComponent<PlayerInputHandler>();
            _cameraManager = GetComponent<CameraManager>();

            if (flashImage != null)
                flashImage.gameObject.SetActive(false);

            // Source audio déclencheur — routée vers SFX mixer via AudioBridge
            _shutterSource = gameObject.AddComponent<AudioSource>();
            _shutterSource.playOnAwake  = false;
            _shutterSource.loop         = false;
            _shutterSource.spatialBlend = 0f;
            if (AudioBridge.SFXMixerGroup != null)
                _shutterSource.outputAudioMixerGroup = AudioBridge.SFXMixerGroup;
        }

        private void Update()
        {
            // Bloqué pendant la pause (galerie téléphone ouverte)
            if (Time.timeScale == 0f) return;

            if (_cooldownTimer > 0f)
                _cooldownTimer -= Time.deltaTime;

            if (!_input.leftClick || _isTakingPhoto || _cooldownTimer > 0f) return;

            _input.leftClick = false; // Consommer l'input

            if (_cameraManager.CurrentMode == CameraMode.PhoneCamera)
            {
                // Check photo memory limit before shooting
                int count = SaveManager.Instance?.CurrentSave?.photoFileNames.Count ?? 0;
                if (count >= MaxPhotos)
                {
                    HUDManager.Instance?.ShowNotification(
                        $"Memory full! ({MaxPhotos}/{MaxPhotos}) Delete a photo to continue shooting.");
                    return; // Don't consume cooldown
                }

                _cooldownTimer = shootCooldown;
                StartCoroutine(TakePhoto());
            }
        }

        // ─── Prise de photo ────────────────────────────────────────────────

        private IEnumerator TakePhoto()
        {
            _isTakingPhoto = true;

            // Son immédiat (avant la capture pour un retour audio réactif) — via SFX mixer
            if (shutterSound != null && _shutterSource != null)
            {
                if (AudioBridge.SFXMixerGroup != null && _shutterSource.outputAudioMixerGroup == null)
                    _shutterSource.outputAudioMixerGroup = AudioBridge.SFXMixerGroup;
                _shutterSource.PlayOneShot(shutterSound, shutterVolume);
            }

            // Attendre la fin du rendu de la frame courante pour capturer une image propre
            yield return new WaitForEndOfFrame();

            // Détection des sujets dans le cadre (avant le flash pour ne pas polluer la détection visuelle)
            List<PhotoSubjectData> subjects = DetectSubjectsInFrame();

            // Capture de la photo (sans flash — flash joué après pour ne pas blanchir l'image)
            string filePath = CaptureToFile();

            // Flash visuel APRÈS la capture
            StartCoroutine(FlashEffect());

            // Construction du résultat et notification des systèmes abonnés (score, quêtes...)
            var result = new PhotoResult
            {
                subjects = subjects,
                filePath = filePath,
                takenAt  = DateTime.Now
            };

            onPhotoTaken?.Invoke(result);

            // Notify HUD of updated photo count
            int newCount = SaveManager.Instance?.CurrentSave?.photoFileNames.Count ?? 0;
            onPhotoCountChanged?.Invoke(newCount);
            HUDManager.Instance?.UpdatePhotoCount(newCount, MaxPhotos);

            // Debug log
            Debug.Log($"[PhotoSystem] Photo prise — {subjects.Count} sujet(s) détecté(s) | Fichier : {filePath}");
            foreach (var s in subjects)
                Debug.Log($"  ↳ {s.subject.name} ({s.subject.subjectType}) | dist={s.distance:F1}m | pos={s.viewportPosition} | taille={s.normalizedSize:P0}");

            _isTakingPhoto = false;
        }

        // ─── Capture RenderTexture → PNG ───────────────────────────────────

        private string CaptureToFile()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[PhotoSystem] Camera.main introuvable — capture annulée.");
                return string.Empty;
            }

            // Dossier de sauvegarde : slot actif si disponible, sinon dossier générique
            string folder = SaveManager.Instance != null
                ? SaveManager.Instance.CurrentPhotoFolder
                : Path.Combine(Application.persistentDataPath, "Photos");
            Directory.CreateDirectory(folder);
            string filePath = Path.Combine(folder, $"photo_{DateTime.Now:yyyyMMdd_HHmmss}.png");

            // Rendu dans une RenderTexture temporaire
            var rt             = new RenderTexture(photoWidth, photoHeight, 24);
            var previousTarget = cam.targetTexture;

            cam.targetTexture = rt;
            cam.Render();

            // Lecture des pixels depuis la RenderTexture
            RenderTexture.active = rt;
            var texture = new Texture2D(photoWidth, photoHeight, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, photoWidth, photoHeight), 0, 0);
            texture.Apply();

            // Nettoyage
            cam.targetTexture    = previousTarget;
            RenderTexture.active = null;
            Destroy(rt);

            // Sauvegarde PNG sur disque
            File.WriteAllBytes(filePath, texture.EncodeToPNG());
            Destroy(texture);

            // Register photo in active save slot
            SaveManager.Instance?.AddPhoto(Path.GetFileName(filePath));

            return filePath;
        }

        // ─── Détection des sujets dans le cadre ───────────────────────────

        private List<PhotoSubjectData> DetectSubjectsInFrame()
        {
            var result  = new List<PhotoSubjectData>();
            var camera  = Camera.main;
            if (camera == null) return result;

            // Exclusion automatique du layer du joueur pour éviter l'auto-blocage
            int mask = occlusionMask & ~(1 << gameObject.layer);

            // Distance de détection scalée par le zoom actuel
            // Formule : maxDetectionDistance × zoom × 10
            // (un zoom x4 permet de détecter jusqu'à 4 × 10 × 25 = 1000 m — bateaux, avions, etc.)
            float zoom           = _cameraManager != null ? _cameraManager.CurrentZoom : 1f;
            float effectiveRange = maxDetectionDistance * zoom * 10f;

            var subjects = FindObjectsByType<PhotoSubject>(FindObjectsSortMode.None);

            foreach (var subject in subjects)
            {
                Vector3 worldPos = subject.DetectionWorldPosition;

                // Conversion en coordonnées viewport (0-1) ; z > 0 = devant la caméra
                Vector3 vp = camera.WorldToViewportPoint(worldPos);

                if (vp.z <= 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f)
                    continue; // Hors cadre ou derrière la caméra

                float distance = Vector3.Distance(camera.transform.position, worldPos);
                if (distance > effectiveRange) continue;

                // Vérification d'occlusion : quelque chose bloque-t-il la vue ?
                Vector3 direction = (worldPos - camera.transform.position).normalized;
                if (Physics.Raycast(camera.transform.position, direction,
                    out RaycastHit hit, distance * 0.98f, mask))
                {
                    // Si le raycast touche un objet qui n'est PAS le sujet ou l'un de ses enfants → occluded
                    if (!hit.transform.IsChildOf(subject.transform) && hit.transform != subject.transform)
                        continue;
                }

                result.Add(new PhotoSubjectData
                {
                    subject          = subject,
                    distance         = distance,
                    viewportPosition = new Vector2(vp.x, vp.y),
                    normalizedSize   = EstimateSubjectFrameSize(subject, camera)
                });
            }

            return result;
        }

        private float EstimateSubjectFrameSize(PhotoSubject subject, Camera camera)
        {
            Renderer r = subject.GetComponentInChildren<Renderer>();
            if (r == null) return 0f;

            Bounds bounds  = r.bounds;
            Vector3 topVP  = camera.WorldToViewportPoint(bounds.center + Vector3.up * bounds.extents.y);
            Vector3 botVP  = camera.WorldToViewportPoint(bounds.center - Vector3.up * bounds.extents.y);
            return Mathf.Clamp01(Mathf.Abs(topVP.y - botVP.y));
        }

        // ─── Flash visuel ──────────────────────────────────────────────────

        private IEnumerator FlashEffect()
        {
            if (flashImage == null) yield break;

            flashImage.gameObject.SetActive(true);
            var c = Color.white;

            // Montée instantanée
            c.a = 1f;
            flashImage.color = c;

            // Descente progressive
            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(1f, 0f, elapsed / flashDuration);
                flashImage.color = c;
                yield return null;
            }

            flashImage.gameObject.SetActive(false);
        }
    }
}
