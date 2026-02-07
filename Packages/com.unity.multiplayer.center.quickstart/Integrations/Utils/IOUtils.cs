using System;
using System.IO;
using UnityEngine;
namespace Unity.Multiplayer.Center
{
    /// <summary>
    /// Utils to manipulate files and directories.
    /// </summary>
    internal static class IOUtils
    {
        /// <summary>
        /// Copies a directory from source to destination. If the destination does not exist, it will be created.
        /// The use of trailing slashes should be consistent for both input paths.
        /// </summary>
        /// <param name="sourcePath">The directory to copy.</param>
        /// <param name="destinationPath">The destination.</param>
        /// <returns>True if the copy was successful, false otherwise.</returns>
        public static bool DirectoryCopy(string sourcePath, string destinationPath)
        {
            try
            {
                Directory.CreateDirectory(destinationPath);
            
                foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                    Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));
                
                // Now copy all the files except potential meta files
                foreach (var newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                {
                    if (newPath.EndsWith(".meta"))
                        continue;
                    File.Copy(newPath, newPath.Replace(sourcePath, destinationPath), true);
                }

                return true;
            }
            catch (IOException e)
            {
                Debug.LogError($"Failed to copy directory from {sourcePath} to {destinationPath}. Error: {e.Message}");
                return false;
            }
        }
    }
}
