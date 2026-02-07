using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.Multiplayer.Center.GettingStartedTab.Tests
{
    /// <summary>
    /// Util functions to do checks and data extraction from URLs
    /// Mostly Regex related functions
    /// </summary>
    static class UrlUtils
    {
        // The URLs and its components that this class is processing
        //
        // Worst case link to check example: https://docs.unity3d.com/Packages/com.unity.entities@1.2/manual/editor-hierarchy-window.html#:~:text=To%20open%20the%20Entities%20Hierarchy,name%2C%20ID%2C%20or%20component.
        // Extracted Package URL with version: https://docs.unity3d.com/Packages/com.unity.entities@1.2
        // Base URL: https://docs.unity3d.com/Packages/com.unity.entities
        // Version: 1.2
        
        // Constants for string searches
        const string k_PackageUrlPrefix = "https://docs.unity3d.com/Packages/";     // Used for quickly checking if the url is a package URL
        const string k_ForwardDestinationInfoEntry = "targetDestination";   // Identifies redirections info in the body data received from the remote server
        
        // Constants for Regex expressions
        const string k_UnityPackageUrlPattern = @"https://docs\.unity3d\.com/Packages/com\.unity\.[^/]+";   // Extracting the package URL without subpages from a full URL
        const string k_FixedPackageVersionPattern = @"@\d+(\.\d+)+$";                                       // Identifying Package URLs with fixed version numbers (instead of @Latest tags)
        const string k_PackageUrlVersionPattern = @"^(.*)@(\d+\.\d+)";                                      // Splitting package URL into base URL and version
        const string k_ForwardDestinationVersionPattern = @"@(\d+\.\d+)'";                                  // Extracting the version number from the redirection info from the remote server
        

        /// <summary>
        /// Creates a WebRequest and checks if its valid or not by the response status code
        /// </summary>
        /// <param name="url">URL to be checked</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool URLStatusCodeIsValid(string url)
        {
            WebRequest request = WebRequest.Create (url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            var responseCode = response.StatusCode;
            response.Close();
            return responseCode == HttpStatusCode.OK;
        }

        /// <summary>
        /// Goes through the list of inputString to find urls. Fails the test in case a malformed URL is found
        /// </summary>
        /// <param name="potentialUrls">String data that can be URLs or not</param>
        /// <returns>Only the URLs</returns>
        public static IEnumerable<string> FilterAndCheckPotentialUrls(IEnumerable<string> potentialUrls)
        {
            var correctUrls = new List<string>();
            
            foreach (var potentialUrl in potentialUrls)
            {
                var shouldBeUrl = potentialUrl.StartsWith("http");
                if (!shouldBeUrl)
                {
                    continue;
                }
                
                var isMalformedUrl = !Uri.TryCreate(potentialUrl, UriKind.Absolute, out var uri);
                if (isMalformedUrl)
                {
                    Assert.Fail($"URL {potentialUrl} is malformed.");
                }
                else
                {
                    correctUrls.Add(potentialUrl);
                }
            }

            return correctUrls;
        }
        
        /// <summary>
        /// Quickly checks if the given URL is a package URL
        /// </summary>
        /// <param name="fullUrl">URL to be checked</param>
        /// <returns>True if the URL is a package URL</returns>
        public static bool IsPackageUrl(string fullUrl)
        {
            return fullUrl.StartsWith(k_PackageUrlPrefix);
        }
        
        /// <summary>
        /// Extracts the package related URL parts from the full URL
        /// and discards the sub-pages and not needed parts.
        /// </summary>
        /// <param name="fullUrl">The URL to be processed</param>
        /// <returns>The package base URL with version: url@version </returns>
        public static string ExtractPackageUrl(string fullUrl)
        {
            var match = Regex.Match(fullUrl, k_UnityPackageUrlPattern);
            return match.Success ? match.Value : null;
        }
        
        /// <summary>
        /// Checks if the given package URL is pointing to a fixed version number, e.g.: @1.2
        /// </summary>
        /// <param name="packageUrl">The base URL of the package</param>
        /// <returns>True if the URL is using a fixed version number, false otherwise (e.g. @latest)</returns>
        public static bool IsFixedVersionPackageUrl(string packageUrl)
        {
            return Regex.IsMatch(packageUrl, k_FixedPackageVersionPattern);
        }

        /// <summary>
        /// Splits the package URL into its base and version parts if possible
        ///
        /// E.g.
        /// Orig: https://docs.unity3d.com/Packages/com.unity.entities@1.2
        ///
        /// After split:
        /// Base: https://docs.unity3d.com/Packages/com.unity.entities
        /// Version: 1.2
        /// </summary>
        /// <param name="packageUrl">The package URL that needs to be split</param>
        /// <param name="urlBase">The URL part before the version number</param>
        /// <param name="packageVersion">The version number from the URL</param>
        /// <returns>True if the splitting was successful</returns>
        public static bool SplitPackageUrl(string packageUrl, out string urlBase, out string packageVersion)
        {
            var match = Regex.Match(packageUrl, k_PackageUrlVersionPattern);

            if (match.Success) {
                urlBase = match.Groups[1].Value; 
                packageVersion = match.Groups[2].Value;

                return true;
            }

            urlBase = null;
            packageVersion = null;
            return false;
        }

        /// <summary>
        /// Determines if the URL is a package URL and in that case returns the URL before the version number and the version number through the output parameters
        /// </summary>
        /// <param name="url"></param>
        /// <param name="urlBase"></param>
        /// <param name="versionNumber"></param>
        /// <returns>False in case the URL is not a Package URL or doesn't contain a package version</returns>
        public static bool TryGetPackageUrlBaseAndVersion(string url, out string urlBase, out string versionNumber)
        {
            urlBase = null;
            versionNumber = null;
            
            if (!IsPackageUrl(url))             // This check is only working on package URLs, e.g.: https://docs.unity3d.com/Packages/com.unity.transport@2.2
            {
                return false;
            }
            
            var packageUrl = ExtractPackageUrl(url);                          // Only need the URL with the version and no sub-pages

            if (!IsFixedVersionPackageUrl(packageUrl))                             // If it has no fixed version number or already pointing to @latest, we don't need any extra checks 
            {
                return false;
            }

            return SplitPackageUrl(packageUrl, out urlBase, out versionNumber);
        }
        
        /// <summary>
        /// Extracts version info from the redirection of a UnityWebRequest
        /// that are already sent and received data from the remote server
        /// </summary>
        /// <param name="redirectedWebRequest">WebRequest containing the redirection info</param>
        /// <returns>Version number where the request is redirected to</returns>
        public static string GetVersionNumberFromRedirection(UnityWebRequest redirectedWebRequest)
        {
            if(redirectedWebRequest.result != UnityWebRequest.Result.Success) 
            {
                Assert.Fail("Error fetching URL: " + redirectedWebRequest.error);
            }

            Assert.NotNull(redirectedWebRequest.downloadHandler);

            return GetVersionNumberFromServerResponse(redirectedWebRequest.downloadHandler.text);
        }

        /// <summary>
        /// Extracts version info from the response of a server received through a UnityWebRequest
        /// </summary>
        /// <param name="serverResponse">Server response containing the redirection info</param>
        /// <returns>Version number where the request is redirected to</returns>
        public static string GetVersionNumberFromServerResponse(string serverResponse)
        {
            string latestVersionNumber = null;

            foreach (var line in serverResponse.Split('\n'))
            {
                if (line.Contains(k_ForwardDestinationInfoEntry))
                {
                    var match = Regex.Match(line, k_ForwardDestinationVersionPattern);
                    if (match.Success)
                    {
                        latestVersionNumber = match.Groups[1].Value; 
                    }
                }
            }

            return latestVersionNumber;
        }
    }
}
