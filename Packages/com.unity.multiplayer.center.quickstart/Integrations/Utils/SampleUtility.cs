using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor.PackageManager.UI;

namespace Unity.Multiplayer.Center
{
    /// <summary>
    /// Utility class for handling samples and scenes if the package has samples. 
    /// </summary>
    internal static class SampleUtility
    {
        /// <summary>
        /// Tries to find the sample in the package manager.
        /// </summary>
        /// <param name="packageId">The package name/id e.g. com.unity.netcode</param>
        /// <param name="sampleName">The exact name of the sample</param>
        /// <param name="sample">The found sample</param>
        /// <returns>False if the sample is not found</returns>
        public static bool TryFindSample(string packageId, string sampleName, out Sample sample)
        {
            try
            {
                var samples = Sample.FindByPackage(packageId, packageVersion: null);
                sample = samples.First(e => e.displayName == sampleName);
                return true;
            }
            catch (Exception)
            {
                sample = default;
                return false;
            }
        }
        
        /// <summary>
        /// Retrieves a sample based on the specified package ID and sample name. 
        /// </summary>
        /// <param name="packageId">The ID of the package to which the sample belongs.</param>
        /// <param name="sampleName">The name of the sample to retrieve.</param>
        /// <param name="cachedSample">A reference to the cached sample object. This will be updated if the sample is not already cached.</param>
        /// <returns>Returns the retrieved or cached sample. If the sample is not found, returns a default sample object.</returns>
        public static Sample GetSample(string packageId, string sampleName, ref Sample cachedSample)
        {
            if (!string.IsNullOrEmpty(cachedSample.displayName))
                return cachedSample;

            if (!TryFindSample(packageId, sampleName, out var sample))
            {
                Debug.LogError($"Could not find the sample {sampleName}");
                return default(Sample);
            }

            cachedSample = sample;
            return sample;
        }

        /// <summary>
        /// Imports the sample if it is not imported yet or imports the scene at the given import path
        /// </summary>
        /// <param name="sample">The sample to be imported or opened.</param>
        /// <param name="sceneName">The name of the scene to open without extension.</param>
        public static void ImportOrOpenSample(Sample sample, string sceneName)
        {
            if (sample.isImported)
            {
                OpenSampleScene(sceneName, sample.importPath);
            }
            else
            {
                sample.Import();
            }
        }

        /// <summary>
        /// Opens the sample scene using the provided scene name and import path.
        /// </summary>
        /// <param name="sceneName">The name of the scene to open.</param>
        /// <param name="importPath">The path to the sample.</param>
        static void OpenSampleScene(string sceneName, string importPath)
        {
            var projectPath = Path.GetDirectoryName(Application.dataPath); // Above assets folder
            var relativeImportPath = Path.GetRelativePath(projectPath, importPath);
            var assets = AssetDatabase.FindAssets($"{sceneName} t:scene", new[] {relativeImportPath});
            if (assets.Length < 1)
            {
                return;
            }

            var scenePath = AssetDatabase.GUIDToAssetPath(assets[0]);
            EditorSceneManager.OpenScene(scenePath);
        }
    }
}
