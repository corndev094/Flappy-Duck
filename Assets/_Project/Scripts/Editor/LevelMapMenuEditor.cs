using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LevelMapMenu))]
public class LevelMapMenuEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        LevelMapMenu script = (LevelMapMenu)target;

        GUILayout.Space(10);
        GUILayout.Label("Debug Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("Refresh Map Data"))
        {
            script.UpdateData();
        }
    }
}