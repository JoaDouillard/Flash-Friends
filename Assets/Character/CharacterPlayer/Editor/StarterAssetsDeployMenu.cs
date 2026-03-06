using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Unity.Cinemachine;

namespace FlashFriends
{
    // Ce script est un outil éditeur pour déployer le player dans la scène.
    public partial class FlashFriendsDeployMenu : ScriptableObject
    {
        public const string MenuRoot = "Tools/Flash Friends";

        // Noms des prefabs
        private const string MainCameraPrefabName      = "MainCamera";
        private const string PlayerCapsulePrefabName   = "PlayerCapsule"; 

        // Noms dans la hiérarchie
        private const string CinemachineVirtualCameraName = "PlayerFollowCamera";

        // Tags
        private const string PlayerTag           = "Player";
        private const string MainCameraTag        = "MainCamera";
        private const string CinemachineTargetTag = "CinemachineTarget";

        private static GameObject _cinemachineVirtualCamera;

        private static void CheckCameras(Transform targetParent, string prefabFolder)
        {
            CheckMainCamera(prefabFolder);

            GameObject vcam = GameObject.Find(CinemachineVirtualCameraName);

            if (!vcam)
            {
                if (TryLocatePrefab(CinemachineVirtualCameraName, new string[] { prefabFolder },
                        new[] { typeof(CinemachineCamera) }, out GameObject vcamPrefab, out string _))
                {
                    HandleInstantiatingPrefab(vcamPrefab, out vcam);
                    _cinemachineVirtualCamera = vcam;
                }
                else
                {
                    Debug.LogError("Impossible de trouver le prefab CinemachineCamera");
                }
            }
            else
            {
                _cinemachineVirtualCamera = vcam;
            }

            GameObject[] targets = GameObject.FindGameObjectsWithTag(CinemachineTargetTag);
            GameObject target = targets.FirstOrDefault(t => t.transform.IsChildOf(targetParent));
            if (target == null)
            {
                target = new GameObject("PlayerCameraRoot");
                target.transform.SetParent(targetParent);
                target.transform.localPosition = new Vector3(0f, 1.375f, 0f);
                target.tag = CinemachineTargetTag;
                Undo.RegisterCreatedObjectUndo(target, "Created new cinemachine target");
            }

            CheckVirtualCameraFollowReference(target, _cinemachineVirtualCamera);
        }

        private static void CheckMainCamera(string inFolder)
        {
            GameObject[] mainCameras = GameObject.FindGameObjectsWithTag(MainCameraTag);

            if (mainCameras.Length < 1)
            {
                if (TryLocatePrefab(MainCameraPrefabName, new string[] { inFolder },
                        new[] { typeof(CinemachineBrain), typeof(Camera) }, out GameObject camera, out string _))
                {
                    HandleInstantiatingPrefab(camera, out _);
                }
                else
                {
                    Debug.LogError("Impossible de trouver le prefab MainCamera");
                }
            }
            else
            {
                if (!mainCameras[0].TryGetComponent(out CinemachineBrain _))
                    mainCameras[0].AddComponent<CinemachineBrain>();
            }
        }

        private static void CheckVirtualCameraFollowReference(GameObject target, GameObject cinemachineVirtualCamera)
        {
            var serializedObject = new SerializedObject(cinemachineVirtualCamera.GetComponent<CinemachineCamera>());
            var serializedProperty = serializedObject.FindProperty("m_Follow");
            serializedProperty.objectReferenceValue = target.transform;
            serializedObject.ApplyModifiedProperties();
        }

        private static bool TryLocatePrefab(string name, string[] inFolders,
            Type[] requiredComponentTypes, out GameObject prefab, out string path)
        {
            string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab", inFolders);
            for (int i = 0; i < allPrefabs.Length; ++i)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(allPrefabs[i]);

                if (assetPath.Contains("/com.unity.starter-assets/"))
                {
                    Object loadedObj = AssetDatabase.LoadMainAssetAtPath(assetPath);

                    if (PrefabUtility.GetPrefabAssetType(loadedObj) != PrefabAssetType.NotAPrefab &&
                        PrefabUtility.GetPrefabAssetType(loadedObj) != PrefabAssetType.MissingAsset)
                    {
                        GameObject loadedGo = loadedObj as GameObject;
                        bool hasAll = true;
                        foreach (var t in requiredComponentTypes)
                        {
                            if (!loadedGo.TryGetComponent(t, out _)) { hasAll = false; break; }
                        }
                        if (hasAll && loadedGo.name == name)
                        {
                            prefab = loadedGo;
                            path   = assetPath;
                            return true;
                        }
                    }
                }
            }

            prefab = null;
            path   = null;
            return false;
        }

        private static void HandleInstantiatingPrefab(GameObject prefab, out GameObject prefabInstance)
        {
            prefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(prefabInstance, "Instantiate Flash Friends Prefab");
            prefabInstance.transform.localPosition    = Vector3.zero;
            prefabInstance.transform.localEulerAngles = Vector3.zero;
            prefabInstance.transform.localScale       = Vector3.one;
        }
    }
}
