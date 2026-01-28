using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ShapeGeneratorWindow : EditorWindow
{
    private List<GameObject> sceneObjects = new List<GameObject>();
    private ShapeType shapeType = ShapeType.Circle;
    private Axis mainAxis = Axis.XZ; // Mặc định: hình trên mặt phẳng XZ (nhìn từ trên xuống)
    private float radius = 5f;
    private Vector2 size = new Vector2(10f, 10f);
    private float spacing = 1f;

    private enum ShapeType
    {
        Circle,
        FilledCircle,
        Rectangle,
        FilledRectangle,
        Spiral,
        Hexagon
    }

    private enum Axis
    {
        XY, // Z = 0
        XZ, // Y = 0 (mặc định)
        YZ  // X = 0
    }

    [MenuItem("Tools/Shape Generator")]
    public static void ShowWindow()
    {
        GetWindow<ShapeGeneratorWindow>("ShapeGen");
    }

    private void OnGUI()
    {
        GUILayout.Label("Shape Generator – Move Existing Objects", EditorStyles.boldLabel);

        shapeType = (ShapeType)EditorGUILayout.EnumPopup("Shape", shapeType);
        mainAxis = (Axis)(EditorGUILayout.EnumPopup("Plane", mainAxis));
        radius = EditorGUILayout.FloatField("Radius", radius);
        size = EditorGUILayout.Vector2Field("Size (Rect)", size);
        spacing = EditorGUILayout.FloatField("Spacing", spacing);

        GUILayout.Space(10);

        // Drag & Drop Area
        EditorGUILayout.LabelField("Drag & Drop **Scene GameObjects**", EditorStyles.boldLabel);
        Rect dropArea = GUILayoutUtility.GetRect(0f, 50f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drop Scene Objects Here");

        if (Event.current.type == EventType.DragUpdated && dropArea.Contains(Event.current.mousePosition))
        {
            bool valid = true;
            foreach (Object obj in DragAndDrop.objectReferences)
            {
                if (!(obj is GameObject go) || PrefabUtility.IsPartOfPrefabAsset(go))
                {
                    valid = false; break;
                }
            }
            DragAndDrop.visualMode = valid ? DragAndDropVisualMode.Move : DragAndDropVisualMode.Rejected;
            Event.current.Use();
        }
        else if (Event.current.type == EventType.DragPerform && dropArea.Contains(Event.current.mousePosition))
        {
            DragAndDrop.AcceptDrag();
            foreach (Object obj in DragAndDrop.objectReferences)
            {
                if (obj is GameObject go && !PrefabUtility.IsPartOfPrefabAsset(go))
                {
                    if (!sceneObjects.Contains(go))
                        sceneObjects.Add(go);
                }
            }
            Event.current.Use();
        }

        // Display list
        GUILayout.Space(10);
        EditorGUILayout.LabelField($"Objects ({sceneObjects.Count}):", EditorStyles.boldLabel);
        for (int i = 0; i < sceneObjects.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            sceneObjects[i] = (GameObject)EditorGUILayout.ObjectField(sceneObjects[i], typeof(GameObject), true);
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                sceneObjects.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Clear All"))
            sceneObjects.Clear();

        GUILayout.Space(10);

        if (GUILayout.Button("Arrange in Shape"))
            ArrangeObjectsInShape();
    }

    private void ArrangeObjectsInShape()
    {
        var validObjects = sceneObjects.FindAll(go => go != null && !PrefabUtility.IsPartOfPrefabAsset(go));
        if (validObjects.Count == 0)
        {
            Debug.LogWarning("No valid scene objects.");
            return;
        }

        Vector2[] planarPositions = shapeType switch
        {
            ShapeType.Circle => GenerateCircle2D(validObjects.Count, radius),
            ShapeType.FilledCircle => GenerateFilledCircle2D(validObjects.Count, radius),
            ShapeType.Rectangle => GenerateRectangle2D(validObjects.Count, size),
            ShapeType.FilledRectangle => GenerateFilledRectangle2D(validObjects.Count, size, spacing),
            ShapeType.Spiral => GenerateSpiral2D(validObjects.Count, radius, spacing),
            ShapeType.Hexagon => GenerateHexagon2D(validObjects.Count, radius),
            _ => new Vector2[validObjects.Count]
        };

        if (planarPositions.Length != validObjects.Count)
            System.Array.Resize(ref planarPositions, validObjects.Count);

        for (int i = 0; i < validObjects.Count; i++)
        {
            Undo.RecordObject(validObjects[i].transform, "Arrange Position");
            validObjects[i].transform.position = ToWorldPosition(planarPositions[i]);
        }

        Debug.Log($"Arranged {validObjects.Count} objects in {shapeType} on {mainAxis} plane.");
    }

    // === 2D shape generators (return Vector2) ===
    private Vector2[] GenerateCircle2D(int count, float radius)
    {
        Vector2[] pts = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            float a = 2f * Mathf.PI * i / count;
            pts[i] = new Vector2(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius);
        }
        return pts;
    }

    private Vector2[] GenerateFilledCircle2D(int count, float radius)
    {
        List<Vector2> pts = new List<Vector2>();
        int rings = Mathf.CeilToInt(Mathf.Sqrt(count));
        int created = 0;

        for (int r = 1; r <= rings && created < count; r++)
        {
            float rNorm = r / (float)rings;
            int ptsInRing = Mathf.Max(6, Mathf.RoundToInt(2 * Mathf.PI * r));
            for (int i = 0; i < ptsInRing && created < count; i++, created++)
            {
                float a = 2f * Mathf.PI * i / ptsInRing;
                pts.Add(new Vector2(Mathf.Cos(a) * radius * rNorm, Mathf.Sin(a) * radius * rNorm));
            }
        }
        return pts.ToArray();
    }

    private Vector2[] GenerateRectangle2D(int count, Vector2 size)
    {
        List<Vector2> pts = new List<Vector2>();
        int perSide = Mathf.Max(1, count / 4);
        float w = size.x / 2f, h = size.y / 2f;

        for (int i = 0; i < perSide; i++)
        {
            float t = i / (float)Mathf.Max(1, perSide - 1);
            if (pts.Count >= count) break; pts.Add(new Vector2(-w + t * size.x, h));
            if (pts.Count >= count) break; pts.Add(new Vector2(w, h - t * size.y));
            if (pts.Count >= count) break; pts.Add(new Vector2(w - t * size.x, -h));
            if (pts.Count >= count) break; pts.Add(new Vector2(-w, -h + t * size.y));
        }

        if (pts.Count > count)
            pts.RemoveRange(count, pts.Count - count);

        return pts.ToArray();
    }

    private Vector2[] GenerateFilledRectangle2D(int count, Vector2 size, float spacing)
    {
        List<Vector2> pts = new List<Vector2>();
        int cols = Mathf.Max(1, Mathf.FloorToInt(size.x / spacing) + 1);
        int rows = Mathf.Max(1, Mathf.FloorToInt(size.y / spacing) + 1);
        float startX = -size.x / 2f + spacing / 2f;
        float startY = -size.y / 2f + spacing / 2f;

        for (int y = 0; y < rows && pts.Count < count; y++)
        {
            for (int x = 0; x < cols && pts.Count < count; x++)
            {
                pts.Add(new Vector2(startX + x * spacing, startY + y * spacing));
            }
        }
        return pts.ToArray();
    }

    private Vector2[] GenerateSpiral2D(int count, float radius, float spacing)
    {
        Vector2[] pts = new Vector2[count];
        float angle = 0f;
        float angleStep = spacing * 0.3f;
        float radiusStep = radius / count;

        for (int i = 0; i < count; i++)
        {
            float r = i * radiusStep;
            angle += angleStep;
            pts[i] = new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
        }
        return pts;
    }

    private Vector2[] GenerateHexagon2D(int count, float radius)
    {
        Vector2[] pts = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)count;
            int side = (int)(t * 6);
            float localT = (t * 6) - side;
            float a1 = side * Mathf.PI / 3f;
            float a2 = ((side + 1) % 6) * Mathf.PI / 3f;
            Vector2 p1 = new Vector2(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius);
            Vector2 p2 = new Vector2(Mathf.Cos(a2) * radius, Mathf.Sin(a2) * radius);
            pts[i] = Vector2.Lerp(p1, p2, localT);
        }
        return pts;
    }

    // Chuyển từ (u, v) trong mặt phẳng 2D → (x, y, z) trong thế giới theo trục đã chọn
    private Vector3 ToWorldPosition(Vector2 uv)
    {
        return mainAxis switch
        {
            Axis.XY => new Vector3(uv.x, uv.y, 0),
            Axis.XZ => new Vector3(uv.x, 0, uv.y),
            Axis.YZ => new Vector3(0, uv.x, uv.y),
            _ => Vector3.zero
        };
    }
}