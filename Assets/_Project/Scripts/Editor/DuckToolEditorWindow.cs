using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class DuckToolEditorWindow : EditorWindow
{
    [SerializeField]
    private List<GameObject> pipeList = new List<GameObject>();

    private Vector3 minRange = Vector3.zero;
    private Vector3 maxRange = Vector3.one;
    private bool roundToInt;
    private bool randomizeX = true;
    private bool randomizeY = true;
    private bool randomizeZ = true;

    private enum DistributionType { Random, EvenlySpaced, PipePlacement }
    private DistributionType distributionType = DistributionType.Random;
    private Vector3 spacing = Vector3.one;
    private Vector3 startPosition = Vector3.zero;

    // Pipe Placement settings
    private enum PipeSpacingType { Fixed, Random }
    private PipeSpacingType pipeSpacingType = PipeSpacingType.Fixed;
    private float pipeSpacing = 5f;
    private float minPipeSpacing = 3f;
    private float maxPipeSpacing = 7f;

    SerializedObject serializedObject;
    SerializedProperty pipeListProperty;
    private Vector2 windowScrollPos;
    private Vector2 scrollPos;

    [MenuItem("Tools/Position Randomizer")]
    public static void ShowWindow()
    {
        GetWindow<DuckToolEditorWindow>("Position Randomizer");
    }

    private void OnEnable()
    {
        if (pipeList == null)
            pipeList = new List<GameObject>();
        serializedObject = new(this);
        pipeListProperty = serializedObject.FindProperty("pipeList");
    }

    private void OnGUI()
    {
        windowScrollPos = EditorGUILayout.BeginScrollView(windowScrollPos);
        EditorGUILayout.LabelField("Target Objects", EditorStyles.boldLabel);
        
        serializedObject.Update();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.PropertyField(pipeListProperty, true);
        EditorGUILayout.EndScrollView();
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Distribution Settings", EditorStyles.boldLabel);
        distributionType = (DistributionType)EditorGUILayout.EnumPopup("Distribution Type", distributionType);

        if (distributionType == DistributionType.Random)
        {
            EditorGUILayout.LabelField("Random Range", EditorStyles.boldLabel);
            minRange = EditorGUILayout.Vector3Field("Min", minRange);
            maxRange = EditorGUILayout.Vector3Field("Max", maxRange);
            roundToInt = EditorGUILayout.Toggle("Round To Int", roundToInt);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Randomize Axes", EditorStyles.boldLabel);
            randomizeX = EditorGUILayout.Toggle("Randomize X", randomizeX);
            randomizeY = EditorGUILayout.Toggle("Randomize Y", randomizeY);
            randomizeZ = EditorGUILayout.Toggle("Randomize Z", randomizeZ);
        }
        else if (distributionType == DistributionType.EvenlySpaced)
        {
            startPosition = EditorGUILayout.Vector3Field("Start Position", startPosition);
            spacing = EditorGUILayout.Vector3Field("Spacing", spacing);
            roundToInt = EditorGUILayout.Toggle("Round To Int", roundToInt);
        }
        else if (distributionType == DistributionType.PipePlacement)
        {
            startPosition = EditorGUILayout.Vector3Field("Start Position", startPosition);
            pipeSpacingType = (PipeSpacingType)EditorGUILayout.EnumPopup("Spacing Type", pipeSpacingType);
            if (pipeSpacingType == PipeSpacingType.Fixed)
            {
                pipeSpacing = EditorGUILayout.FloatField("Spacing", pipeSpacing);
            }
            else // Random
            {
                minPipeSpacing = EditorGUILayout.FloatField("Min Spacing", minPipeSpacing);
                maxPipeSpacing = EditorGUILayout.FloatField("Max Spacing", maxPipeSpacing);
            }
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Arrange Positions"))
        {
            ArrangePositions();
        }

        if (GUILayout.Button("Clear List"))
        {
            pipeList.Clear();
        }

        // Apply modified properties nếu dùng Odin
        if (GUI.changed)
        {
            EditorUtility.SetDirty(this);
        }
        EditorGUILayout.EndScrollView();
    }

    private void ArrangePositions()
    {
        if (pipeList == null || pipeList.Count == 0)
        {
            Debug.LogWarning("No target objects selected.");
            return;
        }

        if (distributionType == DistributionType.Random)
        {
            foreach (var obj in pipeList)
            {
                if (obj != null)
                {
                    Vector3 currentPosition = obj.transform.position;
                    float newX = randomizeX ? Random.Range(minRange.x, maxRange.x) : currentPosition.x;
                    float newY = randomizeY ? Random.Range(minRange.y, maxRange.y) : currentPosition.y;
                    float newZ = randomizeZ ? Random.Range(minRange.z, maxRange.z) : currentPosition.z;

                    Vector3 targetValue = new Vector3(newX, newY, newZ);
                    if (roundToInt)
                        targetValue = Vector3Int.RoundToInt(targetValue);

                    Undo.RecordObject(obj.transform, "Randomize Position");
                    obj.transform.position = targetValue;
                }
            }
            Debug.Log($"Positions randomized for {pipeList.Count} objects.");
        }
        else if (distributionType == DistributionType.EvenlySpaced)
        {
            for (int i = 0; i < pipeList.Count; i++)
            {
                if (pipeList[i] != null)
                {
                    Vector3 targetValue = startPosition + spacing * i;
                    if (roundToInt)
                        targetValue = Vector3Int.RoundToInt(targetValue);

                    Undo.RecordObject(pipeList[i].transform, "Arrange Position");
                    pipeList[i].transform.position = targetValue;
                }
            }
            Debug.Log($"Positions arranged for {pipeList.Count} objects.");
        }
        else if (distributionType == DistributionType.PipePlacement)
        {
            float currentX = startPosition.x;
            for (int i = 0; i < pipeList.Count; i++)
            {
                if (pipeList[i] != null)
                {
                    Vector3 targetValue = new Vector3(currentX, startPosition.y, startPosition.z);
                    Undo.RecordObject(pipeList[i].transform, "Arrange Pipe");
                    pipeList[i].transform.position = targetValue;

                    if (pipeSpacingType == PipeSpacingType.Fixed)
                    {
                        currentX += pipeSpacing;
                    }
                    else // Random
                    {
                        currentX += Random.Range(minPipeSpacing, maxPipeSpacing);
                    }
                }
            }
            Debug.Log($"Pipes arranged for {pipeList.Count} objects.");
        }
    }
}
