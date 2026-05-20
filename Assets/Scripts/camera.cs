using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(PixelPerfectCamera))]
[RequireComponent(typeof(UniversalAdditionalCameraData))]
[ExecuteInEditMode]
public class StaticMapCamera : MonoBehaviour
{
    [SerializeField] private Vector2 cameraPosition = Vector2.zero;
    [Header("Camera Settings")]
    [Range(0.01f, 2f)]
    [SerializeField] private float orthographicSize = 0.095f;
    private Camera cam;
    private PixelPerfectCamera pixelPerfectCamera;
    private UniversalAdditionalCameraData urpCameraData;

    void Start()
    {
        cam = GetComponent<Camera>();
        pixelPerfectCamera = GetComponent<PixelPerfectCamera>();
        urpCameraData = GetComponent<UniversalAdditionalCameraData>();
        
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
        }

        if (pixelPerfectCamera != null)
        {
            // Configure PixelPerfectCamera to prevent rendering artifacts
            pixelPerfectCamera.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;
            pixelPerfectCamera.cropFrame = PixelPerfectCamera.CropFrame.None;
        }

        if (urpCameraData != null)
        {
            // Enable post-processing
            urpCameraData.renderPostProcessing = true;
            urpCameraData.volumeLayerMask = LayerMask.GetMask("Default");
        }
    }

    void Update()
    {
        transform.position = new Vector3(cameraPosition.x, cameraPosition.y, -10f);
    }
}
