using System.IO;
using UnityEditor;
using UnityEngine;

public class EDebugSettingsProvider : SettingsProvider
{
    private static EDebugSettings settings;

    public EDebugSettingsProvider(string path, SettingsScope scope = SettingsScope.User)
        : base(path, scope)
    {
    }

    public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
    {
        settings = GetOrCreateSettings();
    }

    public override void OnGUI(string searchContext)
    {
        if (settings == null)
        {
            settings = GetOrCreateSettings();
        }

        EditorGUI.BeginChangeCheck();

        settings.intColor = EditorGUILayout.ColorField("Int Color", settings.intColor);
        settings.boolTrueColor = EditorGUILayout.ColorField("Bool True Color", settings.boolTrueColor);
        settings.boolFalseColor = EditorGUILayout.ColorField("Bool False Color", settings.boolFalseColor);
        settings.stringColor = EditorGUILayout.ColorField("String Color", settings.stringColor);
        settings.floatColor = EditorGUILayout.ColorField("Float Color", settings.floatColor);
        settings.listColor = EditorGUILayout.ColorField("List Color", settings.listColor);
        settings.warningColor = EditorGUILayout.ColorField("Warning Color", settings.warningColor);
        settings.errorColor = EditorGUILayout.ColorField("Error Color", settings.errorColor);

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
    }

    [SettingsProvider]
    public static SettingsProvider CreateEDebugSettingsProvider()
    {
        var provider = new EDebugSettingsProvider("Preferences/EDebug", SettingsScope.User);
        return provider;
    }

    private static EDebugSettings GetOrCreateSettings()
    {
        var settings = AssetDatabase.FindAssets("t:EDebugSettings");
        if (settings.Length > 0)
        {
            var path = AssetDatabase.GUIDToAssetPath(settings[0]);
            return AssetDatabase.LoadAssetAtPath<EDebugSettings>(path);
        }

        var newSettings = ScriptableObject.CreateInstance<EDebugSettings>();
        AssetDatabase.CreateAsset(newSettings, "Assets/Plugins/Debug.Log Extensions/Resources/EDebugSettings.asset");
        AssetDatabase.SaveAssets();
        return newSettings;
    }
}
