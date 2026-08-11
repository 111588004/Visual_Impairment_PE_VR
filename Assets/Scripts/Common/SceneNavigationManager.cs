using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.UI;
using VISimulation;

public class SceneNavigationManager : MonoBehaviour
{
    public static SceneNavigationManager Instance { get; private set; }

    [Header("Configuration")]
    [Tooltip("List of scene names to cycle through")]
    public List<string> sceneList = new List<string> 
    { 
        "S1_Stairway", 
        "S2_Corridor", 
        "S3_InteriorSignage" 
    };

    [Tooltip("Fade duration in seconds")]
    public float fadeDuration = 0.5f;

    [Header("Behavior")]
    [Tooltip("If true, menu teleports to camera when opened. If false, it stays at the scene's placed position.")]
    public bool summonMenuOnToggle = false;

    [Header("UI Reference")]
    [Tooltip("Assign the Canvas/Menu GameObject here. It will be toggled by the VR Button.")]
    public GameObject sceneMenuCanvas;

    // ... (unchanged code) ...

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeTexture();
        }
        else
        {
            // --- ROBUST HANDOVER LOGIC ---
            // 1. Identify the new scene's menu
            if (this.sceneMenuCanvas != null)
            {
                Debug.Log($"[SceneManager] Handover: Adopted new menu '{this.sceneMenuCanvas.name}' and reparenting to persistent Instance.");
                
                // 2. CLEANUP: Destroy all existing children of the persistent Instance (Old Menus)
                // We deactivate them IMMEDIATELY so they don't respond to Raycasts/Find during this frame.
                foreach (Transform child in Instance.transform)
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }

                // 3. REPARENT: Move the new scene's menu to the persistent Instance
                // Use worldPositionStays = true to keep its design-time position.
                this.sceneMenuCanvas.transform.SetParent(Instance.transform, true);
                
                // 4. UPDATE REFERENCE: Point the Instance to the new menu
                Instance.sceneMenuCanvas = this.sceneMenuCanvas;
                
                // 5. RE-INITIALIZE: Setup VR components and interaction for the new menu
                Instance.CheckVRComponents(Instance.sceneMenuCanvas);
                Instance.FixSceneInteraction();
                
                // 6. RE-BIND BUTTONS: Fix any inspector-linked buttons that were pointing to the local (now-destroyed) manager
                Instance.RebindMenuButtons(Instance.sceneMenuCanvas);

                // 7. INITIAL STATE: Ensure it starts hidden
                Instance.sceneMenuCanvas.SetActive(false);
            }

            // Finally, destroy the temporary manager shell
            Destroy(gameObject); 
            return;
        }
    }
    


    [Tooltip("Key to go to next scene (PC Debug)")]
    public Key nextSceneKey = Key.Space;

    [Tooltip("Key to toggle menu (PC Debug)")]
    public Key toggleMenuKey = Key.M;
    
    [Tooltip("Input Action to toggle the Scene Menu (e.g. Right Hand Primary Button)")]
    public InputActionReference vrToggleMenuAction;

    [Tooltip("Input Action for Joystick (e.g. Left Hand Move). Used globally by menu navigators.")]
    public InputActionReference globalJoystickAction;

    // Internal State
    private int _currentSceneIndex = -1;
    private bool _isTransitioning = false;
    private float _fadeAlpha = 0.0f;
    private Texture2D _blackTexture;

    void OnEnable()
    {
        if (vrToggleMenuAction != null && vrToggleMenuAction.action != null)
        {
            vrToggleMenuAction.action.Enable();
            vrToggleMenuAction.action.performed += ToggleMenu;
        }

        if (globalJoystickAction != null && globalJoystickAction.action != null)
        {
            globalJoystickAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (vrToggleMenuAction != null && vrToggleMenuAction.action != null)
        {
            vrToggleMenuAction.action.performed -= ToggleMenu;
            vrToggleMenuAction.action.Disable();
        }

        if (globalJoystickAction != null && globalJoystickAction.action != null)
        {
            globalJoystickAction.action.Disable();
        }
    }

    void InitializeTexture()
    {
        _blackTexture = new Texture2D(1, 1);
        _blackTexture.SetPixel(0, 0, Color.black);
        _blackTexture.Apply();
    }

    void Start()
    {
        // Try to identify current scene index
        string currentName = SceneManager.GetActiveScene().name;
        int index = sceneList.IndexOf(currentName);
        if (index != -1)
        {
            _currentSceneIndex = index;
        }

        FixSceneInteraction(); // Ensure Interactors and EventSystem are fixed for the initial scene

        // Hide menu by default
        if (sceneMenuCanvas != null) 
        {
            CheckVRComponents(sceneMenuCanvas);
            sceneMenuCanvas.SetActive(false);
        }
    }

    private void CheckVRComponents(GameObject canvasRoot)
    {
        if (canvasRoot == null) return;
        Canvas c = canvasRoot.GetComponent<Canvas>();
        if (c != null)
        {
            // 1. Ensure Event Camera (Always refresh)
            Camera mainCam = Camera.main;
            c.worldCamera = mainCam;
            Debug.Log($"[SceneManager] Canvas '{c.name}' WorldCamera set to: {(mainCam != null ? mainCam.name : "NULL")}");

            // 2. FORCE FIX: Remove standard GraphicRaycaster (Causes conflicts in VR)
            var stdRaycaster = canvasRoot.GetComponent<GraphicRaycaster>();
            if (stdRaycaster != null)
            {
                 // In this version, TrackedDeviceGraphicRaycaster does NOT inherit from GraphicRaycaster,
                 // so stdRaycaster is guaranteed to be the standard Unity UI one. Destroy it.
                 Destroy(stdRaycaster); 
                 Debug.Log("[SceneManager] Removed standard GraphicRaycaster.");
            }

            // 3. Ensure VR Raycaster (The only one we want)
            var vrRaycaster = canvasRoot.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
            if (vrRaycaster == null)
            {
                vrRaycaster = canvasRoot.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
                Debug.Log("[SceneManager] Added missing TrackedDeviceGraphicRaycaster.");
            }
            else
            {
                Debug.Log("[SceneManager] TrackedDeviceGraphicRaycaster already present.");
            }
            
            // 4. ROBUST SETTINGS
            vrRaycaster.blockingMask = -1; // Everything
            vrRaycaster.ignoreReversedGraphics = false; // Allow shooting back of menu
        }
        
        // CRITICAL FIX: Removed recursive call to FixSceneInteractionDelayed()
        // CheckVRComponents should only setup the canvas, NOT trigger a scene-wide re-scan.
        // This breaks the infinite loop: CheckVR -> FixScene -> Optimize -> CheckVR
    }

    // NEW: Auto-fix controller masks and blocking overlays
    public void FixSceneInteraction()
    {
        // 1. Fix Controllers to see UI_Overlay
        var interactors = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(FindObjectsSortMode.None);
        int uiOverlayLayer = LayerMask.NameToLayer("UI_Overlay");
        
        foreach (var ray in interactors)
        {
            // If layer exists, force add it to mask
            if (uiOverlayLayer != -1)
            {
                if ((ray.raycastMask & (1 << uiOverlayLayer)) == 0)
                {
                    ray.raycastMask |= (1 << uiOverlayLayer);
                    Debug.Log($"[SceneManager] Auto-added 'UI_Overlay' to {ray.name} Raycast Mask.");
                }
            }
            // Ensure basic UI access
            ray.raycastMask |= (1 << LayerMask.NameToLayer("UI"));
            ray.raycastMask |= (1 << LayerMask.NameToLayer("Default"));
            
            // CRITICAL FIX: Disable hitting triggers! 
            // Often invisible zones (Audio/Event) block the ray.
            // ray.hitTriggers = false; // API Error in this version
            ray.raycastTriggerInteraction = QueryTriggerInteraction.Ignore;
            Debug.Log($"[SceneManager] Configured {ray.name}: Mask={ray.raycastMask}, TriggerInteraction=Ignore");
        }

        // 1.5 Fix EventSystem for VR
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es != null)
        {
            var xrInput = es.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>();
            if (xrInput == null)
            {
                 // Try old namespace if new one fails or just add new one
                 Debug.LogWarning("[SceneManager] EventSystem missing XRUIInputModule! Adding it for VR interaction.");
                 es.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>();
                 
                 // Disable Standalone if it exists to prevent conflict? Usually they can coexist but XRI needs one.
                 var stdInput = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                 if (stdInput != null) stdInput.enabled = false;
            }
        }

        // 2. Fix Glaucoma Blocking (The "Short Ray" Fix)
        var rawImages = FindObjectsByType<UnityEngine.UI.RawImage>(FindObjectsSortMode.None);
        foreach (var img in rawImages)
        {
            // Identify Glaucoma overlay by name or material
            if (img.gameObject.name.Contains("Glaucoma") || (img.material != null && img.material.name.Contains("Glaucoma")))
            {
                if (img.raycastTarget)
                {
                    img.raycastTarget = false;
                    Debug.Log("[SceneManager] Auto-fixed Glaucoma Canvas: RaycastTarget=False (Was blocking VR Ray!)");
                }
            }
        }

        // 3. Optimized Fix for Scene 3 Menus
        // A. Fix the assigned Environment Menu
        if (sceneMenuCanvas != null)
        {
            OptimizeCanvasInteraction(sceneMenuCanvas.gameObject);
        }

        // B. Fix the "Switch Scenes" Menu (VR_Menu_Canvas / SceneSelectMenu)
        // DEPRECATED: We now use the 'VRCanvasFixer' component attached directly to the menu object.
        // This avoids global searches and brittle name lookups.
    }

    // Helper to fix a specific Canvas hierarchy without freezing the game
    private void OptimizeCanvasInteraction(GameObject canvasRoot)
    {
        if (canvasRoot == null) return;

        // 1. Ensure Components
        CheckVRComponents(canvasRoot);

        // 2. Fix Blockers: Find Colliders that are NOT Buttons
        var colliders = canvasRoot.GetComponentsInChildren<Collider>(true);
        foreach (var col in colliders)
        {
            // Skip if already ignored
            if (col.gameObject.layer == 2) continue;

            // Check if it's a UI element
            var uiElement = col.GetComponent<Selectable>();
            
            // If NO Selectable (Button/Slider), it's a blocker.
            if (uiElement == null)
            {
                 col.gameObject.layer = 2; // Ignore Raycast
            }
        }
    }

    private void ToggleMenu(InputAction.CallbackContext context)
    {
        Debug.Log($"[SceneManager] ToggleMenu ACTION PERFORMED! Context Phase: {context.phase}");

        if (sceneMenuCanvas != null)
        {
            bool isActive = !sceneMenuCanvas.activeSelf;
            sceneMenuCanvas.SetActive(isActive);
            Debug.Log($"[SceneManager] Menu Canvas SetActive to: {isActive}");
            
            // If opening the menu, summon it to front of camera IF enabled
            if (isActive && summonMenuOnToggle)
            {
                SummonMenuToCamera();
            }
        }
        else
        {
            Debug.LogError("[SceneManager] sceneMenuCanvas is NULL in ToggleMenu! Cannot toggle.");
        }
    }

    // Helper to move menu to front of camera
    private void SummonMenuToCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null && sceneMenuCanvas != null)
        {
            // Get flattened forward vector (ignore Up/Down tilt)
            Vector3 flatForward = mainCam.transform.forward;
            flatForward.y = 0; 
            flatForward.Normalize();

            // Calculate flattened right vector
            // Standard Unity: Cross(Up, Forward) = Right
            Vector3 flatRight = Vector3.Cross(Vector3.up, flatForward);

            // Position: 1.5m forward, 0.8m right (Horizon Level)
            // Use camera position but keep menu upright at eye level
            Vector3 targetPos = mainCam.transform.position + flatForward * 1.5f + flatRight * 0.8f;
           
            sceneMenuCanvas.transform.position = targetPos;
            
            // Rotation: Face the user (LookRotation uses Forward, so we point it towards camera by negating forward)
            // Previous logic (flatForward) made it face AWAY from user (Backface).
            sceneMenuCanvas.transform.rotation = Quaternion.LookRotation(flatForward); 
            // Wait, LookRotation(forward) aligns Z with forward. 
            // Canvas standard: +Z is "back" of the text? No, +Z is forward.
            // Text is readable from -Z side? No, standard UI is readable looking down +Z (local).
            // So if Camera looks +Z(world), and Canvas looks +Z(world), Camera sees BACK of Canvas.
            // We want Canvas to look -Z(world) (towards camera).
            sceneMenuCanvas.transform.rotation = Quaternion.LookRotation(flatForward); 
            // Correct: Force it to face camera -> Use standard Billboard logic:
            sceneMenuCanvas.transform.rotation = Quaternion.LookRotation(sceneMenuCanvas.transform.position - mainCam.transform.position);
            // Actually, flatForward is Cam.Forward.
            // If we use LookRotation(flatForward), Canvas.Forward = Cam.Forward. 
            // This means Canvas is facing AWAY. 
            // We want Canvas.Forward = -Cam.Forward (Face Camera).
            sceneMenuCanvas.transform.rotation = Quaternion.LookRotation(flatForward); // Default Unity Canvas often needs to face same dir? No.
            
            // Let's stick to the sure-fire verification:
            // Flip it 180.
            sceneMenuCanvas.transform.rotation = Quaternion.LookRotation(flatForward);
            sceneMenuCanvas.transform.Rotate(0, 180, 0);
        }
    }

    private void RebindMenuButtons(GameObject canvasRoot)
    {
        if (canvasRoot == null) return;

        Debug.Log($"[SceneManager] Starting dynamic button rebinding for {canvasRoot.name}...");
        Button[] buttons = canvasRoot.GetComponentsInChildren<Button>(true);
        int reboundCount = 0;

        foreach (var btn in buttons)
        {
            string btnText = "";
            TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>();
            if (tmp != null) btnText = tmp.text;
            else
            {
                Text legacyText = btn.GetComponentInChildren<Text>();
                if (legacyText != null) btnText = legacyText.text;
            }

            if (string.IsNullOrEmpty(btnText)) continue;

            // Search for matches in our sceneList
            for (int i = 0; i < sceneList.Count; i++)
            {
                string targetScene = sceneList[i];
                
                // Be loose with matching: Button might say "S1", "Stairway", or "S1_Stairway"
                if (btnText.Contains(targetScene) || 
                    (targetScene.Contains("_") && btnText.Contains(targetScene.Split('_')[0])) ||
                    btn.name.Contains(targetScene))
                {
                    int targetIndex = i;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => Instance.LoadSceneByIndex(targetIndex));
                    
                    Debug.Log($"[SceneManager] Rebound button '{btn.name}' (Text: {btnText}) -> Scene {targetScene} (Index {targetIndex})");
                    reboundCount++;
                    break;
                }
            }
        }
        Debug.Log($"[SceneManager] Rebinding complete. {reboundCount} buttons updated.");
    }

    [Header("Debug")]
    public bool debugRayHit = false; // Disable by default for performance!

    void Update()
    {
        if (_isTransitioning) return;

        // DEBUG: Print what the Ray is hitting (WARNING: EXPENSIVE! ONLY ENABLE FOR DEBUGGING)
        if (debugRayHit)
        {
            var interactors = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(FindObjectsSortMode.None);
            foreach (var ray in interactors)
            {
                // Check 3D Hit
                if (ray.TryGetCurrent3DRaycastHit(out RaycastHit hit))
                {
                    Debug.Log($"[SceneManager-RAY] {ray.name} HIT 3D Object: '{hit.collider.name}' (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");
                }
                
                // Check UI Hit
                if (ray.TryGetCurrentUIRaycastResult(out UnityEngine.EventSystems.RaycastResult uiHit))
                {
                    Debug.Log($"[SceneManager-RAY] {ray.name} HIT UI Element: '{uiHit.gameObject.name}'");
                }
            }
        }

        // PC Input
        if (Keyboard.current != null)
        {
            if (Keyboard.current[nextSceneKey].wasPressedThisFrame)
            {
                LoadNextScene();
            }

            if (Keyboard.current[toggleMenuKey].wasPressedThisFrame)
            {
                if (sceneMenuCanvas != null) 
                {
                    bool isActive = !sceneMenuCanvas.activeSelf;
                    sceneMenuCanvas.SetActive(isActive);
                    if (isActive && summonMenuOnToggle) SummonMenuToCamera();
                }
            }

            // Direct Number Keys (1-3)
            if (Keyboard.current.digit1Key.wasPressedThisFrame) LoadSceneByIndex(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) LoadSceneByIndex(1);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) LoadSceneByIndex(2);
        }
    }

    public void LoadNextScene()
    {
        if (sceneList.Count == 0) return;
        
        int nextIndex = (_currentSceneIndex + 1) % sceneList.Count;
        StartCoroutine(TransitionToScene(nextIndex));
    }

    public void LoadSceneByIndex(int index)
    {
        Debug.Log($"[SceneManager] Request to load scene index: {index}");
        if (index >= 0 && index < sceneList.Count)
        {
            // Clear variant if loading scene normally
            if (SimulationVariantManager.Instance != null) SimulationVariantManager.Instance.SetActiveVariant(null);
            StartCoroutine(TransitionToScene(index));
        }
        else
        {
             Debug.LogError($"[SceneManager] Index {index} is out of range! (Count: {sceneList.Count})");
        }
    }

    public void LoadSceneWithVariant(SimulationVariantData variant)
    {
        if (variant == null) return;
        
        int index = sceneList.IndexOf(variant.sceneName);
        if (index != -1)
        {
            if (SimulationVariantManager.Instance != null)
            {
                SimulationVariantManager.Instance.SetActiveVariant(variant);
            }
            StartCoroutine(TransitionToScene(index));
        }
        else
        {
            Debug.LogError($"[SceneManager] Scene '{variant.sceneName}' not found in sceneList!");
        }
    }

    private IEnumerator TransitionToScene(int index)
    {
        _isTransitioning = true;
        Debug.Log($"[SceneManager] Transitioning to: {sceneList[index]}");

        // Fade Out
        yield return StartCoroutine(Fade(0, 1));

        // Load
        _currentSceneIndex = index;
        string sceneName = sceneList[index];
        // Check if scene exists in build settings is hard at runtime without loading, 
        // so we just try to load.
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        // Wait until done
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Fade In
        yield return StartCoroutine(Fade(1, 0));

        _isTransitioning = false;
    }

    private IEnumerator Fade(float start, float end)
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            _fadeAlpha = Mathf.Lerp(start, end, timer / fadeDuration);
            yield return null;
        }
        _fadeAlpha = end;
    }

    void OnGUI()
    {
        if (_fadeAlpha > 0)
        {
            GUI.color = new Color(0, 0, 0, _fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _blackTexture);
            GUI.color = Color.white;
        }
        
        // Optional: Simple Debug GUI to show controls
        GUILayout.BeginArea(new Rect(10, 10, 300, 100));
        GUILayout.Label($"[Scene Manager] Current: {( _currentSceneIndex >= 0 ? sceneList[_currentSceneIndex] : "Unknown" )}");
        GUILayout.Label("Controls: Space (Next), 1-3 (Jump)");
        GUILayout.EndArea();
    }

    private System.Collections.IEnumerator FixSceneInteractionDelayed()
    {
        yield return new WaitForSeconds(0.5f);
        
        // 1. Re-run global interaction fix
        FixSceneInteraction();
        
        // 2. Re-run component check on the menu (Refreshes Camera!)
        if (sceneMenuCanvas != null)
        {
             CheckVRComponents(sceneMenuCanvas);
             Debug.Log("[SceneManager] Delayed fix executed: EventCamera refreshed and Components checked.");
        }
    }
}
