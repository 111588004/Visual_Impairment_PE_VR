using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem; 

public class GlaucomaRenderer : MonoBehaviour
{
    [Header("References")]
    public GlaucomaFieldGenerator generator;
    public Material overlayMaterial;
    public RawImage optionalOverlayPanel;

    [Header("Tracking Integration")]
    [Tooltip("Drag the 'LeftGaze' object here (Left Eye)")]
    public Transform gazeTracker;
    
    [Tooltip("Drag the 'RightGaze' object here (Right Eye)")]
    public Transform rightGazeTracker;
    
    [Tooltip("Optional: Assign the VR Camera here if the script can't find it automatically")]
    public Camera targetCamera;

    [Header("Simulation")]
    public bool simulateWithMouse = true; // Re-enabled for testing as requested
    public bool simulateWithKeys = true; // WASD
    public bool autoDemo = false;
    public bool followCameraCenter = true; // Keep as fallback if mouse not used

    [Header("Global Settings")]
    [Range(0.0f, 2.0f)]
    public float globalContrast = 1.0f;
    [Range(0.0f, 2.0f)]
    public float globalBrightness = 1.0f;

    // Internal runtime material instance
    private Material _runtimeMaterial;
    private Vector2 _currentKeyGaze = new Vector2(0.5f, 0.5f);

    void Start()
    {
        Debug.Log("[GlaucomaRenderer] Start Initialized.");
        
        // Auto-find camera if missing
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) targetCamera = FindFirstObjectByType<Camera>();

        // Create a runtime instance of the material
        if (overlayMaterial != null)
        {
            _runtimeMaterial = new Material(overlayMaterial);
            _runtimeMaterial.name = "GlaucomaRuntimeMat";
        }
        else
        {
            Debug.LogError("[GlaucomaRenderer] Overlay Material is missing!");
            return;
        }

        // Assign to RawImage
        if (optionalOverlayPanel != null)
        {
            optionalOverlayPanel.material = _runtimeMaterial;
        }

        // Texture Assignment
        if (generator != null)
        {
            generator.GenerateTexture();
            Texture texture = generator.GeneratedTexture;
            
            if (texture == null && generator.previewImage != null) 
                texture = generator.previewImage.texture;
            
            if (texture != null)
            {
                _runtimeMaterial.mainTexture = texture;
                if (optionalOverlayPanel != null) optionalOverlayPanel.texture = texture;
            }
        }
    }

    void Update()
    {
        if (_runtimeMaterial == null) return;

        Vector2 gazeUV_Left = new Vector2(0.5f, 0.5f);
        Vector2 gazeUV_Right = new Vector2(0.5f, 0.5f);
        bool isTracking = false;

        // PRIORITY 1: Real Eye Tracking Objects
        // If EITHER tracker is assigned, we attempt to track.
        if (gazeTracker != null || rightGazeTracker != null)
        {
            Camera cam = targetCamera;
            if (cam == null) cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();

            if (cam != null)
            {
                bool leftValid = false;
                bool rightValid = false;

                // Left Eye
                if (gazeTracker != null)
                {
                    // Check if it's actually tracking data
                    var trackerScript = gazeTracker.GetComponent<UpdateEyeGaze>();
                    // If script is missing, assume it's a valid transform dummy (legacy support). If script exists, check IsTracking.
                    if (trackerScript == null || trackerScript.IsTracking)
                    {
                        gazeUV_Left = GetGazeUV(cam, gazeTracker);
                        leftValid = true;
                    }
                }

                // Right Eye
                if (rightGazeTracker != null)
                {
                   var trackerScript = rightGazeTracker.GetComponent<UpdateEyeGaze>();
                   if (trackerScript == null || trackerScript.IsTracking)
                   {
                       gazeUV_Right = GetGazeUV(cam, rightGazeTracker);
                       rightValid = true;
                   }
                }
                
                // Fallback: If only one is valid, mirror to the other
                if (leftValid && !rightValid) gazeUV_Right = gazeUV_Left;
                if (rightValid && !leftValid) gazeUV_Left = gazeUV_Right;

                // Only consider "Tracking Active" if at least one eye is valid
                if (leftValid || rightValid)
                {
                    isTracking = true;
                }
            }
        }

        // PRIORITY 2: Auto Demo / Mouse / Keys / Manual Fallback
        // Only if NO eye tracking is active (e.g. headset off or not assigned)
        if (!isTracking)
        {
             Vector2 commonGaze = new Vector2(0.5f, 0.5f);

             if (simulateWithMouse || simulateWithKeys || autoDemo)
             {
                 if (autoDemo)
                 {
                    float speed = 2.0f;
                    commonGaze = new Vector2(0.5f + Mathf.Sin(Time.time * speed) * 0.3f, 0.5f + Mathf.Cos(Time.time * speed) * 0.3f);
                 }
                 else if (simulateWithKeys && Keyboard.current != null && (Keyboard.current.wKey.isPressed || Keyboard.current.aKey.isPressed || Keyboard.current.sKey.isPressed || Keyboard.current.dKey.isPressed))
                 {
                    float speed = Time.deltaTime * 0.5f;
                    if (Keyboard.current.aKey.isPressed) _currentKeyGaze.x -= speed;
                    if (Keyboard.current.dKey.isPressed) _currentKeyGaze.x += speed;
                    if (Keyboard.current.sKey.isPressed) _currentKeyGaze.y -= speed;
                    if (Keyboard.current.wKey.isPressed) _currentKeyGaze.y += speed;
                    _currentKeyGaze.x = Mathf.Clamp01(_currentKeyGaze.x);
                    _currentKeyGaze.y = Mathf.Clamp01(_currentKeyGaze.y);
                    commonGaze = _currentKeyGaze;
                 }
                 else if (simulateWithMouse)
                 {
                    Vector2 mousePos = Vector2.zero;
                    if (Pointer.current != null) mousePos = Pointer.current.position.ReadValue();
                    if (Screen.width > 0 && Screen.height > 0)
                        commonGaze = new Vector2(mousePos.x / Screen.width, mousePos.y / Screen.height);
                 }
                 
                 // Apply common gaze to both eyes
                 gazeUV_Left = commonGaze;
                 gazeUV_Right = commonGaze;
             }
        }

        // Apply to shader
        // Legacy fallback
        _runtimeMaterial.SetVector("_GazeCenter", new Vector4(gazeUV_Left.x, gazeUV_Left.y, 0, 0));
        
        // Binocular Params
        _runtimeMaterial.SetVector("_GazeCenterLeft", new Vector4(gazeUV_Left.x, gazeUV_Left.y, 0, 0));
        _runtimeMaterial.SetVector("_GazeCenterRight", new Vector4(gazeUV_Right.x, gazeUV_Right.y, 0, 0));
        
        // Global Settings
        _runtimeMaterial.SetFloat("_GlobalContrast", globalContrast);
        _runtimeMaterial.SetFloat("_GlobalBrightness", globalBrightness);
    }

    // Helper to calculate UV from World Position
    private Vector2 GetGazeUV(Camera cam, Transform tracker)
    {
        Vector3 screenPos = cam.WorldToViewportPoint(tracker.position);
         // Check if target is behind camera (z < 0)
        if (screenPos.z > 0)
        {
            return new Vector2(screenPos.x, screenPos.y);
        }
        return new Vector2(0.5f, 0.5f); // Lost tracking or behind camera, default to center? Or keep last known?
    }
}
