using System;
using NUnit.Framework;

namespace Unity.Multiplayer.Center.GettingStartedTab.Tests
{
    class PublicStringExtractionTestClass
    {
        public const string testString = "Test";
        public const string testString2 = "Test2";
        public const string testString3 = "Test3";
    }
    
    [TestFixture]
    class DataExtractionUtilsTests
    {
        const string k_TextLink1 = "https://docs-multiplayer.unity3d.com/netcode/current/terms-concepts/network-topologies/#client-hosted-listen-server";
        const string k_TextLink2 = "https://docs.unity3d.com/Packages/com.unity.netcode@latest";
        const string k_TextWithLinksExample =
            "      MainPackageId: \n" +
            "      DocUrl: " + k_TextLink1 + "\n" +
            "      ShortDescription: A player will be the host of your game. You will not need\n" + 
            "..." + 
            "      ShortDescription: Netcode for Entities is a Unity package that allows you to\n" +
            "        create multiplayer games using the ECS paradigm.\n" +
            "      DocsUrl: " + k_TextLink2 + "\n" +
            "      AdditionalPackages: []\n" + 
            "..." ;

        const string k_ScriptLink1 = "https://docs-multiplayer.unity3d.com/netcode/current/terms-concepts/network-topologies/";
        const string k_ScriptLink2 = "https://docs.unity3d.com/Packages/com.unity.dedicated-server@latest";
        const string k_ScriptWithLinksExample =
            "    internal static class DocLinks\n" +
            "    {\n" +
            "        public const string NetworkTopology = \"" + k_ScriptLink1 + "\";\n" +
            "        public const string DedicatedServerPackage = \"" + k_ScriptLink2 + "\";\n" +
            "    }";
        
        [Test]
        public void ExtractPublicConstStringsFromType_ExtractsCorrectly()
        {
            var extractedValues = DataExtractionUtils.ExtractPublicConstStringsFromType(typeof(PublicStringExtractionTestClass));
            CollectionAssert.AreEquivalent(new [] {PublicStringExtractionTestClass.testString, PublicStringExtractionTestClass.testString2, PublicStringExtractionTestClass.testString3}, extractedValues);
        }
        
        [Test]
        public void ExtractAllUrlsFromText_ExtractsCorrectly()
        {
            var extractedValues = DataExtractionUtils.ExtractAllUrlsFromText(k_TextWithLinksExample.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
            CollectionAssert.AreEquivalent(new [] {k_TextLink1, k_TextLink2}, extractedValues);
        }
        
        [Test]
        public void ExtractAllUrlsFromScript_ExtractsCorrectly()
        {
            var extractedValues = DataExtractionUtils.ExtractAllUrlsFromText(k_ScriptWithLinksExample.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
            CollectionAssert.AreEquivalent(new [] {k_ScriptLink1, k_ScriptLink2}, extractedValues);
        }
    }
}
