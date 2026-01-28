using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "IntList", menuName = "Variable List/Int List", order = 0)]
public class IntListSO : ScriptableObject {
    public List<int> items = new List<int>();
}