using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class DropdownAttribute : PropertyAttribute
{
    public string ListFieldName { get; }
    public DropdownAttribute(string listFieldName) => ListFieldName = listFieldName;
}

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(DropdownAttribute))]
public class DropdownPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
    {
        var attr = (DropdownAttribute)attribute;
        var listProp = prop.serializedObject.FindProperty(attr.ListFieldName);
        if (listProp == null) { EditorGUI.PropertyField(pos, prop, label); return; }

        var options = new List<string>();
        for (int i = 0; i < listProp.arraySize; i++)
            options.Add(listProp.GetArrayElementAtIndex(i).stringValue);

        int idx = options.Count > 0 ? options.IndexOf(prop.stringValue) : 0;
        if (idx < 0) idx = 0;
        if (options.Count == 0) options.Add("<Empty>");

        int newIdx = EditorGUI.Popup(pos, label.text, idx, options.ToArray());
        if (newIdx < options.Count) prop.stringValue = options[newIdx];
    }
}
#endif