using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DuckList", menuName = "GameData/DuckList", order = 0)]
public class DuckList : ScriptableObject {
    public List<DuckBaseData> List;
}