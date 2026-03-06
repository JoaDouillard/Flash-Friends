namespace FlashFriends
{
    /// <summary>
    /// Pont statique entre GameSettings (Assembly-CSharp) et ThirdPersonController
    /// (Unity.StarterAssets). Les asmdefs ne peuvent pas référencer Assembly-CSharp,
    /// donc on passe par ce champ statique accessible des deux côtés.
    ///
    /// GameSettings écrit ici quand la sensibilité change.
    /// ThirdPersonController lit ici chaque frame.
    /// </summary>
    public static class CameraSettings
    {
        /// <summary>
        /// Multiplicateur de sensibilité souris appliqué au delta de la caméra.
        /// 1.0 = comportement par défaut (sensibilité 5 dans les settings).
        /// </summary>
        public static float MouseSensitivityMultiplier = 1f;
    }
}
