using UnityEngine;
using LitMotion;
using LitMotion.Extensions;

namespace FlashFriends
{
    public enum SlideDirection { Right, Left, Up, Down }

    /// <summary>
    /// Animation générique réutilisable pour n'importe quel panel UI.
    /// Combine Fade + Scale + Slide (tous optionnels).
    ///
    /// SETUP :
    /// - Ajouter sur le panel racine (avec RectTransform).
    /// - Configurer les effets voulus dans l'Inspector.
    /// - Appeler Show() / Hide() depuis les autres scripts UI.
    /// </summary>
    public class GenericPanelAnimation : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 0.5f;
        [SerializeField] private Ease  easeType          = Ease.OutCubic;

        [Header("Effects")]
        [SerializeField] private bool enableFade  = true;
        [SerializeField] private bool enableScale = true;
        [SerializeField] private bool enableSlide = false;

        [Header("Scale Settings")]
        [SerializeField] private float initialScale = 0.8f;
        [SerializeField] private float targetScale  = 1f;

        [Header("Slide Settings")]
        [SerializeField] private SlideDirection slideDirection = SlideDirection.Down;
        [SerializeField] private float          slideDistance  = 50f;

        [Header("Auto Animation")]
        [Tooltip("Lance automatiquement Show() quand le GameObject est activé")]
        [SerializeField] private bool autoAnimateOnEnable = true;

        [Header("Time Scale")]
        [Tooltip("Continue l'animation même si le jeu est en pause (Time.timeScale = 0)")]
        [SerializeField] private bool ignoreTimeScale = false;

        private RectTransform rectTransform;
        private CanvasGroup   canvasGroup;
        private MotionHandle  fadeMotion;
        private MotionHandle  scaleMotion;
        private MotionHandle  slideMotion;
        private Vector2       originalPosition;
        private Vector2       hiddenPosition;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup   = GetComponent<CanvasGroup>();

            if (enableFade && canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable   = true;
            }

            if (rectTransform == null)
            {
                Debug.LogError($"[GenericPanelAnimation] RectTransform manquant sur {gameObject.name}!");
                return;
            }

            if (enableSlide)
            {
                originalPosition = rectTransform.anchoredPosition;
                switch (slideDirection)
                {
                    case SlideDirection.Right: hiddenPosition = originalPosition + Vector2.right * slideDistance; break;
                    case SlideDirection.Left:  hiddenPosition = originalPosition + Vector2.left  * slideDistance; break;
                    case SlideDirection.Up:    hiddenPosition = originalPosition + Vector2.up    * slideDistance; break;
                    default:                   hiddenPosition = originalPosition + Vector2.down  * slideDistance; break;
                }
            }
        }

        private void OnEnable()
        {
            if (autoAnimateOnEnable) Show();
        }

        // ─── API publique ─────────────────────────────────────────────────

        public void Show()
        {
            if (rectTransform == null) return;
            CancelAllMotions();
            gameObject.SetActive(true);

            // Fade in
            if (enableFade && canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                var b = LMotion.Create(0f, 1f, animationDuration).WithEase(Ease.OutQuad);
                if (ignoreTimeScale) b = b.WithScheduler(MotionScheduler.UpdateIgnoreTimeScale);
                fadeMotion = b.Bind(a => canvasGroup.alpha = a);
            }

            // Scale in
            if (enableScale)
            {
                rectTransform.localScale = Vector3.one * initialScale;
                var b = LMotion.Create(Vector3.one * initialScale, Vector3.one * targetScale, animationDuration).WithEase(easeType);
                if (ignoreTimeScale) b = b.WithScheduler(MotionScheduler.UpdateIgnoreTimeScale);
                scaleMotion = b.BindToLocalScale(rectTransform);
            }

            // Slide in
            if (enableSlide)
            {
                rectTransform.anchoredPosition = hiddenPosition;
                var b = LMotion.Create(hiddenPosition, originalPosition, animationDuration).WithEase(easeType);
                if (ignoreTimeScale) b = b.WithScheduler(MotionScheduler.UpdateIgnoreTimeScale);
                slideMotion = b.BindToAnchoredPosition(rectTransform);
            }
        }

        public void Hide()
        {
            if (rectTransform == null) return;
            CancelAllMotions();
            float dur = animationDuration * 0.7f;

            // Fade out
            if (enableFade && canvasGroup != null)
            {
                if (!enableScale)
                {
                    var b = LMotion.Create(canvasGroup.alpha, 0f, dur).WithEase(Ease.InQuad)
                        .WithOnComplete(() => { gameObject.SetActive(false); ResetState(); });
                    if (ignoreTimeScale) b = b.WithScheduler(MotionScheduler.UpdateIgnoreTimeScale);
                    fadeMotion = b.Bind(a => canvasGroup.alpha = a);
                }
                else
                {
                    var b = LMotion.Create(canvasGroup.alpha, 0f, dur).WithEase(Ease.InQuad);
                    if (ignoreTimeScale) b = b.WithScheduler(MotionScheduler.UpdateIgnoreTimeScale);
                    fadeMotion = b.Bind(a => canvasGroup.alpha = a);
                }
            }

            // Scale out (gère SetActive + Reset)
            if (enableScale)
            {
                var b = LMotion.Create(rectTransform.localScale, Vector3.one * initialScale, dur).WithEase(Ease.InBack)
                    .WithOnComplete(() => { gameObject.SetActive(false); ResetState(); });
                if (ignoreTimeScale) b = b.WithScheduler(MotionScheduler.UpdateIgnoreTimeScale);
                scaleMotion = b.BindToLocalScale(rectTransform);
            }

            // Slide out
            if (enableSlide)
            {
                var b = LMotion.Create(rectTransform.anchoredPosition, hiddenPosition, dur).WithEase(Ease.InCubic);
                if (ignoreTimeScale) b = b.WithScheduler(MotionScheduler.UpdateIgnoreTimeScale);
                slideMotion = b.BindToAnchoredPosition(rectTransform);
            }
        }

        // ─── Utilitaires ─────────────────────────────────────────────────

        private void ResetState()
        {
            if (rectTransform == null) return;
            rectTransform.localScale = Vector3.one * targetScale;
            if (enableSlide) rectTransform.anchoredPosition = originalPosition;
            if (enableFade && canvasGroup != null) canvasGroup.alpha = 1f;
        }

        private void CancelAllMotions()
        {
            if (fadeMotion.IsActive())  fadeMotion.Cancel();
            if (scaleMotion.IsActive()) scaleMotion.Cancel();
            if (slideMotion.IsActive()) slideMotion.Cancel();
        }

        private void OnDestroy() => CancelAllMotions();
    }
}
