using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class CataractRenderer : MonoBehaviour
{
    public enum SnellenAcuity
    {
        Normal_20_20 = 20,
        Mild_20_40 = 40,
        Moderate_20_80 = 80,
        Severe_20_200 = 200
    }

    [Header("Clinical Parameters")]
    [Tooltip("Visual Acuity (Snellen Denominator). 20/20 is normal.")]
    public SnellenAcuity snellenValue = SnellenAcuity.Moderate_20_80;
    
    [Tooltip("Contrast Sensitivity (logMAR-like factor). 1.0 is Normal, 0.6 is Cataract.")]
    [Range(0.1f, 1.5f)]
    public float contrastSensitivity = 0.6f;

    [Header("Cataract Type")]
    [Tooltip("Simulate Yellowing of the lens (Nuclear Sclerosis).")]
    public bool enableNuclearTint = true;
    public Color nuclearTint = new Color(1.0f, 0.95f, 0.85f, 1.0f);
    
    [Tooltip("Simulate Glare/Bloom (Light Scattering).")]
    [Range(0f, 1f)]
    public float glareIntensity = 0.3f;

    [Header("References")]
    public Shader cataractShader;

    private Material _material;
    private RawImage _rawImage;

    void Start()
    {
        _rawImage = GetComponent<RawImage>();
        if (cataractShader == null) cataractShader = Shader.Find("Medical/CataractSimulation");
        
        if (cataractShader != null)
        {
            _material = new Material(cataractShader);
            _rawImage.material = _material;
            // Ensure RawImage covers screen
            SetupFullScreen();
        }
        else
        {
            Debug.LogError("Cataract Shader not found!");
        }
        
        ApplyClinicalParams();
    }

    void OnValidate()
    {
        ApplyClinicalParams();
    }

    void Update()
    {
        // Continuously update in case runtime changes occur
        ApplyClinicalParams();
    }

    private void SetupFullScreen()
    {
        RectTransform rt = GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public void ApplyClinicalParams()
    {
        if (_material == null) return;

        // 1. Map Snellen 20/X to Blur Size
        // 20/20 = 0 blur
        // 20/80 = 4x blur factor
        float blurSigma = 0f;
        switch (snellenValue)
        {
            case SnellenAcuity.Normal_20_20: blurSigma = 0.0f; break;
            case SnellenAcuity.Mild_20_40:   blurSigma = 1.0f; break;
            case SnellenAcuity.Moderate_20_80: blurSigma = 2.5f; break;
            case SnellenAcuity.Severe_20_200: blurSigma = 5.0f; break;
        }
        
        // 2. Map logMAR/Contrast
        // Note: Clinical logMAR 0.6 is 20/80 equivalent acuity, but contrast is separate.
        // We use the slider value directly as the multipliers.
        
        _material.SetFloat("_BlurSize", blurSigma);
        _material.SetFloat("_Contrast", contrastSensitivity);
        
        // 3. Tint & Glare
        if (enableNuclearTint)
        {
            _material.SetColor("_Tint", nuclearTint);
        }
        else
        {
            _material.SetColor("_Tint", Color.white);
        }

        _material.SetColor("_OverlayColor", new Color(1, 1, 1, glareIntensity)); // Alpha controls glare
    }
}
