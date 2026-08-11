using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.UI;

public class ForceFixInteraction : MonoBehaviour
{
    [Header("Settings")]
    public bool forceLayerToUI = true;
    public bool addTrackedRaycaster = true;
    public bool fixEventSystem = true;
    public bool fixInteractors = true;

    void Start()
    {
        Debug.Log(">>> FORCE FIX INTERACTION START <<<");
        FixCanvas();
        FixEventSystem();
        FixInteractors();
        Debug.Log(">>> FORCE FIX INTERACTION END <<<");
    }

    void FixCanvas()
    {
        Canvas c = GetComponent<Canvas>();
        if (c == null)
        {
            Debug.LogError("No Canvas found on this object!");
            return;
        }

        // 1. Force World Camera
        if (c.renderMode == RenderMode.WorldSpace && c.worldCamera == null)
        {
            c.worldCamera = Camera.main;
            Debug.Log("Fixed: Assigned Camera.main to Canvas WorldCamera.");
        }

        // 2. Force Layer
        if (forceLayerToUI)
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            if (gameObject.layer != uiLayer)
            {
                // Recursive set layer
                SetLayerRecursive(gameObject, uiLayer);
                Debug.Log($"Fixed: Forced Canvas Layer to 'UI' ({uiLayer}).");
            }
        }

        // 3. Force Raycaster
        if (addTrackedRaycaster)
        {
            // Remove standard Raycaster if present (it blocks VR sometimes or is redundant)
            var br = GetComponent<BaseRaycaster>();
            if (br != null && !(br is TrackedDeviceGraphicRaycaster))
            {
                Destroy(br); // Destroy standard one to avoid conflict
                Debug.Log("Fixed: Removed standard Raycaster.");
            }

            var tgr = GetComponent<TrackedDeviceGraphicRaycaster>();
            if (tgr == null)
            {
                tgr = gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                Debug.Log("Fixed: Added TrackedDeviceGraphicRaycaster.");
            }
            
            // Ensure mask allows everything
            // tgr.blockingMask = -1; // -1 is Everything
        }
    }

    void FixEventSystem()
    {
        if (!fixEventSystem) return;

        EventSystem es = EventSystem.current;
        if (es == null)
        {
            Debug.LogError("CRITICAL: No EventSystem in scene!");
            return;
        }

        // Check for Standalone Input Module (Mouse) - Disable it to prefer VR
        var std = es.GetComponent<StandaloneInputModule>();
        if (std != null && std.enabled)
        {
            std.enabled = false;
            Debug.Log("Fixed: Disabled StandaloneInputModule to prevent conflict.");
        }

        // Check for XRUIInputModule
        var xrUI = es.GetComponent<XRUIInputModule>();
        if (xrUI == null)
        {
            es.gameObject.AddComponent<XRUIInputModule>();
            Debug.Log("Fixed: Added XRUIInputModule to EventSystem.");
        }
        else
        {
            // Force activate actions
            if (!xrUI.enabled) xrUI.enabled = true;
            Debug.Log("Verified: XRUIInputModule is present and enabled.");
        }
    }

    void FixInteractors()
    {
        if (!fixInteractors) return;

        // Find all Ray Interactors (Old and New namespaces)
        var interactors = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(FindObjectsSortMode.None);
        
        foreach (var ray in interactors)
        {
            // Force mask to include UI layer
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer != -1)
            {
                if ((ray.raycastMask & (1 << uiLayer)) == 0)
                {
                    ray.raycastMask |= (1 << uiLayer);
                    Debug.Log($"Fixed: Added 'UI' layer to RayInteractor '{ray.name}' mask.");
                }
            }
            
            // Force mask to include UI_Overlay layer
            int overlayLayer = LayerMask.NameToLayer("UI_Overlay");
            if (overlayLayer != -1)
            {
                 if ((ray.raycastMask & (1 << overlayLayer)) == 0)
                {
                    ray.raycastMask |= (1 << overlayLayer);
                    Debug.Log($"Fixed: Added 'UI_Overlay' layer to RayInteractor '{ray.name}' mask.");
                }
            }
        }
    }

    void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform)
        {
            SetLayerRecursive(t.gameObject, layer);
        }
    }
}
