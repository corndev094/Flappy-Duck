using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.Center.Integrations.DynamicSample
{
    /// <summary>
    /// Object in charge of importing a dynamic sample. We use a scriptable object mainly to handle compilation / domain
    /// reload gracefully.
    /// <example>
    /// <code>
    ///    // In function that starts the import
    ///    m_Importer = ScriptableObject.CreateInstance&lt;DynamicSampleImporter&gt;();
    ///    m_Importer.OnImported += OnImportFinished;
    ///    m_Importer.Import(new MyDynamicSampleType());
    ///
    /// 
    ///    // In the OnImportFinished callback
    ///    m_Importer.OnImported -= OnImportFinished;
    ///    Object.DestroyImmediate(m_Importer);
    /// </code>
    /// </example>
    /// </summary>
    internal class DynamicSampleImporter : ScriptableObject
    {
        [SerializeReference]
        IDynamicSample m_Setup;

        [SerializeField]
        bool m_RequirePostImport;

        public event Action<bool> OnImported;

        public void Import(IDynamicSample setup)
        {
            m_Setup = setup;
            if (!setup.PreImportCheck() || !setup.CopyScripts())
            {
                InvokeOnImported(false);
                return;
            }

            if (setup.IsReadyForPostImport)
            {
                InvokeOnImported(m_Setup.PostImport());
            }
            else
            {
                RefreshAndWaitToDoPostImport();
            }
        }

        void RefreshAndWaitToDoPostImport()
        {
            m_RequirePostImport = true;
            EditorUtility.DisplayProgressBar("Importing " + m_Setup.SampleName, "Please wait...", 0.5f);
            AssetDatabase.Refresh();
        }

        void OnEnable()
        {
            // Do not take the risk of leaving our progress bar hanging.
            EditorUtility.ClearProgressBar();
            
            // On enable will be called after domain reload.
            // This is a good place to check if we need to run the post import.
            if (!m_RequirePostImport) return;
            
            m_RequirePostImport = false;
            EditorApplication.delayCall += RunPostImport;
        }

        void RunPostImport()
        {
            if (!m_Setup.IsReadyForPostImport)
            {
                // TODO: we might need to wait a bit more and retry (observed when adding a asmdef in the sample)
                Debug.LogWarning($"The sample {m_Setup.SampleName} is not ready for post import. Please report a bug if the issue persists.");
                InvokeOnImported(false);
            }
            else
            {
                InvokeOnImported(m_Setup.PostImport());
            }
        }

        void InvokeOnImported(bool success)
        {
            Debug.Log($"Imported {m_Setup.SampleName} with success: {success}");
            OnImported?.Invoke(success);
        }
    }
}
