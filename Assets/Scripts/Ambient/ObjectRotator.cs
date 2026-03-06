using UnityEngine;

namespace FlashFriends
{
    /// <summary>
    /// Rotates a GameObject continuously on one or more axes at a constant speed.
    /// Useful for Ferris wheels, fans, windmills, spinning decorations, etc.
    ///
    /// SETUP : attach to the object to rotate, tick the desired axes, set speed.
    /// Space.Self = rotates around the object's own axes (recommended for most cases).
    /// Space.World = rotates around world axes.
    /// </summary>
    public class ObjectRotator : MonoBehaviour
    {
        [Header("Axes")]
        [Tooltip("Rotate around the object's local X axis.")]
        public bool rotateX;

        [Tooltip("Rotate around the object's local Y axis (vertical spin).")]
        public bool rotateY;

        [Tooltip("Rotate around the object's local Z axis (side roll — good for Ferris wheels).")]
        public bool rotateZ = true;

        [Header("Speed")]
        [Tooltip("Rotation speed in degrees per second. Negative = reverse direction.")]
        public float speed = 30f;

        [Header("Options")]
        [Tooltip("Rotation space. Self = object's own axes. World = world axes.")]
        public Space rotationSpace = Space.Self;

        [Tooltip("If ticked, rotation pauses when Time.timeScale == 0 (during menus).")]
        public bool pauseWithGame = true;

        // ─── Update ────────────────────────────────────────────────────────

        private void Update()
        {
            if (pauseWithGame && Time.timeScale == 0f) return;

            float delta = speed * (pauseWithGame ? Time.deltaTime : Time.unscaledDeltaTime);

            Vector3 axis = new Vector3(
                rotateX ? 1f : 0f,
                rotateY ? 1f : 0f,
                rotateZ ? 1f : 0f);

            if (axis == Vector3.zero) return;

            transform.Rotate(axis * delta, rotationSpace);
        }
    }
}
