using UnityEngine;

/* Note: les animations sont appelées via le controller pour le personnage et la capsule
 * via des null checks sur l'Animator.
 */

namespace FlashFriends
{
    [RequireComponent(typeof(CharacterController))]
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Joueur")]
        [Tooltip("Vitesse de déplacement en m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Vitesse de sprint en m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("Vitesse de rotation vers la direction de mouvement")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Taux d'accélération et décélération")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Header("Accroupissement")]
        [Tooltip("Vitesse de déplacement en m/s lorsqu'accroupi")]
        public float CrouchSpeed = 1.0f;

        [Tooltip("Hauteur du CharacterController en position accroupie")]
        public float CrouchHeight = 0.9f;

        [Tooltip("Son joué en s'accroupissant et en se relevant")]
        public AudioClip CrouchAudioClip;

        [Tooltip("Multiplicateur de volume des pas en mode accroupi (1 = volume normal)")]
        [Range(0, 1)] public float CrouchFootstepVolumeMultiplier = 0.4f;

        [Space(10)]
        [Tooltip("Hauteur de saut")]
        public float JumpHeight = 1.2f;

        [Tooltip("Gravité du personnage (moteur = -9.81)")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Délai avant de pouvoir re-sauter")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Délai avant d'entrer en chute libre")]
        public float FallTimeout = 0.15f;

        [Header("Détection du sol")]
        public bool Grounded = true;

        [Tooltip("Offset de détection du sol")]
        public float GroundedOffset = -0.14f;

        [Tooltip("Rayon de détection du sol (doit correspondre au CharacterController)")]
        public float GroundedRadius = 0.28f;

        [Tooltip("Layers considérés comme sol")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("La cible que la Cinemachine suit (PlayerCameraRoot)")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("Angle maximum vers le haut")]
        public float TopClamp = 70.0f;

        [Tooltip("Angle maximum vers le bas")]
        public float BottomClamp = -30.0f;

        [Tooltip("Override d'angle supplémentaire")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("Verrouille la caméra en position fixe")]
        public bool LockCameraPosition = false;

        // ─── Mode caméra (géré par CameraManager) ─────────────────────────
        /// <summary>Mode de caméra actuel. Modifié par CameraManager.</summary>
        public CameraMode CurrentCameraMode { get; set; } = CameraMode.ThirdPerson;

        /// <summary>True si le personnage est actuellement accroupi.</summary>
        public bool IsCrouching => _isCrouching;

        // ─── Cinemachine interne ───────────────────────────────────────────
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // ─── Joueur ────────────────────────────────────────────────────────
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // ─── Accroupissement ───────────────────────────────────────────────
        private bool  _isCrouching;
        private float _standingHeight;
        private Vector3 _standingCenter;

        // ─── Timeouts ──────────────────────────────────────────────────────
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // ─── IDs animation ─────────────────────────────────────────────────
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDCrouch;

        // ─── Références ────────────────────────────────────────────────────
        private Animator          _animator;
        private CharacterController _controller;
        private PlayerInputHandler  _input;
        private GameObject          _mainCamera;
        private AudioSource         _footstepSource;

        private const float _threshold = 0.01f;
        private bool _hasAnimator;

        // ─── Cycle de vie ──────────────────────────────────────────────────

        private void Awake()
        {
            if (_mainCamera == null)
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _hasAnimator = TryGetComponent(out _animator);
            _controller  = GetComponent<CharacterController>();
            _input        = GetComponent<PlayerInputHandler>();

            AssignAnimationIDs();

            // Sauvegarde de la hauteur et du centre debout (pour restaurer après accroupissement)
            _standingHeight = _controller.height;
            _standingCenter = _controller.center;

            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            // Source audio pour les sons du joueur (footsteps, landing, crouch)
            // spatialBlend = 0 : 2D, toujours entendu (expérience FPS/TPS classique)
            // Routé vers le groupe SFX via AudioBridge pour être affecté par le slider SFX
            _footstepSource = gameObject.AddComponent<AudioSource>();
            _footstepSource.playOnAwake  = false;
            _footstepSource.loop         = false;
            _footstepSource.spatialBlend = 0f;
            if (AudioBridge.SFXMixerGroup != null)
                _footstepSource.outputAudioMixerGroup = AudioBridge.SFXMixerGroup;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            JumpAndGravity();
            GroundedCheck();
            UpdateCrouch();
            Move();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        // ─── Animation ─────────────────────────────────────────────────────

        private void AssignAnimationIDs()
        {
            _animIDSpeed       = Animator.StringToHash("Speed");
            _animIDGrounded    = Animator.StringToHash("Grounded");
            _animIDJump        = Animator.StringToHash("Jump");
            _animIDFreeFall    = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDCrouch      = Animator.StringToHash("Crouch");
        }

        // ─── Sol ───────────────────────────────────────────────────────────

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x,
                transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            if (_hasAnimator)
                _animator.SetBool(_animIDGrounded, Grounded);
        }

        // ─── Accroupissement ───────────────────────────────────────────────

        private void UpdateCrouch()
        {
            bool wantsCrouch = _input.crouch;

            if (wantsCrouch == _isCrouching) return;

            // Pour se relever : doit être au sol ET avoir de l'espace au-dessus
            if (!wantsCrouch && (!Grounded || !CanStandUp())) return;

            _isCrouching = wantsCrouch;

            if (_isCrouching)
            {
                _controller.height = CrouchHeight;
                _controller.center = new Vector3(0f, CrouchHeight / 2f, 0f);
            }
            else
            {
                _controller.height = _standingHeight;
                _controller.center = _standingCenter;
            }

            if (_hasAnimator)
                _animator.SetBool(_animIDCrouch, _isCrouching);

            // Son d'accroupissement / de relèvement
            if (CrouchAudioClip != null && _footstepSource != null)
                _footstepSource.PlayOneShot(CrouchAudioClip, FootstepAudioVolume);
        }

        /// <summary>Vérifie qu'il y a assez d'espace au-dessus pour se relever.</summary>
        private bool CanStandUp()
        {
            float radius = _controller.radius;
            // Vérifie l'espace entre le sommet de la capsule accroupie et le sommet de la capsule debout.
            // On exclut le layer du joueur lui-même pour ne pas détecter son propre CharacterController.
            int obstacleMask = ~(1 << gameObject.layer);
            Vector3 crouchTop = transform.position + Vector3.up * (CrouchHeight  - radius);
            Vector3 standTop  = transform.position + Vector3.up * (_standingHeight - radius);
            return !Physics.CheckCapsule(crouchTop, standTop, radius, obstacleMask, QueryTriggerInteraction.Ignore);
        }

        // ─── Rotation caméra (commune à tous les modes) ────────────────────

        private void CameraRotation()
        {
            // Ne pas tourner la caméra quand le jeu est en pause (galerie téléphone)
            if (Time.timeScale == 0f) return;

            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                // Souris : pas de deltaTime (delta déjà relatif à la frame)
                // Gamepad : multiplier par deltaTime
                float deltaTimeMultiplier = _input.isMouseDevice ? 1.0f : Time.deltaTime;

                // Sensibilité souris lue depuis le pont statique CameraSettings
                // (mis à jour par GameSettings sans dépendance d'assembly)
                float sensitivity = CameraSettings.MouseSensitivityMultiplier;

                _cinemachineTargetYaw   += _input.look.x * deltaTimeMultiplier * sensitivity;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier * sensitivity;
            }

            _cinemachineTargetYaw   = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
                _cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        // ─── Mouvement ─────────────────────────────────────────────────────

        private void Move()
        {
            // Priorité : accroupi > normal > sprint
            float targetSpeed = _isCrouching ? CrouchSpeed
                              : _input.sprint ? SprintSpeed
                              : MoveSpeed;

            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            float currentHorizontalSpeed = new Vector3(
                _controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset    = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
            Vector3 targetDirection;

            if (CurrentCameraMode == CameraMode.ThirdPerson)
            {
                // ── Mode 3e personne : le personnage tourne vers la direction de mouvement ──
                if (_input.move != Vector2.zero)
                {
                    _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z)
                                      * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                    float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y,
                        _targetRotation, ref _rotationVelocity, RotationSmoothTime);
                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                }
                targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
            }
            else
            {
                // ── Mode 1ère personne / téléphone : strafe, corps aligné sur la caméra ──
                _targetRotation = _mainCamera.transform.eulerAngles.y;
                transform.rotation = Quaternion.Euler(0.0f, _targetRotation, 0.0f);

                if (_input.move != Vector2.zero)
                {
                    Vector3 camForward = Vector3.ProjectOnPlane(
                        _mainCamera.transform.forward, Vector3.up).normalized;
                    Vector3 camRight = Vector3.ProjectOnPlane(
                        _mainCamera.transform.right, Vector3.up).normalized;

                    targetDirection = camForward * _input.move.y + camRight * _input.move.x;
                    if (targetDirection.sqrMagnitude > 1f) targetDirection.Normalize();
                }
                else
                {
                    targetDirection = Vector3.zero;
                }
            }

            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime)
                + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        // ─── Saut & gravité ────────────────────────────────────────────────

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                if (_verticalVelocity < 0.0f)
                    _verticalVelocity = -2f;

                // Pas de saut si accroupi
                if (_input.jump && _jumpTimeoutDelta <= 0.0f && !_isCrouching)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    if (_hasAnimator)
                        _animator.SetBool(_animIDJump, true);
                }

                if (_jumpTimeoutDelta >= 0.0f)
                    _jumpTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    if (_hasAnimator)
                        _animator.SetBool(_animIDFreeFall, true);
                }

                _input.jump = false;
            }

            if (_verticalVelocity < _terminalVelocity)
                _verticalVelocity += Gravity * Time.deltaTime;
        }

        // ─── Utilitaires ───────────────────────────────────────────────────

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f)  lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed   = new Color(1.0f, 0.0f, 0.0f, 0.35f);
            Gizmos.color = Grounded ? transparentGreen : transparentRed;
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        // ─── Callbacks audio animation ─────────────────────────────────────

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && FootstepAudioClips.Length > 0)
            {
                int index = Random.Range(0, FootstepAudioClips.Length);
                float volume = FootstepAudioVolume * (_isCrouching ? CrouchFootstepVolumeMultiplier : 1f);
                if (_footstepSource != null)
                    _footstepSource.PlayOneShot(FootstepAudioClips[index], volume);
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && _footstepSource != null)
            {
                _footstepSource.PlayOneShot(LandingAudioClip, FootstepAudioVolume);
            }
        }
    }
}
