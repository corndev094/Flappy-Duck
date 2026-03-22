using UnityEngine;
using System.Collections.Generic;

public abstract class VariableListSO<T> : ScriptableObject {
    public List<T> list = new List<T>();
}