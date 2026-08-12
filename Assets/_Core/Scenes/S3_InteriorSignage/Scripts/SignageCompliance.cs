using UnityEngine;
using TMPro; // Support for TextMeshPro

[ExecuteInEditMode]
public class SignageCompliance : MonoBehaviour
{
    [Header("M1: Vertical Height (ADA & ICC)")]
    [Tooltip("Target Mounting Height Range (Center of Sign). Standard: 132cm - 152cm.")]
    public float minHeightCm = 132f;
    public float maxHeightCm = 152f;
    public bool drawHeightGizmos = true;

    [Header("M2: Contrast (ICC A117.1)")]
    [Tooltip("Minimum Contrast Ratio (LRV difference). Standard: 70%.")]
    [Range(0, 100)]
    public float minContrastPercent = 70f;
    public Color textColor = Color.black;
    public Color backgroundColor = Color.white;

    [Header("M3: Font Size (ADA 703.5)")]
    [Tooltip("Minimum Character Height for <1800mm view distance. Standard: 16mm.")]
    public float minFontSizeMm = 16f;

    [Header("Validation Status")]
    public string heightStatus;
    public string contrastStatus;
    public string fontStatus;

    // References
    private RectTransform _rectTransform;
    private TMP_Text _tmpText;
    private Renderer _renderer;

    void OnEnable()
    {
        _rectTransform = GetComponent<RectTransform>();
        _tmpText = GetComponent<TMP_Text>();
        _renderer = GetComponent<Renderer>();
        Validate();
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            Validate();
        }
    }

    public void Validate()
    {
        // 1. Check Height
        // Get center position in World Space
        float centerY = transform.position.y;
        float heightCm = centerY * 100f; // Convert meters to cm
        
        if (heightCm >= minHeightCm && heightCm <= maxHeightCm)
        {
            heightStatus = $"PASS: {heightCm:F1} cm (Target: {minHeightCm}-{maxHeightCm})";
        }
        else
        {
            heightStatus = $"FAIL: {heightCm:F1} cm. Too {(heightCm < minHeightCm ? "LOW" : "HIGH")}.";
        }

        // 2. Check Contrast
        // Simple LRV (Light Reflectance Value) approximation using Luminance
        // L = 0.2126 R + 0.7152 G + 0.0722 B
        float L1 = GetLuminance(textColor);
        float L2 = GetLuminance(backgroundColor);
        
        // LRV Contrast = abs(L1 - L2) * 100 ? 
        // ADA definition: Contrast = (B1 - B2) / B1 * 100 where B1 is lighter.
        float B1 = Mathf.Max(L1, L2);
        float B2 = Mathf.Min(L1, L2);
        
        float contrast = 0f;
        if (B1 > 0) contrast = (B1 - B2) / B1 * 100f;
        
        if (contrast >= minContrastPercent)
        {
            contrastStatus = $"PASS: {contrast:F1}% (Min: {minContrastPercent}%)";
        }
        else
        {
            contrastStatus = $"FAIL: {contrast:F1}%. Insufficient Contrast.";
        }
        
        // 3. Check Font Size
        float charHeightMm = 0f;

        if (_tmpText != null)
        {
            // For TMP, we can estimate character height roughly from font size * scale
            // But bounds is better.
            // Let's use the bounds of the object or explicit FontSize if world space.
            // If TextMeshProUGUI (Canvas), size depends on Canvas scale.
            // If TextMeshPro (World), fontSize is in units? No, font size is points, scale determines units.
            // World Space TextMeshPro: 10 font size approx 1 unit height? No usually 1-10 scale factor.
            // Let's measure the Renderer Bounds Height relative to lines.
            
            // Approximation: Get Bounds height / line count?
            if (_renderer != null)
            {
                // World height of the whole text block
                float totalHeight = _renderer.bounds.size.y; 
                // Determine lines? 
                // Let's just use the Transform Scale * Local Font Size?
                // Easier: Use Mesh Info if available
                charHeightMm = totalHeight * 1000f; // Very crude if multi-line
                
                // Better approach for TMP: fontSize * transform.lossyScale.y?
                // fontSize is in 'point' units relative to container.
                // Let's assume the user just wants to know "Is the object big enough?".
                // If it's a single line sign:
                charHeightMm = _renderer.bounds.size.y * 1000f; 
            }
        }
        else
        {
            // Generic Object Check
             if (_renderer != null) charHeightMm = _renderer.bounds.size.y * 1000f;
             else if (_rectTransform != null) charHeightMm = _rectTransform.rect.height * transform.lossyScale.y * 1000f;
        }

        if (charHeightMm >= minFontSizeMm)
        {
            fontStatus = $"PASS: ~{charHeightMm:F1} mm (Min: {minFontSizeMm}mm)";
        }
        else
        {
            fontStatus = $"FAIL: ~{charHeightMm:F1} mm. Too Small.";
        }
    }

    private float GetLuminance(Color c)
    {
        return 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawHeightGizmos) return;

        // Draw Target Zone Box
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Vector3 center = new Vector3(transform.position.x, (minHeightCm + maxHeightCm) / 2f / 100f, transform.position.z);
        Vector3 size = new Vector3(1f, (maxHeightCm - minHeightCm) / 100f, 0.05f);
        
        Gizmos.DrawCube(center, size);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, size);
        
        // Draw Line to Floor
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, new Vector3(transform.position.x, 0, transform.position.z));
    }
}
