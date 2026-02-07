#if N4E_INSTALLED
using System;
using System.Collections.Generic;
using Unity.Multiplayer.Center.Common;
using Unity.Multiplayer.Center.Integrations.DynamicSample;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Unity.Multiplayer.Center.Integrations
{
    /// <summary>
    /// Netcode for Entities onboarding section
    /// </summary>
    [OnboardingSection(OnboardingSectionCategory.Netcode, "Netcode for Entities", TargetPackageId = "com.unity.netcode", 
        DisplayCondition = DisplayCondition.PackageInstalled)]
    class NecsOnboardingSection : DefaultSection, IOnboardingSection
    {
        const string k_WindowReopenWarning = "Please reopen the Multiplayer Center to recheck the requirements.";
        const string k_RequirementPretext = "To try the example setup for Netcode for Entities, please";
        const string k_EntitiesGraphicsAndInputRequirement = " install the Entities Graphics package and the Input System package";
        const string k_RenderPipelineRequirement = " configure either URP or HDRP.";
        const string k_RenderPipelineDetails = 
            "URP: Install the Universal RP package and use the Window > Rendering > Render Pipeline Converter."
            + "\nHDRP: Install the High Definition RP package and use the Window > Rendering > HDRP Wizard.";
        const string k_NetcodeForEntitiesDescription = "Unity's Data Oriented Technology Stack (DOTS) multiplayer netcode layer - a high level netcode system built on entities. This package provides a foundation for creating networked multiplayer applications within DOTS.";
        const string k_ExampleDescription = "The example setup creates a connection UI and networked cubes in the current scene.";
        
#if N4E_1_3_INSTALLED
        const string k_OpenBuildSettingsPath = "Project/Multiplayer/Build"; // Project settings path should always start with "Project/"
#else
        const string k_OpenBuildSettingsPath = "Project/Entities/Build"; // Project settings path should always start with "Project/"
#endif
        static void OpenBuildSettings() => SettingsService.OpenProjectSettings(k_OpenBuildSettingsPath);
        
        public override string Title => "Netcode for Entities";
        static bool IsBuiltInPipelineUsed => GraphicsSettings.currentRenderPipeline == null;
        
        public override IEnumerable<(string, string)> Links => new[]
        {
            ("Documentation", DocLinks.NetcodeForEntities),
            ("Megacity Sample", SampleLinks.MegacitySample),
            ("Netcode for Entities Samples", SampleLinks.NetcodeForEntitiesSamples)
        };
        
        public override (string, string)[] LessImportantLinks => new[]
        {
            ("Forum", CommunityLinks.NetcodeForEntities)
        };

#if !ENTITIES_GRAPHICS_INSTALLED || !INPUT_SYSTEM_INSTALLED
        public override string ButtonLabel => null;
        
        public override Action OnButtonClicked => null;

        public override string ShortDescription =>
            k_NetcodeForEntitiesDescription + "\n\n"
            + k_RequirementPretext + k_EntitiesGraphicsAndInputRequirement
            + (IsBuiltInPipelineUsed
                ? " and" + k_RenderPipelineRequirement
                : ".");
#else
        public override string ButtonLabel => "Create and open scene with netcode setup";
        
        public override void Load()
        {
            base.Load(); 
            AddButton("Entities Build Settings", OpenBuildSettings, "Configure your builds for your Entities projects in Project Settings");
        }

        public override string ShortDescription => 
            k_NetcodeForEntitiesDescription + "\n\n"
            + (IsBuiltInPipelineUsed
            ? k_RequirementPretext + k_RenderPipelineRequirement + "\n\n" + k_RenderPipelineDetails + "\n\n" + k_WindowReopenWarning 
            : k_ExampleDescription
#if MPPM_1_3_INSTALLED
            + "\nThe <b>Multiplayer Play Mode</b> window will open and allow you to select additional players (learn more in the section below).\n"
#endif
            );
        
        public override Action OnButtonClicked => (GraphicsSettings.currentRenderPipeline == null) ? null : ImportBasicSetupInNewScene;

        [SerializeReference] DynamicSampleImporter m_Importer;
        void ImportBasicSetupInNewScene()
        {
            Debug.Log("Importing Netcode for Entities setup");
            if (m_Importer == null)
                m_Importer = ScriptableObject.CreateInstance<DynamicSampleImporter>();
            m_Importer.OnImported += OnImportFinished;
            m_Importer.Import(new NetcodeForEntitiesSetup());
#if MPPM_1_3_INSTALLED
            EditorApplication.ExecuteMenuItem("Window/Multiplayer/Multiplayer Play Mode");
#endif
        }

        void OnImportFinished(bool success)
        {
            m_Importer.OnImported -= OnImportFinished;
            Object.DestroyImmediate(m_Importer);
        }
#endif
    }
}
#endif
