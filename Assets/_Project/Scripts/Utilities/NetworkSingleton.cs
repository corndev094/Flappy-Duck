using Unity.Netcode;
using UnityEngine;

public class NetworkSingleton<T> : NetworkBehaviour where T : Component
{    
    private static T _instance;
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                SetupInstance();
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        RemoveDuplucate();
    }

    private static void SetupInstance()
    {
        _instance = Object.FindFirstObjectByType<T>();
    }

    private void RemoveDuplucate()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            Debug.Log($"Destroy duplicated gameObject: {gameObject.name}");
        }
        else
        {
            _instance = this as T;
        }
    }
}