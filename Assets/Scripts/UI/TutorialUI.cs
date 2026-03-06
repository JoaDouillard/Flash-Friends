using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FlashFriends
{
    /// <summary>
    /// Tutorial panel UI — accessible from the Main Menu and the Settings menu.
    /// Manages its panel directly via SetActive, no UIManager dependency.
    ///
    /// SETUP :
    /// 1. Attach this script to a GameObject inside the Canvas.
    /// 2. Drag the TutorialPanel (disabled by default) into "Tutorial Panel".
    /// 3. Assign buttons and text fields in the Inspector.
    /// </summary>
    public class TutorialUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Tutorial panel (disabled by default).")]
        [SerializeField] private GameObject tutorialPanel;

        [Tooltip("Current page title.")]
        [SerializeField] private TextMeshProUGUI titleText;

        [Tooltip("Scrollable page content.")]
        [SerializeField] private TextMeshProUGUI contentText;

        [Header("Navigation")]
        [SerializeField] private Button previousPageButton;
        [SerializeField] private Button nextPageButton;
        [Tooltip("Page indicator text (e.g. '1 / 5').")]
        [SerializeField] private TextMeshProUGUI pageIndicatorText;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;

        // ─── State ─────────────────────────────────────────────────────────

        private System.Action _onCloseCallback;
        private int           _currentPage;

        // ─── Page content ──────────────────────────────────────────────────

        private readonly string[] _pages =
        {
            // Page 1 — Movement controls
            @"<size=28><b>CONTROLS — MOVEMENT</b></size>

<b>Move:</b>
<b>W</b>     — Move Forward
<b>S</b>     — Move Backward
<b>A</b>     — Move Left
<b>D</b>     — Move Right

<b>Actions:</b>
<b>Shift</b>  — Sprint
<b>Ctrl</b>   — Crouch (hold)
<b>Space</b>  — Jump

<b>Camera:</b>
<b>Mouse</b>  — Look around
<b>V</b>      — Toggle view (3rd person / 1st person)",

            // Page 2 — Phone camera mode
            @"<size=28><b>PHONE CAMERA MODE</b></size>

<b>Q</b> — Take out / put away the phone

Once the phone is out:
<b>Scroll wheel</b> — Zoom in / out
<b>Left click</b>  — Take a photo

<b>Tips:</b>
- Get close to your subject
- Center it in the frame
- Use zoom to reframe distant subjects

<b>Tab</b> — Open / close the Gallery
<i>Warning: the game does NOT pause while the gallery is open!</i>",

            // Page 3 — Scoring
            @"<size=28><b>SCORING — GOOD VIBES</b></size>

Each photo earns <b>Good Vibes</b> based on:

<b>Base:</b> 100 pts per subject in frame

<b>Bonuses:</b>
• <b>Centering</b>  — up to +50 pts (subject centered)
• <b>Distance</b>   — up to +50 pts (optimal range ~5 m)
• <b>Frame size</b> — up to +50 pts (subject fills the frame)
• <b>NPC</b>        — +30 pts
• <b>POI</b>        — +20 pts
• <b>Object</b>     — +10 pts
• <b>Landmark</b>   — +15 pts

<b>Multi-subject bonus:</b> +25 pts per extra subject",

            // Page 4 — Gallery & subjects
            @"<size=28><b>GALLERY & SUBJECTS</b></size>

<b>Tab</b> — Open / close the Gallery
The gallery shows all the photos you have taken.

<b>Photographable subjects:</b>
• <b>NPC</b>      — Festival characters (best score)
• <b>POI</b>      — Notable points of interest
• <b>Landmark</b> — Iconic city locations
• <b>Object</b>   — Decorative items and props

<b>Interaction:</b>
<b>E</b> — Interact with a nearby element",

            // Page 5 — Tips
            @"<size=28><b>PHOTOGRAPHER TIPS</b></size>

<b>To maximize your Good Vibes:</b>
✓ Center the subject in the frame
✓ Aim for the right distance (~5 m ideal)
✓ Capture multiple subjects in one shot
✓ Explore the festival to discover POIs

<b>Movement:</b>
✓ Use 1st person view (V) for precise aiming
✓ Crouch (Ctrl) for creative low-angle shots
✓ Zoom helps with subjects far away

<b>Good luck and happy shooting!</b>"
        };

        // ─── Lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            if (closeButton        != null) closeButton.onClick.AddListener(OnCloseClicked);
            if (previousPageButton != null) previousPageButton.onClick.AddListener(OnPreviousClicked);
            if (nextPageButton     != null) nextPageButton.onClick.AddListener(OnNextClicked);
        }

        // ─── Public API ────────────────────────────────────────────────────

        /// <summary>Opens the tutorial. The callback is invoked on close.</summary>
        public void OpenTutorial(System.Action onClose = null)
        {
            _onCloseCallback = onClose;
            if (tutorialPanel != null) tutorialPanel.SetActive(true);
            _currentPage = 0;
            ShowPage(_currentPage);
        }

        /// <summary>Closes the tutorial and invokes the back callback.</summary>
        public void CloseTutorial()
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            _onCloseCallback?.Invoke();
        }

        // ─── Page display ──────────────────────────────────────────────────

        private void ShowPage(int index)
        {
            if (index < 0 || index >= _pages.Length) return;
            _currentPage = index;

            if (titleText != null)
                titleText.text = $"Tutorial — Page {_currentPage + 1}";

            if (contentText != null)
                contentText.text = _pages[_currentPage];

            if (pageIndicatorText != null)
                pageIndicatorText.text = $"{_currentPage + 1} / {_pages.Length}";

            if (previousPageButton != null)
                previousPageButton.interactable = _currentPage > 0;

            if (nextPageButton != null)
                nextPageButton.interactable = _currentPage < _pages.Length - 1;
        }

        // ─── Button callbacks ──────────────────────────────────────────────

        private void OnCloseClicked()    => CloseTutorial();
        private void OnPreviousClicked() { if (_currentPage > 0)                 ShowPage(_currentPage - 1); }
        private void OnNextClicked()     { if (_currentPage < _pages.Length - 1) ShowPage(_currentPage + 1); }
    }
}
