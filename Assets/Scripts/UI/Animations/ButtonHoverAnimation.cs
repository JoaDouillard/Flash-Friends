using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using LitMotion;
using LitMotion.Extensions;

namespace FlashFriends
{
    /// <summary>
    /// Animation hover et click pour les boutons UI.
    /// Ajouter ce composant à n'importe quel bouton pour l'animer automatiquement.
    /// Ignoré par timeScale (fonctionnel en pause).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonHoverAnimation : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler,  IPointerUpHandler
    {
        [Header("Hover")]
        [SerializeField] private float hoverScale    = 1.1f;
        [SerializeField] private float hoverDuration = 0.2f;
        [SerializeField] private Ease  hoverEase     = Ease.OutCubic;

        [Header("Click")]
        [SerializeField] private float clickScale    = 0.95f;
        [SerializeField] private float clickDuration = 0.1f;

        private RectTransform rectTransform;
        private Vector3       originalScale;
        private MotionHandle  scaleMotion;
        private bool          isHovering;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            originalScale = rectTransform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            CancelMotion();
            isHovering  = true;
            scaleMotion = LMotion.Create(rectTransform.localScale, originalScale * hoverScale, hoverDuration)
                .WithEase(hoverEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToLocalScale(rectTransform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            CancelMotion();
            isHovering  = false;
            scaleMotion = LMotion.Create(rectTransform.localScale, originalScale, hoverDuration)
                .WithEase(Ease.InCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToLocalScale(rectTransform);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            CancelMotion();
            scaleMotion = LMotion.Create(rectTransform.localScale, originalScale * clickScale, clickDuration)
                .WithEase(Ease.OutQuad)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToLocalScale(rectTransform);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            CancelMotion();
            Vector3 target = isHovering ? originalScale * hoverScale : originalScale;
            scaleMotion = LMotion.Create(rectTransform.localScale, target, clickDuration)
                .WithEase(Ease.OutBack)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToLocalScale(rectTransform);
        }

        private void CancelMotion()
        {
            if (scaleMotion.IsActive()) scaleMotion.Cancel();
        }

        private void OnDestroy() => CancelMotion();
    }
}
