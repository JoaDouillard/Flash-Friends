using UnityEngine;

namespace FlashFriends
{
    /// <summary>
    /// Positions the FP camera eye point to follow the character's head.
    ///
    /// Strategy:
    ///   • X, Z — copied from the head bone (follows walk sway, body lean, etc.)
    ///   • Y    — derived from CharacterController.height so crouching correctly
    ///            lowers the camera regardless of what the crouch animation does to the head bone.
    ///
    /// SETUP :
    /// 1. Create a child of PlayerCameraRoot named "FPEyeAnchor".
    /// 2. Attach this script.
    /// 3. Assign headBone (e.g. mixamorig:Head) in the Inspector.
    /// 4. Drag "FPEyeAnchor" into CameraManager → Fp Eye Target.
    /// </summary>
    public class FPEyeAnchor : MonoBehaviour
    {
        [Tooltip("Head bone to follow for X/Z position (e.g. mixamorig:Head).")]
        public Transform headBone;

        [Tooltip("Eye height as a fraction of CharacterController total height. " +
                 "0.88 = eye at 88% of total standing height (~1.58 m for a 1.8 m capsule).")]
        [Range(0.5f, 1f)]
        public float eyeHeightFraction = 0.88f;

        // ─── Cached refs ───────────────────────────────────────────────────

        private CharacterController _cc;
        private Transform           _ccRoot;

        // ─── Lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            _cc = GetComponentInParent<CharacterController>();
            if (_cc != null) _ccRoot = _cc.transform;
        }

        private void LateUpdate()
        {
            Vector3 pos;

            if (_cc != null && headBone != null)
            {
                // Y: from CharacterController height — correctly tracks crouch state
                //    Formula: capsule center Y + height * (fraction - 0.5)
                //    Works regardless of whether the CC pivot is at feet or center.
                float eyeY = _ccRoot.position.y
                           + _cc.center.y
                           + _cc.height * (eyeHeightFraction - 0.5f);

                // X, Z: from head bone — follows body sway, lateral lean
                pos = new Vector3(headBone.position.x, eyeY, headBone.position.z);
            }
            else if (headBone != null)
            {
                // No CharacterController: pure bone tracking
                pos = headBone.position;
            }
            else if (_cc != null)
            {
                // No head bone: pure CC-based height
                float eyeY = _ccRoot.position.y
                           + _cc.center.y
                           + _cc.height * (eyeHeightFraction - 0.5f);
                pos = new Vector3(_ccRoot.position.x, eyeY, _ccRoot.position.z);
            }
            else
            {
                return; // Nothing to follow
            }

            transform.position = pos;
        }
    }
}
