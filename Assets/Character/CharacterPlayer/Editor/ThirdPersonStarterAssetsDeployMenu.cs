using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlashFriends
{
    public partial class FlashFriendsDeployMenu : ScriptableObject
    {
        private const string PlayerArmaturePrefabName = "PlayerArmature";

        [MenuItem(MenuRoot + "/Reset Third Person Controller Armature", false)]
        static void ResetThirdPersonControllerArmature()
        {
            var controllers = FindObjectsByType<ThirdPersonController>(FindObjectsSortMode.None);
            var player = controllers.FirstOrDefault(c =>
                c.GetComponent<Animator>() && c.CompareTag(PlayerTag));

            GameObject playerGameObject = null;

            if (player == null)
            {
                if (TryLocatePrefab(PlayerArmaturePrefabName, null,
                        new[] { typeof(ThirdPersonController), typeof(PlayerInputHandler) },
                        out GameObject prefab, out string _))
                {
                    HandleInstantiatingPrefab(prefab, out playerGameObject);
                }
                else
                {
                    Debug.LogError("Impossible de trouver le prefab PlayerArmature");
                }
            }
            else
            {
                playerGameObject = player.gameObject;
            }

            if (playerGameObject != null)
                CheckCameras(playerGameObject.transform, GetThirdPersonPrefabPath());
        }

        [MenuItem(MenuRoot + "/Reset Third Person Controller Capsule", false)]
        static void ResetThirdPersonControllerCapsule()
        {
            var controllers = FindObjectsByType<ThirdPersonController>(FindObjectsSortMode.None);
            var player = controllers.FirstOrDefault(c =>
                !c.GetComponent<Animator>() && c.CompareTag(PlayerTag));

            GameObject playerGameObject = null;

            if (player == null)
            {
                if (TryLocatePrefab(PlayerCapsulePrefabName, null,
                        new[] { typeof(ThirdPersonController), typeof(PlayerInputHandler) },
                        out GameObject prefab, out string _))
                {
                    HandleInstantiatingPrefab(prefab, out playerGameObject);
                }
                else
                {
                    Debug.LogError("Impossible de trouver le prefab PlayerCapsule");
                }
            }
            else
            {
                playerGameObject = player.gameObject;
            }

            if (playerGameObject != null)
                CheckCameras(playerGameObject.transform, GetThirdPersonPrefabPath());
        }

        static string GetThirdPersonPrefabPath()
        {
            if (TryLocatePrefab(PlayerArmaturePrefabName, null,
                    new[] { typeof(ThirdPersonController), typeof(PlayerInputHandler) },
                    out GameObject _, out string prefabPath))
            {
                var pathString = new StringBuilder();
                var currentDirectory = new FileInfo(prefabPath).Directory;
                while (currentDirectory.Name != "Packages")
                {
                    pathString.Insert(0, $"/{currentDirectory.Name}");
                    currentDirectory = currentDirectory.Parent;
                }
                pathString.Insert(0, currentDirectory.Name);
                return pathString.ToString();
            }
            return null;
        }
    }
}
