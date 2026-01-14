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
    [Tooltip("Drag the 'LeftGaze' object here to use real eye tracking")]
    public Transform gazeTracker;
    
    [Tooltip("Optional: Assign the VR Camera here if the script can't find it automatically")]
    public Camera targetCamera;

    [Header("Simulation")]
    public bool simulateWithMouse = true;
    public bool simulateWithKeys = true; // WASD
    public bool autoDemo = false;
    public bool followCameraCenter = false;

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

        Vector2 gazeUV = new Vector2(0.5f, 0.5f);
        bool isTracking = false;

        // PRIORITY 1: Real Eye Tracking Object
        if (gazeTracker != null)
        {
            // Robust Camera Lookup
            Camera cam = targetCamera;
            if (cam == null) cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();

            if (cam != null)
            {
                // Convert the 3D Gaze Point (on wall/object) to 2D Viewport Space (0-1)
                Vector3 screenPos = cam.WorldToViewportPoint(gazeTracker.position);
                
                // Check if target is behind camera (z < 0)
                if (screenPos.z > 0)
                {
                    gazeUV = new Vector2(screenPos.x, screenPos.y);
                    isTracking = true;
                }
            }
        }

        // PRIORITY 2: Auto Demo (if no tracking or specifically enabled)
        if (!isTracking && autoDemo)
        {
            float speed = 2.0f;
            gazeUV = new Vector2(0.5f + Mathf.Sin(Time.time * speed) * 0.3f, 0.5f + Mathf.Cos(Time.time * speed) * 0.3f);
        }
        // PRIORITY 3: Keyboard (WASD)
        else if (!isTracking && simulateWithKeys && Keyboard.current != null && (Keyboard.current.wKey.isPressed || Keyboard.current.aKey.isPressed || Keyboard.current.sKey.isPressed || Keyboard.current.dKey.isPressed))
        {
            // WASD Control
            float speed = Time.deltaTime * 0.5f;
            if (Keyboard.current.aKey.isPressed) _currentKeyGaze.x -= speed;
            if (Keyboard.current.dKey.isPressed) _currentKeyGaze.x += speed;
            if (Keyboard.current.sKey.isPressed) _currentKeyGaze.y -= speed;
            if (Keyboard.current.wKey.isPressed) _currentKeyGaze.y += speed;
            
            _currentKeyGaze.x = Mathf.Clamp01(_currentKeyGaze.x);
            _currentKeyGaze.y = Mathf.Clamp01(_currentKeyGaze.y);
            gazeUV = _currentKeyGaze;
        }
        // PRIORITY 4: Mouse
        else if (!isTracking && simulateWithMouse)
        {
            Vector2 mousePos = Vector2.zero;
            if (Pointer.current != null) mousePos = Pointer.current.position.ReadValue();
            
            if (Screen.width > 0 && Screen.height > 0)
                gazeUV = new Vector2(mousePos.x / Screen.width, mousePos.y / Screen.height);
        }
        else if (!isTracking && followCameraCenter)
        {
            gazeUV = new Vector2(0.5f, 0.5f);
        }

        // Apply to shader
        _runtimeMaterial.SetVector("_GazeCenter", new Vector4(gazeUV.x, gazeUV.y, 0, 0));
    }
}
