using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace VISimulation
{
    public class VRSignageMenuS3 : MonoBehaviour
    {
        [Header("控制器 (Controller Reference)")]
        public SignageControllerS3 signageController;
        public GameObject menuCanvas;

        [Header("UI 滑桿 (Sliders)")]
        public Slider sliderHeight;      // Vertical Height
        public Slider sliderFontSize;    // Char Height (mm)
        [Header("LRV 控制")]
        public Slider sliderWallLrv;
        public Slider sliderBoardLrv;
        public Slider sliderTextLrv;     // Previously Contrast

        [Header("UI 數值顯示 (Value Labels)")]
        public Text textHeight;
        public Text textFontSize;
        public Text textWallLrv;
        public Text textBoardLrv;
        public Text textTextLrv;

        [Header("UI 標題文字 (Title Labels - Drag current labels here to rename)")]
        public Text labelHeightTitle;
        public Text labelFontSizeTitle;
        public Text labelWallLrvTitle;
        public Text labelBoardLrvTitle;
        public Text labelTextLrvTitle;
        
        [Header("不需要的物件 (Unused Objects to Hide)")]
        public GameObject[] objectsToHide;

        [Header("輸入設定 (Input Action)")]
        public InputActionReference menuButtonAction;

        private void Awake()
        {
             EnsureJoystickNavigator();
        }

        private void EnsureJoystickNavigator()
        {
            if (menuCanvas != null)
            {
                var nav = menuCanvas.GetComponent<VRMenuJoystickNavigator>();
                if (nav == null) nav = menuCanvas.AddComponent<VRMenuJoystickNavigator>();
            }
        }

        private void OnEnable()
        {
            if (menuButtonAction != null)
            {
                menuButtonAction.action.Enable();
                menuButtonAction.action.performed += ToggleMenu;
            }

            if (sliderHeight) sliderHeight.onValueChanged.AddListener(OnHeightChanged);
            if (sliderFontSize) sliderFontSize.onValueChanged.AddListener(OnFontSizeChanged);

            if (sliderWallLrv) sliderWallLrv.onValueChanged.AddListener(OnWallLrvChanged);
            if (sliderBoardLrv) sliderBoardLrv.onValueChanged.AddListener(OnBoardLrvChanged);
            if (sliderTextLrv) sliderTextLrv.onValueChanged.AddListener(OnTextLrvChanged);

            InitializeUI();
        }

        private void OnDisable()
        {
            if (menuButtonAction != null)
            {
                menuButtonAction.action.performed -= ToggleMenu;
                menuButtonAction.action.Disable();
            }
        }

        public void InitializeUI()
        {
            if (signageController == null) signageController = FindFirstObjectByType<SignageControllerS3>();
            if (signageController == null) return;

            // Fix for World Space Canvas Mouse Interaction
            if (menuCanvas != null)
            {
                Canvas c = menuCanvas.GetComponent<Canvas>();
                if (c != null && c.worldCamera == null)
                {
                    c.worldCamera = Camera.main;
                }
            }

            // 1. Rename Labels (Visual Fix)
            if (labelHeightTitle) labelHeightTitle.text = "Height (cm)";
            if (labelFontSizeTitle) labelFontSizeTitle.text = "Char Size (mm)";
            if (labelWallLrvTitle) labelWallLrvTitle.text = "Wall (LRV)";
            if (labelBoardLrvTitle) labelBoardLrvTitle.text = "Board (LRV)";
            if (labelTextLrvTitle) labelTextLrvTitle.text = "Text (LRV)";

            // 2. Hide Unused (Global)
            if (objectsToHide != null)
            {
                foreach (var obj in objectsToHide)
                {
                    if (obj) obj.SetActive(false);
                }
            }

            // 3. Sync Sliders to Controller's initial values
            if (sliderHeight) { sliderHeight.minValue = 0f; sliderHeight.maxValue = 350f; sliderHeight.value = signageController.heightCm; }
            if (sliderFontSize) { sliderFontSize.minValue = 5f; sliderFontSize.maxValue = 300f; sliderFontSize.value = signageController.charHeightMm; }
            if (sliderWallLrv) { sliderWallLrv.minValue = 0f; sliderWallLrv.maxValue = 100f; sliderWallLrv.value = signageController.wallLrv; }
            if (sliderBoardLrv) { sliderBoardLrv.minValue = 0f; sliderBoardLrv.maxValue = 100f; sliderBoardLrv.value = signageController.boardLrv; }
            if (sliderTextLrv) { sliderTextLrv.minValue = 0f; sliderTextLrv.maxValue = 100f; sliderTextLrv.value = signageController.textLrv; }

            ApplyDynamicUI();
            UpdateLabels();
        }

        private void ApplyDynamicUI()
        {
            if (SimulationVariantManager.Instance != null && SimulationVariantManager.Instance.ActiveVariant != null)
            {
                var variant = SimulationVariantManager.Instance.ActiveVariant;
                
                SetGroupVisibility(sliderHeight, labelHeightTitle, textHeight, IsAdjustable(variant, "Height"));
                SetGroupVisibility(sliderFontSize, labelFontSizeTitle, textFontSize, IsAdjustable(variant, "FontSize"));
                SetGroupVisibility(sliderTextLrv, labelTextLrvTitle, textTextLrv, IsAdjustable(variant, "TextLRV"));
                SetGroupVisibility(sliderWallLrv, labelWallLrvTitle, textWallLrv, IsAdjustable(variant, "WallLRV"));
                SetGroupVisibility(sliderBoardLrv, labelBoardLrvTitle, textBoardLrv, IsAdjustable(variant, "BoardLRV"));

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
            layout.spacing = 30f;

            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            container.anchorMax = new Vector2(0.5f, 0.5f);
            container.anchorMin = new Vector2(0.5f, 0.5f);
            container.pivot = new Vector2(0.5f, 0.5f);
            container.sizeDelta = new Vector2(600f, container.sizeDelta.y); // Force 600
            container.anchoredPosition = Vector2.zero;

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
            return p != null ? p.isAdjustable : false; // Safe default
        }

        private void SetGroupVisibility(Slider s, Text label, Text valText, bool visible)
        {
            if (s) ToggleRow(s.gameObject, visible);
            if (label) ToggleRow(label.gameObject, visible);
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

        private void ToggleMenu(InputAction.CallbackContext context)
        {
            if (menuCanvas == null) return;
            Canvas c = menuCanvas.GetComponent<Canvas>();
            if (c != null)
            {
                c.enabled = !c.enabled;
                if (c.enabled)
                {
                    Transform cam = Camera.main != null ? Camera.main.transform : null;
                    if (cam != null)
                    {
                        menuCanvas.transform.position = cam.position + cam.forward * 1.2f;
                        menuCanvas.transform.rotation = Quaternion.LookRotation(menuCanvas.transform.position - cam.position);
                    }
                }
                var vrRaycaster = menuCanvas.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
                if (vrRaycaster) vrRaycaster.enabled = c.enabled;
            }
        }

        public void OnHeightChanged(float value) { if (signageController) { signageController.heightCm = value; signageController.UpdateAll(); } UpdateLabels(); }
        public void OnFontSizeChanged(float value) { if (signageController) { signageController.charHeightMm = value; signageController.UpdateAll(); } UpdateLabels(); }
        public void OnWallLrvChanged(float value) { if (signageController) { signageController.wallLrv = value; signageController.UpdateAll(); } UpdateLabels(); }
        public void OnBoardLrvChanged(float value) { if (signageController) { signageController.boardLrv = value; signageController.UpdateAll(); } UpdateLabels(); }
        public void OnTextLrvChanged(float value) { if (signageController) { signageController.textLrv = value; signageController.UpdateAll(); } UpdateLabels(); }

        private void UpdateLabels()
        {
            if (!signageController) return;
            if (textHeight) textHeight.text = $"{signageController.heightCm:F0}";
            if (textFontSize) textFontSize.text = $"{signageController.charHeightMm:F0}";
            if (textBoardLrv) textBoardLrv.text = $"{signageController.boardLrv:F1}";
            if (textTextLrv) textTextLrv.text = $"{signageController.textLrv:F1}";
            if (textWallLrv) textWallLrv.text = $"{signageController.wallLrv:F1}";
        }
    }
}
