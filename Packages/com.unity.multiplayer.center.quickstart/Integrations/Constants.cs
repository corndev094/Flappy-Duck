namespace Unity.Multiplayer.Center.Common
{
    /***
     * Important note: all the constants here are for convenience. We will move to dynamic data in the future,
     * so this data will not be the source of truth.
     */

    /// <summary>
    /// Documentation links that will be used in the recommendation view or any place that requires it.
    /// </summary>
    internal static class DocLinks
    {
        public const string NetcodeForGameObjects = "https://docs-multiplayer.unity3d.com/netcode/current/about/";

        public const string NetcodeForEntities = "https://docs.unity3d.com/Packages/com.unity.netcode@latest";

        public const string Vivox = "https://docs.unity.com/ugs/en-us/manual/vivox-unity/manual/Unity/Unity";

        public const string VivoxQuickStart = "https://docs.unity.com/ugs/en-us/manual/vivox-unity/manual/Unity/vivox-unity-first-steps";

        public const string MultiplayerTools = "https://docs-multiplayer.unity3d.com/tools/current/about/";

        public const string MultiplayerPlayMode = "https://docs-multiplayer.unity3d.com/mppm/current/about/";

        public const string MultiplayerServices = "https://docs.unity.com/ugs/en-us/manual/mps-sdk/manual";

        public const string MpsSessionsCreation = "https://docs.unity.com/ugs/en-us/manual/mps-sdk/manual/create-session";

        public const string MultiplayHosting = "https://docs.unity.com/ugs/manual/game-server-hosting/manual/welcome";

        public const string MultiplayHostingUnityCloudDashboardGetStarted = "https://docs.unity.com/ugs/en-us/manual/game-server-hosting/manual/guides/get-started";

        public const string MultiplayHostingMpsSdk = "https://docs.unity.com/ugs/en-us/manual/mps-sdk/manual/game-server-hosting-support";

        public const string EntitiesEditorWorkflows = "https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/manual/editor-workflows.html";

        public const string NetcodeConfig = "https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/api/Unity.NetCode.NetCodeConfig.html";

        public const string PlayModeTools = "https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/testing/playmode-tool.html";

        public const string DedicatedServerPackage = "https://docs.unity3d.com/Packages/com.unity.dedicated-server@latest";
        
        public const string CloudCodeManual = "https://docs.unity.com/ugs/manual/cloud-code/manual";
        
        public const string CloudCodeDashboard = "https://dashboard.unity3d.com/cloud-code";

        public const string CloudDashboard = "https://cloud.unity.com/home";
        
        public const string Transport = "https://docs.unity3d.com/Packages/com.unity.transport@latest";

        public const string AssetStore = "https://assetstore.unity.com";

        public const string VerifiedSolutions = "https://unity.com/partners/verified-solutions";

        public const string DistributedAuthorityExplanation = "https://docs-multiplayer.unity3d.com/netcode/current/terms-concepts/distributed-authority/";

        public const string DistributedAuthorityQuickstartGuide = "https://docs-multiplayer.unity3d.com/netcode/current/learn/distributed-authority-quick-start/";

        public const string MultiplayerBuildingBlock = "https://docs.unity3d.com/Manual/building-blocks-multiplayer-sessions.html";
        
        public const string BuildingBlockPrerequisites = "https://docs.unity3d.com/Manual/building-blocks-multiplayer-prerequisites.html";
    }

    /// <summary>
    /// Short form links to the various asset store items.
    /// </summary>
    internal static class AssetStoreSlugs
    {
        public const string MultiplayerSessionBuildingBlock = "https://assetstore.unity.com/packages/slug/341930";
    }

    /// <summary>
    /// Discussions (f.k.a. Forum) links that will be used in the recommendation view or any place that requires it.
    /// </summary>
    internal static class CommunityLinks
    {
        public const string NetcodeForGameObjects = "https://discussions.unity.com/tags/c/unity-engine/52/netcode-for-gameobjects";

        public const string NetcodeForEntities = "https://discussions.unity.com/tags/c/unity-engine/52/netcode-for-entities";

        public const string Vivox = "https://discussions.unity.com/tags/c/unity-services/53/voice-and-text-chat";

        public const string MultiplayerTools = "https://discussions.unity.com/tags/c/unity-engine/52/unity-multiplayer-tools";

        public const string MultiplayerPlayModeForum = "https://discussions.unity.com/tags/c/unity-engine/52/multiplayer-play-mode";

        public const string MultiplayerPlayModeDiscord = "https://discord.com/channels/449263083769036810/1039213437793861662";

        public const string TransportForum = "https://discussions.unity.com/tags/c/unity-engine/52/unity-transport";
    }

    /// <summary>
    /// Relative paths in the package.
    /// </summary>
    internal static class Paths
    {
        public const string PathInPackage = "Packages/com.unity.multiplayer.center.quickstart/";
    }

    /// <summary>
    /// Sample links that will be used in the recommendation view or any place that requires it.
    /// </summary>
    internal static class SampleLinks
    {
        public const string DSS = "https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/tree/main/Experimental/DedicatedGameServer";

        public const string MegacitySample = "https://github.com/Unity-Technologies/Megacity-Sample";

        public const string BattleRoyalSample = "https://unity.com/demos/photon-fusion-battle-royale";

        public const string BiteSizeSamples = "https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize";

        public const string NetcodeForEntitiesSamples = "https://github.com/Unity-Technologies/EntityComponentSystemSamples/tree/master/NetcodeSamples";
        
        public const string CloudeCodeChessExample = "https://github.com/Unity-Technologies/com.unity.services.samples.multiplayer-chess-cloud-code";

        public const string DaAsteroidsSample = "https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/tree/main/Experimental/Asteroids-CMB-NGO-Sample";
    }

    /// <summary>
    /// Strings that are used inside the UI
    /// </summary>
    internal static class UIStrings
    {
            public const string SetupExists = "Setup already exists";
                
            public const string SetupExistsOverwriteWarning = "The setup was already imported. Do you want to overwrite it?";

            public const string ExploreSampelSetupCTA = "See it in action with our sample setup.";
    }
}
