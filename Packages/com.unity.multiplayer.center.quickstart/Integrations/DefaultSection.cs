using System;
using System.Collections.Generic;
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
    /// Some default section that can be used in the onboarding tab.
    /// It has a title, a set of links and a main button that can be used to open a sample or a window.
    /// To use it: inherit from it and IOnboardingSection, then add the OnboardingSection attribute.
    /// </summary>
    [Serializable]
    abstract class DefaultSection
#if MULTIPLAYER_CENTER_ANALYTICS_AVAILABLE
        : ISectionWithAnalytics
#endif
    {
        /// <summary>
        /// The root visual element that will be accessed.
        /// </summary>
        public VisualElement Root { get; protected set; }

        /// <summary>
        /// The title of that section, which is often the readable name of a package
        /// </summary>
        public abstract string Title { get; }

        /// <summary>
        /// The label of the main button
        /// </summary>
        public abstract string ButtonLabel { get; }

        /// <summary>
        /// Optional Short Description of what this Section offers to the user.
        /// </summary>
        public virtual string ShortDescription { get; } = "";

        /// <summary>
        /// The action that will be executed when the main button is clicked.
        /// </summary>
        public abstract Action OnButtonClicked { get; }

        /// <summary>
        /// List of tuples with a label and a link
        /// </summary>
        public abstract IEnumerable<(string, string)> Links { get; }
        
        /// <summary>
        /// Less important links that will be hidden in the three dots menu.
        /// </summary>
        public virtual (string, string)[] LessImportantLinks => Array.Empty<(string, string)>();

        // This is for waiting for package sample loading (i.e Vivox) to be ready
        protected virtual bool IsReady { get; set; } = true;
        
#if MULTIPLAYER_CENTER_ANALYTICS_AVAILABLE
        public IOnboardingSectionAnalyticsProvider AnalyticsProvider { get; set; }
#endif

        Button m_Button;

        VisualElement m_ButtonContainer;

        public virtual void Load()
        {
            Root ??= new VisualElement();

            // Wait if we need to load a package sample. Otherwise, this is not called
            if (!IsReady)
            {
                Root.style.display = DisplayStyle.None;
                EditorApplication.delayCall += Load;
                return;
            }

            Root.style.display = DisplayStyle.Flex;
            Root.AddToClassList(StyleConstants.OnBoardingSectionClass);

            CreateSectionHeader();
            AddMainContent();
            AddMainButton();
            AddResourcesLinks();
        }

        public virtual void Unload()
        {
            if (m_Button != null)
                m_Button.clicked -= OnButtonClicked;
        }

        protected void AddButton(string buttonText, Action clickAction, string description=null)
        {
            if (description != null)
            {
                m_ButtonContainer.Add(new Label(description));    
            }
            
            Button button = new Button(clickAction) {text = buttonText};
            button.AddToClassList(StyleConstants.OnBoardingSectionMainButton);
#if MULTIPLAYER_CENTER_ANALYTICS_AVAILABLE
            button.clicked += () => AnalyticsProvider.SendInteractionEvent(InteractionDataType.CallToAction, buttonText);
#endif
            m_ButtonContainer?.Add(button);
        }

        void CreateSectionHeader()
        {
            var titleContainer = new VisualElement();
            titleContainer.AddToClassList(StyleConstants.HorizontalContainerClass);
            var title = new Label(Title);
            title.AddToClassList("onboarding-section-title");
            titleContainer.Add(title);
            AddThreeDotMenuIn(titleContainer);
            Root.Add(titleContainer);
        }

        void AddMainContent()
        {
            if (!string.IsNullOrEmpty(ShortDescription))
            {
                var text = new Label(ShortDescription);
                text.AddToClassList(StyleConstants.OnBoardingShortDescription);
                Root.Add(text);
            }
        }

        void AddMainButton()
        {
            m_ButtonContainer = new VisualElement();
            Root.Add(m_ButtonContainer);
            if (!string.IsNullOrEmpty(ButtonLabel) && OnButtonClicked != null)
            {
                m_Button = new Button(OnButtonClicked) {text = ButtonLabel};
#if MULTIPLAYER_CENTER_ANALYTICS_AVAILABLE
                m_Button.clicked += () => AnalyticsProvider.SendInteractionEvent(InteractionDataType.CallToAction, ButtonLabel);
#endif
                m_Button.AddToClassList(StyleConstants.OnBoardingSectionMainButton);
                m_ButtonContainer.Add(m_Button);
            }
        }

        void AddResourcesLinks()
        {
            var links = new List<(string, string)>(Links);
            if (links.Count == 0)
                return; 
            
            Root.Add(new Label("Resources"));
            
            var linksDisplay = new VisualElement();
            linksDisplay.AddToClassList(StyleConstants.HorizontalContainerClass);
            linksDisplay.AddToClassList("flex-wrap");
            for (var index = 0; index < links.Count; index++)
            {
                var (label, link) = links[index];
                var docButton = new Button() {text = label};
                docButton.AddToClassList(StyleConstants.DocButtonClass);
                docButton.clicked += () => OpenResourceLink(link, label);
                linksDisplay.Add(docButton);
                
                if(index < links.Count - 1)
                    linksDisplay.Add(new Label("|") {name = "doc-button-separator"});
            }

            Root.Add(linksDisplay);
        }

        void AddThreeDotMenuIn(VisualElement titleContainer)
        {
            if(LessImportantLinks.Length < 1) return;
            
            var threeDotButton = new Button(() =>
            {
                var menu = new GenericMenu();
                foreach (var (label, link) in LessImportantLinks)
                {
                    menu.AddItem(new GUIContent(label), false, () => OpenResourceLink(link, label));
                }

                menu.ShowAsContext();
            });
            threeDotButton.AddToClassList("three-dot-button");
            threeDotButton.AddToClassList("icon-three-dots");
            var flexSpacer = new VisualElement();
            flexSpacer.AddToClassList("flex-spacer");
            titleContainer.Add(flexSpacer);
            titleContainer.Add(threeDotButton);
        }

        void OpenResourceLink(string link, string label)
        {
#if MULTIPLAYER_CENTER_ANALYTICS_AVAILABLE
            AnalyticsProvider.SendInteractionEvent(InteractionDataType.Link, label);
#endif
            Application.OpenURL(link);
        }
    }
}
