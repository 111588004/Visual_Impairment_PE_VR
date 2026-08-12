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
            InitializeUI();
        }

        private void OnEnable()
        {
            if (menuButtonAction != null && menuButtonAction.action != null)
            {
                menuButtonAction.action.Enable();
                menuButtonAction.action.performed += OnMenuButtonPressed;
                Debug.Log($"[VRMenuS2] Action Enabled: {menuButtonAction.action.name}");
            }
            InitializeUI();
        }

        public void InitializeUI()
        {
            if (contrastController == null)
            {
                contrastController = FindFirstObjectByType<ContrastControllerS2>();
            }
            
            if (menuCanvas != null)
            {
                canvasComponent = menuCanvas.GetComponent<Canvas>();
                if (canvasComponent != null && canvasComponent.worldCamera == null)
                {
                    canvasComponent.worldCamera = Camera.main;
                }
            }

            // Sync Sliders to Controller
            if (contrastController != null)
            {
                if (sliderFloor) { sliderFloor.onValueChanged.RemoveAllListeners(); sliderFloor.value = contrastController.lrvFloor; sliderFloor.onValueChanged.AddListener(OnFloorChanged); }
                
                // BorderF is hidden/not used as per user request
                if (sliderBorderFloor) { sliderBorderFloor.onValueChanged.RemoveAllListeners(); sliderBorderFloor.gameObject.SetActive(false); }
                
                if (sliderBorderWall) { sliderBorderWall.onValueChanged.RemoveAllListeners(); sliderBorderWall.value = contrastController.lrvBorderWall; sliderBorderWall.onValueChanged.AddListener(OnBorderWallChanged); }
                if (sliderWall) { sliderWall.onValueChanged.RemoveAllListeners(); sliderWall.value = contrastController.lrvWall; sliderWall.onValueChanged.AddListener(OnWallChanged); }
                if (sliderBench) { sliderBench.onValueChanged.RemoveAllListeners(); sliderBench.value = contrastController.lrvBench; sliderBench.onValueChanged.AddListener(OnBenchChanged); }
                if (sliderLux) { sliderLux.onValueChanged.RemoveAllListeners(); sliderLux.value = contrastController.emLux; sliderLux.onValueChanged.AddListener(OnLuxChanged); }
            }

            UpdateLabels();
            ApplyDynamicUI();
            SetMenuVisibility(true);
        }

        private void ApplyDynamicUI()
        {
            if (SimulationVariantManager.Instance != null && SimulationVariantManager.Instance.ActiveVariant != null)
            {
                var variant = SimulationVariantManager.Instance.ActiveVariant;
                
                // Permanent Hide BorderF (as per user request)
                SetGroupVisibility(sliderBorderFloor, textBorderFloor, false);

                // Task-based visibility
                SetGroupVisibility(sliderFloor, textFloor, IsAdjustable(variant, "FloorLRV"));
                
                // SKIRTING Mapper: "SkirtingLRV" in Excel/Variant -> "BorderWallLRV" in S2
                bool isSkirtingAdj = IsAdjustable(variant, "SkirtingLRV") || IsAdjustable(variant, "BorderW") || IsAdjustable(variant, "BorderWallLRV");
                SetGroupVisibility(sliderBorderWall, textBorderWall, isSkirtingAdj);
                
                SetGroupVisibility(sliderWall, textWall, IsAdjustable(variant, "WallLRV"));
                SetGroupVisibility(sliderBench, textBench, IsAdjustable(variant, "BenchLRV"));
                SetGroupVisibility(sliderLux, textLux, IsAdjustable(variant, "EmLux"));

                // Refresh Layout to Center remaining items
                RebuildLayout();
            }
        }

        private void RebuildLayout()
        {
            if (menuCanvas == null) return;
            
            RectTransform container = GetLayoutContainer();
            if (container == null) return;

            // Preserve Panel Width (standardized to 600)
            float currentWidth = container.rect.width;
            if (currentWidth < 100) currentWidth = 600f;

            var layout = container.GetComponent<VerticalLayoutGroup>();
            if (layout == null) layout = container.gameObject.AddComponent<VerticalLayoutGroup>();
            
            var fitter = container.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = container.gameObject.AddComponent<ContentSizeFitter>();

            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.padding = new RectOffset(40, 40, 40, 40);
            layout.spacing = 30f; // Tighter spacing for cleaner look

            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Center the container
            container.anchorMax = new Vector2(0.5f, 0.5f);
            container.anchorMin = new Vector2(0.5f, 0.5f);
            container.pivot = new Vector2(0.5f, 0.5f);
            container.sizeDelta = new Vector2(600f, container.sizeDelta.y); // Force 600 width
            container.anchoredPosition = Vector2.zero;

            // Optional: Rename labels if they exist
            if (textBorderWall && textBorderWall.text.Contains("Border(W)"))
            {
                // We'll update labels in UpdateLabels()
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(container);
        }

        private RectTransform GetLayoutContainer()
        {
            if (menuCanvas == null) return null;
            var layout = menuCanvas.GetComponentInChildren<VerticalLayoutGroup>(true);
            if (layout != null) return layout.GetComponent<RectTransform>();
            
            if (menuCanvas.transform.childCount > 0)
            {
                var firstChild = menuCanvas.transform.GetChild(0) as RectTransform;
                if (firstChild != null) return firstChild;
            }
            return menuCanvas.GetComponent<RectTransform>();
        }

        private bool IsAdjustable(SimulationVariantData variant, string id)
        {
            var p = variant.GetParameter(id);
            return p != null ? p.isAdjustable : false; // Default to false if not mentioned (fixed)
        }

        private void SetGroupVisibility(Slider s, Text valText, bool visible)
        {
            if (s) ToggleRow(s.gameObject, visible);
            if (valText) ToggleRow(valText.gameObject, visible);
        }

        private void ToggleRow(GameObject obj, bool active)
        {
            if (obj == null) return;

            RectTransform container = GetLayoutContainer();
            if (container == null || container.gameObject == obj)
            {
                obj.SetActive(active);
                return;
            }

            Transform t = obj.transform;
            while (t.parent != null && t.parent != container && t.parent.gameObject != menuCanvas)
            {
                t = t.parent;
            }

            if (t != null && t.gameObject != menuCanvas && t.gameObject != container.gameObject)
            {
                t.gameObject.SetActive(active);
            }
            else
            {
                obj.SetActive(active);
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
            isMenuVisible = !isMenuVisible;
            SetMenuVisibility(isMenuVisible);
            
            // Re-center logic
            if (isMenuVisible && menuCanvas != null)
            {
                Transform cam = Camera.main.transform;
                if (cam != null)
                {
                    // Summon to Eye Level, 1.2m front
                    menuCanvas.transform.position = cam.position + cam.forward * 1.2f;
                    menuCanvas.transform.rotation = Quaternion.LookRotation(menuCanvas.transform.position - cam.position);
                }
            }
        }

        public void SetMenuVisibility(bool visible)
        {
            isMenuVisible = visible;

            if (canvasComponent == null && menuCanvas != null)
            {
                canvasComponent = menuCanvas.GetComponent<Canvas>();
            }

            if (canvasComponent != null)
            {
                canvasComponent.enabled = visible;
                
                // Fix for VR Raycaster
                var vrRaycaster = menuCanvas.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
                if (vrRaycaster) vrRaycaster.enabled = visible;
            }
            else if (menuCanvas != null && menuCanvas != gameObject)
            {
                menuCanvas.SetActive(visible);
            }
        }

        // --- Callbacks ---
        private void OnFloorChanged(float v) { if(contrastController) { contrastController.lrvFloor = v; contrastController.UpdateAll(); } UpdateLabels(); }
        private void OnBorderFloorChanged(float v) { if(contrastController) { contrastController.lrvBorderFloor = v; contrastController.UpdateAll(); } UpdateLabels(); }
        private void OnBorderWallChanged(float v) { if(contrastController) { contrastController.lrvBorderWall = v; contrastController.UpdateAll(); } UpdateLabels(); }
        private void OnWallChanged(float v) { if(contrastController) { contrastController.lrvWall = v; contrastController.UpdateAll(); } UpdateLabels(); }
        private void OnBenchChanged(float v) { if(contrastController) { contrastController.lrvBench = v; contrastController.UpdateAll(); } UpdateLabels(); }
        private void OnLuxChanged(float v) { if(contrastController) { contrastController.emLux = v; contrastController.UpdateAll(); } UpdateLabels(); }

        private void UpdateLabels()
        {
            if(!contrastController) return;
            if (textFloor) textFloor.text = $"{contrastController.lrvFloor:F1}";
            if (textBorderFloor) textBorderFloor.text = $"{contrastController.lrvBorderFloor:F1}";
            if (textBorderWall) textBorderWall.text = $"{contrastController.lrvBorderWall:F1}";
            if (textWall) textWall.text = $"{contrastController.lrvWall:F1}";
            if (textBench) textBench.text = $"{contrastController.lrvBench:F1}";
            if (textLux) textLux.text = $"{contrastController.emLux:F0}";
        }
    }
}
