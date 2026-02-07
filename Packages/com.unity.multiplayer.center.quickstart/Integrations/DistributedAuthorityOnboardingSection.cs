#if MULTIPLAYER_CENTER_1_0 && NGO_2_OR_NEWER && SERVICES_INSTALLED
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Multiplayer.Center.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

using Unity.Multiplayer.Center.NetcodeForGameObjectsExample.DistributedAuthority;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;

namespace Unity.Multiplayer.Center.Integrations
{

    [OnboardingSection(OnboardingSectionCategory.ServerInfrastructure, "Distributed Authority", TargetPackageId = "com.unity.netcode.gameobjects"
        , HostingModelDependency = SelectedSolutionsData.HostingModel.DistributedAuthority, DisplayCondition = DisplayCondition.PackageInstalled, Order = 1)]
    [Serializable]
    class DistributedAuthorityOnboardingSection : DefaultSection, IOnboardingSection
    {
        public override string Title => "Distributed Authority";
        public override string ButtonLabel => "Create and open scene with distributed authority setup";
        public override Action OnButtonClicked => CreateSetup;
        
        // Source Paths
        const string k_DaMinimalExampleSourcePath = Paths.PathInPackage + "Runtime/NetcodeForGameObjectsExample/DistributedAuthority/";
        const string k_UIPath = k_DaMinimalExampleSourcePath + "UI";
        const string k_MainUxmlPath = k_UIPath + "/GameUI.uxml";

        // Target Paths
        const string k_DaMinimalExampleTargetFolderName = "DA_Minimal_Setup";
        internal const string DaMinimalExampleTargetFolderPath = "Assets/" + k_DaMinimalExampleTargetFolderName;
        const string k_DaMinimalExampleTargetScenePath = DaMinimalExampleTargetFolderPath + "/DA_Setup.unity";
        const string k_PlayerPrefabPath = DaMinimalExampleTargetFolderPath + "/PlayerPrefab.prefab";
        const string k_SpherePrefab = DaMinimalExampleTargetFolderPath + "/SpherePrefab.prefab";
        const string k_NetworkPrefabsListPath = DaMinimalExampleTargetFolderPath + "/NetworkPrefabsList.asset";

        public override string ShortDescription =>
            "In a distributed authority topology, game clients share responsibility for owning and " +
            "tracking the state of objects in the network. It solves visual and input-related issues while keeping costs relatively low.\n\n" +
#if MULTIPLAYER_TOOLS_INSTALLED
            "To visualize and debug the ownership of GameObjects, use the Multiplayer Tools found under the Category Netcode and Tools. \n\n" +
#endif   
            UIStrings.ExploreSampelSetupCTA;
        
        /// <inheritdoc />
        public override IEnumerable<(string, string)> Links => new[]
        {
            ("Documentation", DocLinks.DistributedAuthorityExplanation),
            ("Quickstart Guide", DocLinks.DistributedAuthorityQuickstartGuide),
            ("Asteroids Sample", SampleLinks.DaAsteroidsSample)
        };
        
        void CreateSetup()
        {

            if (!CheckForAlreadyImportedSetup()) return;
            if (!SceneManagementUtils.EnsureTheCurrentScenesAreSaved()) return;
            SceneManagementUtils.SwitchToNewEmptyScene("DA_Setup", DaMinimalExampleTargetFolderPath);
            CreateSceneContent();
            SceneManagementUtils.SaveAllScenes();
        }
        
        static bool CheckForAlreadyImportedSetup()
        {
            return !File.Exists(k_DaMinimalExampleTargetScenePath) 
                || EditorUtility.DisplayDialog(UIStrings.SetupExists,
                    UIStrings.SetupExistsOverwriteWarning, "Yes", "No");
        }
  
        static void CreateSceneContent()
        {
            var networkManagerGameObject = new GameObject("NetworkManager", typeof(NetworkManager));
            var networkManager = networkManagerGameObject.GetComponent<NetworkManager>();
            
            // Set Topology to Distributed Authority
            networkManager.NetworkConfig.NetworkTopology = NetworkTopologyTypes.DistributedAuthority;
            
            var transport = networkManagerGameObject.AddComponent<UnityTransport>();
            networkManager.NetworkConfig.NetworkTransport = transport;
            
            var networkedCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            networkedCube.AddComponent<ClientAuthoritativeMovement>();
            networkedCube.AddComponent<NetworkTransform>();
            networkedCube.AddComponent<SetColorBasedOnOwnerId>();
            networkedCube.AddComponent<Spawner>();
            
            var cubeNetworkObject = networkedCube.AddComponent<NetworkObject>();
            // Set the ownership status to None to prevent distribution.
            cubeNetworkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.None);
            
            var networkedSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            networkedSphere.AddComponent<SphereMovement>();
            networkedSphere.AddComponent<SetColorBasedOnOwnerId>();
            networkedSphere.AddComponent<NetworkTransform>();
            
            var sphereNetworkObject = networkedSphere.AddComponent<NetworkObject>();
            // Make sure the Spheres are not destroyed when the current owner leaves the session.
            sphereNetworkObject.DontDestroyWithOwner = true;
            
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(10, 10, 10); 
            floor.transform.position = new Vector3(0, -0.5f, 0); 
            
            var ui = new GameObject(){name = "UI"};
            ui.AddComponent<UIManager>();
            ui.AddComponent<ConnectionManager>();

            var uiDocument = ui.AddComponent<UIDocument>();
            uiDocument.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_MainUxmlPath);

            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
            uiDocument.panelSettings = panelSettings;
            
            if (!AssetDatabase.IsValidFolder(DaMinimalExampleTargetFolderPath))
                AssetDatabase.CreateFolder("Assets", k_DaMinimalExampleTargetFolderName);
            
            var spherePrefab = PrefabUtility.SaveAsPrefabAsset(networkedSphere, k_SpherePrefab);
            networkedCube.GetComponent<Spawner>().PrefabToSpawn = spherePrefab;
            var playerPrefab = PrefabUtility.SaveAsPrefabAsset(networkedCube, k_PlayerPrefabPath);
            
            Object.DestroyImmediate(networkedCube);
            Object.DestroyImmediate(networkedSphere);
            
            var networkPrefabsList = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            AssetDatabase.CreateAsset(networkPrefabsList, k_NetworkPrefabsListPath);
            networkPrefabsList.Add(new NetworkPrefab { Prefab = playerPrefab });
            networkPrefabsList.Add(new NetworkPrefab { Prefab = spherePrefab });
            
            EditorUtility.SetDirty(networkPrefabsList);
            AssetDatabase.SaveAssets();
            
            networkManager.NetworkConfig.PlayerPrefab = playerPrefab;
            networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear();
            networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(networkPrefabsList);
        }
    }
}
#endif
