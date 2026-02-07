using System;
using System.Collections.Generic;
using Unity.Multiplayer.Center.Common;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.Center.Integrations
{
    /// <summary>
    /// Onboarding Section for Multiplayer Roles, which are part of the dedicated server package.
    /// </summary>
    [OnboardingSection(OnboardingSectionCategory.ServerInfrastructure, "Multiplayer Roles", TargetPackageId = "com.unity.dedicated-server",
        DisplayCondition = DisplayCondition.PackageInstalled, Order = 3)]
    class MultiplayerRolesOnboardingSection : DefaultSection, IOnboardingSection
    {
        public override string Title => "Multiplayer Roles";
        public override string ButtonLabel => "Open Multiplayer Role Settings";
        public override Action OnButtonClicked => () => { SettingsService.OpenProjectSettings("Project/Multiplayer/Multiplayer Roles"); };

        public override string ShortDescription =>
            "Remove specific Gameobjects and Components from Server or Client builds. " +
            "This means that you can control the elements of a game that exist in each Server and Client build.\n\n" +
            "Multiplayer Roles is part of the Dedicated Server package. " +
            "To use it, enable the Multiplayer Roles property in the Project Settings window. ";

        /// <inheritdoc />
        public override IEnumerable<(string, string)> Links => new[]
        {
            ("Documentation", MultiplayerRoles: DocLinks.DedicatedServerPackage)
        };
    }
}
