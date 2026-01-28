using UnityEngine;

[CreateAssetMenu(fileName = "EDebugSettings", menuName = "EDebug/Settings", order = 1)]
public class EDebugSettings : ScriptableObject
{
    [Header("Log Colors")]
    public Color intColor = Color.green;
    public Color boolTrueColor = Color.greenYellow;
    public Color boolFalseColor = Color.red;
    public Color stringColor = new Color(0.529f, 0.808f, 0.922f); // skyBlue
    public Color floatColor = Color.cyan;
    public Color listColor = Color.white;
    public Color warningColor = new Color(1.0f, 0.647f, 0.0f); // orange
    public Color errorColor = Color.red;
}
