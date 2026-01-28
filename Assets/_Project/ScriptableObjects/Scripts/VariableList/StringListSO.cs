using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StringList", menuName = "Variable List/String List", order = 0)]
public class StringListSO : ScriptableObject {
    public List<string> items = new List<string>();
}