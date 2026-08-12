using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using System.Collections;

public class DebugS3Interaction : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return new WaitForSeconds(1.0f); // Wait for initialization

        Debug.Log("=== DEBUG S3 INTERACTION START ===");

        // 1. Check EventSystem
        var es = EventSystem.current;
        if (es == null)
        {
            Debug.LogError("CRITICAL: No EventSystem found in scene!");
        }
        else
        {
            Debug.Log($"EventSystem found: {es.name}");
            var minput = es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (minput != null) Debug.Log(" - InputSystemUIInputModule detected.");
            
            var xrInput = es.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>(); // Correct Namespace
            
            if (xrInput != null) Debug.Log(" - XRUIInputModule detected.");
            else Debug.LogError("CRITICAL: XRUIInputModule is MISSING from EventSystem!");
        }

        // 2. Check Interactors
        var rays = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(FindObjectsSortMode.None);
        Debug.Log($"Found {rays.Length} XRRayInteractors.");
        foreach (var r in rays)
        {
            Debug.Log($" - Ray: {r.name}, Mask: {r.raycastMask.value}, Layer: {LayerMask.LayerToName(r.gameObject.layer)}");
            int uiOverlay = LayerMask.NameToLayer("UI_Overlay");
            if (uiOverlay != -1)
            {
                bool hasLayer = (r.raycastMask & (1 << uiOverlay)) != 0;
                Debug.Log($"   -> Can hit UI_Overlay? {hasLayer}");
            }
        }

        // 3. Check Canvas
        var canvas = GameObject.Find("VR_Menu_Canvas");
        if (canvas != null)
        {
            var c = canvas.GetComponent<Canvas>();
            Debug.Log($"Canvas found. WorldCamera: {c.worldCamera}");
            
            var gr = canvas.GetComponent<TrackedDeviceGraphicRaycaster>();
            if (gr != null) Debug.Log(" - TrackedDeviceGraphicRaycaster present.");
            else Debug.LogError("CRITICAL: TrackedDeviceGraphicRaycaster MISSING on Canvas!");
        }
        else
        {
            Debug.LogError("VR_Menu_Canvas not found by name.");
        }

        Debug.Log("=== DEBUG S3 INTERACTION END ===");
    }
}
