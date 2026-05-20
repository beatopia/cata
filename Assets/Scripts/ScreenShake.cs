using UnityEngine;
using System.Collections;

public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance;

    [Header("Camera Target")]
    public Transform cameraTransform; // Assign your main camera here in the inspector

    private Vector3 originalPosition;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Prevent duplicates
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Optional: keep between scenes

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform != null)
        {
            originalPosition = cameraTransform.localPosition;
        }
    }

    /// <summary>
    /// Shakes the camera with the given intensity and duration.
    /// </summary>
    /// <param name="intensity">How far the screen moves.</param>
    /// <param name="duration">How long the shake lasts.</param>
    public void Shake(float intensity, float duration)
    {
        if (cameraTransform != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShakeRoutine(intensity, duration));
        }
    }

    private IEnumerator ShakeRoutine(float intensity, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            Vector2 offset = Random.insideUnitCircle * intensity;
            cameraTransform.localPosition = originalPosition + (Vector3)offset;
            elapsed += Time.deltaTime;
            yield return null;
        }

        cameraTransform.localPosition = originalPosition;
    }
}
