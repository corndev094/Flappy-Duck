using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "Level_", menuName = "GameData/LevelThumbnail_Data", order = 0)]
public class LevelSO : ScriptableObject {
    public int ID;
    [PreviewField] public Sprite Thumbnail;
    public bool BossLevel;
    public GameObject LevelPrefab;
    public AudioClip BackgroundMusic;
}