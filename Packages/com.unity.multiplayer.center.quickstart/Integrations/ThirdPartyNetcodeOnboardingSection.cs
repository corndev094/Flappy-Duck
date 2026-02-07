using System;
using System.Collections.Generic;
using Unity.Multiplayer.Center.Common;
using Unity.Multiplayer.Center.Integrations;

namespace Integrations
{
    namespace Unity.Multiplayer.Center.Integrations
    {
#if MULTIPLAYER_CENTER_1_0
        [OnboardingSection(OnboardingSectionCategory.Netcode, "CustomNetcode", NetcodeDependency = SelectedSolutionsData.NetcodeSolution.CustomNetcode)]
#endif
        class ThirdPartyNetcodeOnboardingSection : DefaultSection, IOnboardingSection
        {
            public override string Title => "Third-party Netcode Solutions";
            public override string ButtonLabel { get; }

            public override string ShortDescription => "You can create a custom Netcode solution or visit the Asset Store or Verified Solution page to explore third-party solutions.";
            public override Action OnButtonClicked => null;
            public override IEnumerable<(string, string)> Links =>
                new[]
                {
                    ("Asset Store", DocLinks.AssetStore),
                    ("Unity verified Solutions", DocLinks.VerifiedSolutions)
                };
        }
    }
}
