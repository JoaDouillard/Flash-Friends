using UnityEngine;

namespace FlashFriends
{
    /// <summary>
    /// Zone de son ambiant : un AudioClip se fade-in quand le joueur entre,
    /// et se fade-out quand il sort.
    ///
    /// Idéal pour : scène de concert, stand de nourriture, fontaine, foule, etc.
    ///
    /// SETUP :
    ///   1. Crée un Empty GameObject, ajoute ce script.
    ///   2. Ajoute un Collider (Sphere ou Box) → coche "Is Trigger".
    ///   3. Assigne un AudioClip dans le champ "Clip".
    ///   4. Redimensionne le collider pour définir la zone audible.
    ///   5. Optionnel : ajoute plusieurs AmbientSoundZone sur des objets différents
    ///      pour des ambiances distinctes dans chaque stand/zone.
    ///
    /// NOTE : le volume est géré par ce script (fade 2D).
    /// Si tu veux une atténuation 3D naturelle (plus fort près de la source),
    /// utilise plutôt un AudioSource avec Spatial Blend = 1 sans ce script.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class AmbientSoundZone : MonoBehaviour
    {
        [Header("Son")]
        [Tooltip("Le son ambiant de cette zone.")]
        [SerializeField] private AudioClip clip;

        [Tooltip("Volume maximum quand le joueur est dans la zone.")]
        [Range(0f, 1f)]
        [SerializeField] private float maxVolume = 0.7f;

        [Tooltip("Vitesse du fade in/out (unités de volume par seconde).")]
        [SerializeField] private float fadeSpeed = 1.5f;

        [Tooltip("Si coché, le son joue en boucle. Décocher pour un son ponctuel (ex: applaudissements).")]
        [SerializeField] private bool loop = true;

        [Header("Démarrage")]
        [Tooltip("Si coché, le son démarre même si le joueur n'est pas dans la zone (ex: son de fond constant).")]
        [SerializeField] private bool playOnAwake = false;

        // ─── State ─────────────────────────────────────────────────────────

        private AudioSource _source;
        private bool        _playerInside;

        // ─── Lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;

            _source              = gameObject.AddComponent<AudioSource>();
            _source.clip         = clip;
            _source.loop         = loop;
            _source.spatialBlend = 0f;   // 2D : le fade est géré manuellement par ce script
            _source.volume       = playOnAwake ? maxVolume : 0f;
            _source.playOnAwake  = false;
            _source.Play();

            if (playOnAwake) _playerInside = true;
        }

        private void Update()
        {
            if (_source == null) return;

            float target = _playerInside ? maxVolume : 0f;
            _source.volume = Mathf.MoveTowards(_source.volume, target, fadeSpeed * Time.deltaTime);
        }

        // ─── Trigger ───────────────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;
            _playerInside = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other)) return;
            _playerInside = false;
        }

        private static bool IsPlayer(Collider other)
        {
            if (other.CompareTag("Player")) return true;
            return other.GetComponentInParent<PlayerInputHandler>() != null;
        }

        // ─── API publique ──────────────────────────────────────────────────

        /// <summary>Change le clip à la volée (ex: musique différente le jour/la nuit).</summary>
        public void SetClip(AudioClip newClip)
        {
            if (newClip == clip) return;
            clip         = newClip;
            _source.clip = newClip;
            if (_source.isPlaying || _playerInside) _source.Play();
        }
    }
}
