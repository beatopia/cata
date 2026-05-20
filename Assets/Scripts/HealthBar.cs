using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private Vector3 offset = new Vector3(0, -0.6f, 0);
    private Transform target;

    public void Init(Transform followTarget)
    {
        target = followTarget;
    }

    public void SetHealth(float current, float max)
    {
        fillImage.fillAmount = Mathf.Clamp01(current / max);
    }

    private void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
            transform.rotation = Quaternion.identity;
        }
    }
}