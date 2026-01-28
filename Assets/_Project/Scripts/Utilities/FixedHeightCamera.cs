using UnityEngine;

public class FixedHeightCamera : MonoBehaviour
{
    public float targetHeight = 10f;

    void Update()
    {
        Camera.main.orthographicSize = targetHeight / 2f;
    }
}
