using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Component
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

        if (_instance == null)
        {
            GameObject gameObj = new GameObject();
            gameObj.name = typeof(T).Name;
            _instance = gameObj.AddComponent<T>();
        }
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