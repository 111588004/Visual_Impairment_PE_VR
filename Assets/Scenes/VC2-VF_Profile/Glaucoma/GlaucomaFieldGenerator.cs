using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class GlaucomaFieldGenerator : MonoBehaviour
{
    [System.Serializable]
    public struct FieldZone
    {
        [Range(0, 180)] public float startAngle; // Degrees
        [Range(0, 180)] public float endAngle;   // Degrees
        [Range(0, 1)] public float sensitivity;  // 1=Clear, 0=Blind
    }

    [Header("Settings")]
    public int resolution = 512;
    [Tooltip("The vertical Field of View to assume for projection. Usually matches the Camera's FOV.")]
    public float referenceFov = 90f; 
    
    [Space]
    public List<FieldZone> zones = new List<FieldZone>();

    [Header("Preview")]
    public RawImage previewImage;
    
    // The generated texture
    private Texture2D _texture;
    public Texture2D GeneratedTexture => _texture;

    void Start()
    {
        // Add default zones if empty OR if they are just zero-initialized placeholders
        bool isEffectivelyEmpty = (zones == null || zones.Count == 0);
        if (!isEffectivelyEmpty && zones.Count > 0 && zones[0].endAngle <= 0.01f && zones[0].sensitivity <= 0.01f)
        {
            isEffectivelyEmpty = true;
        }

        if (isEffectivelyEmpty)
        {
            ApplyTunnelVision20(); // Default to Tunnel Vision if empty
        }
    }

    [ContextMenu("Apply 20deg Tunnel Vision")]
    public void ApplyTunnelVision20()
    {
        zones = new List<FieldZone>
        {
            // Clear Central Vision (0 to 20)
            new FieldZone { startAngle = 0, endAngle = 20, sensitivity = 1.0f },
            // Sharp Transition (20 to 20.1) - avoids hard aliasing artifact
            new FieldZone { startAngle = 20, endAngle = 20.1f, sensitivity = 0.5f },
            // Blind Peripheral (20.1 to 180)
            new FieldZone { startAngle = 20.1f, endAngle = 180, sensitivity = 0.0f }
        };
        GenerateTexture();
        Debug.Log("Applied 20-degree Tunnel Vision settings.");
    }

    [ContextMenu("Apply Realistic Tunnel (Soft Edge)")]
    public void ApplyRealisticTunnel20()
    {
        // Force Recompile verification
        zones = new List<FieldZone>();
        
        // 1. Central Clear Zone (0 to 20)
        zones.Add(new FieldZone { startAngle = 0, endAngle = 20, sensitivity = 1.0f });

        // 2. Gradient Falloff (20 to 30)
        // Create 10 steps of hardening blur
        int steps = 20;
        float startDeg = 20f;
        float endDeg = 30f; // Wider fade for realism
        
        for (int i = 0; i < steps; i++)
        {
            float t1 = (float)i / steps;
            float t2 = (float)(i + 1) / steps;
            
            float angleA = Mathf.Lerp(startDeg, endDeg, t1);
            float angleB = Mathf.Lerp(startDeg, endDeg, t2);
            
            // Sensitivity drops from 1.0 to 0.0
            // Using SmoothStep for natural feel? Or just linear.
            // Linear: 1 - t1
            float s = 1.0f - t1; 
            // Make it drop faster at end? Pow(s, 0.5)?
            // Let's stick to linear for predictable results
            
            zones.Add(new FieldZone { startAngle = angleA, endAngle = angleB, sensitivity = s });
        }

        // 3. Rest is Blind (35 to 180)
        zones.Add(new FieldZone { startAngle = endDeg, endAngle = 180, sensitivity = 0.0f });

        GenerateTexture();
        Debug.Log($"Applied Realistic Tunnel Vision ({startDeg}-{endDeg} deg falloff).");
    }

    [ContextMenu("Generate Texture")]
    public void GenerateTexture()
    {
        _texture = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
        _texture.wrapMode = TextureWrapMode.Clamp;
        _texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[resolution * resolution];
        float halfRes = resolution * 0.5f;

        // Calculate maximum physical semi-width at the projection plane
        // tan(theta) = opposite / adjacent
        // We assume the projection plane is at distance 1.
        // maxRadiusInternal corresponds to the edge of the texture (UV=1)
        // If referenceFov is 90, half is 45. Tan(45) = 1.
        float maxTan = Mathf.Tan(referenceFov * 0.5f * Mathf.Deg2Rad);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                // 1. Normalized Coordinates (-1 to 1)
                float u = (x - halfRes) / halfRes;
                float v = (y - halfRes) / halfRes;
                
                // 2. Distance from center in UV space
                float r_uv = Mathf.Sqrt(u * u + v * v);

                // 3. Convert UV distance to Physical Angle (Deg)
                // Angle = Atan( r_uv * maxTan )
                // Note: This assumes a circular fisheye-like conceptual mapping or simply
                // that 'r_uv=1' corresponds to 'referenceFov/2' in all directions.
                // For a perfect rectangular frustum, the corner angle > vertical angle,
                // but for a 'Vision Field' cone, radial symmetry is usually desired.
                float angleRad = Mathf.Atan(r_uv * maxTan);
                float angleDeg = angleRad * Mathf.Rad2Deg;

                // 4. Sample Zones
                float sensitivity = GetSensitivity(angleDeg);

                pixels[y * resolution + x] = new Color(sensitivity, sensitivity, sensitivity);
            }
        }

        _texture.SetPixels(pixels);
        _texture.Apply();

        if (previewImage != null)
        {
            previewImage.texture = _texture;
        }
        
        Debug.Log($"Generated Texture with Ref FOV {referenceFov}. Center=0 deg, Edge={Mathf.Atan(1.0f * maxTan) * Mathf.Rad2Deg:F1} deg.");
    }

    private float GetSensitivity(float angle)
    {
        // Default to 0 (blind) if no zone matches? Or 1?
        // Let's iterate zones.
        foreach (var zone in zones)
        {
            if (angle >= zone.startAngle && angle < zone.endAngle)
            {
                return zone.sensitivity;
            }
        }
        return 0f; // Outside defined zones is blind
    }

    [Header("Quick Actions (Click to Apply)")]
    [Tooltip("Click to apply sharp 20-degree tunnel vision")]
    public bool action_ApplyStandard20 = false;
    
    [Tooltip("Click to apply realistic 20-30 degree gradient tunnel vision")]
    public bool action_ApplyRealistic20 = false;

    [Header("Debug")]
    public bool showGizmos = true;
    public float gizmoRayLength = 10.0f;

    // Auto-update in Editor when values change
    private void OnValidate()
    {
        if (action_ApplyStandard20)
        {
            ApplyTunnelVision20();
            action_ApplyStandard20 = false;
        }

        if (action_ApplyRealistic20)
        {
            ApplyRealisticTunnel20();
            action_ApplyRealistic20 = false;
        }
    }

    [ContextMenu("Match Main Camera FOV")]
    public void MatchMainCameraFov()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            referenceFov = cam.fieldOfView;
            Debug.Log($"Matched Reference FOV to Camera: {referenceFov}");
            GenerateTexture();
        }
        else
        {
            Debug.LogError("No Main Camera found!");
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Camera cam = Camera.main;
        // fallback to finding any camera
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        if (cam == null) return;

        Vector3 origin = cam.transform.position;
        Vector3 forward = cam.transform.forward;
        Vector3 up = cam.transform.up;
        Vector3 right = cam.transform.right;

        foreach (var zone in zones)
        {
            if (zone.endAngle <= 0) continue;
            
            // Draw rays for this zone boundary
            Color c = Color.Lerp(Color.red, Color.green, zone.sensitivity);
            Gizmos.color = c;
            
            // Draw 4 cardinal rays at the "End Angle" deviation
            // Angle is deviation from center (Radius)
            DrawRay(origin, forward, right, zone.endAngle);
            DrawRay(origin, forward, -right, zone.endAngle);
            DrawRay(origin, forward, up, zone.endAngle);
            DrawRay(origin, forward, -up, zone.endAngle);
        }
    }

    void DrawRay(Vector3 origin, Vector3 fwd, Vector3 axis, float angle)
    {
        // Rotate forward vector by 'angle' towards 'axis'
        // This is simplified. Axis should be perpendicular.
        // If axis is 'right', we rotate around 'up'? No.
        // We want to rotate 'fwd' towards 'right' by 'angle'.
        // Axis of rotation is 'up'.
        
        Vector3 rotAxis = Vector3.Cross(fwd, axis).normalized;
        // Actually we can just use Quaternion.AngleAxis around rotAxis?
        // Wait, Cross(Forward, Right) = Up. 
        // So to rotate Fwd to Right, we rotate around Up. Correct.
        
        if (rotAxis.sqrMagnitude < 0.01f) return; // Zero vector check
        
        Vector3 dir = Quaternion.AngleAxis(angle, rotAxis) * fwd;
        Gizmos.DrawRay(origin, dir * gizmoRayLength);
    }
}
