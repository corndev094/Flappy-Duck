using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

[CreateAssetMenu(fileName = "DuckBaseData", menuName = "GameData/DuckBaseData", order = 0)]
public class DuckBaseData : ADescription {
    public Sprite Thumbnail;
    public SkinID SkinId;

    [Min(0),] public int HP;
    [Min(0),] public int Stamina;
    [Min(0),] public bool UseMP;
    [Min(0),] public int MP;

    [Min(0)] public float JumpForce = 17f;
    [Min(0)] public float DefaultMoveSpeed = 5f;
    [Min(0)] public float JumpDelay = 0.3f;
    [Min(0)] public float ShootDelay = 0.1f;

    [Min(0)] public float JumpStamina = 1;
    [Min(0)] public float RefillStaminaSpeed = 5;
    [Min(0)] public float RefillStaminaDelay = 0.5f;
    

    public List<AudioClip> TakeDamageSfx;
    public List<AudioClip> FlappingSfx;
    public ShakeSettings TakeDamageCamShakeSettings;
}