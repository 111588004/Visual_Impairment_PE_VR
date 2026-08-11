using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Fixes global environment issues that interfere with VR UI interaction.
/// 1. Disables RaycastTarget on full-screen RawImages (Glaucoma, Vignette).
/// 2. Disables Gaze Interactors to prevent head-movement from stealing slider control.
/// </summary>
public class VREnvironmentFixer : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("If true, searches for 'Glaucoma', 'Vignette', or 'Overlay' RawImages and makes them transparent to Raycasts.")]
    public bool fixGlobalOverlays = true;

    [Tooltip("If true, finds and disables 'Gaze Interactor' objects to prevent slider interference.")]
    public bool disableGazeInteractors = true;

    [Tooltip("Run fixes every few seconds to catch new objects (e.g. instantiated effects)?")]
    public bool runPeriodicCheck = true;
    public float checkInterval = 2.0f;

    private float _timer;

    void Start()
    {
        PerformEnvironmentFix();
    }

    void Update()
    {
        if (runPeriodicCheck)
        {
            _timer += Time.deltaTime;
            if (_timer >= checkInterval)
            {
                PerformEnvironmentFix();
                _timer = 0;
            }
        }
    }

    public void PerformEnvironmentFix()
    {
        // 1. Fix RawImage Blockers (Glaucoma, Vignette, etc.)
        if (fixGlobalOverlays)
        {
            var rawImages = FindObjectsByType<RawImage>(FindObjectsSortMode.None);
            foreach (var img in rawImages)
            {
                // Logic: If it's a full-screen effect (usually unnamed or specific names), it shouldn't block rays.
                // We filter by name to avoid breaking legit UI RawImages (like minimaps).
                string n = img.name.ToLower();
                bool isEffect = n.Contains("glaucoma") || n.Contains("vignette") || n.Contains("overlay") || n.Contains("blur");
                
                // OR check if it has a material that looks like an effect
                if (!isEffect && img.material != null)
                {
                    string matName = img.material.name.ToLower();
                    isEffect = matName.Contains("glaucoma") || matName.Contains("blur");
                }

                // If identified as a blocker, disable raycast target
                if (isEffect && img.raycastTarget)
                {
                    img.raycastTarget = false;
                    // Debug.Log($"[VREnvironmentFixer] Fixed Blocker: {img.name} (RaycastTarget -> False)");
                }
            }
        }

        // 2. Disable Gaze Interactors (Prevent "Head Slider" conflict)
        if (disableGazeInteractors)
        {
            // Find inputs/interactors that use Gaze
            // Note: In XRI, they are usually GameObjects named "Gaze Interactor" or have XRGazeInteractor component
            
            // A. Search by Component (Newer XRI)
            // var gazeInteractors = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRGazeInteractor>(FindObjectsSortMode.None);
            // foreach (var g in gazeInteractors) g.enabled = false;

            // B. Search by Look (Classic / XRI 2.x - 3.x common setups)
            // We search for the GameObject to disable interaction entirely
            var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (!obj.activeInHierarchy) continue;

                if (obj.name.Contains("Gaze Interactor") || obj.name.Contains("Gaze Stabilized"))
                {
                    obj.SetActive(false);
                    // Debug.Log($"[VREnvironmentFixer] Auto-disabled Gaze Interactor: {obj.name}");
                }
            }
        }
    }
}
