using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace VISimulation
{
    public class VRContrastMenu : MonoBehaviour
    {
        [Header("References")]
        public ContrastController contrastController;
        public GameObject menuCanvas; // The Canvas GameObject to toggle

        [Header("UI Sliders")]
        public Slider sliderNosing;
        public Slider sliderTread;
        public Slider sliderWall;
        public Slider sliderLux;

        [Header("UI Value Labels")]
        public Text textNosing;
        public Text textTread;
        public Text textWall;
        public Text textLux;

        [Header("Input Action")]
        public InputActionReference menuButtonAction; // Reference to XRI Left/Right Hand Menu Button

        private bool isMenuVisible = true; // Start visible

        private void Start()
        {
            if (contrastController == null)
            {
                contrastController = FindObjectOfType<ContrastController>();
            }

            Debug.Log("[VRMenu] Script Started. Initializing...");

            // Initialize Sliders with current values
            if (contrastController != null)
            {
                if (sliderNosing) sliderNosing.value = contrastController.lrvNosing;
                if (sliderTread) sliderTread.value = contrastController.lrvTread;
                if (sliderWall) sliderWall.value = contrastController.lrvWall;
                if (sliderLux) sliderLux.value = contrastController.emLux;
                
                // Add Listeners
                if (sliderNosing) sliderNosing.onValueChanged.AddListener(OnNosingChanged);
                if (sliderTread) sliderTread.onValueChanged.AddListener(OnTreadChanged);
                if (sliderWall) sliderWall.onValueChanged.AddListener(OnWallChanged);
                if (sliderLux) sliderLux.onValueChanged.AddListener(OnLuxChanged);
            }

            UpdateLabels();
            
            // Start visible as requested
            SetMenuVisibility(true);
        }

        private void OnEnable()
        {
            if (menuButtonAction != null && menuButtonAction.action != null)
            {
                menuButtonAction.action.Enable();
                menuButtonAction.action.performed += OnMenuButtonPressed;
                Debug.Log($"[VRMenu] Input Action Enabled: {menuButtonAction.action.name}");
            }
            else
            {
                Debug.LogError("[VRMenu] Menu Button Action is missing or not assigned!");
            }
        }

        private void OnDisable()
        {
            if (menuButtonAction != null && menuButtonAction.action != null)
            {
                menuButtonAction.action.performed -= OnMenuButtonPressed;
                menuButtonAction.action.Disable();
            }
        }

        private void OnMenuButtonPressed(InputAction.CallbackContext context)
        {
            Debug.Log("[VRMenu] Button Pressed!"); // Check if input is received
            isMenuVisible = !isMenuVisible;
            SetMenuVisibility(isMenuVisible);
            
            // Optional: Move menu to front of user when opened
            if (isMenuVisible && menuCanvas != null)
            {
                // Simple positioning: 1m in front of camera at eye level
                Transform cam = Camera.main.transform;
                if (cam != null)
                {
                    menuCanvas.transform.position = cam.position + cam.forward * 1.5f;
                    // Make it face the camera
                    menuCanvas.transform.rotation = Quaternion.LookRotation(menuCanvas.transform.position - cam.position);
                    Debug.Log($"[VRMenu] Moved Canvas to: {menuCanvas.transform.position}");
                }
            }
        }

        private Canvas canvasComponent;

        public void SetMenuVisibility(bool visible)
        {
            isMenuVisible = visible;
            
            // Fix: Don't use SetActive(false) on the root object, because it kills the Input Listener!
            // Instead, we toggle the Canvas component or the child Panel.
            
            if (canvasComponent == null && menuCanvas != null)
            {
                canvasComponent = menuCanvas.GetComponent<Canvas>();
            }

            if (canvasComponent != null)
            {
                canvasComponent.enabled = visible;
            }
            else if (menuCanvas != null)
            {
                // Fallback: If user assigned a Panel (not the root Canvas), SetActive is fine.
                // But if they assigned the Root Object (which has this script), we must NOT disable it.
                if (menuCanvas != gameObject)
                {
                    menuCanvas.SetActive(visible);
                }
                else
                {
                     // If they assigned the self object, try to find a canvas to disable
                     Canvas c = GetComponent<Canvas>();
                     if(c) c.enabled = visible;
                }
            }
        }

        // --- Slider Callbacks ---

        private void OnNosingChanged(float value)
        {
            if (contrastController) contrastController.lrvNosing = value;
            UpdateLabels();
        }

        private void OnTreadChanged(float value)
        {
            if (contrastController) contrastController.lrvTread = value;
            UpdateLabels();
        }

        private void OnWallChanged(float value)
        {
            if (contrastController) contrastController.lrvWall = value;
            UpdateLabels();
        }

        private void OnLuxChanged(float value)
        {
            if (contrastController) contrastController.emLux = value;
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            if (textNosing) textNosing.text = $"Nosing LRV: {contrastController.lrvNosing:F1}";
            if (textTread) textTread.text = $"Tread LRV: {contrastController.lrvTread:F1}";
            if (textWall) textWall.text = $"Wall LRV: {contrastController.lrvWall:F1}";
            if (textLux) textLux.text = $"Em Lux: {contrastController.emLux:F0}";
        }
    }
}
