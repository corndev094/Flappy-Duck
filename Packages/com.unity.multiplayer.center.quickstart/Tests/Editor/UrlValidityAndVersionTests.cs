using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Multiplayer.Center.Common;
using Unity.Multiplayer.Center.Integrations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.Multiplayer.Center.GettingStartedTab.Tests
{
    [TestFixture]
    class UrlValidityAndVersionTests
    {
        static Type[] s_UrlContainingClasses = {                   // Checking public const string fields for URLs in these classes
            typeof(DocLinks),
            typeof(CommunityLinks),
            typeof(SampleLinks),
            typeof(AssetStoreSlugs)
        };
        
        static string[] s_UrlContainingTextFiles = {               // Extracting URLs with regex from these files and checking them
            "Packages/com.unity.multiplayer.center/Editor/Recommendations/RecommendationAuthoringData.cs",
            "Packages/com.unity.multiplayer.center/Editor/Recommendations/RecommendationData_6000.0.recommendations",
        };
        
        const string k_LatestPackageVersionTag = "@latest";                 // Used for constructing URLs for the latest package versions
        
        [Test]
        public async Task Classes_UrlsAreValid([ValueSource(nameof(s_UrlContainingClasses))] Type urlContainingClass)
        {
            var publicConstStrings = DataExtractionUtils.ExtractPublicConstStringsFromType(urlContainingClass);
            var precheckedUrls =  UrlUtils.FilterAndCheckPotentialUrls(publicConstStrings);
            
            await AssertUrlsAreCorrect(precheckedUrls);
        }

        [Test]
        public async Task AllOnboardingSections_HaveValidUrlsInDocLinks()
        {
            // Check all the public const string in case they contain URLs
            var typesWithAttribute = TypeCache.GetTypesWithAttribute<OnboardingSectionAttribute>();
            var potentialUrls = typesWithAttribute
                    .Select(DataExtractionUtils.ExtractPublicConstStringsFromType)
                    .Select(UrlUtils.FilterAndCheckPotentialUrls)
                    .SelectMany(potentialUrls => potentialUrls);
            
            await AssertUrlsAreCorrect(potentialUrls);

            // Check all the links in the Links tuple of the OnboardingSections
            var defaultSectionInstancesLinks = new List<string>();
            var derivedTypes = TypeCache.GetTypesDerivedFrom<DefaultSection>();

            try
            {
                foreach (var type in derivedTypes)
                {
                    var instance = (DefaultSection)Activator.CreateInstance(type);
                    var links = instance.Links.Select(link => link.Item2);
                    links = UrlUtils.FilterAndCheckPotentialUrls(links);
                    defaultSectionInstancesLinks.AddRange(links);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to create an instance of a DefaultSection: {e.Message}");
            }
            
            await AssertUrlsAreCorrect(defaultSectionInstancesLinks);
        }

        [Test]
        public async Task TextFiles_UrlsAreValid([ValueSource(nameof(s_UrlContainingTextFiles))] string urlContainingFilePath)
        {
            var urlStrings = DataExtractionUtils.ExtractAllUrlsFromFile(urlContainingFilePath);
            var potentialUrls =  UrlUtils.FilterAndCheckPotentialUrls(urlStrings);
            
            await AssertUrlsAreCorrect(potentialUrls);
        }
        
        /// <summary>
        /// Checks if the given UnityWebRequest was successful or not
        /// </summary>
        /// <param name="webRequest">The web request that should be checked</param>
        static void AssertWebRequestResult(UnityWebRequest webRequest)
        {
            if (webRequest.result == UnityWebRequest.Result.ConnectionError && webRequest.responseCode != 200)
            {
                Assert.Fail($"Network or HTTP error occurred while opening URL: {webRequest.url}");
            }
            else
            {
                Debug.Log($"URL {webRequest.url} is valid.");       // Todo: Maybe Remove, but it's a great help for debugging
            }
        }
        
        /// <summary>
        /// Checks if the URLs are correctly formatted,
        /// and they are pointing to a valid page,
        /// and in case they are package links, they are pointing to the latest version
        /// </summary>
        /// <param name="urls">The urls to check</param>
        static async Task AssertUrlsAreCorrect(IEnumerable<string> urls)
        {
            foreach (var url in urls)
            {
                var webRequest = UnityWebRequest.Get(url);
                await webRequest.SendWebRequest();
                AssertWebRequestResult(webRequest);
                
                await AssertPackageVersionIsLatest(url);
            }
        }

        /// <summary>
        /// If the given URL is a package URL with a fixed version number,
        /// this method will assert that its version is the latest
        /// </summary>
        /// <param name="potentialPackageUrl">URL that need to be checked</param>
        static async Task AssertPackageVersionIsLatest(string potentialPackageUrl)
        {
            if (UrlUtils.TryGetPackageUrlBaseAndVersion(potentialPackageUrl, out var urlBase, out var packageVersion))      // Only proceed to get the latest version if the URL 
            {                                                                                                                            // is a package URL with a fixed version number
                Debug.Log("Found a package URL with a fixed version, base URL: " + urlBase + ", fix version: " + packageVersion + ", Checking if it's the latest version.");
                    
                using var latestVersionWebRequest = UnityWebRequest.Get(urlBase + k_LatestPackageVersionTag);
                await latestVersionWebRequest.SendWebRequest();
                AssertWebRequestResult(latestVersionWebRequest);
                    
                var latestVersionNumber = UrlUtils.GetVersionNumberFromRedirection(latestVersionWebRequest);
                Assert.AreEqual(latestVersionNumber, packageVersion, $"The URL {potentialPackageUrl} does not point to the latest version ({latestVersionNumber})");
            }
        }
    }
}
