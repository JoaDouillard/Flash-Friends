using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace FlashFriends
{
    // PNJ autonome : déambulation NavMesh, réaction aux photos (ignore / fuite / colère), sons 3D.
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(AudioSource))]
    public class NpcController : MonoBehaviour
    {
        // ─── Enums ─────────────────────────────────────────────────────────

        public enum NpcPersonality { Neutral, Shy, Aggressive }

        // ─── Inspector ─────────────────────────────────────────────────────

        [Header("Personnalité")]
        [Tooltip("Neutral : utilise les probabilités configurées. " +
                 "Shy : fuit plus souvent. Aggressive : crie plus souvent.")]
        [SerializeField] private NpcPersonality personality = NpcPersonality.Neutral;

        [Header("Déambulation")]
        [Tooltip("Rayon autour du point de spawn dans lequel le PNJ se déplace.")]
        [SerializeField] private float wanderRadius = 8f;

        [Tooltip("Secondes entre chaque nouvelle destination (± 1s de variation aléatoire).")]
        [SerializeField] private float wanderInterval = 4f;

        [Tooltip("Vitesse de marche normale. Doit correspondre au seuil 'walk' du blend tree.")]
        [SerializeField] private float walkSpeed = 1.5f;

        [Header("Fuite")]
        [Tooltip("Vitesse pendant la fuite. Doit correspondre au seuil 'run' du blend tree.")]
        [SerializeField] private float fleeSpeed = 4f;

        [Tooltip("Durée pendant laquelle le PNJ joue l'animation de peur AVANT de partir en courant.")]
        [SerializeField] private float fearReactionDuration = 1.5f;

        [Tooltip("Durée de la fuite en secondes avant de reprendre la déambulation.")]
        [SerializeField] private float fleeDuration = 5f;

        [Header("Probabilités de réaction (mode Neutral — somme idéalement = 1)")]
        [Range(0f, 1f)]
        [Tooltip("Probabilité d'ignorer la photo.")]
        [SerializeField] private float chanceIgnore = 0.34f;

        [Range(0f, 1f)]
        [Tooltip("Probabilité de fuir.")]
        [SerializeField] private float chanceFlee = 0.33f;

        // chanceAngry = 1 - chanceIgnore - chanceFlee

        [Header("Sons")]
        [Tooltip("Joué quand le PNJ s'enfuit (peut contenir plusieurs clips, un est choisi au hasard).")]
        [SerializeField] private AudioClip[] fleeClips;

        [Tooltip("Joué quand le PNJ est en colère.")]
        [SerializeField] private AudioClip[] angryClips;

        [Tooltip("Joué quand le PNJ ignore la photo (réaction neutre discrète).")]
        [SerializeField] private AudioClip[] neutralClips;

        [Header("Animations de réaction (optionnel)")]
        [Tooltip("Nom exact du Trigger/Bool dans l'Animator pour la réaction 'Colère'. Laisser vide pour désactiver.")]
        [SerializeField] private string angryAnimParam = "";

        [Tooltip("Nom exact du Trigger/Bool dans l'Animator pour la réaction 'Fuite'. Laisser vide pour désactiver.")]
        [SerializeField] private string fleeAnimParam = "";

        [Header("Sons de pas (Animation Events)")]
        [Tooltip("Clips de pas joués aléatoirement — mêmes clips que le joueur si tu veux.")]
        [SerializeField] private AudioClip[] footstepClips;

        [Tooltip("Volume de base des pas.")]
        [Range(0f, 1f)]
        [SerializeField] private float footstepVolume = 0.4f;

        [Tooltip("Multiplicateur de volume pendant la fuite (pas plus forts en courant).")]
        [Range(1f, 2f)]
        [SerializeField] private float fleeFootstepMultiplier = 1.4f;

        // ─── State ─────────────────────────────────────────────────────────

        private enum NpcState { Wander, Flee, Angry }

        private NpcState     _state = NpcState.Wander;
        private NavMeshAgent _agent;
        private Animator     _animator;
        private AudioSource  _audio;
        private PhotoSubject _photoSubject;
        private PhotoSystem  _photoSystem;
        private Transform    _playerTransform;
        private float        _wanderTimer;
        private bool         _isDancing;

        private static readonly int SpeedHash       = Animator.StringToHash("Speed");
        private static readonly int MotionSpeedHash = Animator.StringToHash("MotionSpeed");
        private static readonly int DanceHash       = Animator.StringToHash("Dance");

        // ─── Lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            _agent        = GetComponent<NavMeshAgent>();
            _animator     = GetComponentInChildren<Animator>();
            _audio        = GetComponent<AudioSource>();
            _photoSubject = GetComponent<PhotoSubject>();
        }

        private void Start()
        {
            _agent.speed = walkSpeed;
            _wanderTimer = Random.Range(0f, wanderInterval); // stagger les PNJ pour qu'ils ne bougent pas tous en même temps

            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null) _playerTransform = playerGO.transform;

            // Connecter l'AudioSource au groupe SFX du mixer → volume SFX affecte tous les NPCs
            // spatialBlend = 1 : son 3D, émane de la position du NPC (pas entendu à l'autre bout de la map)
            if (_audio != null)
            {
                _audio.spatialBlend = 1f;
                _audio.maxDistance  = 15f;
                _audio.rolloffMode  = AudioRolloffMode.Linear;
                if (AudioManager.Instance != null)
                {
                    var sfxGroup = AudioManager.Instance.GetSFXMixerGroup();
                    if (sfxGroup != null) _audio.outputAudioMixerGroup = sfxGroup;
                }
            }

            _photoSystem = FindFirstObjectByType<PhotoSystem>();
            if (_photoSystem != null)
                _photoSystem.onPhotoTaken.AddListener(OnPhotoTaken);
        }

        private void OnDestroy()
        {
            if (_photoSystem != null)
                _photoSystem.onPhotoTaken.RemoveListener(OnPhotoTaken);
        }

        private void Update()
        {
            if (Time.timeScale == 0f) return;

            UpdateAnimation();

            if (_state == NpcState.Wander)
                UpdateWander();
        }

        // ─── Déambulation ──────────────────────────────────────────────────

        private void UpdateWander()
        {
            if (_agent.pathPending) return;

            if (_agent.remainingDistance < 0.5f)
            {
                _wanderTimer -= Time.deltaTime;
                if (_wanderTimer <= 0f)
                {
                    _wanderTimer = wanderInterval + Random.Range(-1f, 1f);
                    _agent.speed = walkSpeed;
                    MoveToRandomPoint();
                }
            }
        }

        private void MoveToRandomPoint()
        {
            Vector3 randomPos = transform.position + Random.insideUnitSphere * wanderRadius;
            if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
                _agent.SetDestination(hit.position);
        }

        // ─── Réaction photo ────────────────────────────────────────────────

        private void OnPhotoTaken(PhotoResult result)
        {
            // Déjà en train de réagir → ignorer
            if (_state != NpcState.Wander) return;

            // Vérifier que CE PNJ est bien dans la photo
            if (_photoSubject == null) return;

            bool inPhoto = false;
            foreach (var s in result.subjects)
            {
                if (s.subject == _photoSubject) { inPhoto = true; break; }
            }
            if (!inPhoto) return;

            TriggerReaction();
        }

        private void TriggerReaction()
        {
            float flee, angry;

            switch (personality)
            {
                case NpcPersonality.Shy:
                    flee  = 0.65f;
                    angry = 0.05f;
                    break;
                case NpcPersonality.Aggressive:
                    flee  = 0.10f;
                    angry = 0.65f;
                    break;
                default: // Neutral
                    flee  = chanceFlee;
                    angry = Mathf.Max(0f, 1f - chanceIgnore - chanceFlee);
                    break;
            }

            float roll = Random.value;

            if (roll < flee)
                StartCoroutine(FleeRoutine());
            else if (roll < flee + angry)
                StartCoroutine(AngryRoutine());
            else
                ReactNeutral();
        }

        // ─── Réactions ─────────────────────────────────────────────────────

        private IEnumerator FleeRoutine()
        {
            _state = NpcState.Flee;

            // ── Phase 1 : réaction de peur sur place ───────────────────────
            _agent.ResetPath();
            _agent.velocity = Vector3.zero;

            // Se tourne vers le joueur (ou tourne le dos — ici on se retourne brusquement)
            if (_playerTransform != null)
            {
                Vector3 dir = _playerTransform.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(dir);
            }

            PlayRandom(fleeClips);
            SetAnimParam(fleeAnimParam, true);  // joue l'animation "Flee/Fear"

            // Attendre que l'animation de peur se termine
            yield return new WaitForSeconds(fearReactionDuration);

            SetAnimParam(fleeAnimParam, false); // reset le trigger/bool

            // ── Phase 2 : fuite en courant ─────────────────────────────────
            _agent.speed = fleeSpeed;

            float elapsed = 0f;
            while (elapsed < fleeDuration)
            {
                if (_playerTransform != null)
                {
                    // Recalcule la direction de fuite toutes les 0.8s
                    Vector3 away = transform.position +
                                   (transform.position - _playerTransform.position).normalized * 10f;
                    if (NavMesh.SamplePosition(away, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                        _agent.SetDestination(hit.position);
                }
                elapsed += 0.8f;
                yield return new WaitForSeconds(0.8f);
            }

            // ── Phase 3 : retour à la déambulation normale ─────────────────
            _agent.speed = walkSpeed;
            _state = NpcState.Wander;
        }

        private IEnumerator AngryRoutine()
        {
            _state = NpcState.Angry;
            _agent.ResetPath();
            _agent.velocity = Vector3.zero;

            // Se tourne vers le joueur
            if (_playerTransform != null)
            {
                Vector3 dir = _playerTransform.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(dir);
            }

            PlayRandom(angryClips);
            SetAnimParam(angryAnimParam, true);

            yield return new WaitForSeconds(2.5f);

            SetAnimParam(angryAnimParam, false);
            _state = NpcState.Wander;
        }

        private void ReactNeutral()
        {
            PlayRandom(neutralClips);
            // Continue à déambuler normalement — pas de changement d'état
        }

        // ─── Danse (appelé par DanceZone) ──────────────────────────────────

        /// <summary>
        /// Appelé par DanceZone quand le NPC entre/sort de la zone musicale.
        /// Le NPC danse uniquement s'il est en mode Wander ET à l'arrêt.
        /// </summary>
        public void SetDancing(bool dance)
        {
            _isDancing = dance;
            // Si on arrête de danser, on désactive immédiatement le paramètre
            if (!dance && _animator != null)
                _animator.SetBool(DanceHash, false);
        }

        // ─── Animation ─────────────────────────────────────────────────────

        private void UpdateAnimation()
        {
            if (_animator == null) return;

            float speed = _agent.velocity.magnitude;
            _animator.SetFloat(SpeedHash,       speed, 0.1f, Time.deltaTime);
            _animator.SetFloat(MotionSpeedHash, 1f);

            // Danse : seulement quand en mode Wander et quasi-immobile
            if (_state == NpcState.Wander)
            {
                bool shouldDance = _isDancing && speed < 0.1f;
                _animator.SetBool(DanceHash, shouldDance);
            }
        }

        private void SetAnimParam(string paramName, bool state)
        {
            if (_animator == null || string.IsNullOrEmpty(paramName)) return;

            // Cherche si le paramètre existe avant de l'appliquer (évite les erreurs console)
            foreach (var p in _animator.parameters)
            {
                if (p.name != paramName) continue;

                if (p.type == AnimatorControllerParameterType.Trigger)
                {
                    if (state) _animator.SetTrigger(paramName);
                    else       _animator.ResetTrigger(paramName);
                }
                else if (p.type == AnimatorControllerParameterType.Bool)
                {
                    _animator.SetBool(paramName, state);
                }
                return;
            }
        }

        // ─── Audio réactions ───────────────────────────────────────────────

        private void PlayRandom(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0 || _audio == null) return;
            var clip = clips[Random.Range(0, clips.Length)];
            if (clip != null) _audio.PlayOneShot(clip);
        }

        // ─── Sons de pas — Animation Events ────────────────────────────────
        // Appelés automatiquement par l'Animation Event "OnFootstep" dans les clips.
        // CAS A : l'Animator est sur le même GameObject que NpcController → méthodes appelées directement.
        // CAS B : l'Animator est sur un enfant → ajoute NpcFootstepRelay sur cet enfant,
        //         il transférera l'appel ici via ForwardFootstep / ForwardLand.

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight < 0.5f) return;
            PlayFootstep();
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight < 0.5f) return;
            PlayFootstep(); // même son pour l'atterrissage (ou assigne un clip séparé si besoin)
        }

        /// <summary>Appelé par NpcFootstepRelay quand l'Animator est sur un enfant.</summary>
        public void ForwardFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight < 0.5f) return;
            PlayFootstep();
        }

        /// <summary>Appelé par NpcFootstepRelay pour le landing.</summary>
        public void ForwardLand(AnimationEvent animationEvent) => PlayFootstep();

        private void PlayFootstep()
        {
            if (footstepClips == null || footstepClips.Length == 0 || _audio == null) return;
            var clip = footstepClips[Random.Range(0, footstepClips.Length)];
            if (clip == null) return;

            // PlayOneShot via l'AudioSource du NPC → passe par le groupe SFX du mixer
            float vol = footstepVolume * (_state == NpcState.Flee ? fleeFootstepMultiplier : 1f);
            _audio.PlayOneShot(clip, vol);
        }

        // ─── Editor gizmos ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            UnityEditor.Handles.color = new Color(0f, 1f, 0.5f, 0.15f);
            UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, wanderRadius);
            UnityEditor.Handles.color = new Color(0f, 1f, 0.5f, 0.8f);
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, wanderRadius);
        }
#endif
    }
}
