using UnityEngine;

public class CameraController : Singleton<CameraController> {
    [SerializeField] private bool enable = true;
    [SerializeField] private bool ignoreY = true;
    [SerializeField] private float cameraDistance = 10;
    [SerializeField] private Transform target;
    [SerializeField] private float damping = 2f;
    [SerializeField] private Vector2 cameraOffset;
    private Camera cam;
    private Vector3 velocity = Vector3.zero;
    private const float THRESHOLD = 0.1f;
    public Camera Camera {
        get
        {
            if (cam == null) cam = Camera.main;
            return cam;
        } 
    }
    
    public Transform Target { get => target; set => target = value;}

    void Update()
    {
        if (!enable || target == null || !target.gameObject.activeSelf) return;
        var cameraPos = Camera.transform.position;
        
        var targetPosition = new Vector3(
            target.position.x + cameraOffset.x,
            ignoreY ? cameraPos.y : target.position.y + cameraOffset.y, 
            -cameraDistance);
        
        Camera.transform.position = Vector3.SmoothDamp(Camera.transform.position, targetPosition, ref velocity, 1 / damping);
        if (Camera.transform.position.x <= targetPosition.x + THRESHOLD || Camera.transform.position.x >= targetPosition.x - THRESHOLD) Camera.transform.position = new Vector3(targetPosition.x, Camera.transform.position.y, targetPosition.z); 
        if (Camera.transform.position.y <= targetPosition.y + THRESHOLD || Camera.transform.position.y >= targetPosition.y - THRESHOLD) Camera.transform.position = new Vector3(Camera.transform.position.x, targetPosition.y, targetPosition.z);  
    }
}