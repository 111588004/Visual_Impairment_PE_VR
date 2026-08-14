using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace VISimulation
{
    public class VREnvpMenuS1 : MonoBehaviour
    {
        [Header("References")]
        public EnvpController contrastController;
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
        private Canvas canvasComponent;

        private void Start()
        {
            InitializeUI();
            EnsureJoystickNavigator();
        }

        private void EnsureJoystickNavigator()
        {
            if (menuCanvas != null)
            {
                var nav = menuCanvas.GetComponent<VRMenuJoystickNavigator>();
                if (nav == null) nav = menuCanvas.AddComponent<VRMenuJoystickNavigator>();
                
                // If you have a specific Input Action for XRI Left Hand Move, 
                // we can try to auto-find it, but usually better if user assigns in Inspector.
                // For now, it will use the public field in the inspector.
            }
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
            InitializeUI();
        }

        private void OnDisable()
        {
            if (menuButtonAction != null && menuButtonAction.action != null)
            {
                menuButtonAction.action.performed -= OnMenuButtonPressed;
                menuButtonAction.action.Disable();
            }
        }

        public void InitializeUI()
        {
            if (contrastController == null)
            {
                contrastController = FindFirstObjectByType<EnvpController>();
            }

            if (contrastController != null)
            {
                // Sync Sliders
                if (sliderNosing) 
                {
                    sliderNosing.onValueChanged.RemoveAllListeners();
                    sliderNosing.value = contrastController.lrvNosing;
                    sliderNosing.onValueChanged.AddListener(OnNosingChanged);
                }
                if (sliderTread) 
                {
                    sliderTread.onValueChanged.RemoveAllListeners();
                    sliderTread.value = contrastController.lrvTread;
                    sliderTread.onValueChanged.AddListener(OnTreadChanged);
                }
                if (sliderWall) 
                {
                    sliderWall.onValueChanged.RemoveAllListeners();
                    sliderWall.value = contrastController.lrvWall;
                    sliderWall.onValueChanged.AddListener(OnWallChanged);
                }
                if (sliderLux) 
                {
                    sliderLux.onValueChanged.RemoveAllListeners();
                    sliderLux.value = contrastController.emLux;
                    sliderLux.onValueChanged.AddListener(OnLuxChanged);
                }
            }

            UpdateLabels();
            ApplyDynamicUI();
            
            // Fix World Camera
            if (menuCanvas != null)
            {
                if (canvasComponent == null) canvasComponent = menuCanvas.GetComponent<Canvas>();
                if (canvasComponent != null && canvasComponent.worldCamera == null) canvasComponent.worldCamera = Camera.main;
            }
        }

        private void ApplyDynamicUI()
        {
            if (SimulationVariantManager.Instance != null && SimulationVariantManager.Instance.ActiveVariant != null)
            {
                var variant = SimulationVariantManager.Instance.ActiveVariant;
                
                SetGroupVisibility(sliderNosing, textNosing, IsAdjustable(variant, "NosingLRV"));
                SetGroupVisibility(sliderTread, textTread, IsAdjustable(variant, "TreadLRV"));
                SetGroupVisibility(sliderWall, textWall, IsAdjustable(variant, "WallLRV"));
                SetGroupVisibility(sliderLux, textLux, IsAdjustable(variant, "EmLux"));

                // Refresh Layout to Center remaining items
                RebuildLayout();
                
                // Refresh Joystick Navigator to update the list of active sliders
                var nav = menuCanvas?.GetComponent<VRMenuJoystickNavigator>();
                if (nav != null) nav.RefreshSliders();
            }
        }

        private void RebuildLayout()
        {
            if (menuCanvas == null) return;
            
            RectTransform container = GetLayoutContainer();
            if (container == null) return;

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
            layout.spacing = 40f;

            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            container.anchorMax = new Vector2(0.5f, 0.5f);
            container.anchorMin = new Vector2(0.5f, 0.5f);
            container.pivot = new Vector2(0.5f, 0.5f);
            container.sizeDelta = new Vector2(600f, container.sizeDelta.y);
            container.anchoredPosition = Vector2.zero;

            LayoutRebuilder.ForceRebuildLayoutImmediate(container);
        }

        private RectTransform GetLayoutContainer()
        {
            if (menuCanvas == null) return null;
            
            // 1. Try to find existing VerticalLayoutGroup
            var layout = menuCanvas.GetComponentInChildren<VerticalLayoutGroup>(true);
            if (layout != null) return layout.GetComponent<RectTransform>();
            
            // 2. Fallback: First child that isn't null and has RectTransform
            if (menuCanvas.transform.childCount > 0)
            {
                var firstChild = menuCanvas.transform.GetChild(0) as RectTransform;
                if (firstChild != null) return firstChild;
            }
            
            // 3. Last fallback: menuCanvas itself
            return menuCanvas.GetComponent<RectTransform>();
        }

        private bool IsAdjustable(SimulationVariantData variant, string id)
        {
            var p = variant.GetParameter(id);
            return p != null ? p.isAdjustable : true;
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
            // Go up until we are a direct child of the layout container
            while (t.parent != null && t.parent != container && t.parent.gameObject != menuCanvas)
            {
                t = t.parent;
            }

            // Set the row active/inactive IF it's not the container itself
            if (t != null && t.gameObject != menuCanvas && t.gameObject != container.gameObject)
            {
                t.gameObject.SetActive(active);
            }
            else
            {
                obj.SetActive(active);
            }
        }

        private void SetParentOrSelfActive(GameObject obj, bool active)
        {
            if (obj == null) return;
            obj.SetActive(active);
            if (obj.transform.parent != null && (obj.transform.parent.name.Contains("Row") || obj.transform.parent.name.Contains("Group")))
            {
                obj.transform.parent.gameObject.SetActive(active);
            }
        }

        private void OnMenuButtonPressed(InputAction.CallbackContext context)
        {
            Debug.Log("[VRMenu] Button Pressed!");
            isMenuVisible = !isMenuVisible;
            SetMenuVisibility(isMenuVisible);
            
            if (isMenuVisible && menuCanvas != null)
            {
                Transform cam = Camera.main.transform;
                if (cam != null)
                {
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

            if (canvasComponent != null && canvasComponent.worldCamera == null)
            {
                canvasComponent.worldCamera = Camera.main;
            }

            if (canvasComponent != null)
            {
                canvasComponent.enabled = visible;
            }
            else if (menuCanvas != null)
            {
                if (menuCanvas != gameObject)
                {
                    menuCanvas.SetActive(visible);
                }
                else
                {
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
            if (contrastController) contrastController.UpdateAll();
        }

        private void OnTreadChanged(float value)
        {
            if (contrastController) contrastController.lrvTread = value;
            UpdateLabels();
            if (contrastController) contrastController.UpdateAll();
        }

        private void OnWallChanged(float value)
        {
            if (contrastController) contrastController.lrvWall = value;
            UpdateLabels();
            if (contrastController) contrastController.UpdateAll();
        }

        private void OnLuxChanged(float value)
        {
            if (contrastController) contrastController.emLux = value;
            UpdateLabels();
            if (contrastController) contrastController.UpdateAll();
        }

        private void UpdateLabels()
        {
            if (!contrastController) return;
            if (textNosing) textNosing.text = $"{contrastController.lrvNosing:F1}";
            if (textTread) textTread.text = $"{contrastController.lrvTread:F1}";
            if (textWall) textWall.text = $"{contrastController.lrvWall:F1}";
            if (textLux) textLux.text = $"{contrastController.emLux:F0}";
        }
    }
}
