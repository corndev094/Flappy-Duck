using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

[CreateAssetMenu(fileName = "ThumbnailList", menuName = "GameData/ThumbnalList", order = 0)]
public class LevelListSO : ScriptableObject {
    [Expandable]
    public List<LevelSO> List = new();
}