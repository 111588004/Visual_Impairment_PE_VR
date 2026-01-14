using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace VISimulation
{
    public class VRContrastMenuS2 : MonoBehaviour
    {
        [Header("References")]
        public ContrastControllerS2 contrastController;
        public GameObject menuCanvas;
        
        // Don't disable the script itself, toggle this canvas component
        private Canvas canvasComponent;

        [Header("UI Sliders")]
        public Slider sliderFloor;
        public Slider sliderBorderFloor;
        public Slider sliderBorderWall;
        public Slider sliderWall;
        public Slider sliderBench;
        public Slider sliderLux;

        [Header("UI Value Labels")]
        public Text textFloor;
        public Text textBorderFloor;
        public Text textBorderWall;
        public Text textWall;
        public Text textBench;
        public Text textLux;

        [Header("Input Action")]
        public InputActionReference menuButtonAction; 

        private bool isMenuVisible = true; // Start Visible

        private void Start()
        {
            if (contrastController == null)
            {
                contrastController = FindObjectOfType<ContrastControllerS2>();
            }
            
            if (menuCanvas != null)
            {
                canvasComponent = menuCanvas.GetComponent<Canvas>();
            }

            Debug.Log("[VRMenuS2] Script Started. Initializing...");

            if (contrastController != null)
            {
                // Init Values
                if (sliderFloor) sliderFloor.value = contrastController.lrvFloor;
                if (sliderBorderFloor) sliderBorderFloor.value = contrastController.lrvBorderFloor;
                if (sliderBorderWall) sliderBorderWall.value = contrastController.lrvBorderWall;
                if (sliderWall) sliderWall.value = contrastController.lrvWall;
                if (sliderBench) sliderBench.value = contrastController.lrvBench;
                if (sliderLux) sliderLux.value = contrastController.emLux;
                
                // Add Listeners
                if (sliderFloor) sliderFloor.onValueChanged.AddListener(OnFloorChanged);
                if (sliderBorderFloor) sliderBorderFloor.onValueChanged.AddListener(OnBorderFloorChanged);
                if (sliderBorderWall) sliderBorderWall.onValueChanged.AddListener(OnBorderWallChanged);
                if (sliderWall) sliderWall.onValueChanged.AddListener(OnWallChanged);
                if (sliderBench) sliderBench.onValueChanged.AddListener(OnBenchChanged);
                if (sliderLux) sliderLux.onValueChanged.AddListener(OnLuxChanged);
            }

            UpdateLabels();
            SetMenuVisibility(true);
        }

        private void OnEnable()
        {
            if (menuButtonAction != null && menuButtonAction.action != null)
            {
                menuButtonAction.action.Enable();
                menuButtonAction.action.performed += OnMenuButtonPressed;
                Debug.Log($"[VRMenuS2] Action Enabled: {menuButtonAction.action.name}");
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
            Debug.Log("[VRMenuS2] Button Pressed!");
            isMenuVisible = !isMenuVisible;
            SetMenuVisibility(isMenuVisible);
            
            // Re-center logic
            if (isMenuVisible && menuCanvas != null)
            {
                Transform cam = Camera.main.transform;
                if (cam != null)
                {
                    menuCanvas.transform.position = cam.position + cam.forward * 1.5f;
                    menuCanvas.transform.rotation = Quaternion.LookRotation(menuCanvas.transform.position - cam.position);
                }
            }
        }

        public void SetMenuVisibility(bool visible)
        {
            isMenuVisible = visible;

            if (canvasComponent != null)
            {
                canvasComponent.enabled = visible;
            }
            else if (menuCanvas != null && menuCanvas != gameObject)
            {
                menuCanvas.SetActive(visible);
            }
        }

        // --- Callbacks ---
        private void OnFloorChanged(float v) { if(contrastController) contrastController.lrvFloor = v; UpdateLabels(); }
        private void OnBorderFloorChanged(float v) { if(contrastController) contrastController.lrvBorderFloor = v; UpdateLabels(); }
        private void OnBorderWallChanged(float v) { if(contrastController) contrastController.lrvBorderWall = v; UpdateLabels(); }
        private void OnWallChanged(float v) { if(contrastController) contrastController.lrvWall = v; UpdateLabels(); }
        private void OnBenchChanged(float v) { if(contrastController) contrastController.lrvBench = v; UpdateLabels(); }
        private void OnLuxChanged(float v) { if(contrastController) contrastController.emLux = v; UpdateLabels(); }

        private void UpdateLabels()
        {
            if(!contrastController) return;
            if (textFloor) textFloor.text = $"Floor: {contrastController.lrvFloor:F1}";
            if (textBorderFloor) textBorderFloor.text = $"Border(F): {contrastController.lrvBorderFloor:F1}";
            if (textBorderWall) textBorderWall.text = $"Border(W): {contrastController.lrvBorderWall:F1}";
            if (textWall) textWall.text = $"Wall: {contrastController.lrvWall:F1}";
            if (textBench) textBench.text = $"Bench: {contrastController.lrvBench:F1}";
            if (textLux) textLux.text = $"Lux: {contrastController.emLux:F0}";
        }
    }
}
