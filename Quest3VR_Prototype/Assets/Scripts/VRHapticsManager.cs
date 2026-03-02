using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using System.Collections.Generic;

namespace Quest3VR.Prototype
{
    /// <summary>
    /// Haptics Manager for Quest 3 VR
    /// Provides advanced haptic feedback patterns and intensity control
    /// </summary>
    public class VRHapticsManager : MonoBehaviour
    {
        [Header("Controller References")]
        [SerializeField] private UnityEngine.XR.Interaction.Toolkit.XRBaseController leftController;
        [SerializeField] private UnityEngine.XR.Interaction.Toolkit.XRBaseController rightController;
        
        [Header("Default Settings")]
        [SerializeField] private float defaultIntensity = 0.5f;
        [SerializeField] private float defaultDuration = 0.1f;
        
        [Header("Haptic Patterns")]
        [SerializeField] private List<HapticPattern> hapticPatterns = new List<HapticPattern>();
        
        public static VRHapticsManager Instance { get; private set; }
        
        private Dictionary<string, HapticPattern> patternDictionary = new Dictionary<string, HapticPattern>();
        private Coroutine currentPatternCoroutine;
        
        [System.Serializable]
        public class HapticPattern
        {
            public string patternName;
            public List<HapticPulse> pulses = new List<HapticPulse>();
            public bool loop = false;
            public int loopCount = 1;
        }
        
        [System.Serializable]
        public class HapticPulse
        {
            public float intensity = 0.5f;
            public float duration = 0.1f;
            public float delayAfter = 0f;
        }
        
        public enum ControllerType
        {
            Left,
            Right,
            Both
        }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePatterns();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // Auto-find controllers if not assigned
            if (leftController == null || rightController == null)
            {
                FindControllers();
            }
        }
        
        private void InitializePatterns()
        {
            // Initialize built-in patterns
            patternDictionary.Clear();
            
            foreach (var pattern in hapticPatterns)
            {
                if (!string.IsNullOrEmpty(pattern.patternName))
                {
                    patternDictionary[pattern.patternName.ToLower()] = pattern;
                }
            }
            
            // Add default patterns if not present
            AddDefaultPatterns();
        }
        
        private void AddDefaultPatterns()
        {
            // Short tap pattern
            if (!patternDictionary.ContainsKey("tap"))
            {
                var tapPattern = new HapticPattern
                {
                    patternName = "tap",
                    pulses = new List<HapticPulse>
                    {
                        new HapticPulse { intensity = 0.3f, duration = 0.05f }
                    }
                };
                patternDictionary["tap"] = tapPattern;
            }
            
            // Strong feedback pattern
            if (!patternDictionary.ContainsKey("strong"))
            {
                var strongPattern = new HapticPattern
                {
                    patternName = "strong",
                    pulses = new List<HapticPulse>
                    {
                        new HapticPulse { intensity = 0.8f, duration = 0.2f }
                    }
                };
                patternDictionary["strong"] = strongPattern;
            }
            
            // Double pulse pattern
            if (!patternDictionary.ContainsKey("double"))
            {
                var doublePattern = new HapticPattern
                {
                    patternName = "double",
                    pulses = new List<HapticPulse>
                    {
                        new HapticPulse { intensity = 0.5f, duration = 0.08f, delayAfter = 0.1f },
                        new HapticPulse { intensity = 0.5f, duration = 0.08f }
                    }
                };
                patternDictionary["double"] = doublePattern;
            }
            
            // Warning pattern
            if (!patternDictionary.ContainsKey("warning"))
            {
                var warningPattern = new HapticPattern
                {
                    patternName = "warning",
                    pulses = new List<HapticPulse>
                    {
                        new HapticPulse { intensity = 0.6f, duration = 0.1f, delayAfter = 0.1f },
                        new HapticPulse { intensity = 0.6f, duration = 0.1f, delayAfter = 0.1f },
                        new HapticPulse { intensity = 0.6f, duration = 0.1f }
                    }
                };
                patternDictionary["warning"] = warningPattern;
            }
            
            // Success pattern
            if (!patternDictionary.ContainsKey("success"))
            {
                var successPattern = new HapticPattern
                {
                    patternName = "success",
                    pulses = new List<HapticPulse>
                    {
                        new HapticPulse { intensity = 0.3f, duration = 0.05f, delayAfter = 0.05f },
                        new HapticPulse { intensity = 0.6f, duration = 0.15f }
                    }
                };
                patternDictionary["success"] = successPattern;
            }
            
            // Error pattern
            if (!patternDictionary.ContainsKey("error"))
            {
                var errorPattern = new HapticPattern
                {
                    patternName = "error",
                    pulses = new List<HapticPulse>
                    {
                        new HapticPulse { intensity = 0.8f, duration = 0.2f, delayAfter = 0.1f },
                        new HapticPulse { intensity = 0.4f, duration = 0.4f }
                    }
                };
                patternDictionary["error"] = errorPattern;
            }
        }
        
        private void FindControllers()
        {
            var controllers = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.XRBaseController>();
            
            foreach (var controller in controllers)
            {
                var controllerNode = controller.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRController>();
                if (controllerNode != null)
                {
                    if (controllerNode.controllerNode == XRNode.LeftHand && leftController == null)
                    {
                        leftController = controller;
                    }
                    else if (controllerNode.controllerNode == XRNode.RightHand && rightController == null)
                    {
                        rightController = controller;
                    }
                }
            }
        }
        
        #region Public Haptic Methods
        
        /// <summary>
        /// Trigger a simple haptic impulse on specified controller
        /// </summary>
        public void TriggerHaptic(ControllerType controllerType, float intensity, float duration)
        {
            switch (controllerType)
            {
                case ControllerType.Left:
                    TriggerControllerHaptic(leftController, intensity, duration);
                    break;
                case ControllerType.Right:
                    TriggerControllerHaptic(rightController, intensity, duration);
                    break;
                case ControllerType.Both:
                    TriggerControllerHaptic(leftController, intensity, duration);
                    TriggerControllerHaptic(rightController, intensity, duration);
                    break;
            }
        }
        
        /// <summary>
        /// Trigger a named haptic pattern
        /// </summary>
        public void TriggerPattern(string patternName, ControllerType controllerType)
        {
            string key = patternName.ToLower();
            if (patternDictionary.ContainsKey(key))
            {
                HapticPattern pattern = patternDictionary[key];
                StartCoroutine(PlayPatternCoroutine(pattern, controllerType));
            }
            else
            {
                Debug.LogWarning($"[VRHapticsManager] Pattern '{patternName}' not found");
            }
        }
        
        /// <summary>
        /// Trigger haptic feedback based on collision intensity
        /// </summary>
        public void TriggerCollisionHaptic(ControllerType controllerType, float collisionForce)
        {
            float intensity = Mathf.Clamp01(collisionForce / 10f);
            float duration = Mathf.Lerp(0.05f, 0.3f, intensity);
            
            TriggerHaptic(controllerType, intensity, duration);
        }
        
        /// <summary>
        /// Trigger continuous haptic feedback
        /// </summary>
        public void TriggerContinuousHaptic(ControllerType controllerType, float intensity)
        {
            TriggerHaptic(controllerType, intensity, 0.05f);
        }
        
        /// <summary>
        /// Stop all haptic feedback
        /// </summary>
        public void StopAllHaptics()
        {
            if (currentPatternCoroutine != null)
            {
                StopCoroutine(currentPatternCoroutine);
            }
        }
        
        /// <summary>
        /// Add a custom haptic pattern at runtime
        /// </summary>
        public void AddCustomPattern(HapticPattern pattern)
        {
            if (!string.IsNullOrEmpty(pattern.patternName))
            {
                patternDictionary[pattern.patternName.ToLower()] = pattern;
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void TriggerControllerHaptic(UnityEngine.XR.Interaction.Toolkit.XRBaseController controller, float intensity, float duration)
        {
            if (controller != null)
            {
                controller.SendHapticImpulse(intensity, duration);
            }
        }
        
        private IEnumerator PlayPatternCoroutine(HapticPattern pattern, ControllerType controllerType)
        {
            int loopsRemaining = pattern.loop ? pattern.loopCount : 1;
            
            while (loopsRemaining > 0)
            {
                foreach (var pulse in pattern.pulses)
                {
                    TriggerHaptic(controllerType, pulse.intensity, pulse.duration);
                    
                    if (pulse.delayAfter > 0)
                    {
                        yield return new WaitForSeconds(pulse.duration + pulse.delayAfter);
                    }
                    else
                    {
                        yield return new WaitForSeconds(pulse.duration);
                    }
                }
                
                loopsRemaining--;
                
                if (pattern.loop && loopsRemaining > 0)
                {
                    yield return new WaitForSeconds(0.2f); // Brief pause between loops
                }
            }
        }
        
        #endregion
        
        #region Predefined Haptic Shortcuts
        
        public void Tap(ControllerType controllerType) => TriggerPattern("tap", controllerType);
        public void Strong(ControllerType controllerType) => TriggerPattern("strong", controllerType);
        public void DoublePulse(ControllerType controllerType) => TriggerPattern("double", controllerType);
        public void Warning(ControllerType controllerType) => TriggerPattern("warning", controllerType);
        public void Success(ControllerType controllerType) => TriggerPattern("success", controllerType);
        public void Error(ControllerType controllerType) => TriggerPattern("error", controllerType);
        
        #endregion
    }
}
