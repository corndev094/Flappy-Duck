using UnityEngine;
using UnityEditor;
using Cysharp.Threading.Tasks;

[CustomEditor(typeof(EntitySpawnTemplate))]
public class EntitySpawnTemplateEditor : Editor
{
    private SerializedProperty wavesProp;
    private readonly Color[] pathColors = {
        Color.yellow, Color.cyan, Color.magenta, Color.green, Color.red, Color.blue, Color.white
    };

    void OnEnable()
    {
        wavesProp = serializedObject.FindProperty("waves");
    }

    private bool _showSpeedSimulation = false;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var spawner = (EntitySpawnTemplate)target;
        EditorGUILayout.Space();

        _showSpeedSimulation = EditorGUILayout.Toggle("Show Speed Simulation", _showSpeedSimulation);

        if (Application.isPlaying)
        {
            if (GUILayout.Button("Start Wave Sequence", GUILayout.Height(30)))
            {
                spawner.StartSequence().Forget();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Enter Play Mode to enable the 'Start Wave Sequence' button.", MessageType.Info);
        }
    }

    private void OnSceneGUI()
    {
        serializedObject.Update();
        
        for (int i = 0; i < wavesProp.arraySize; i++)
        {
            SerializedProperty waveProp = wavesProp.GetArrayElementAtIndex(i);
            DrawPathForWave(waveProp, i);
        }

        serializedObject.ApplyModifiedProperties();

        if (_showSpeedSimulation)
        {
            for (int i = 0; i < wavesProp.arraySize; i++)
            {
                SerializedProperty waveProp = wavesProp.GetArrayElementAtIndex(i);
                SimulateSpeedForWave(waveProp);
            }
            SceneView.RepaintAll(); // Keep repainting for animation
        }
    }

    private void SimulateSpeedForWave(SerializedProperty waveProp)
    {
        var startTransform = waveProp.FindPropertyRelative("startPoint").objectReferenceValue as Transform;
        var endTransform = waveProp.FindPropertyRelative("endPoint").objectReferenceValue as Transform;
        var speed = waveProp.FindPropertyRelative("speed").floatValue;

        if (startTransform == null || endTransform == null || speed <= 0) return;

        Vector3 startPos = startTransform.position;
        Vector3 endPos = endTransform.position;
        float distance = Vector3.Distance(startPos, endPos);
        float duration = distance / speed;

        if (duration <= 0) return;

        float t = (float)(EditorApplication.timeSinceStartup % duration) / duration;
        Vector3 simPos = Vector3.Lerp(startPos, endPos, t);

        Handles.color = Color.white;
        Handles.SphereHandleCap(0, simPos, Quaternion.identity, 0.5f, EventType.Repaint);
    }


    private void DrawPathForWave(SerializedProperty waveProp, int index)
    {
        SerializedProperty startPointProp = waveProp.FindPropertyRelative("startPoint");
        SerializedProperty endPointProp = waveProp.FindPropertyRelative("endPoint");
        SerializedProperty waveNameProp = waveProp.FindPropertyRelative("waveName");

        var startTransform = startPointProp.objectReferenceValue as Transform;
        var endTransform = endPointProp.objectReferenceValue as Transform;

        if (startTransform == null || endTransform == null) return;

        Vector3 startPos = startTransform.position;
        Vector3 endPos = endTransform.position;
        Color pathColor = pathColors[index % pathColors.Length];

        // --- Visualization ---
        Handles.color = pathColor;
        string waveLabel = string.IsNullOrEmpty(waveNameProp.stringValue) ? $"Wave {index}" : waveNameProp.stringValue;
        Handles.Label(startPos + Vector3.up * 0.2f, $"{waveLabel} Start");
        Handles.Label(endPos + Vector3.up * 0.2f, $"{waveLabel} End");
        Handles.DrawDottedLine(startPos, endPos, 5.0f);

        // --- Handles for moving points ---
        EditorGUI.BeginChangeCheck();
        Vector3 newStartPos = Handles.PositionHandle(startPos, Quaternion.identity);
        Vector3 newEndPos = Handles.PositionHandle(endPos, Quaternion.identity);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObjects(new Object[] { startTransform, endTransform }, $"Move {waveLabel} Points");
            startTransform.position = newStartPos;
            endTransform.position = newEndPos;
            EditorUtility.SetDirty(startTransform);
            EditorUtility.SetDirty(endTransform);
        }
    }
}
