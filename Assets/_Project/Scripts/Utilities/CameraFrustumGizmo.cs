using UnityEngine;

/// <summary>
/// Draws a gizmo in the Scene view to represent the camera's view frustum
/// at a specific distance. This helps visualize what the camera sees.
/// This script should be attached to a GameObject with a Camera component.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class CameraFrustumGizmo : MonoBehaviour
{
    [Tooltip("The distance from the camera at which to draw the frustum rectangle.")]
    [Range(0.1f, 1000f)]
    public float distance = 10f;

    [Tooltip("The color of the gizmo lines.")]
    public Color gizmoColor = Color.yellow;

    private Camera _camera;

    /// <summary>
    /// OnDrawGizmos is called by Unity to draw gizmos in the scene view.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (_camera == null)
        {
            _camera = GetComponent<Camera>();
        }

        // Only execute if we have a camera and it's in perspective mode
        if (_camera == null || _camera.orthographic)
        {
            return;
        }

        // This array will hold the four corners of the frustum
        Vector3[] corners = new Vector3[4];

        // Calculate the frustum corners at the specified distance
        _camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), distance, Camera.MonoOrStereoscopicEye.Mono, corners);

        Transform cameraTransform = _camera.transform;

        // Convert the local corner positions to world space
        Vector3 worldCorner0 = cameraTransform.TransformPoint(corners[0]); // Bottom-left
        Vector3 worldCorner1 = cameraTransform.TransformPoint(corners[1]); // Top-left
        Vector3 worldCorner2 = cameraTransform.TransformPoint(corners[2]); // Top-right
        Vector3 worldCorner3 = cameraTransform.TransformPoint(corners[3]); // Bottom-right

        // Set the color for the gizmos
        Gizmos.color = gizmoColor;

        // Draw the rectangle representing the camera's view
        Gizmos.DrawLine(worldCorner0, worldCorner1); // Left edge
        Gizmos.DrawLine(worldCorner1, worldCorner2); // Top edge
        Gizmos.DrawLine(worldCorner2, worldCorner3); // Right edge
        Gizmos.DrawLine(worldCorner3, worldCorner0); // Bottom edge

        // Optional: Draw lines from the camera's position to the corners to form the frustum pyramid
        Gizmos.DrawLine(cameraTransform.position, worldCorner0);
        Gizmos.DrawLine(cameraTransform.position, worldCorner1);
        Gizmos.DrawLine(GetComponent<Camera>().transform.position, worldCorner2);
        Gizmos.DrawLine(cameraTransform.position, worldCorner3);
    }
}
