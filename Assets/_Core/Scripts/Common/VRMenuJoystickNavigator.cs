using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

namespace VISimulation
{
    /// <summary>
    /// Handles redirection of Left-Hand Joystick input to UI Sliders within a Menu.
    /// Uses Y-axis for scrolling between sliders and X-axis for value adjustment.
    /// </summary>
    public class VRMenuJoystickNavigator : MonoBehaviour
    {
        [Header("Input Settings")]
        [Tooltip("Reference to the Left Hand Move/Joystick Action")]
        public InputActionReference joystickAction;
        public float stickDeadzone = 0.2f;
        public float repeatDelay = 0.3f;
        public float scrollSensitivity = 1.0f;

        [Header("Visual Feedback")]
        public Color selectedColor = Color.yellow;
        public Color normalColor = Color.white;

        private List<Slider> activeSliders = new List<Slider>();
        private int selectedIndex = 0;
        private Canvas targetCanvas;

        private void Awake()
        {
            targetCanvas = GetComponent<Canvas>();
            TryFindJoystickAction();
        }

        private void TryFindJoystickAction()
        {
            if (joystickAction == null || joystickAction.action == null)
            {
                // 1. Try Global Manager (New Centralized Plan)
                if (SceneNavigationManager.Instance != null && SceneNavigationManager.Instance.globalJoystickAction != null)
                {
                    joystickAction = SceneNavigationManager.Instance.globalJoystickAction;
                    Debug.Log($"[JoystickNav] Linked to Global Action from SceneNavigationManager.");
                }
                
                // 2. Try parent (Standard)
                if (joystickAction == null)
                {
                    var selector = GetComponentInParent<SimulationVariantSelector>();
                    if (selector == null) selector = FindFirstObjectByType<SimulationVariantSelector>();

                    if (selector != null && selector.joystickAction != null)
                    {
                        joystickAction = selector.joystickAction;
                        Debug.Log($"[JoystickNav] Linked to fallback action from {selector.name}");
                    }
                }

                if (joystickAction == null)
                {
                    Debug.LogWarning($"[JoystickNav] NO joystick action found on {gameObject.name} or in manager!");
                }
            }
        }

        private void OnEnable()
        {
            TryFindJoystickAction();
            if (joystickAction != null && joystickAction.action != null) joystickAction.action.Enable();
            RefreshSliders();
        }

        private void OnDisable()
        {
            ResetHighlights();
        }

        private float repeatTimer = 0f;
        private float currentRepeatDelay = 0f;
        private int consecutiveSteps = 0;
        private const float InitialRepeatDelay = 0.25f; // Faster initial start
        private const float MinRepeatDelay = 0.03f;     // Even faster max repeat rate

        private void Update()
        {
            if (targetCanvas != null && !targetCanvas.enabled) return;
            
            Vector2 joystickInput = Vector2.zero;
            if (joystickAction != null && joystickAction.action != null)
            {
                joystickInput = joystickAction.action.ReadValue<Vector2>();
            }

            // --- Keyboard Test Support ---
            float kx = 0;
            float ky = 0;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.leftArrowKey.isPressed) kx = -1f;
                if (Keyboard.current.rightArrowKey.isPressed) kx = 1f;
                if (Keyboard.current.upArrowKey.wasPressedThisFrame) ky = 1f;
                if (Keyboard.current.downArrowKey.wasPressedThisFrame) ky = -1f;
            }

            // Use keyboard if joystick is neutral
            float finalX = (Mathf.Abs(kx) > 0.1f) ? kx : joystickInput.x;
            float finalY = (Mathf.Abs(ky) > 0.1f) ? ky : joystickInput.y;

            // Handle Selection Change (Y-axis)
            // Use Keyboard's wasPressedThisFrame OR Joystick axis with a small threshold
            if (Mathf.Abs(finalY) > 0.5f)
            {
                // To avoid rapid scrolling, we check if it was just pushed (similar to keyboard)
                // or if we have a timer. Here we'll use the same logic as keyboard for now.
                // But specifically for VR, we want to allow joystick even if ky == 0.
                if (ky != 0 || (joystickInput != Vector2.zero && Time.frameCount % 20 == 0)) 
                {
                    if (finalY > 0) MoveSelection(-1); 
                    else MoveSelection(1);             
                }
            }

            // Handle Value Adjustment (X-axis) with Repeat/Acceleration
            if (Mathf.Abs(finalX) > stickDeadzone)
            {
                // TRACE: Helping debug Scene 3
                if (Time.frameCount % 30 == 0)
                {
                     string sliderName = (activeSliders.Count > 0 && selectedIndex < activeSliders.Count) ? activeSliders[selectedIndex].name : "None";
                     Debug.Log($"[JoystickNav] TRACE: InputX={finalX:F2}, SelectedSlider={sliderName}, ActiveCount={activeSliders.Count}");
                }

                if (repeatTimer <= 0)
                {
                    // Increase step size significantly when held for a long time
                    float multiplier = 1f;
                    if (consecutiveSteps > 15) multiplier = 4f; 
                    else if (consecutiveSteps > 8) multiplier = 2f;

                    AdjustSliderValue(finalX > 0 ? 1f : -1f, multiplier);
                    consecutiveSteps++;

                    // Accelerate repeat speed
                    if (currentRepeatDelay == 0)
                        currentRepeatDelay = InitialRepeatDelay;
                    else
                        currentRepeatDelay = Mathf.Max(MinRepeatDelay, currentRepeatDelay * 0.75f);
                    
                    repeatTimer = currentRepeatDelay;
                }
                else
                {
                    repeatTimer -= Time.deltaTime;
                }
            }
            else
            {
                repeatTimer = 0;
                currentRepeatDelay = 0;
                consecutiveSteps = 0; // Reset acceleration
            }

            if (Time.frameCount % 60 == 0) RefreshSliders();
            UpdateHighlights();
        }

        public bool HasActiveSliders()
        {
            return activeSliders != null && activeSliders.Count > 0;
        }

        public void RefreshSliders()
        {
            // Find all sliders under this canvas that are currently visible/active
            var newSliders = GetComponentsInChildren<Slider>(false)
                .Where(s => s.gameObject.activeInHierarchy)
                .ToList();

            if (!newSliders.SequenceEqual(activeSliders))
            {
                activeSliders = newSliders;
                Debug.Log($"[JoystickNav] Refresh: Found {activeSliders.Count} active sliders in {gameObject.name}");
                
                if (activeSliders.Count > 0)
                {
                    selectedIndex = Mathf.Clamp(selectedIndex, 0, activeSliders.Count - 1);
                }
            }
        }

        /// <summary>
        /// External method to set which slider is currently controlled by the joystick.
        /// </summary>
        public void SetSelectedIndex(int index)
        {
            if (activeSliders.Count == 0) return;
            selectedIndex = Mathf.Clamp(index, 0, activeSliders.Count - 1);
            UpdateHighlights();
        }

        /// <summary>
        /// Sets the selection based on a specific slider reference.
        /// </summary>
        public void SetSelectedSlider(Slider slider)
        {
            int index = activeSliders.IndexOf(slider);
            if (index != -1) SetSelectedIndex(index);
        }

        private void MoveSelection(int delta)
        {
            if (activeSliders.Count == 0) return;
            
            selectedIndex = (selectedIndex + delta + activeSliders.Count) % activeSliders.Count;
            Debug.Log($"[JoystickNav] Selected: {activeSliders[selectedIndex].name}");
        }

        private void AdjustSliderValue(float direction, float multiplier = 1f)
        {
            if (activeSliders.Count == 0 || selectedIndex >= activeSliders.Count) return;

            Slider current = activeSliders[selectedIndex];
            float range = current.maxValue - current.minValue;
            
            // Base step size: 5 for Lux (0-1000), 1 for LRV (0-100)
            float baseStep = (range > 500f) ? 5f : 1f;
            float totalStep = baseStep * multiplier;

            // Apply step
            float newValue = current.value + (direction > 0 ? totalStep : -totalStep);
            
            // Snap to nearest baseStep multiple for precision, but allow multiplier to grow
            current.value = Mathf.Round(newValue / baseStep) * baseStep;
            current.value = Mathf.Clamp(current.value, current.minValue, current.maxValue);
        }

        private void UpdateHighlights()
        {
            for (int i = 0; i < activeSliders.Count; i++)
            {
                if (activeSliders[i] == null) continue;
                
                Image handle = activeSliders[i].handleRect != null ? activeSliders[i].handleRect.GetComponent<Image>() : null;
                if (handle != null)
                {
                    handle.color = (i == selectedIndex) ? selectedColor : normalColor;
                }
            }
        }

        private void ResetHighlights()
        {
            foreach (var s in activeSliders)
            {
                if (s == null) continue;
                Image handle = s.handleRect != null ? s.handleRect.GetComponent<Image>() : null;
                if (handle != null) handle.color = normalColor;
            }
        }
    }
}
