using System;
using UnityEngine;

namespace Unity.Multiplayer.Center.Integrations.DynamicSample
{
    /// <summary>
    /// Interface for a dynamic sample. A dynamic sample is like a package sample, but it is not managed by the
    /// package manager, and it has more features like doing pre-import checks and post-import setups.
    /// To import a IDynamicSample, use the <see cref="DynamicSampleImporter"/>.
    /// </summary>
    internal interface IDynamicSample
    {
        /// <summary>
        /// A human-readable name of the sample.
        /// </summary>
        public string SampleName { get; }
        
        /// <summary>
        /// Path to imported sample. Should be relative to the project root and start with "Assets".
        /// </summary>
        public string PathToImportedSample { get; }
        
        /// <summary>
        /// Checks if the sample import can start.
        /// This should include any check for existing import, user confirmation in case of overwrite, etc.
        /// </summary>
        /// <returns>True if the sample can be imported.</returns>
        public bool PreImportCheck();

        /// <summary>
        /// Copy the scripts that need to be compiled.
        /// </summary>
        /// <returns>True if the operation was successful.</returns>
        public bool CopyScripts();
        
        /// <summary>
        /// If the sample needs to do any post-import setup, this should return true when the application is ready for it.
        /// A typical use-case would be to check if some types are available in the project.
        /// If no post import is needed, this should return true.
        /// </summary>
        /// <returns>True if PostImport can be called.</returns>
        public bool IsReadyForPostImport { get; }
        
        /// <summary>
        /// Wires up prefabs etc. If no PostImport is needed, this should return true.
        /// </summary>
        /// <returns>True in case of success, false otherwise.</returns>
        public bool PostImport();
    }
}
