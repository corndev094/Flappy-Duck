using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.Center
{
    internal static class PrefabUtils
    {
        public static GameObject InstantiatePrefabAndUnlink(string path, string newName = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                throw new FileNotFoundException($"Could not find prefab at path {path}");

            var instance = (GameObject) PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            if (newName != null)
                instance.name = newName;

            EditorUtility.SetDirty(instance);
            return instance;
        }

        /// <summary>
        /// Creates a prefab asset from a game object and destroys the game object.
        /// </summary>
        /// <param name="go">The game object to turn into a prefab</param>
        /// <param name="prefabPath">Path relative to project. Should be in asset folder</param>
        /// <returns>The created prefab asset or null</returns>
        public static GameObject TurnIntoPrefab(GameObject go, string prefabPath)
        {
            var directory = Path.GetDirectoryName(prefabPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var prefabAsset = PrefabUtility.SaveAsPrefabAsset(go, prefabPath, out var res);
            Object.DestroyImmediate(go);
            return prefabAsset;
        }
    }
}
