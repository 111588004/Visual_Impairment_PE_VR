using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class CataractRenderer : MonoBehaviour
{

    [Header("Clinical Parameters")]
    [Tooltip("Visual Acuity Denominator (20/X). e.g. 20, 40, 60, 80, 100...")]
    [Range(20, 200)]
    public int snellenDenominator = 80;
    
    [Tooltip("Contrast Sensitivity (logMAR-like factor). 1.0 is Normal, 0.6 is Cataract.")]
    [Range(0.1f, 1.5f)]
    public float contrastSensitivity = 0.6f;

    [Header("Cataract Type")]
    [Tooltip("Simulate Yellowing of the lens (Nuclear Sclerosis).")]
    public bool enableNuclearTint = true;
    
    [Range(0.0f, 5.0f)]
    [Tooltip("Clinical Nuclear Sclerosis Grade (0: Normal, 4: Brunescent, 5: Nigra/Black).")]
    public float nuclearSclerosisGrade = 1.0f;

    [Tooltip("Manual Tint adjustment (Overrides Grade if not using Auto-Calculation).")]
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
        // Snapping: Align to 20 units as requested
        snellenDenominator = Mathf.RoundToInt(snellenDenominator / 20f) * 20;
        if (snellenDenominator < 20) snellenDenominator = 20;

        // Formula: (Denominator - 20) * factor. 
        // 20/80 (gap 60) -> 2.5 blur means factor = 2.5/60 = 0.0416
        float blurSigma = (snellenDenominator - 20) * 0.0416f;
        
        // 2. Map logMAR/Contrast
        // Note: Clinical logMAR 0.6 is 20/80 equivalent acuity, but contrast is separate.
        // We use the slider value directly as the multipliers.
        
        _material.SetFloat("_BlurSize", blurSigma);
        _material.SetFloat("_Contrast", contrastSensitivity);
        
        // 3. Tint & Glare
        if (enableNuclearTint)
        {
            // Calculate Tint from Clinical Grade (0-4)
            Color calculatedTint = GetNSTint(nuclearSclerosisGrade);
            _material.SetColor("_Tint", calculatedTint);
            
            // Sync the preview color in inspector (optional, but helpful)
            nuclearTint = calculatedTint;
        }
        else
        {
            _material.SetColor("_Tint", Color.white);
        }

        _material.SetColor("_OverlayColor", new Color(1, 1, 1, glareIntensity)); // Alpha controls glare
    }

    private Color GetNSTint(float grade)
    {
        // Clinical NS Color Scale (Simplified)
        Color[] nsColors = new Color[] {
            new Color(1.0f, 1.0f, 1.0f),         // 0: Normal
            new Color(1.0f, 0.95f, 0.75f),      // 1: Mild Yellow
            new Color(1.0f, 0.85f, 0.45f),      // 2: Moderate Amber
            new Color(0.85f, 0.55f, 0.15f),     // 3: Severe Brown/Amber
            new Color(0.60f, 0.30f, 0.05f),     // 4: Brunescent (Deep Chocolate)
            new Color(0.15f, 0.08f, 0.02f)      // 5: Nigra (Black/Dark Brown)
        };

        grade = Mathf.Clamp(grade, 0, 5);
        int index = Mathf.FloorToInt(grade);
        if (index >= 5) return nsColors[5];
        
        float t = grade - index;
        return Color.Lerp(nsColors[index], nsColors[index + 1], t);
    }
}
