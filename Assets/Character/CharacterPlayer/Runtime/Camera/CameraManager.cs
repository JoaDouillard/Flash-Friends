using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace FlashFriends
{
    /// <summary>
    /// Modes de caméra disponibles.
    /// </summary>
    public enum CameraMode
    {
        ThirdPerson,
        FirstPerson,
        PhoneCamera
    }

    /// <summary>
    /// Gère le switch entre 3 modes de caméra via une seule CinemachineCamera.
    ///
    /// SETUP SCÈNE :
    /// 1. Ajouter ce script sur le Player (avec ThirdPersonController + PlayerInputHandler).
    /// 2. CinemachineCamera → laisser vide (auto-détecté) OU glisser "PlayerFollowCamera".
    ///
    /// SETUP 1ÈRE PERSONNE :
    /// 3. Créer un GameObject enfant de PlayerCameraRoot → "FPEyeAnchor".
    /// 4. Ajouter le script FPEyeAnchor dessus, assigner l'os de la tête dans "Head Bone".
    /// 5. Glisser "FPEyeAnchor" dans "Fp Eye Target".
    ///
    /// SETUP CULLING MASK :
    /// 6. Créer un layer "PlayerBody" (Project Settings → Tags and Layers).
    /// 7. Assigner ce layer à tous les meshes du personnage.
    /// 8. Dans "Player Body Mask", sélectionner le layer "PlayerBody".
    /// </summary>
    [RequireComponent(typeof(PlayerInputHandler))]
    [RequireComponent(typeof(ThirdPersonController))]
    public class CameraManager : MonoBehaviour
    {
        [Header("Cinemachine (auto-détecté si vide)")]
        [Tooltip("La CinemachineCamera unique. Laisser vide pour auto-détection dans la scène.")]
        public CinemachineCamera cinemachineCamera;

        [Header("1ère personne")]
        [Tooltip("FPEyeAnchor : enfant de PlayerCameraRoot avec le script FPEyeAnchor. " +
                 "La caméra suit la tête en temps réel (animations, accroupissement).")]
        public Transform fpEyeTarget;

        [Header("Caméra téléphone")]
        [Tooltip("ShoulderOffset appliqué en mode téléphone.")]
        public Vector3 phoneShoulderOffset = new Vector3(0.3f, 0f, 0.15f);

        [Tooltip("FOV de la caméra téléphone (plus petit = plus zoomé).")]
        [Range(20f, 90f)]
        public float phoneFOV = 45f;

        [Header("Masquage corps en 1ère personne (Culling Mask)")]
        [Tooltip("Layer des meshes du joueur (ex: PlayerBody). " +
                 "En 1ère personne, retiré du culling mask : corps absent visuellement " +
                 "mais ombres et collisions restent actives.")]
        public LayerMask playerBodyMask;

        [Header("UI Téléphone")]
        [Tooltip("Canvas/Panel de l'overlay téléphone (désactivé par défaut).")]
        public GameObject phoneUIPanel;

        [Header("Audio mode photo")]
        [Tooltip("Son joué à l'activation du mode caméra téléphone.")]
        public AudioClip phoneCameraSound;

        [Range(0f, 1f)]
        public float phoneCameraVolume = 0.8f;

        [Tooltip("Niveaux de zoom disponibles (multiplicateur du FOV de base). " +
                 "Ex : 1 = normal, 2 = x2, 0.5 = dézoom.")]
        public float[] zoomLevels = { 0.5f, 1f, 2f, 3f, 4f };

        [Tooltip("TextMeshProUGUI affichant le niveau de zoom actuel (ex: x2). " +
                 "À placer dans le phoneUIPanel.")]
        public TextMeshProUGUI zoomText;

        [Header("HUD masqué en mode photo")]
        [Tooltip("GameObjects du HUD à masquer quand la caméra téléphone est active " +
                 "(ex : ScoreText). Glisser les objets à cacher ici.")]
        public GameObject[] hudElementsToHide;

        // La galerie ouverte/fermée est trackée via PlayerInputHandler.galleryOpen (set par PhoneGalleryUI)

        [Header("Notifications HUD")]
        [Tooltip("Invoqué avec un message quand une action est bloquée. " +
                 "Brancher ici le futur script HUD pour afficher les notifications.")]
        public UnityEvent<string> onHUDNotification;

        // ─── État interne ──────────────────────────────────────────────────

        private CameraMode _currentMode = CameraMode.ThirdPerson;

        private CinemachineThirdPersonFollow _thirdPersonFollow;
        private Transform _originalFollowTarget;

        private float   _originalCameraDistance;
        private Vector3 _originalShoulderOffset;
        private float   _originalVerticalArmLength;
        private float   _originalFOV;

        private PlayerInputHandler    _input;
        private ThirdPersonController _controller;

        private int _zoomIndex = 1; // index dans zoomLevels (1 = x1 par défaut)

        // ─── Cycle de vie ──────────────────────────────────────────────────

        private void Awake()
        {
            _input      = GetComponent<PlayerInputHandler>();
            _controller = GetComponent<ThirdPersonController>();
        }

        private void Start()
        {
            if (cinemachineCamera == null)
            {
                cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
                if (cinemachineCamera == null)
                    Debug.LogWarning("[CameraManager] Aucune CinemachineCamera trouvée dans la scène !", this);
            }

            if (cinemachineCamera != null)
            {
                _thirdPersonFollow = cinemachineCamera.GetComponentInChildren<CinemachineThirdPersonFollow>();
                if (_thirdPersonFollow == null)
                    Debug.LogWarning("[CameraManager] CinemachineThirdPersonFollow introuvable !", this);

                _originalFollowTarget = cinemachineCamera.Follow;
            }

            if (_thirdPersonFollow != null)
            {
                _originalCameraDistance    = _thirdPersonFollow.CameraDistance;
                _originalShoulderOffset    = _thirdPersonFollow.ShoulderOffset;
                _originalVerticalArmLength = _thirdPersonFollow.VerticalArmLength;
            }

            if (cinemachineCamera != null)
                _originalFOV = cinemachineCamera.Lens.FieldOfView;

            ApplyMode(CameraMode.ThirdPerson);
        }

        private void Update()
        {
            HandleSwitchCamera();
            HandlePhoneCamera();
            HandleZoom();
        }

        // ─── Gestion des inputs ────────────────────────────────────────────

        private void HandleSwitchCamera()
        {
            if (!_input.switchCamera) return;
            _input.switchCamera = false;

            ApplyMode(_currentMode == CameraMode.ThirdPerson
                ? CameraMode.FirstPerson
                : CameraMode.ThirdPerson);
        }

        private void HandlePhoneCamera()
        {
            if (!_input.phoneCamera) return;
            _input.phoneCamera = false;

            // Bloquer si la galerie de photos est ouverte (flag mis à jour par PhoneGalleryUI)
            if (_input.galleryOpen)
            {
                onHUDNotification?.Invoke("Close the gallery before using the camera.");
                return;
            }

            // Bloquer l'accès à la caméra téléphone si le joueur est accroupi
            if (_controller.IsCrouching)
            {
                onHUDNotification?.Invoke("Cannot use the camera while crouching.");
                return;
            }

            ApplyMode(_currentMode == CameraMode.PhoneCamera
                ? CameraMode.ThirdPerson
                : CameraMode.PhoneCamera);
        }

        // ─── Application du mode ───────────────────────────────────────────

        private void ApplyMode(CameraMode mode)
        {
            _currentMode = mode;
            _controller.CurrentCameraMode = mode;

            bool isPhoneMode = mode == CameraMode.PhoneCamera;
            SetHUDElementsVisible(!isPhoneMode);

            switch (mode)
            {
                case CameraMode.ThirdPerson:
                    SetThirdPersonSettings();
                    SetPlayerBodyVisible(true);
                    SetPhoneUI(false);
                    break;

                case CameraMode.FirstPerson:
                    SetFirstPersonSettings();
                    SetPlayerBodyVisible(false);
                    SetPhoneUI(false);
                    break;

                case CameraMode.PhoneCamera:
                    SetPhoneCameraSettings();
                    SetPlayerBodyVisible(true);
                    SetPhoneUI(true);
                    break;
            }
        }

        // ─── Réglages par mode ─────────────────────────────────────────────

        private void SetThirdPersonSettings()
        {
            if (cinemachineCamera != null && _originalFollowTarget != null)
                cinemachineCamera.Follow = _originalFollowTarget;

            if (_thirdPersonFollow != null)
            {
                _thirdPersonFollow.CameraDistance    = _originalCameraDistance;
                _thirdPersonFollow.ShoulderOffset    = _originalShoulderOffset;
                _thirdPersonFollow.VerticalArmLength = _originalVerticalArmLength;
            }
            SetFOV(_originalFOV);
        }

        private void SetFirstPersonSettings()
        {
            if (cinemachineCamera != null)
                cinemachineCamera.Follow = fpEyeTarget;

            if (_thirdPersonFollow != null)
            {
                _thirdPersonFollow.CameraDistance    = 0f;
                _thirdPersonFollow.ShoulderOffset    = Vector3.zero;
                _thirdPersonFollow.VerticalArmLength = 0f;
            }
            SetFOV(_originalFOV);
        }

        private void SetPhoneCameraSettings()
        {
            if (cinemachineCamera != null && _originalFollowTarget != null)
                cinemachineCamera.Follow = _originalFollowTarget;

            if (_thirdPersonFollow != null)
            {
                _thirdPersonFollow.CameraDistance    = 0f;
                _thirdPersonFollow.ShoulderOffset    = phoneShoulderOffset;
                _thirdPersonFollow.VerticalArmLength = 0f;
            }

            // Son d'activation du mode photo
            if (phoneCameraSound != null)
                AudioSource.PlayClipAtPoint(phoneCameraSound, transform.position, phoneCameraVolume);

            // Réinitialise le zoom à x1 à chaque entrée en mode photo
            _zoomIndex = 1;
            SetFOV(phoneFOV);
            UpdateZoomText();
        }

        // ─── Utilitaires ───────────────────────────────────────────────────

        private void SetFOV(float fov)
        {
            if (cinemachineCamera == null) return;
            var lens = cinemachineCamera.Lens;
            lens.FieldOfView = fov;
            cinemachineCamera.Lens = lens;
        }

        private void SetPlayerBodyVisible(bool visible)
        {
            if (playerBodyMask == 0) return;
            var cam = Camera.main;
            if (cam == null) return;

            if (visible)
                cam.cullingMask |= playerBodyMask;
            else
                cam.cullingMask &= ~playerBodyMask;
        }

        private void SetPhoneUI(bool active)
        {
            if (phoneUIPanel != null)
                phoneUIPanel.SetActive(active);
        }

        private void SetHUDElementsVisible(bool visible)
        {
            foreach (var go in hudElementsToHide)
                if (go != null) go.SetActive(visible);
        }

        // ─── Zoom ──────────────────────────────────────────────────────────

        private void HandleZoom()
        {
            if (_currentMode != CameraMode.PhoneCamera) return;

            float scroll = Mouse.current?.scroll.ReadValue().y ?? 0f;
            if (scroll == 0f) return;

            int newIndex = scroll > 0f ? _zoomIndex + 1 : _zoomIndex - 1;
            newIndex = Mathf.Clamp(newIndex, 0, zoomLevels.Length - 1);

            if (newIndex == _zoomIndex) return;

            _zoomIndex = newIndex;
            float zoom = zoomLevels[_zoomIndex];
            SetFOV(phoneFOV / zoom);
            UpdateZoomText();
        }

        private void UpdateZoomText()
        {
            if (zoomText == null) return;
            float zoom = zoomLevels[_zoomIndex];
            zoomText.text = (zoom == Mathf.Round(zoom))
                ? $"x{(int)zoom}"
                : $"x{zoom:F1}";
        }

        public CameraMode CurrentMode => _currentMode;

        /// <summary>
        /// Niveau de zoom actuel (1 = normal, 2 = x2...).
        /// Vaut toujours 1 si on n'est pas en mode PhoneCamera.
        /// </summary>
        public float CurrentZoom => (_currentMode == CameraMode.PhoneCamera && zoomLevels != null && zoomLevels.Length > 0)
            ? zoomLevels[_zoomIndex]
            : 1f;
    }
}
