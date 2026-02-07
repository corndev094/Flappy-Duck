using System;
using System.Collections.Generic;
using Unity.Multiplayer.Center.Common;
#if MULTIPLAYER_TOOLS_INSTALLED
using Unity.Multiplayer.Tools.Editor.MultiplayerToolsWindow;
#endif
using UnityEngine;

namespace Unity.Multiplayer.Center.Integrations
{
    /// <summary>
    /// Example of multiplayer tools section that can be shipped with the multiplayer center but depends on some type
    /// in the multiplayer tools package.
    /// This can be overridden by the multiplayer tools package to add its own section.
    /// </summary>
    [OnboardingSection(OnboardingSectionCategory.Netcode, "Netcode for GameObjects Tools", TargetPackageId = "com.unity.multiplayer.tools",
        DisplayCondition = DisplayCondition.PackageInstalled, Order = 2)]
    [Serializable]
    class MultiplayerToolsSection : DefaultSection, IOnboardingSection
    {
        public override string Title => "Multiplayer Tools - Debug and optimize";

#if MULTIPLAYER_TOOLS_INSTALLED
        public override string ButtonLabel => "Open window";
        public override Action OnButtonClicked => MultiplayerToolsWindow.Open;
        public override string ShortDescription => 
            "These tools help you understand ownership, simulate network conditions, and optimize the amount of bandwidth your game uses.\n\n" +
            "To explore and use these tools, open the Tool window.";
#else
        public override string ButtonLabel => null;
        public override Action OnButtonClicked => null; 
#endif
        
        public override IEnumerable<(string, string)> Links => new[]
        {
            ("Documentation", DocLinks.MultiplayerTools)
        };

        /// <inheritdoc />
        public override (string, string)[] LessImportantLinks => new[]
        {
            ("Forum", CommunityLinks.MultiplayerTools)
        };
    }
}
