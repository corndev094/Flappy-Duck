using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFixedAspect : MonoBehaviour
{
    public float targetAspect = 16f / 9f;

    void Start()
    {
        Camera cam = GetComponent<Camera>();
        float windowAspect = (float)Screen.width / Screen.height;
        float scale = windowAspect / targetAspect;

        if (scale < 1f)
        {
            // pillarbox
            cam.rect = new Rect((1f - scale) / 2f, 0f, scale, 1f);
        }
        else
        {
            // letterbox
            float scaleY = 1f / scale;
            cam.rect = new Rect(0f, (1f - scaleY) / 2f, 1f, scaleY);
        }
    }
}
