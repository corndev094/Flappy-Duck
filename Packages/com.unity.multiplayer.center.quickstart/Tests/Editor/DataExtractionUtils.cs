using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Unity.Multiplayer.Center.GettingStartedTab.Tests
{
    static class DataExtractionUtils
    {
        // Constants for Regex expressions
        const string k_UrlStartPattern = @"https://[^\n""]*";                                               // Identifying and extracting URLs from text files with multiple lines
        
        /// <summary>
        /// Extracting the value of all public const string fields from a given type
        /// </summary>
        /// <param name="type">The class type that should be processed</param>
        /// <returns>List of the values of all const string fields in the class</returns>
        public static List<string> ExtractPublicConstStringsFromType(Type type)
        {
            var stringConstants = type
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string));

            return stringConstants.Select(fieldInfo => (string)fieldInfo.GetValue(null)).ToList();
        }

        /// <summary>
        /// Reads the file at the given path line by line
        /// and extracts all the URLs found there
        /// </summary>
        /// <param name="urlContainingFilePath">Path of the file that should be scanned</param>
        /// <returns>The URLs found in this file</returns>
        public static IEnumerable<string> ExtractAllUrlsFromFile(string urlContainingFilePath)
        {
            return ExtractAllUrlsFromText(File.ReadAllLines(urlContainingFilePath));
        }
        
        /// <summary>
        /// Reads the text array line by line
        /// and extracts all the URLs found there
        /// </summary>
        /// <param name="urlContainingText">Text that should be scanned</param>
        /// <returns>The URLs found in this text</returns>
        public static IEnumerable<string> ExtractAllUrlsFromText(IEnumerable<string> urlContainingText)
        {
            var extractedUrls = new List<string>();

            foreach (var line in urlContainingText)
            {
                var matches = Regex.Matches(line, k_UrlStartPattern);
                foreach (Match match in matches)
                {
                    extractedUrls.Add(match.Value);
                }
            }

            return extractedUrls;
        }
    }
}
