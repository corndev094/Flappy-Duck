using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Unity.Multiplayer.Center.GettingStartedTab.Tests
{
    [TestFixture]
    class UrlUtilsTests
    {
        const string k_ValidUrlGoogle = "https://www.google.com";
        const string k_ComplexFixedVersionPackageUrl = "https://docs.unity3d.com/Packages/com.unity.entities@1.2/manual/editor-hierarchy-window.html#:~:text=To%20open%20the%20Entities%20Hierarchy,name%2C%20ID%2C%20or%20component.";
        const string k_ComplexFixedVersionPackageUrlBase = "https://docs.unity3d.com/Packages/com.unity.entities@1.2";
        const string k_ComplexFixedVersionPackageBase = "https://docs.unity3d.com/Packages/com.unity.entities";
        const string k_ComplexFixedVersionPackageVersion = "1.2";
        const string k_SimplePackageUrl = "https://docs.unity3d.com/Packages/com.unity.netcode@latest";
        
        static string[] s_ValidUrls = {
            k_ValidUrlGoogle,
            k_ComplexFixedVersionPackageUrl
        };
        
        static string[] s_InvalidUrls = {
            "/Packages/com.unity.entities@1.2",
            "Test",
            ""
        };
        
        static string[] s_MalformedUrls = {
            "https://www.goo gle.com",
            "https://d o c s"
        };
        
        static string[] s_PackageUrls = {
            k_SimplePackageUrl,
            k_ComplexFixedVersionPackageUrl
        };

        const string k_ServerResponseWithRedirectionInfo =
@"<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Strict//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd'>
<html lang='en' xml:lang='en' xmlns='http://www.w3.org/1999/xhtml'>
<head>
    ...
    <script type='text/javascript'>
        ...
        let targetDestination = (isPreview ? '../com.unity.entities@1.2' : '../com.unity.entities@1.2') + (subFolder || '/index.html') + anchorString;
        ...
    </script>
    ...
</html>";

        [Test]
        public void URLStatusCodeIsValid_ValidDetectedCorrectly([ValueSource(nameof(s_ValidUrls))] string validUrl)
        {
            Assert.True(UrlUtils.URLStatusCodeIsValid(validUrl));
        }
        
        [Test]
        public void URLStatusCodeIsValid_InvalidDetectedCorrectly([ValueSource(nameof(s_InvalidUrls))] string invalidUrl)
        {
            var exception = Assert.Catch(() => UrlUtils.URLStatusCodeIsValid(invalidUrl));
            Assert.True(exception is UriFormatException or System.Net.WebException);
        }

        [Test]
        public void FilterAndCheckPotentialUrls_URLsValidityDetectedCorrectly()
        {
            var filteredValidUrls = UrlUtils.FilterAndCheckPotentialUrls(s_ValidUrls);
            Assert.AreEqual(s_ValidUrls.Length, filteredValidUrls.Count());
            
            var filteredInvalidUrls = UrlUtils.FilterAndCheckPotentialUrls(s_InvalidUrls);
            Assert.AreEqual(0, filteredInvalidUrls.Count());
            
            var filteredMalformedUrls = new List<string>();
            Assert.Throws<AssertionException>(() =>
            {
                filteredMalformedUrls.AddRange(UrlUtils.FilterAndCheckPotentialUrls(s_MalformedUrls));
            });
            Assert.AreEqual(0, filteredMalformedUrls.Count());
        }
        
        [Test]
        public void IsPackageUrl_DetectsPackageUrls([ValueSource(nameof(s_PackageUrls))] string packageUrl)
        {
            Assert.True(UrlUtils.IsPackageUrl(packageUrl));
        }
        
        [Test]
        public void IsPackageUrl_DetectsNotPackageUrls()
        {
            Assert.False(UrlUtils.IsPackageUrl(k_ValidUrlGoogle));
        }
        
        [Test]
        public void ExtractPackageUrl_ExtractsUrlBaseCorrectly()
        {
            var result = UrlUtils.ExtractPackageUrl(k_ComplexFixedVersionPackageUrl);
            Assert.AreEqual(k_ComplexFixedVersionPackageUrlBase, result);
        }
        
        [Test]
        public void IsFixedVersionPackageUrl_FixedVersionDetected()
        {
            Assert.True(UrlUtils.IsFixedVersionPackageUrl(k_ComplexFixedVersionPackageUrlBase));
            Assert.False(UrlUtils.IsFixedVersionPackageUrl(k_SimplePackageUrl));
        }
        
        [Test]
        public void SplitPackageUrl_SimpleSplittingIsCorrect()
        {
            Assert.True(UrlUtils.SplitPackageUrl(k_ComplexFixedVersionPackageUrlBase, out var urlPart, out var version));
            Assert.AreEqual(k_ComplexFixedVersionPackageBase, urlPart);
            Assert.AreEqual(k_ComplexFixedVersionPackageVersion, version);
        }
        
        [Test]
        public void SplitPackageUrl_ComplexSplittingIsCorrect()
        {
            Assert.False(UrlUtils.TryGetPackageUrlBaseAndVersion(s_InvalidUrls.FirstOrDefault(), out _, out _));
            
            Assert.True(UrlUtils.TryGetPackageUrlBaseAndVersion(k_ComplexFixedVersionPackageUrl, out var urlPart, out var version));
            Assert.AreEqual(k_ComplexFixedVersionPackageBase, urlPart);
            Assert.AreEqual(k_ComplexFixedVersionPackageVersion, version);
        }
        
        [Test]
        public void GetVersionNumberFromRedirection_LatestVersionDetectedCorrectly()
        {
            var latestVersionNumber = UrlUtils.GetVersionNumberFromServerResponse(k_ServerResponseWithRedirectionInfo);
            Assert.AreEqual(k_ComplexFixedVersionPackageVersion, latestVersionNumber);
        }
    }
}
