using UnityEngine;

[CreateAssetMenu(fileName = "BaseEnemyData", menuName = "GameData/BaseEnemyData", order = 0)]
public class BaseEnemyData : ScriptableObject {
    public float HP = 1;
    public float Damage = 1;
}