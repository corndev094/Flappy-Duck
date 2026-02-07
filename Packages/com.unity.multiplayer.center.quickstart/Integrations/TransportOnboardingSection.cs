using System;
using System.Collections.Generic;
using Unity.Multiplayer.Center.Common;
using UnityEditor.PackageManager.UI;

// show this tab if no netcode is installed but transport.
// It is an indicator that users wants to build their own solution.

namespace Unity.Multiplayer.Center.Integrations
{
#if MULTIPLAYER_CENTER_1_0
    [OnboardingSection(OnboardingSectionCategory.Netcode, "Transport", TargetPackageId = "com.unity.transport",
        DisplayCondition = DisplayCondition.PackageInstalled, NetcodeDependency = SelectedSolutionsData.NetcodeSolution.CustomNetcode)]
#endif
    class TransportOnboardingSection : DefaultSection, IOnboardingSection
    {
        const string k_PackageId = "com.unity.transport";
        const string k_SampleName = "Simple Client and Server";
        const string k_SceneName = "SimpleClientServer";

        public override string Title => "Unity Transport";
        public override string ButtonLabel => Sample.isImported ? "Open sample scene" : "Import sample";
        public override Action OnButtonClicked => ImportOrOpenSample;

        public override string ShortDescription =>
            "Unity Transport is a low-level networking library for connecting and sending data through a network. It can serve as the base for your custom netcode solution.";
        
        // Package management needs a frame to load the sample. This is false until the sample is ready.
        protected override bool IsReady => SampleUtility.TryFindSample(k_PackageId, k_SampleName, out m_Sample);

        [NonSerialized]
        Sample m_Sample;

        Sample Sample => SampleUtility.GetSample(k_PackageId, k_SampleName, ref m_Sample);

        public override IEnumerable<(string, string)> Links => new[]
        {
            ("Documentation", DocLinks.Transport)
        };
        
        public override (string, string)[] LessImportantLinks => new[]
        {
            ("Forum", CommunityLinks.TransportForum),
        };

        void ImportOrOpenSample()
        {
            SampleUtility.ImportOrOpenSample(Sample, k_SceneName);
        }
    }
}
