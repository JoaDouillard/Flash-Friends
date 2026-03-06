using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlashFriends
{
    /// <summary>
    /// Intro panel shown at the start of the game scene.
    /// Displays a short welcome message, then auto-hides after a few seconds.
    /// The game timer is paused during the intro and resumed on close.
    ///
    /// SETUP :
    /// 1. Add this script to the Canvas (or any always-active GameObject).
    ///    Do NOT place it on the panel itself.
    /// 2. Assign introPanel, messageText, and optionally skipButton in the Inspector.
    ///
    /// HIERARCHY EXAMPLE :
    ///   OverlayCanvas              ← attach GameIntroPanel script here
    ///   └─ IntroPanel              ← introPanel (active at scene start)
    ///       ├─ BackgroundImage     [Image — semi-transparent black]
    ///       ├─ MessageText         [TextMeshProUGUI] ← messageText
    ///       └─ SkipButton          [Button "Tap to start"] ← skipButton (optional)
    /// </summary>
    public class GameIntroPanel : MonoBehaviour
    {
        [Header("Panel")]
        [Tooltip("The intro panel GameObject (should be ACTIVE at scene start).")]
        [SerializeField] private GameObject introPanel;

        [Tooltip("Text component for the intro message.")]
        [SerializeField] private TextMeshProUGUI messageText;

        [Tooltip("Optional button to skip the intro early.")]
        [SerializeField] private Button skipButton;

        [Header("Settings")]
        [Tooltip("How long the panel stays visible (seconds, real-time).")]
        [SerializeField] private float displayDuration = 5f;

        [Tooltip("Intro message shown to the player.")]
        [TextArea(3, 8)]
        [SerializeField] private string introMessage =
            "Welcome to the festival!\n\n" +
            "You have 24 hours to capture the best moments.\n" +
            "The better your photos, the higher your Good Vibes score.\n\n" +
            "Complete all your quests, then return to the FESTIVAL ENTRANCE to finish the game!\n\n" +
            "Tap to start →";

        // ─── Lifecycle ─────────────────────────────────────────────────────

        private void Start()
        {
            if (introPanel == null)
            {
                Debug.LogWarning("[GameIntroPanel] introPanel not assigned — skipping intro.");
                return;
            }

            // Pause the game timer while intro is shown
            GameTimer.Instance?.PauseTimer();

            introPanel.SetActive(true);
            Time.timeScale   = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            if (messageText != null)
                messageText.text = introMessage;

            if (skipButton != null)
                skipButton.onClick.AddListener(HideIntro);

            StartCoroutine(AutoHide());
        }

        // ─── Auto-hide ─────────────────────────────────────────────────────

        private IEnumerator AutoHide()
        {
            // WaitForSecondsRealtime ignores Time.timeScale = 0
            yield return new WaitForSecondsRealtime(displayDuration);
            HideIntro();
        }

        private void HideIntro()
        {
            StopAllCoroutines();

            if (introPanel != null) introPanel.SetActive(false);

            Time.timeScale   = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;

            // Resume the game timer
            GameTimer.Instance?.ResumeTimer();
        }
    }
}
