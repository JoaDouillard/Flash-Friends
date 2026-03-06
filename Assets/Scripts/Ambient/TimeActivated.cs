using UnityEngine;

namespace FlashFriends
{
    /// <summary>
    /// Activates or deactivates a list of GameObjects based on the in-game time
    /// from DayNightCycle. Useful for streetlights (on at night), shop signs,
    /// stage lighting, etc.
    ///
    /// SETUP :
    /// 1. Attach to any always-active GameObject.
    /// 2. Drag the objects to control into the "targets" list.
    /// 3. Set activeDuringNight (or activeDuringDay) to match your use-case.
    ///
    /// Requires DayNightCycle to be present in the scene.
    /// Falls back to always-on if DayNightCycle is missing.
    /// </summary>
    public class TimeActivated : MonoBehaviour
    {
        [Tooltip("Objects to activate/deactivate based on time of day.")]
        [SerializeField] private GameObject[] targets;

        [Tooltip("If true, objects are ON during night and OFF during day (e.g. streetlights). " +
                 "If false, objects are ON during day and OFF at night.")]
        [SerializeField] private bool activeAtNight = true;

        // ─── Update ────────────────────────────────────────────────────────

        private void Update()
        {
            if (DayNightCycle.Instance == null) return;

            bool shouldBeActive = activeAtNight
                ? DayNightCycle.Instance.IsNight()
                : DayNightCycle.Instance.IsDay();

            foreach (var go in targets)
                if (go != null && go.activeSelf != shouldBeActive)
                    go.SetActive(shouldBeActive);
        }
    }
}
