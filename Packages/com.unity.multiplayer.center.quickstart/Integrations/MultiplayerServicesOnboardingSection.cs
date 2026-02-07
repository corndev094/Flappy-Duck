using System;
using System.Collections.Generic;
using Unity.Multiplayer.Center.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using DisplayCondition = Unity.Multiplayer.Center.Common.DisplayCondition;

namespace Unity.Multiplayer.Center.Integrations
{
    [OnboardingSection(OnboardingSectionCategory.ConnectingPlayers, "Multiplayer Services", TargetPackageId = "com.unity.services.multiplayer", 
        DisplayCondition = DisplayCondition.PackageInstalled)]
    [Serializable]
    class MultiplayerServicesOnboardingSection : DefaultSection, ISectionDependingOnUserChoices
    {
        VisualElement m_ServicesSubSection;
        VisualElement m_ClientHostedSubSection;
        VisualElement m_DedicatedServerSubSection;
        SelectedSolutionsData m_SelectedSolutionData; 

        public override string Title => "Multiplayer Services";
        public override string ShortDescription => "The Multiplayer Services package simplifies multiplayer game " + 
            "development by abstracting the Relay, Lobby, Matchmaker services into the concept of sessions." +
            " A session represents a group of players connected for a set period.\n\n" + 
            "Create a session programmatically or with the Multiplayer Widgets and monitor it in the Sessions Viewer.";
        public override string ButtonLabel => "Open Sessions Viewer";
        public override Action OnButtonClicked => () => EditorApplication.ExecuteMenuItem("Services/Multiplayer/Sessions Viewer");
        
        public override IEnumerable<(string, string)> Links => new []
        {
            ("General Documentation", DocLinks.MultiplayerServices),
            ("Create Session Documentation", DocLinks.MpsSessionsCreation),
            ("Dashboard", DocLinks.CloudDashboard)
        };

        public override void Load()
        {
            base.Load();
            CreateServicesSubSection();
            UpdateServicesSubSection();
            Root.Add(m_ServicesSubSection);
        }
        
        public override void Unload()
        {
            base.Unload();
            m_ServicesSubSection?.Clear();
        }

        public void HandleUserSelectionData(SelectedSolutionsData selectedSolutionData)
        {
            m_SelectedSolutionData = selectedSolutionData;
            UpdateServicesSubSection();
        }

        void CreateServicesSubSection()
        {
            if(m_ServicesSubSection != null)
                m_ServicesSubSection.Clear();
            else
                m_ServicesSubSection = new VisualElement();
            
            m_ClientHostedSubSection = CreateClientHostedSubSection();
            m_DedicatedServerSubSection = CreateDedicatedServerSubSection();
            
            m_ServicesSubSection.Add(m_ClientHostedSubSection);
            m_ServicesSubSection.Add(m_DedicatedServerSubSection);
        }

        VisualElement CreateClientHostedSubSection()
        {
            return new VisualElement();
            // return new Label("Client Hosted SubSection\nWill contain info about Sessions (with Relay and Lobby functionality)");
        }
        
        VisualElement CreateDedicatedServerSubSection()
        {
            return new VisualElement();
            // return new Label("Dedicated Server SubSection\nWill contain info about Hosting and Matchmaking");
        }

        void UpdateServicesSubSection()
        {
            if(m_ClientHostedSubSection == null || m_DedicatedServerSubSection == null || m_SelectedSolutionData == null)
                return;
            
            switch (m_SelectedSolutionData.SelectedHostingModel)
            {
                case SelectedSolutionsData.HostingModel.None:
                    SetSubSectionVisibility(false, false);
                    break;
                case SelectedSolutionsData.HostingModel.ClientHosted:
                    SetSubSectionVisibility(true, false);
                    break;
                case SelectedSolutionsData.HostingModel.DedicatedServer:
                    SetSubSectionVisibility(false, true);
                    break;
                case SelectedSolutionsData.HostingModel.CloudCode:
                    SetSubSectionVisibility(false, false);
                    break;
                default:
                    SetSubSectionVisibility(false, false);
                    break;
            }
        }
        
        void SetSubSectionVisibility(bool clientHostedVisible, bool dedicatedServerVisible)
        {
            m_ClientHostedSubSection.style.display = clientHostedVisible ? DisplayStyle.Flex : DisplayStyle.None;
            m_DedicatedServerSubSection.style.display = dedicatedServerVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
