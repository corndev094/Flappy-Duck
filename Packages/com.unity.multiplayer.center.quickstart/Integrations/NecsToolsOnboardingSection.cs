using System;
using System.Collections.Generic;
using Unity.Multiplayer.Center.Common;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.Center.Integrations
{
    [OnboardingSection(OnboardingSectionCategory.Netcode, "Netcode for Entities Tools", TargetPackageId = "com.unity.netcode",
        DisplayCondition = DisplayCondition.PackageInstalled, Order = 2)]
    class NecsToolsOnboardingSection : DefaultSection, IOnboardingSection
    {
        public override string Title => "Tools - Debug and Optimize - Netcode for Entities";
        public override string ButtonLabel => "PlayMode Tools";
        
#if N4E_1_3_INSTALLED
        const string k_PlayModeToolsPath = "Window/Multiplayer/PlayMode Tools";
#else
        const string k_PlayModeToolsPath = "Multiplayer/Window: PlayMode Tools";
#endif   
        public override Action OnButtonClicked => () => EditorApplication.ExecuteMenuItem(k_PlayModeToolsPath);

        public override string ShortDescription => "Debug and analyze your Multiplayer Game using the Netcode For Entities Playmode Tools. " + "\n" +
            "Simulate network conditions, visualize and debug client side prediction. " + "\n\n" +
            "You can find the dedicated Profiler and all available for Netcode for Entities inside Multiplayer on the menu bar. ";

        public override IEnumerable<(string, string)> Links => new[]
        {
            ("Documentation", DocLinks.PlayModeTools),
            ("NetcodeConfig", DocLinks.NetcodeConfig),
            ("Editor Workflows", DocLinks.EntitiesEditorWorkflows),
        };
    }
}
