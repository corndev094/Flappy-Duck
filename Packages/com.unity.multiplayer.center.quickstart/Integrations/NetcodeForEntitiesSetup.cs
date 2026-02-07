#if N4E_INSTALLED && ENTITIES_GRAPHICS_INSTALLED
using System;
using System.IO;
using Unity.Multiplayer.Center.Common;
using Unity.Multiplayer.Center.Integrations.DynamicSample;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Unity.NetCode;

namespace Unity.Multiplayer.Center.Integrations
{
    /// <summary>
    /// Setup for Netcode for entities. It consists of a connection UI that can start a host/client and two
    /// synchronized cubes.
    /// The setup requires the Entities Graphics package to be installed.
    /// </summary>
    [Serializable]
    internal class NetcodeForEntitiesSetup : IDynamicSample
    {
        internal const string TargetSetupFolder = "Assets/NetcodeForEntitiesSetup/";
        internal const string CustomBootStrapFullName = k_TargetNamespace + "NetCodeBootstrap";

        const string k_LinkToUIPrefab = Paths.PathInPackage + "Runtime/Prefabs/TemporaryUI.prefab";
        const string k_PathInPackage = Paths.PathInPackage + "Samples~/NetcodeForEntitiesSetup/";

        const string k_PlayerPrefabPath = TargetSetupFolder + "Prefabs/PlayerPrefab.prefab";
        const string k_TargetScenesPath = TargetSetupFolder + "Scenes/";
        const string k_ConnectionUISceneName = "ConnectionUI";
        const string k_GameplaySceneName = "Gameplay";
        const string k_SubScenePath = k_TargetScenesPath + "GameplaySubScene.unity";
        const string k_GameplayScenePath = k_TargetScenesPath + k_GameplaySceneName + ".unity";
        const string k_ConnectionUIScenePath = k_TargetScenesPath + k_ConnectionUISceneName + ".unity";

        const string k_ConnectionUiName = "Connection UI"; // game object name
        const string k_TargetNamespace = "Unity.Multiplayer.Center.NetcodeForEntitiesSetup.";
        const string k_CustomBootStrapPath = TargetSetupFolder + "Scripts/CustomBootstrap.cs";
        
        const string k_RenderPipelineTemplate = "[[RENDERPIPELINE]]";

        public string SampleName => "Netcode for Entities Setup";

        public string PathToImportedSample => TargetSetupFolder;

        public bool IsReadyForPostImport => FindType("ConnectionUI", false) != null;

        /// <summary>
        /// Checks if a former setup exists (imported with quickstart older than version 1.0.0).
        /// </summary>
        bool OldSetupExists => File.Exists("Assets/NetcodeForEntitiesBasicSetup/BasicSetupSubScene.unity") || File.Exists("Assets/NetcodeForEntitiesBasicSetup/BasicSetup.unity");
        
        public bool PreImportCheck()
        {
            if(OldSetupExists)
            {
                if (!EditorUtility.DisplayDialog("Former setup detected",
                    "A former version of the Netcode for Entities setup was detected. We recommend backing up your project and deleting the folder NetcodeForEntitiesBasicSetup. Continuing may result in unexpected behaviour.",
                    "Continue", "Cancel"))
                {
                    return false;
                }
            }
            
            var otherBootstrap = EntitiesUtils.AnotherBootstrapExists(k_CustomBootStrapPath, CustomBootStrapFullName);
            if (otherBootstrap != null && !EditorUtility.DisplayDialog("Existing bootstrap in project",
                    $"Another bootstrap script exists in the project: {otherBootstrap}. Continuing may result in unexpected behaviour.",
                    "Continue", "Cancel"))
            {
                return false;
            }

            if (GraphicsSettings.currentRenderPipeline == null)
            {
                Debug.LogError("Entities Graphics require the HD or Universal Render Pipeline.");
                return false;
            }

            if (!SceneManagementUtils.EnsureTheCurrentScenesAreSaved())
                return false;

            if (File.Exists(k_GameplayScenePath) || File.Exists(k_SubScenePath) || File.Exists(k_ConnectionUIScenePath))
            {
                const string infoText = "The scene(s) you are about to import are already in your project. Do you want to overwrite them?";
                if (!EditorUtility.DisplayDialog("Overwrite existing scene(s)?", infoText, "Yes", "No"))
                    return false;
                AssetDatabase.DeleteAsset(TargetSetupFolder);
            }

            return true;
        }

        public bool CopyScripts()
        {
            if (!IOUtils.DirectoryCopy(k_PathInPackage, TargetSetupFolder))
                return false;
            
            string rp = IsHdrp()? "HDRP" : "URP";
            
            foreach (var file in Directory.GetFiles(TargetSetupFolder + "Scripts/", "*.cs"))
            {
                var fileContent = File.ReadAllText(file);
                if(fileContent.Contains(k_RenderPipelineTemplate))
                {
                    fileContent = fileContent.Replace(k_RenderPipelineTemplate, rp);
                    File.WriteAllText(file, fileContent);
                }
            }

            return true;
        }

        public bool PostImport()
        {
            var sceneToLoad = SceneManagementUtils.CreateScene(k_GameplaySceneName, k_TargetScenesPath,
                NewSceneMode.Single);
            
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.transform.position = new Vector3(0, -0.5f, 0);
            
            var autoDeactivateType = new GameObject("AutoDeactivateExtraComponents");
            AddAutoDeactivateExtraComponents(autoDeactivateType);
            var cubeSpawner = CreateCubePlayerPrefabAndSpawner();
            
            _ = new GameObject("Netcode for Entities - OverrideAutomaticNetcodeBootstrap", typeof(OverrideAutomaticNetcodeBootstrap));
            var subScene = EntitiesUtils.CreateSubScene(k_SubScenePath, sceneToLoad, new[] {plane, cubeSpawner});
            subScene.AutoLoadScene = true; // Show users the Plane, and allows rendering and
                                           // names etc to appear when entering Play Mode.
            EditorSceneManager.MarkSceneDirty(sceneToLoad);
            EditorSceneManager.SaveScene(sceneToLoad);

            AddToBuildSettings(k_GameplayScenePath);

            var uiScene = SceneManagementUtils.SwitchToNewEmptyScene(k_ConnectionUISceneName, k_TargetScenesPath);
            if (!SetupUiManager())
                return false;
            EditorSceneManager.MarkSceneDirty(uiScene);
            EditorSceneManager.SaveScene(uiScene);

            return true;
        }

        static void AddAutoDeactivateExtraComponents(GameObject parent)
        {
            var autoDeactivateType = FindType("AutoDeactivateExtraComponents");
            if (autoDeactivateType == null) return;
            parent.AddComponent(autoDeactivateType);
        }
        
        bool IsHdrp()
        {
            if(GraphicsSettings.defaultRenderPipeline?.GetType() == null) return false;
            return GraphicsSettings.defaultRenderPipeline.GetType().ToString().Contains("HDRenderPipelineAsset");
        }
        
        static bool SetupUiManager()
        {
            var connectionUiType = FindType("ConnectionUI");
            if (connectionUiType == null) return false;
            var sceneToLoadField = connectionUiType.GetField("SceneToLoad");
            var connectionLabelField = connectionUiType.GetField("ConnectionLabel");
            var startHostButtonField = connectionUiType.GetField("StartHostButton");
            var startClientButtonField = connectionUiType.GetField("StartClientButton");
            if (sceneToLoadField == null || connectionLabelField == null || startHostButtonField == null || startClientButtonField == null)
            {
                Debug.LogError("Could not find ConnectionUI or one of its fields. Please check the script.");
                return false;
            }

            var ui = PrefabUtils.InstantiatePrefabAndUnlink(k_LinkToUIPrefab, k_ConnectionUiName);
            var uiManager = ui.AddComponent(connectionUiType);
            var children = ui.GetComponentsInChildren<Transform>();
            sceneToLoadField.SetValue(uiManager, k_GameplaySceneName);
            foreach (var t in children)
            {
                if (t.name == "StartHostButton")
                    startHostButtonField.SetValue(uiManager, t.GetComponent<Button>());
                else if (t.name == "StartClientButton")
                    startClientButtonField.SetValue(uiManager, t.GetComponent<Button>());
                else if (t.name == "ConnectionLabel")
                    connectionLabelField.SetValue(uiManager, t.GetComponent<Text>());
            }

            return true;
        }

        static void AddToBuildSettings(string scenePath)
        {
            var previous = EditorBuildSettings.scenes;
            for (var index = 0; index < previous.Length; index++)
            {
                var scene = previous[index];
                if (scene.path == scenePath)
                {
                    previous[index] = new EditorBuildSettingsScene(scenePath, true);
                    EditorBuildSettings.scenes = previous;
                    return;
                }
            }

            Array.Resize(ref previous, previous.Length + 1);
            previous[^1] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = previous;
        }

        static GameObject CreateCubePlayerPrefabAndSpawner()
        {
            var cubeAuthoringType = FindType("CubeAuthoring");
            var cubeInputAuthoringType = FindType("CubeInputAuthoring");
            var cubeSpawnerAuthoringType = FindType("CubeSpawnerAuthoring");
            var debugColorAuthoringType = FindType("SetPlayerToDebugColorAuthoring");
            if (cubeAuthoringType == null || cubeInputAuthoringType == null || cubeSpawnerAuthoringType == null || debugColorAuthoringType == null)
                return null;

            // find Cube field in cubeSpawnerAuthoringType
            var cubeField = cubeSpawnerAuthoringType.GetField("Cube");
            if (cubeField == null)
            {
                Debug.LogError("Could not find Cube field in CubeSpawnerAuthoring. Please check the script.");
                return null;
            }

            var networkedCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            networkedCube.AddComponent(cubeAuthoringType);
            networkedCube.AddComponent(cubeInputAuthoringType);
            var ga = networkedCube.AddComponent<NetCode.GhostAuthoringComponent>();
            ga.DefaultGhostMode = NetCode.GhostMode.OwnerPredicted;
            ga.HasOwner = true;
            networkedCube.AddComponent(debugColorAuthoringType);
            var playerPrefabAsset = PrefabUtils.TurnIntoPrefab(networkedCube, k_PlayerPrefabPath);
            var spawner = new GameObject("CubeSpawner", cubeSpawnerAuthoringType);

            // Basically: spawner.GetComponent<CubeSpawnerAuthoring>().Cube = playerPrefabAsset;
            var spawnerComponent = spawner.GetComponent(cubeSpawnerAuthoringType);
            cubeField.SetValue(spawnerComponent, playerPrefabAsset);
            return spawner;
        }

        static Type FindType(string typeName, bool logError = true)
        {
            var typeString = $"{k_TargetNamespace}{typeName}, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
            var type = Type.GetType(typeString);

            if (type != null)
                return type;

            if (logError)
                Debug.LogError($"Could not find {typeName}. Please check the script location.");

            return null;
        }
    }
}
#endif
