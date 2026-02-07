using System;
using System.Collections.Generic;
using System.IO;
using Unity.Multiplayer.Center.Common;
using UnityEngine;

#if NGO_INSTALLED
using UnityEditor;
using Unity.Netcode;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode.Transports.UTP;
using Object = UnityEngine.Object;
#endif

namespace Unity.Multiplayer.Center.Integrations
{
    [Serializable]
    [OnboardingSection(OnboardingSectionCategory.Netcode, "Netcode for GameObjects", TargetPackageId = "com.unity.netcode.gameobjects", DisplayCondition = DisplayCondition.PackageInstalled, Order = 0)]
    class NgoOnboardingSection : DefaultSection, IOnboardingSection
    {
        /// <inheritdoc />
        public override string Title => "Netcode for GameObjects";
        
        /// <inheritdoc />
        public override string ButtonLabel => "Create and open scene with netcode setup";

        public override string ShortDescription =>
            "Start your multiplayer game with Netcode for GameObjects, which takes care of low-level protocols and networking tasks for you." +
            " To get started, you need a NetworkManager and a Player Prefab.\n\n"+
            UIStrings.ExploreSampelSetupCTA
#if MPPM_1_3_INSTALLED
            + "\nThe <b>Multiplayer Play Mode</b> window will open and allow you to select additional players (learn more in the section below).\n"
#endif
            ;
        
        /// <inheritdoc />
        public override Action OnButtonClicked => CreateSetup;
        
        const string k_NgoExamplePath = Paths.PathInPackage + "Runtime/NetcodeForGameObjectsExample/";
        const string k_TemporaryUIPrefab = k_NgoExamplePath + "TemporaryUI.prefab";
        const string k_NgoMinimalSetupFolderName = "NGO_Minimal_Setup";
        internal const string NgoMinimalSetupFolderPath = "Assets/" + k_NgoMinimalSetupFolderName;
        const string k_NgoMinimalSetupScene = "Assets/" + k_NgoMinimalSetupFolderName + "/NGO_Setup.unity";
        const string k_PlayerPrefabPath = NgoMinimalSetupFolderPath + "/PlayerPrefab.prefab";
        const string k_NetworkPrefabsListPath = NgoMinimalSetupFolderPath + "/NetworkPrefabsList.asset";

        void CreateSetup()
        {
#if NGO_INSTALLED
            if (!CheckForAlreadyImportedSetup()) return;
            if (!SceneManagementUtils.EnsureTheCurrentScenesAreSaved()) return;
            SceneManagementUtils.SwitchToNewEmptyScene("NGO_Setup", NgoMinimalSetupFolderPath);
            CreateMinimalSetup();
#if MPPM_1_3_INSTALLED
            EditorApplication.ExecuteMenuItem("Window/Multiplayer/Multiplayer Play Mode");
#endif
            SceneManagementUtils.SaveAllScenes();
#endif
        }
        
#if NGO_INSTALLED
        static bool CheckForAlreadyImportedSetup()
        {
            return !File.Exists(k_NgoMinimalSetupScene) 
                || EditorUtility.DisplayDialog(UIStrings.SetupExists,
                    UIStrings.SetupExistsOverwriteWarning, "Yes", "No");
        }

        static void CreateMinimalSetup()
        {
            var existing = Object.FindAnyObjectByType<NetworkManager>();
            if(existing != null)
            {
                Debug.LogWarning("Netcode for GameObjects setup already exists in the scene.");
                EditorGUIUtility.PingObject(existing);
                return;
            }

            PrefabUtils.InstantiatePrefabAndUnlink(k_TemporaryUIPrefab, "Temporary UI -- can be deleted");
            var networkManager = CreateNetworkManagerAndPlayerPrefab();
            EditorGUIUtility.PingObject(networkManager);
            Selection.activeGameObject = networkManager;
        }

        static GameObject CreateNetworkManagerAndPlayerPrefab()
        {
            // Create NetworkManager.
            var networkManagerGameObject = new GameObject("NetworkManager", typeof(NetworkManager));
            var networkManager = networkManagerGameObject.GetComponent<NetworkManager>();
            
            // Assign Unity Transport to NetworkManager.
            var transport = networkManagerGameObject.AddComponent<UnityTransport>();
            // Automatically set when object is selected, which annoyingly sets the gameobject dirty
            // To avoid this we set this directly here
            transport.ConnectionData.ServerListenAddress = "127.0.0.1"; 
            networkManager.NetworkConfig.NetworkTransport = transport;
            
            // Create the Player Prefab.
            var networkedCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            networkedCube.AddComponent<NetworkObject>();
            networkedCube.AddComponent<ClientNetworkTransform>();
            networkedCube.AddComponent<ClientAuthoritativeMovement>();
            networkedCube.AddComponent<SetColorBasedOnOwnerId>();
            
            // Create a folder for the Player Prefab.
            if (!AssetDatabase.IsValidFolder(NgoMinimalSetupFolderPath))
                AssetDatabase.CreateFolder("Assets", k_NgoMinimalSetupFolderName);
            
            // Save the Player Prefab as an asset.
            var playerPrefab = PrefabUtility.SaveAsPrefabAsset(networkedCube, k_PlayerPrefabPath);
            
            // Clean up the scene.
            Object.DestroyImmediate(networkedCube);
            
            // Create a NetworkPrefabsList and add the Player Prefab to it.
            var networkPrefabsList = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            AssetDatabase.CreateAsset(networkPrefabsList, k_NetworkPrefabsListPath);
            networkPrefabsList.Add(new NetworkPrefab { Prefab = playerPrefab });
            
            // Save the NetworkPrefabsList.
            EditorUtility.SetDirty(networkPrefabsList);
            AssetDatabase.SaveAssets();
            
            // Also assign the Player Prefab to the NetworkManager.
            networkManager.NetworkConfig.PlayerPrefab = playerPrefab;
            
            // Assign the NetworkPrefabsList to the NetworkManager.
            networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear();
            networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(networkPrefabsList);
            
            return networkManagerGameObject;
        }
#endif

        public override IEnumerable<(string, string)> Links => new[]
        {
            ("Documentation", DocLinks.NetcodeForGameObjects),
            ("Bite-size Samples", SampleLinks.BiteSizeSamples)
        };

        public override (string, string)[] LessImportantLinks => new[]
        {
            ("Forum", CommunityLinks.NetcodeForGameObjects)
        };
    }
}
