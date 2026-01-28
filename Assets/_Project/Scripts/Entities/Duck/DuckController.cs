using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DuckController : Singleton<DuckController> {
    [Header("Duck References")]
    [SerializeField] private ABaseDuck normalDuck, ramboDuck;

    private Dictionary<SkinID, ABaseDuck> ducks;
    public Dictionary<SkinID, ABaseDuck> Ducks => ducks;

    protected override void Awake()
    {
        ducks = new()
        {
            {SkinID.Normal, normalDuck},
            {SkinID.Rambo, ramboDuck}
        };
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