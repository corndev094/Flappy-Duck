using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "ThumbnailList", menuName = "GameData/ThumbnalList", order = 0)]
public class LevelListSO : ScriptableObject {
    [InlineEditor(Expanded = true)]
    public List<LevelSO> List = new();
}