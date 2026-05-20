using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(PixelPerfectCamera))]
public class PlayerCamera : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 0, -10); // -10 for 2D games to keep camera behind the scene
    [Header("Camera Settings")]
    [Range(1f, 20f)]
    public float defaultZoom = 0.9f; // Exact value from scene camera

    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = defaultZoom;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Calculate desired position
        Vector3 desiredPosition = target.position + offset;
        
        // Smoothly move camera
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            // Immediately position camera at target
            transform.position = target.position + offset;
        }
    }
} 