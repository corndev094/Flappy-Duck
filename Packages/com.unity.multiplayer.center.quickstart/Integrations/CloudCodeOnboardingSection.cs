using System;
using System.Collections.Generic;
using Unity.Multiplayer.Center.Common;
using UnityEngine;

namespace Unity.Multiplayer.Center.Integrations
{
    /// <summary>
    /// Onboarding Section for Unity Cloud Code.
    /// </summary>
    [OnboardingSection(OnboardingSectionCategory.Netcode, "Cloud Code", TargetPackageId = "com.unity.services.cloudcode",
        DisplayCondition = DisplayCondition.PackageInstalled, Order = 1)]
    class CloudCodeOnboardingSection : DefaultSection, IOnboardingSection
    {
        public override string Title => "Cloud Code";
        public override string ButtonLabel => null;
        public override Action OnButtonClicked => null;

        public override string ShortDescription =>
            "Run your game logic in the cloud as serverless functions and interact with other backend services. " +
            "Cloud Code offers an easy, safe, and cost-effective way of creating turn-based or async multiplayer games that " +
            "do not require real-time updates.";
        
        /// <inheritdoc />
        public override IEnumerable<(string, string)> Links => new[]
        {
            ("Documentation", DocLinks.CloudCodeManual),
            ("Dashboard", DocLinks.CloudCodeDashboard),
            ("Chess Example", SampleLinks.CloudeCodeChessExample)
        };
    }
}
