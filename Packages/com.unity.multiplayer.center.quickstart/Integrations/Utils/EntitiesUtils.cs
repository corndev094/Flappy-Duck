#if ENTITIES_INSTALLED && N4E_INSTALLED
using System;
using System.IO;
using System.Reflection;
using Unity.NetCode;
using Unity.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Center.Integrations
{
    internal static class EntitiesUtils
    {
        public static SubScene CreateSubScene(string dstPath, Scene parentScene, GameObject[] topLevelObjects)
        {
            if (File.Exists(dstPath))
                throw new InvalidOperationException("Sub Scene already exists.");

            Directory.CreateDirectory(Path.GetDirectoryName(dstPath));
            var success = CreateSceneFile(dstPath);
            if (!success)
                throw new InvalidOperationException("Creating Sub Scene failed.");

            var scene = EditorSceneManager.OpenScene(dstPath, OpenSceneMode.Additive);
            scene.isSubScene = true;

            foreach (var go in topLevelObjects)
            {
                go.transform.SetParent(null, true);
                SceneManager.MoveGameObjectToScene(go, scene);
            }

            var name = Path.GetFileNameWithoutExtension(dstPath);
            var gameObject = new GameObject(name, typeof(SubScene));
            gameObject.SetActive(false);
            var subSceneComponent = gameObject.GetComponent<SubScene>();
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(dstPath);
            subSceneComponent.SceneAsset = sceneAsset;
            SceneManager.MoveGameObjectToScene(gameObject, parentScene);
            gameObject.SetActive(true);
            Selection.activeObject = gameObject;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.CloseScene(scene, true);
            return subSceneComponent;
        }

        // Evil? maybe. But it is how it is done in the dots repo.
        static bool CreateSceneFile(string scenePath)
        {
            var createSceneAssetMethod = typeof(EditorSceneManager).GetMethod("CreateSceneAsset", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            var addDefaultGameObjects = false;
            return (bool) createSceneAssetMethod.Invoke(null, new object[] {scenePath, addDefaultGameObjects});
        }

        public static string AnotherBootstrapExists(string ourBootstrapPath, string ourBootStrapFullName)
        {
            var allTypes = TypeCache.GetTypesDerivedFrom<ClientServerBootstrap>();
            
            if (allTypes.Count == 0)
                return null;
            
            var ourFileExists = File.Exists(ourBootstrapPath);
            foreach (var type in allTypes)
            {
                // The type cannot tell us the file path, so we do this approximation
                var isOurFile = ourFileExists && type.FullName == ourBootStrapFullName;
                if (!isOurFile)
                    return $"{type.FullName} in {type.Assembly.GetName().Name}";
            }

            return null;
        }
    }
}
#endif
