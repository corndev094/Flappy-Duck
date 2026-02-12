using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class DuckController : NetworkBehaviour {
    [Header("Duck References")]
    [SerializeField] private ABaseDuck normalDuck, ramboDuck;

    private Dictionary<SkinID, ABaseDuck> ducks;
    public Dictionary<SkinID, ABaseDuck> Ducks => ducks;
    public ABaseDuck CurrentDuck;

    void Awake()
    {
        ducks = new()
        {
            {SkinID.Normal, normalDuck},
            {SkinID.Rambo, ramboDuck}
        };
        CurrentDuck = normalDuck;
    }

    public ABaseDuck ActiveDuck(SkinID skin)
    {
        if (ducks.TryGetValue(skin, out var duck))
        {
            if (duck == null)
            {
                EDebug.LogError($"Duck with type {skin} is null");
                return null;
            }
            foreach (var d in ducks.Values)
            {
                d.gameObject.SetActive(false);
            }
            duck.gameObject.SetActive(true);
            CurrentDuck = duck;
            return duck;
        }
        else
        {
            EDebug.LogError($"Does not contains type: {skin}");
            return null;
        }
    }

    public void DisableAll()
    {
        foreach (var duck in ducks.Values)
        {
            duck.gameObject.SetActive(false);
        }
    }
}