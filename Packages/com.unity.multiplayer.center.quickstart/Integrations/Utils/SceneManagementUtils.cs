using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Center
{
    internal static class SceneManagementUtils
    {
        /// <summary>
        /// Creates a new scene at the given path with the given name, opens it (single mode)
        /// and sets it active. Newly created GameObjects will be created to this scene.
        /// </summary>
        /// <param name="sceneName">Scene name without extension</param>
        /// <param name="scenePath">Path to folder in which the scene should be created (under Assets)</param>
        /// <returns>The created scene</returns>
        /// <exception cref="ArgumentException">When the path is empty</exception>
        public static Scene SwitchToNewEmptyScene(string sceneName, string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
                throw new ArgumentException("Scene path cannot be empty!");
            var newScene = CreateScene(sceneName, scenePath, NewSceneMode.Single);
            SceneManager.SetActiveScene(newScene);
            return newScene;
        }

        /// <summary>
        /// Create and save new scene. It handles setting the name, path, mode and setup.
        /// It does not set the scene active.
        /// </summary>
        /// <param name="name">Scene Name without extension</param>
        /// <param name="path">Path to folder in which the scene should be created (under Assets)</param>
        /// <param name="mode">To control whether the scene is created alone or additively</param>
        /// <param name="setup">Empty or with default gameobjects</param>
        /// <returns>The created scene</returns>
        public static Scene CreateScene(string name, string path, NewSceneMode mode = NewSceneMode.Additive,
            NewSceneSetup setup = NewSceneSetup.DefaultGameObjects)
        {
            var newScene = EditorSceneManager.NewScene(setup, mode);
            newScene.name = name;
            Directory.CreateDirectory(path);
            var scenePath = $"{path}/{name}.unity";
            EditorSceneManager.SaveScene(newScene, scenePath);
            return newScene;
        }

        /// <summary>
        /// Forces saving all open scenes
        /// </summary>
        public static void SaveAllScenes()  
        {  
            EditorSceneManager.MarkAllScenesDirty();  
            EditorSceneManager.SaveOpenScenes();  
        }

        public static bool EnsureTheCurrentScenesAreSaved()
        {
            if (IsThereASceneToSave())
            {
                if(!EditorUtility.DisplayDialog("Save Scene(s)", "Some scene needs to be saved before proceeding. Continue?", "Yes", "Cancel"))
                    return false;
                
                SaveAllScenes();
            }

            return true;
        }
        
        static bool IsThereASceneToSave()
        {
            var sceneCount = SceneManager.sceneCount;
            for(var i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if(scene.isDirty || string.IsNullOrEmpty(scene.path))
                    return true;
            }

            return false;
        }
    }
}
