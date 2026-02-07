using System;
using System.Collections.Generic;
using Unity.Multiplayer.Center.Common;
using UnityEngine;

namespace Unity.Multiplayer.Center.Integrations
{
    [OnboardingSection(OnboardingSectionCategory.ConnectingPlayers, "Unity Building Block - Multiplayer Session", Order = -15)]
    [Serializable]
    class MultiplayerSessionBuildingBlockSection : DefaultSection, IOnboardingSection
    {
        public override string Title => "Unity Building Block - Multiplayer Session";

        public override string ShortDescription => "The Multiplayer Session Building Block helps you integrate Unity’s " +
            "multiplayer sessions into your project to connect players. This Building Block can serve as a starting" +
            " point for your multiplayer projects, or as a quick integration of sessions throughout development.";

        public override string ButtonLabel => "Open Asset Store";
        public override Action OnButtonClicked => OnAssetStoreButtonClicked;

        private void OnAssetStoreButtonClicked()
        {
            Application.OpenURL(AssetStoreSlugs.MultiplayerSessionBuildingBlock);
        }

        public override IEnumerable<(string, string)> Links => new[]
        {
            ("General Documentation", DocLinks.MultiplayerBuildingBlock),
            ("Building Block Perquisites", DocLinks.BuildingBlockPrerequisites),
        };
    }
}
