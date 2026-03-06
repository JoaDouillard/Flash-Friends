using UnityEditor;
using UnityEngine;

namespace FlashFriends.Editor
{
    /// <summary>
    /// Unity Editor tool — FlashFriends > Setup Street Lamps
    ///
    /// Adds a StreetLamp component + a pre-configured Point Light child to every
    /// SM_FloorProps_Light prefab found under Assets/JC_LP_MegaCity.
    ///
    /// Safe to run multiple times: skips prefabs that already have StreetLamp.
    /// </summary>
    public static class SetupStreetLampsEditor
    {
        private const string SearchRoot      = "Assets/JC_LP_MegaCity/Prefabs/FloorProps";
        private const string PrefabNameFilter = "SM_FloorProps_Light";

        private const float DefaultLightHeight    = 5f;
        private const float DefaultLightRange     = 14f;
        private const float DefaultLightIntensity = 1.5f;
        private static readonly Color DefaultLightColor = new Color(1f, 0.88f, 0.6f);

        [MenuItem("FlashFriends/Setup Street Lamps")]
        public static void SetupStreetLamps()
        {
            string[] guids = AssetDatabase.FindAssets(
                $"t:Prefab {PrefabNameFilter}", new[] { SearchRoot });

            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Setup Street Lamps",
                    $"No prefab matching '{PrefabNameFilter}' found under {SearchRoot}.",
                    "OK");
                return;
            }

            int modified = 0;
            int skipped  = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);

                if (root.GetComponent<StreetLamp>() != null)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                    skipped++;
                    continue;
                }

                // Add StreetLamp to root
                StreetLamp lamp = root.AddComponent<StreetLamp>();
                lamp.lightHeight    = DefaultLightHeight;
                lamp.lightRange     = DefaultLightRange;
                lamp.lightIntensity = DefaultLightIntensity;
                lamp.lightColor     = DefaultLightColor;

                // Create LampLight child
                var lightGO = new GameObject("LampLight");
                lightGO.transform.SetParent(root.transform, false);
                lightGO.transform.localPosition = Vector3.up * DefaultLightHeight;

                Light pointLight = lightGO.AddComponent<Light>();
                pointLight.type      = LightType.Point;
                pointLight.range     = DefaultLightRange;
                pointLight.intensity = DefaultLightIntensity;
                pointLight.color     = DefaultLightColor;

                lamp.lampLight = pointLight;

                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);

                modified++;
                Debug.Log($"[SetupStreetLamps] Configured: {path}");
            }

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Setup Street Lamps",
                $"Done!\n\n  Modified : {modified}\n  Skipped (already set up) : {skipped}",
                "OK");
        }
    }
}
