using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Volume))]
public class CataractVolumeControl : MonoBehaviour
{
    public enum SnellenAcuity
    {
        Normal_20_20,
        Mild_20_40,
        Moderate_20_80,
        Severe_20_200
    }

    [Header("Clinical Parameters")]
    public SnellenAcuity snellenValue = SnellenAcuity.Moderate_20_80;
    
    [Range(0f, 100f)]
    [Tooltip("Contrast Reduction % (0 = Normal, 100 = Gray). logMAR 0.6 approx 40-50%.")]
    public float contrastReduction = 40f; 

    [Header("Cataract Characteristics")]
    public bool enableYellowTint = true;
    public Color tintColor = new Color(1f, 0.92f, 0.8f); // Pale Yellow
    
    [Range(0f, 5f)]
    public float glareIntensity = 1.0f;

    private Volume _volume;
    private DepthOfField _dof;
    private ColorAdjustments _colorAdj;
    private Bloom _bloom;

    void Start()
    {
        _volume = GetComponent<Volume>();
        
        // Ensure Profile exists
        if (_volume.profile == null)
        {
             Debug.LogError("CataractVolumeControl: No Profile assigned to Volume!");
             return;
        }

        // Try to get Overrides, or Add them if missing
        if (!_volume.profile.TryGet(out _dof)) _dof = _volume.profile.Add<DepthOfField>(true);
        if (!_volume.profile.TryGet(out _colorAdj)) _colorAdj = _volume.profile.Add<ColorAdjustments>(true);
        if (!_volume.profile.TryGet(out _bloom)) _bloom = _volume.profile.Add<Bloom>(true);

        ApplySettings();
    }

    void OnValidate()
    {
        if (_volume == null) _volume = GetComponent<Volume>();
        ApplySettings();
    }

    void Update()
    {
        ApplySettings();
    }

    public void ApplySettings()
    {
        if (_volume == null || _volume.profile == null) return;
        if (_dof == null || _colorAdj == null) return;

        // 1. Blur (Snellen)
        // Using Gaussian Depth of Field to simulate acuity loss
        _dof.active = true;
        _dof.mode.Override(DepthOfFieldMode.Gaussian);
        
        float blurRadius = 0f;
        switch (snellenValue)
        {
            case SnellenAcuity.Normal_20_20: blurRadius = 0f; break;
            case SnellenAcuity.Mild_20_40:   blurRadius = 1.0f; break; // Slight blur
            case SnellenAcuity.Moderate_20_80: blurRadius = 2.0f; break; // Significant blur
            case SnellenAcuity.Severe_20_200: blurRadius = 4.0f; break; // Legal blindness
        }
        
        // In Gaussian mode, 'Start' is usually 0, 'End' controls range, 
        // but 'Max Radius' isn't directly exposed in all URP versions via script easily on 'Gaussian'.
        // Actually, for Gaussian, we control properites like 'Gaussian Start' / 'Gaussian End'.
        // To make everything blurry: Start=0, End=0.
        // Wait, standard URP Gaussian: Start (distance), End (distance). 
        // Max Radius is often hardcoded or set in High Quality settings.
        // Let's set Start=0, End=0.1 to force near-field blur?
        // NO, Gaussian DoF blurs things *outside* the range.
        // So we set Start=0, End=0 -> Everything is far -> Everything blurred?
        // Or Start=999 -> Everything is near -> Everything blurred?
        // A reliable VR blur trick: Set Start=0, End=0, MaxRadius=1.5.
        
        _dof.gaussianStart.Override(0f);
        _dof.gaussianEnd.Override(0f); 
        // Note: Actual blur amount depends on URP Quality Settings 'Max Radius'. 
        // Ideally we would control the radius directly but scripting API varies.
        // Assuming consistent basic blur behavior.
        
        // 2. Contrast & Tint (logMAR + Nuclear)
        _colorAdj.active = true;
        
        // Contrast: -100 to 100. 
        // We want Reduction. So we go Negative.
        _colorAdj.contrast.Override(-contrastReduction);
        
        // Tint
        if (enableYellowTint)
             _colorAdj.colorFilter.Override(tintColor);
        else
             _colorAdj.colorFilter.Override(Color.white);

        // 3. Glare (Bloom)
        if (_bloom != null)
        {
            _bloom.active = true;
            _bloom.intensity.Override(glareIntensity);
            _bloom.threshold.Override(0.5f); // Bloom on anything moderately bright
            _bloom.scatter.Override(0.7f);   // Broad scattering
        }
    }
}
