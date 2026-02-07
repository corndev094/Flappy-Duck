using System;
using System.Collections.Generic;
using Unity.Multiplayer.Center.Common;
using UnityEditor.PackageManager.UI;
using UnityEngine;

namespace Unity.Multiplayer.Center.Integrations
{
    [Serializable]
    [OnboardingSection(OnboardingSectionCategory.ConnectingPlayers, "Vivox", TargetPackageId = "com.unity.services.vivox",
        DisplayCondition = DisplayCondition.PackageInstalled)]
    class VivoxOnboardingSection : DefaultSection, IOnboardingSection
    {
        const string k_PackageId = "com.unity.services.vivox";
        const string k_SampleName = "Chat Channel Sample";
        const string k_SceneName = "MainScene";

        public override string Title => "Vivox - Voice and Text Chat";
        public override string ButtonLabel => Sample.isImported ? "Open sample scene" : "Import sample";
        public override Action OnButtonClicked => ImportOrOpenSample;

        public override string ShortDescription =>
            "Set up a voice and text chat service that players can use to communicate with each other in real-time.";
        
        // Package management needs a frame to load the sample. This is false until the sample is ready.
        protected override bool IsReady => SampleUtility.TryFindSample(k_PackageId, k_SampleName, out m_Sample);

        [NonSerialized]
        Sample m_Sample;

        Sample Sample => SampleUtility.GetSample(k_PackageId, k_SampleName, ref m_Sample);

        public override IEnumerable<(string, string)> Links => new[]
        {
            ("Documentation", DocLinks.Vivox),
            ("Quickstart Guide", DocLinks.VivoxQuickStart)
        };
        
        public override (string, string)[] LessImportantLinks => new[]
        {
            ("Forum", CommunityLinks.Vivox),
        };

        void ImportOrOpenSample()
        {
            SampleUtility.ImportOrOpenSample(Sample, k_SceneName);
        }
    }
}
