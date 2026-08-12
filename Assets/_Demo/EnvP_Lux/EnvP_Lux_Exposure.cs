using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;

public class EnvP_Lux_Exposure : MonoBehaviour
{
    [Header("References")]
    public Volume globalVolume;
    public Slider exposureSlider;
    public TextMeshProUGUI valueText; // Optional: Show EV value

    [Header("Settings")]
    public float minExposure = -2.0f;
    public float maxExposure = 2.0f;

    private ColorAdjustments _colorAdjustments;

    void Start()
    {
        // 0. Auto-assign Event Camera for World Space Canvas
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
        {
            canvas.worldCamera = Camera.main;
        }

        // 1. Validate References
        if (globalVolume == null)
        {
            Debug.LogError("EnvP_Lux_Exposure: Global Volume is not assigned!");
            return;
        }

        // 2. Try to get ColorAdjustments from the Volume Profile
        if (!globalVolume.profile.TryGet(out _colorAdjustments))
        {
            Debug.LogError("EnvP_Lux_Exposure: 'Color Adjustments' not found in Volume Profile. Please Add Override -> Post-processing -> Color Adjustments.");
            return;
        }

        // 3. Setup Slider
        if (exposureSlider != null)
        {
            exposureSlider.minValue = 0f;
            exposureSlider.maxValue = 1f; // Slider 0~1 normalized
            exposureSlider.value = 0.5f; // Start at middle (0 EV) roughly
            
            // Listen to changes
            exposureSlider.onValueChanged.AddListener(OnSliderChanged);
            
            // Initialize once
            OnSliderChanged(exposureSlider.value);
        }
    }

    void OnSliderChanged(float value)
    {
        if (_colorAdjustments == null) return;

        // Map 0~1 slider to minExposure~maxExposure
        float ev = Mathf.Lerp(minExposure, maxExposure, value);

        // Update Post Exposure
        _colorAdjustments.postExposure.value = ev;

        // Update Text if exists
        if (valueText != null)
        {
            valueText.text = $"Exposure: {ev:F2} EV";
        }
    }

    void OnDestroy()
    {
        if (exposureSlider != null)
            exposureSlider.onValueChanged.RemoveListener(OnSliderChanged);
    }
}
