using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessManager : MonoBehaviour
{
    [Header("Color Grading")]
    [Range(-100f, 100f)]
    public float postExposure = -0.5f;
    [Range(-100f, 100f)]
    public float saturation = 10f;
    [Range(-100f, 100f)]
    public float contrast = 5f;
    [Range(-180f, 180f)]
    public float hueShift = 0f;

    [Header("Bloom")]
    [Range(0f, 3f)]
    public float bloomIntensity = 0.8f;
    [Range(0f, 1f)]
    public float bloomThreshold = 0.7f;

    [Header("Vignette")]
    [Range(0f, 1f)]
    public float vignetteIntensity = 0.4f;
    [Range(0f, 1f)]
    public float vignetteSmoothness = 0.5f;

    [Header("Chromatic Aberration")]
    [Range(0f, 1f)]
    public float chromaticAberrationIntensity = 0.2f;

    private Volume postProcessVolume;
    private ColorAdjustments colorAdjustments;
    private Bloom bloom;
    private Vignette vignette;
    private ChromaticAberration chromaticAberration;

    void Start()
    {
        // Get or create the post-process volume
        postProcessVolume = GetComponent<Volume>();
        if (postProcessVolume == null)
        {
            postProcessVolume = gameObject.AddComponent<Volume>();
            postProcessVolume.isGlobal = true;
            postProcessVolume.priority = 1;
        }

        // Get or create profile
        if (postProcessVolume.profile == null)
        {
            postProcessVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }

        // Add and configure effects
        SetupEffects();
    }

    void SetupEffects()
    {
        // Color Adjustments
        if (!postProcessVolume.profile.TryGet(out colorAdjustments))
        {
            colorAdjustments = postProcessVolume.profile.Add<ColorAdjustments>(true);
        }

        // Bloom
        if (!postProcessVolume.profile.TryGet(out bloom))
        {
            bloom = postProcessVolume.profile.Add<Bloom>(true);
        }

        // Vignette
        if (!postProcessVolume.profile.TryGet(out vignette))
        {
            vignette = postProcessVolume.profile.Add<Vignette>(true);
        }

        // Chromatic Aberration
        if (!postProcessVolume.profile.TryGet(out chromaticAberration))
        {
            chromaticAberration = postProcessVolume.profile.Add<ChromaticAberration>(true);
        }

        UpdateEffects();
    }

    void Update()
    {
        UpdateEffects();
    }

    void UpdateEffects()
    {
        // Update Color Adjustments
        colorAdjustments.postExposure.value = postExposure;
        colorAdjustments.saturation.value = saturation;
        colorAdjustments.contrast.value = contrast;
        colorAdjustments.hueShift.value = hueShift;

        // Update Bloom
        bloom.intensity.value = bloomIntensity;
        bloom.threshold.value = bloomThreshold;

        // Update Vignette
        vignette.intensity.value = vignetteIntensity;
        vignette.smoothness.value = vignetteSmoothness;

        // Update Chromatic Aberration
        chromaticAberration.intensity.value = chromaticAberrationIntensity;
    }
} 