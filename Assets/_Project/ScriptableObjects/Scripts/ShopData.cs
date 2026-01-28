using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ShopData", menuName = "GameData/ShopData", order = 0)]
public class ShopData : ScriptableObject {
    public List<SkinData> skins;
}

[System.Serializable]
public struct SkinData
{
    public SkinID SkinId;
    [Min(0)] public int Price;
}