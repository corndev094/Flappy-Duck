using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FloatList", menuName = "Variable List/Float List", order = 0)]
public class FloatListSO : ScriptableObject {
    public List<float> items = new List<float>();
}