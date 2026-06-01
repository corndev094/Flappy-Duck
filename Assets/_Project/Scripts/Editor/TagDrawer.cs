using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using _Project.Scripts.CustomPropertyAttribute;

namespace _Project.Scripts.Editor
{
    [CustomPropertyDrawer(typeof(TagAttribute))]
    public class TagDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                EditorGUI.BeginProperty(position, label, property);

                // Lấy tất cả tag hiện có trong dự án
                string[] allTags = UnityEditorInternal.InternalEditorUtility.tags;
                
                // Phân tách các tag đã chọn từ chuỗi (dùng dấu phẩy phân cách)
                string currentValue = property.stringValue;
                List<string> selectedTags = string.IsNullOrEmpty(currentValue) 
                    ? new List<string>() 
                    : currentValue.Split(',')
                                  .Select(t => t.Trim())
                                  .Where(t => !string.IsNullOrEmpty(t))
                                  .ToList();

                // Tạo nhãn hiển thị nút bấm
                string buttonText = "";
                if (selectedTags.Count == 0)
                {
                    buttonText = "Nothing";
                }
                else if (selectedTags.Count == allTags.Length)
                {
                    buttonText = "Everything";
                }
                else
                {
                    buttonText = string.Join(", ", selectedTags);
                }

                // Vẽ nhãn của property
                position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

                // Vẽ nút bấm để hiển thị dropdown menu multi-select
                if (GUI.Button(position, buttonText, EditorStyles.popup))
                {
                    ShowMultiSelectMenu(allTags, selectedTags, property);
                }

                EditorGUI.EndProperty();
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Sử dụng [Tag] cho kiểu dữ liệu String.");
            }
        }

        private void ShowMultiSelectMenu(string[] allTags, List<string> selectedTags, SerializedProperty property)
        {
            GenericMenu menu = new GenericMenu();

            // Tùy chọn "Nothing"
            bool isNoneSelected = selectedTags.Count == 0;
            menu.AddItem(new GUIContent("Nothing"), isNoneSelected, () =>
            {
                property.stringValue = "";
                property.serializedObject.ApplyModifiedProperties();
            });

            // Tùy chọn "Everything"
            bool isAllSelected = selectedTags.Count == allTags.Length;
            menu.AddItem(new GUIContent("Everything"), isAllSelected, () =>
            {
                property.stringValue = string.Join(",", allTags);
                property.serializedObject.ApplyModifiedProperties();
            });

            menu.AddSeparator("");

            // Danh sách các tag
            for (int i = 0; i < allTags.Length; i++)
            {
                string tag = allTags[i];
                bool isSelected = selectedTags.Contains(tag);

                menu.AddItem(new GUIContent(tag), isSelected, () =>
                {
                    if (isSelected)
                    {
                        selectedTags.Remove(tag);
                    }
                    else
                    {
                        selectedTags.Add(tag);
                    }
                    
                    // Sắp xếp lại tag theo thứ tự ban đầu của Unity
                    var orderedSelection = allTags.Where(t => selectedTags.Contains(t));
                    property.stringValue = string.Join(",", orderedSelection);
                    property.serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }
    }
}