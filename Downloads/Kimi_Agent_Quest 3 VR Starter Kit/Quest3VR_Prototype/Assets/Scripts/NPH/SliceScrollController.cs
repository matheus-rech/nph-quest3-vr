using UnityEngine;
using UnityEngine.InputSystem;
using Quest3VR.Prototype;

namespace Quest3VR.NPH
{
    /// <summary>
    /// Controls CT slice scrolling via controller thumbstick.
    /// Supports both Input System actions and fallback to legacy input.
    /// </summary>
    public class SliceScrollController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CTSliceViewer sliceViewer;
        
        [Header("Input (Input System)")]
        [SerializeField] private InputActionProperty rightThumbstickAction;
        [SerializeField] private InputActionProperty scrollAction;
        
        [Header("Input (Fallback)")]
        [SerializeField] private bool useLegacyInput = false;
        [SerializeField] private string legacyAxisName = "Vertical";
        
        [Header("Scroll Settings")]
        [SerializeField] private float scrollThreshold = 0.5f;
        [SerializeField] private float scrollCooldown = 0.25f;
        [SerializeField] private bool invertScroll = false;
        
        [Header("Haptics")]
        [SerializeField] private bool enableScrollHaptics = true;
        [SerializeField] private float scrollHapticIntensity = 0.3f;
        [SerializeField] private float scrollHapticDuration = 0.05f;

        private float lastScrollTime;
        private bool wasScrolling;

        private void Start()
        {
            // Try to find slice viewer if not assigned
            if (sliceViewer == null)
            {
                sliceViewer = FindObjectOfType<CTSliceViewer>();
                if (sliceViewer == null)
                {
                    Debug.LogWarning("[SliceScrollController] No CTSliceViewer found. Scrolling disabled.");
                    enabled = false;
                    return;
                }
            }

            // Enable input action if assigned
            if (rightThumbstickAction.action != null && !rightThumbstickAction.action.enabled)
            {
                rightThumbstickAction.action.Enable();
            }
            if (scrollAction.action != null && !scrollAction.action.enabled)
            {
                scrollAction.action.Enable();
            }

            Debug.Log("[SliceScrollController] Initialized. Use right thumbstick up/down to scroll slices.");
        }

        private void Update()
        {
            if (sliceViewer == null) return;
            if (Time.time - lastScrollTime < scrollCooldown) return;

            float scrollInput = GetScrollInput();
            if (invertScroll) scrollInput = -scrollInput;

            // Only trigger when crossing threshold from neutral
            if (Mathf.Abs(scrollInput) > scrollThreshold)
            {
                if (!wasScrolling)
                {
                    if (scrollInput > 0)
                    {
                        sliceViewer.NextSlice();
                        TriggerScrollHaptic();
                    }
                    else
                    {
                        sliceViewer.PreviousSlice();
                        TriggerScrollHaptic();
                    }
                    
                    lastScrollTime = Time.time;
                    wasScrolling = true;
                }
            }
            else
            {
                wasScrolling = false;
            }
        }

        /// <summary>
        /// Get scroll input from available sources.
        /// </summary>
        private float GetScrollInput()
        {
            // Priority 1: Dedicated scroll action
            if (scrollAction.action != null)
            {
                return scrollAction.action.ReadValue<float>();
            }

            // Priority 2: Right thumbstick Y
            if (rightThumbstickAction.action != null)
            {
                Vector2 thumbstick = rightThumbstickAction.action.ReadValue<Vector2>();
                return thumbstick.y;
            }

            // Priority 3: Legacy input fallback
            if (useLegacyInput)
            {
                return Input.GetAxis(legacyAxisName);
            }

            // Priority 4: Try to read from VR controllers directly
            return GetVRThumbstickInput();
        }

        /// <summary>
        /// Attempt to read thumbstick input from VR controllers using common input paths.
        /// </summary>
        private float GetVRThumbstickInput()
        {
            // Try various common VR controller thumbstick bindings
            // These may work depending on the input system configuration
            
            // Oculus/Meta Quest controller thumbstick
            if (Gamepad.current != null)
            {
                return Gamepad.current.rightStick.ReadValue().y;
            }

            return 0f;
        }

        private void TriggerScrollHaptic()
        {
            if (!enableScrollHaptics) return;
            VRHapticsManager.Instance?.TriggerHaptic(
                VRHapticsManager.ControllerType.Right, 
                scrollHapticIntensity, 
                scrollHapticDuration);
        }

        /// <summary>
        /// Set up default input actions programmatically if none are assigned.
        /// </summary>
        public void SetupDefaultInput()
        {
            #if ENABLE_INPUT_SYSTEM
            // Create a simple binding for right thumbstick Y axis
            var action = new InputAction("ScrollSlices", binding: "<XRController>{RightHand}/thumbstick");
            action.Enable();
            
            // Create InputActionProperty wrapper
            var actionRef = ScriptableObject.CreateInstance<InputActionReference>();
            // Note: In production, you'd want to properly set up the action reference
            Debug.Log("[SliceScrollController] Default input actions created.");
            #endif
        }

        private void OnEnable()
        {
            // Re-enable actions when component is enabled
            if (rightThumbstickAction.action != null)
                rightThumbstickAction.action.Enable();
            if (scrollAction.action != null)
                scrollAction.action.Enable();
        }

        private void OnDisable()
        {
            // Disable actions when component is disabled
            if (rightThumbstickAction.action != null)
                rightThumbstickAction.action.Disable();
            if (scrollAction.action != null)
                scrollAction.action.Disable();
        }
    }
}
