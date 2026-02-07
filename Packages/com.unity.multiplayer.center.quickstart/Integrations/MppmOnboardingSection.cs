#if MPPM_INSTALLED
using System;
using System.Collections.Generic;
using Unity.Multiplayer.Center.Common;
using UnityEditor;

namespace Unity.Multiplayer.Center.Integrations
{
    [OnboardingSection(OnboardingSectionCategory.Netcode, "MPPM", TargetPackageId = "com.unity.multiplayer.playmode",
        DisplayCondition = DisplayCondition.PackageInstalled, Order = 1)]
    class MultiplayerPlayModeOnboardingSection : DefaultSection, IOnboardingSection
    {
        public override string Title => "Multiplayer Play Mode - Test and iterate on your game locally";
        
        public override string ButtonLabel => "Open window";

        public override string ShortDescription =>
            "Run your game with multiple virtual players when you enter Play mode. This makes it easy to test and iterate without connecting to another machine.\n\n" +
            "Create a virtual player and select Play to experience the minimal example.";
        
        public override Action OnButtonClicked => OpenWindow;
        
        public override IEnumerable<(string, string)> Links => new[]
        {
            ("Documentation", DocLinks.MultiplayerPlayMode)
        };
        
        public override (string, string)[] LessImportantLinks => new[]
        {
            ("Forum", CommunityLinks.MultiplayerPlayModeForum),
            ("Discord", CommunityLinks.MultiplayerPlayModeDiscord)
        };
        
        static void OpenWindow()
        {
            #if MPPM_1_3_INSTALLED
            EditorApplication.ExecuteMenuItem("Window/Multiplayer/Multiplayer Play Mode");
            #else
            EditorApplication.ExecuteMenuItem("Window/Multiplayer Play Mode");
            #endif
        }
    }
}
#endif
