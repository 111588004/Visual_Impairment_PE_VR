using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Attaches to a Canvas to ensure it works with VR Raycasts.
/// fixes Event Camera, adds TrackedDeviceGraphicRaycaster, and removes invisible blockers.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class VRCanvasFixer : MonoBehaviour
{
    [Header("Settings")]
    public bool fixEventCamera = true;
    public bool addVRRaycaster = true;
    public bool removeBlockers = true; // Auto-ignore raycast for background quads

    private Canvas _canvas;

    void Awake()
    {
        _canvas = GetComponent<Canvas>();
    }

    void OnEnable()
    {
        // Run fixes whenever the menu is opened/enabled
        PerformFixes();
    }

    void Update()
    {
        // Continuous Self-Repair: If Camera is lost (e.g. scene load), find it again immediately.
        if (_canvas != null && _canvas.worldCamera == null && fixEventCamera)
        {
            PerformFixes();
        }
    }

    public void PerformFixes()
    {
        if (_canvas == null) _canvas = GetComponent<Canvas>();

        // 1. Fix Event Camera (Critical for Raycast to work)
        if (fixEventCamera)
        {
            // SAFETY: If user already assigned a camera manually, DON'T touch it!
            if (_canvas.worldCamera != null) 
            {
                // Debug.Log("[VRCanvasFixer] WorldCamera already assigned. Skipping fix.");
            }
            else
            {
                Camera mainCam = Camera.main;
                // Fallback: If no MainCamera tag, find ANY camera (usually the VR camera)
                if (mainCam == null)
                {
                     mainCam = FindFirstObjectByType<Camera>();
                }
    
                if (mainCam != null)
                {
                    _canvas.worldCamera = mainCam;
                    // Debug.Log($"[VRCanvasFixer] Set WorldCamera for '{name}' to {mainCam.name}");
                }
                else
                {
                    Debug.LogWarning($"[VRCanvasFixer] Could not find ANY Camera for '{name}'! Input may fail.");
                }
            }
        }

        // 2. Fix Raycaster (Critical for VR Controller interaction)
        if (addVRRaycaster)
        {
            // Remove standard raycaster if present (it conflicts)
            var bRaycaster = GetComponent<BaseRaycaster>();
            if (bRaycaster != null && !(bRaycaster is TrackedDeviceGraphicRaycaster))
            {
                Destroy(bRaycaster);
            }

            // Ensure VR Raycaster exists
            var vrRaycaster = GetComponent<TrackedDeviceGraphicRaycaster>();
            if (vrRaycaster == null)
            {
                vrRaycaster = gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            }
            
            // Configure for robustness
            vrRaycaster.blockingMask = -1; // Everything
            vrRaycaster.ignoreReversedGraphics = false; 
            vrRaycaster.checkFor3DOcclusion = false; // Disable physics occlusion checks entirely
            vrRaycaster.checkFor2DOcclusion = false;
        }

        // 3. Remove Blockers (The "Invisible Wall" Fix)
        if (removeBlockers)
        {
            // A. Physics Colliders
            var colliders = GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders)
            {
                if (col.gameObject.layer == 2) continue;
                var uiElement = col.GetComponent<Selectable>();
                if (uiElement == null)
                {
                    col.gameObject.layer = 2; // Ignore Raycast
                }
            }

            // B. Graphics Blockers (Images masking buttons)
            var images = GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                // Skip if it's part of a Button/Toggle/Slider
                if (img.GetComponentInParent<Selectable>() != null) continue;
                if (img.GetComponent<Selectable>() != null) continue;

                // If it's a background image (has RaycastTarget but no interactive component)
                // DISABLE RaycastTarget so the ray shoots THROUGH it
                if (img.raycastTarget)
                {
                    img.raycastTarget = false;
                    // Debug.Log($"[VRCanvasFixer] Disabled RaycastTarget on non-interactive Image: {img.name}");
                }
            }
        }
    }
}
