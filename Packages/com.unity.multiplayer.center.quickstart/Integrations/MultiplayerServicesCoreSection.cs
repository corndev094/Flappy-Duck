using System;
using Unity.Multiplayer.Center.Common;
#if MULTIPLAYER_CENTER_ANALYTICS_AVAILABLE
using Unity.Multiplayer.Center.Common.Analytics;
#endif
using Unity.Multiplayer.Center.Onboarding;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


namespace Unity.Multiplayer.Center.Integrations
{
    /// <summary>
    /// Checks whether the project is bound to a project in the services core package.
    /// </summary>
#if CLOUDCODE_INSTALLED
    [OnboardingSection(OnboardingSectionCategory.Netcode, "Services Project Link", TargetPackageId = "com.unity.services.cloudcode", 
        DisplayCondition = DisplayCondition.PackageInstalled, Order = 0)]
#else
    [OnboardingSection(OnboardingSectionCategory.ConnectingPlayers, "Services Project Link", TargetPackageId = "com.unity.services.core",
        DisplayCondition = DisplayCondition.PackageInstalled, Order = -10)]
#endif
    [Serializable]
    class MultiplayerServicesCoreSection : IOnboardingSection
#if MULTIPLAYER_CENTER_ANALYTICS_AVAILABLE
        , ISectionWithAnalytics
#endif
    {
        const string k_ButtonLabel = "Open Settings";

        public VisualElement Root { get; private set; }

#if MULTIPLAYER_CENTER_ANALYTICS_AVAILABLE
        public IOnboardingSectionAnalyticsProvider AnalyticsProvider { get; set; }
#endif

        Button m_Button;

        bool m_IsBoundPreviousValue;

        public void Load()
        {
            Root ??= new VisualElement();
            Root.Clear();
            FillHelpBox(CloudProjectSettings.projectBound);
            EditorApplication.update -= UpdateHelpBox;
            EditorApplication.update += UpdateHelpBox;

            Root.style.marginBottom = 8;
        }

        public void Unload()
        {
            EditorApplication.update -= UpdateHelpBox;
            Clear();
        }

        void OpenProjectSettings()
        {
#if MULTIPLAYER_CENTER_ANALYTICS_AVAILABLE
            AnalyticsProvider.SendInteractionEvent(InteractionDataType.CallToAction, k_ButtonLabel);
#endif
            SettingsService.OpenProjectSettings("Project/Services");
        }

        void Clear()
        {
            if (m_Button != null)
                m_Button.clicked -= OpenProjectSettings;
            Root?.Clear();
        }

        void UpdateHelpBox()
        {
#if SERVICES_CORE_INSTALLED
            var isBound = CloudProjectSettings.projectBound;
            if (m_IsBoundPreviousValue == isBound)
                return;

            Clear();
            FillHelpBox(isBound);
#endif
        }

        void FillHelpBox(bool isBound)
        {
            m_IsBoundPreviousValue = isBound;
            if (isBound)
            {
                Root.AddToClassList(StyleConstants.OnBoardingSectionClass);
                var horizContainer = new VisualElement();
                horizContainer.style.flexDirection = FlexDirection.Row;
                var checkmark = new VisualElement();
                checkmark.AddToClassList(StyleConstants.CheckmarkClass);
                horizContainer.Add(checkmark);
                horizContainer.Add(new Label($"This project is correctly setup to use Unity services."));
                Root.Add(horizContainer);
                return;
            }

            Root.RemoveFromClassList(StyleConstants.OnBoardingSectionClass);
            var helpBox = new HelpBox("Please link your project in the Services section of Project Settings.", HelpBoxMessageType.Warning);
            m_Button = new Button(OpenProjectSettings) {text = k_ButtonLabel};
            helpBox.Add(m_Button);
            Root.Add(helpBox);
        }
    }
}
