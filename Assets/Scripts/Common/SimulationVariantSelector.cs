using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace VISimulation
{
    /// <summary>
    /// Attach this to a UI Panel (e.g. in your VR Menu) to automatically 
    /// generate buttons for selecting different scene variants/tasks.
    /// </summary>
    public class SimulationVariantSelector : MonoBehaviour
    {
        [Header("Assets")]
        [Tooltip("Drag the Variant (.asset) files you want to show as buttons here.")]
        public List<SimulationVariantData> variants = new List<SimulationVariantData>();

        [Header("UI References")]
        [Tooltip("A Button prefab with a Text or TMP_Text component.")]
        public GameObject buttonPrefab;
        [Tooltip("The vertical/horizontal layout group where buttons will be spawned.")]
        public Transform buttonContainer;

        [Header("Controls")]
        [Tooltip("Optional: Joystick Input to cycle through tasks.")]
        public InputActionReference joystickAction;
        public float joystickDeadzone = 0.7f;
        private float _joystickCooldown = 0f;

        void Start()
        {
            if (buttonContainer == null) buttonContainer = transform;
            GenerateButtons();
        }

        public void GenerateButtons()
        {
            // Clear existing buttons
            foreach (Transform child in buttonContainer)
            {
                Destroy(child.gameObject);
            }

            if (variants == null || variants.Count == 0)
            {
                Debug.LogWarning("[VariantSelector] No variants assigned to the list.");
                return;
            }

            foreach (var variant in variants)
            {
                if (variant == null) continue;

                GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);
                btnObj.name = "Button_" + variant.variantName;

                // Sync Text
                TMP_Text tmp = btnObj.GetComponentInChildren<TMP_Text>();
                if (tmp != null) tmp.text = variant.variantName.Replace("_", " ");
                else
                {
                    Text legacy = btnObj.GetComponentInChildren<Text>();
                    if (legacy != null) legacy.text = variant.variantName.Replace("_", " ");
                }

                // Add Click Listener
                Button btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => OnVariantClick(variant));
                }
            }
        }

        void Update()
        {
            // Only respond to keyboard/joystick shortcuts if the UI is actually visible to the user.
            if (!gameObject.activeInHierarchy) return;
            
            Canvas c = GetComponentInParent<Canvas>();
            if (c != null && !c.enabled) return;

            HandleKeyboardDebug();
            HandleJoystickInput();
        }

        private void HandleJoystickInput()
        {
            if (joystickAction == null || joystickAction.action == null) return;

            // Priority: If we have sliders to navigate, let the Navigator handle Up/Down.
            if (IsJoystickReservedForSliders()) return;

            // Handle Cooldown
            if (_joystickCooldown > 0)
            {
                _joystickCooldown -= Time.deltaTime;
                return;
            }

            Vector2 input = joystickAction.action.ReadValue<Vector2>();
            
            // Check Y-axis for Task cycling (Up -> Prev, Down -> Next)
            if (Mathf.Abs(input.y) > joystickDeadzone)
            {
                int direction = (input.y > 0) ? -1 : 1;
                CycleVariant(direction);
                _joystickCooldown = 0.3f; // Responsive repeat delay (0.3s)
            }
        }

        private bool IsJoystickReservedForSliders()
        {
            // LOOSENED: We no longer block task switching just because sliders exist.
            // This allows the user to cycle tasks in S2 Task 1 as requested.
            // The Slider navigator and Task selector will BOTH respond to Up/Down.
            return false;
        }

        public void CycleVariant(int delta)
        {
            if (variants == null || variants.Count == 0) return;

            // Find current variant index
            var current = (SimulationVariantManager.Instance != null) ? SimulationVariantManager.Instance.ActiveVariant : null;
            int currentIndex = (current != null) ? variants.IndexOf(current) : -1;

            // Calculate next (Looping)
            int nextIndex = (currentIndex + delta + variants.Count) % variants.Count;
            OnVariantClick(variants[nextIndex]);
        }

        private void HandleKeyboardDebug()
        {
            if (UnityEngine.InputSystem.Keyboard.current == null) return;

            // 1. Map F1 -> Variant 0, F2 -> Variant 1, etc. (ALWAYS works)
            for (int i = 0; i < variants.Count; i++)
            {
                var key = GetFunctionKey(i);
                if (key != UnityEngine.InputSystem.Key.None && UnityEngine.InputSystem.Keyboard.current[key].wasPressedThisFrame)
                {
                    OnVariantClick(variants[i]);
                    break;
                }
            }

            // 2. Map Up/Down Arrow Keys to Cycle Variants (Only if not reserved for sliders)
            if (IsJoystickReservedForSliders()) return;

            if (UnityEngine.InputSystem.Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                CycleVariant(-1);
            }
            else if (UnityEngine.InputSystem.Keyboard.current.downArrowKey.wasPressedThisFrame)
            {
                CycleVariant(1);
            }
        }

        private UnityEngine.InputSystem.Key GetFunctionKey(int index)
        {
            switch (index)
            {
                case 0: return UnityEngine.InputSystem.Key.F1;
                case 1: return UnityEngine.InputSystem.Key.F2;
                case 2: return UnityEngine.InputSystem.Key.F3;
                case 3: return UnityEngine.InputSystem.Key.F4;
                case 4: return UnityEngine.InputSystem.Key.F5;
                case 5: return UnityEngine.InputSystem.Key.F6;
                default: return UnityEngine.InputSystem.Key.None;
            }
        }

        private void OnVariantClick(SimulationVariantData variant)
        {
            if (variant == null) return;
            
            // Safety Check: If this component is on a deactivated/hidden menu, ignore input.
            if (!gameObject.activeInHierarchy) return;

            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            Debug.Log($"[VariantSelector] TRACE: User clicked '{variant.variantName}'. " + 
                      $"Target Scene (Asset): '{variant.sceneName}', Current Scene: '{currentScene}'");
            
            // 1. Update Global State
            if (SimulationVariantManager.Instance != null)
            {
                SimulationVariantManager.Instance.SetActiveVariant(variant);
            }

            // 2. IMMEDIATE FEEDBACK: If already in the target scene, apply directly
            // Note: We use string.Equals to avoid case-sensitivity issues
            if (string.Equals(variant.sceneName, currentScene, System.StringComparison.OrdinalIgnoreCase))
            {
                ApplyToCurrentScene(variant);
                return;
            }

            // 3. DIFFERENT SCENE: Use Navigation Manager to load
            // IMPORTANT: If variant.sceneName is empty, we assume it's for the current scene too!
            if (string.IsNullOrEmpty(variant.sceneName))
            {
                ApplyToCurrentScene(variant);
                return;
            }

            Debug.Log($"[VariantSelector] Scene mismatch (Current: {currentScene}, Target: {variant.sceneName}). Loading new scene.");
            if (SceneNavigationManager.Instance != null)
            {
                SceneNavigationManager.Instance.LoadSceneWithVariant(variant);
            }
        }

        private void ApplyToCurrentScene(SimulationVariantData variant)
        {
            var controller = FindAnyObjectByType<MonoBehaviour>() as ISimulationController;
            if (controller == null)
            {
                var allMB = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                foreach (var mb in allMB)
                {
                    if (mb is ISimulationController sc)
                    {
                        controller = sc;
                        break;
                    }
                }
            }

            if (controller != null)
            {
                Debug.Log($"[VariantSelector] Applying variant {variant.variantName} to current scene.");
                controller.ApplyVariant(variant);
            }
            else
            {
                Debug.LogWarning("[VariantSelector] No ISimulationController found to apply variant!");
            }
        }
    }
}
