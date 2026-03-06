using UnityEngine;
using UnityEngine.InputSystem;

namespace FlashFriends
{
    /// <summary>
    /// Gestionnaire d'input du joueur.
    /// Remplace StarterAssetsInputs — utilise InputSysteme (auto-généré depuis InputSysteme.inputactions).
    /// À placer sur le GameObject Player avec ThirdPersonController et CameraManager.
    /// Ne pas utiliser le composant PlayerInput : ce script gère tout directement.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour, InputSysteme.IPlayerActions
    {
        [Header("Valeurs d'input")]
        public Vector2 move;
        public Vector2 look;
        public bool jump;
        public bool sprint;
        public bool crouch;
        public bool interact;
        public bool leftClick;
        public bool rightClick;
        public bool tab;

        // Booleans à consommer (remis à false après lecture par les systèmes)
        [HideInInspector] public bool switchCamera;
        [HideInInspector] public bool phoneCamera;

        /// <summary>True quand la galerie de photos est ouverte. Mis à jour par PhoneGalleryUI.</summary>
        [HideInInspector] public bool galleryOpen;

        [Header("Paramètres")]
        public bool analogMovement;

        [Header("Curseur")]
        public bool cursorLocked = true;
        public bool cursorInputForLook = true;

        /// <summary>True si le dernier input Look vient de la souris.</summary>
        public bool isMouseDevice { get; private set; } = true;

        private InputSysteme _actions;

        // ─── Cycle de vie ────────────────────────────────────────────────

        private void Awake()
        {
            _actions = new InputSysteme();
        }

        private void OnEnable()
        {
            _actions.Player.Enable();
            _actions.Player.AddCallbacks(this);
        }

        private void OnDisable()
        {
            _actions.Player.RemoveCallbacks(this);
            _actions.Player.Disable();
        }

        private void OnDestroy()
        {
            _actions.Dispose();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            SetCursorState(cursorLocked);
        }

        private void SetCursorState(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        }

        // ─── Callbacks InputSysteme.IPlayerActions ───────────────────────

        public void OnMove(InputAction.CallbackContext context)
        {
            move = context.ReadValue<Vector2>();
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            if (!cursorInputForLook) return;
            look = context.ReadValue<Vector2>();
            if (context.control != null)
                isMouseDevice = context.control.device is Mouse;
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.started)  jump = true;
            if (context.canceled) jump = false;
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            sprint = context.ReadValueAsButton();
        }

        public void OnCrouch(InputAction.CallbackContext context)
        {
            if (context.started)  crouch = true;
            if (context.canceled) crouch = false;
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (context.started)  interact = true;
            if (context.canceled) interact = false;
        }

        public void OnLeftClick(InputAction.CallbackContext context)
        {
            if (context.started)  leftClick = true;
            if (context.canceled) leftClick = false;
        }

        public void OnRightClick(InputAction.CallbackContext context)
        {
            if (context.started)  rightClick = true;
            if (context.canceled) rightClick = false;
        }

        public void OnTab(InputAction.CallbackContext context)
        {
            if (context.started)  tab = true;
            if (context.canceled) tab = false;
        }

        /// <summary>Toggle 3e / 1ère personne (touche V). Consommé par CameraManager.</summary>
        public void OnSwitchCamera(InputAction.CallbackContext context)
        {
            if (context.started) switchCamera = true;
        }

        /// <summary>Caméra téléphone (touche Q). Consommé par CameraManager.</summary>
        public void OnPhoneCamera(InputAction.CallbackContext context)
        {
            if (context.started) phoneCamera = true;
        }

        // ─── Support input mobile (VirtualInput) ─────────────────────────

        public void MoveInput(Vector2 direction)  => move = direction;
        public void LookInput(Vector2 direction)  { if (cursorInputForLook) look = direction; }
        public void JumpInput(bool state)         => jump = state;
        public void SprintInput(bool state)       => sprint = state;
    }
}
