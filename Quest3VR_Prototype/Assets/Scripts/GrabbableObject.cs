using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace Quest3VR.Prototype
{
    /// <summary>
    /// Grabbable Object component for Quest 3 VR
    /// Makes any object interactable with VR controllers
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class GrabbableObject : MonoBehaviour
    {
        [Header("Grab Behavior")]
        [SerializeField] private GrabMode grabMode = GrabMode.Kinematic;
        [SerializeField] private bool preserveScale = true;
        [SerializeField] private bool useDynamicAttach = true;
        
        [Header("Physics Settings")]
        [SerializeField] private float grabbedDrag = 10f;
        [SerializeField] private float grabbedAngularDrag = 10f;
        [SerializeField] private bool disableGravityOnGrab = true;
        
        [Header("Visual Feedback")]
        [SerializeField] private bool highlightOnHover = true;
        [SerializeField] private Color highlightColor = new Color(0.5f, 0.8f, 1f, 0.5f);
        [SerializeField] private float highlightIntensity = 0.3f;
        
        [Header("Haptics")]
        [SerializeField] private bool useCustomHaptics = false;
        [SerializeField] private float grabHapticIntensity = 0.5f;
        [SerializeField] private float grabHapticDuration = 0.1f;
        [SerializeField] private float releaseHapticIntensity = 0.3f;
        [SerializeField] private float releaseHapticDuration = 0.05f;
        
        [Header("Events")]
        public UnityEngine.Events.UnityEvent onGrab;
        public UnityEngine.Events.UnityEvent onRelease;
        public UnityEngine.Events.UnityEvent onHoverEnter;
        public UnityEngine.Events.UnityEvent onHoverExit;
        
        private Rigidbody rb;
        private XRGrabInteractable grabInteractable;
        private Renderer objectRenderer;
        private Material originalMaterial;
        private Material highlightMaterial;
        
        private float originalDrag;
        private float originalAngularDrag;
        private bool originalGravity;
        
        private bool isGrabbed = false;
        private bool isHovered = false;
        
        public enum GrabMode
        {
            Kinematic,      // Object becomes kinematic while grabbed
            PhysicsBased,   // Physics-based grabbing with constraints
            ParentBased     // Parent to controller while grabbed
        }
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            grabInteractable = GetComponent<XRGrabInteractable>();
            objectRenderer = GetComponentInChildren<Renderer>();
            
            // Store original physics values
            originalDrag = rb.drag;
            originalAngularDrag = rb.angularDrag;
            originalGravity = rb.useGravity;
            
            // Setup highlight material
            if (objectRenderer != null && highlightOnHover)
            {
                originalMaterial = objectRenderer.material;
                highlightMaterial = new Material(originalMaterial);
            }
            
            // Configure grab interactable
            ConfigureGrabInteractable();
        }
        
        private void OnEnable()
        {
            // Subscribe to interaction events
            grabInteractable.selectEntered.AddListener(OnGrabStarted);
            grabInteractable.selectExited.AddListener(OnGrabEnded);
            grabInteractable.hoverEntered.AddListener(OnHoverStarted);
            grabInteractable.hoverExited.AddListener(OnHoverEnded);
        }
        
        private void OnDisable()
        {
            // Unsubscribe from events
            grabInteractable.selectEntered.RemoveListener(OnGrabStarted);
            grabInteractable.selectExited.RemoveListener(OnGrabEnded);
            grabInteractable.hoverEntered.RemoveListener(OnHoverStarted);
            grabInteractable.hoverExited.RemoveListener(OnHoverEnded);
        }
        
        private void ConfigureGrabInteractable()
        {
            if (grabInteractable == null) return;
            
            // Configure movement type based on grab mode
            switch (grabMode)
            {
                case GrabMode.Kinematic:
                    grabInteractable.movementType = XRBaseInteractable.MovementType.Kinematic;
                    break;
                case GrabMode.PhysicsBased:
                    grabInteractable.movementType = XRBaseInteractable.MovementType.VelocityTracking;
                    break;
                case GrabMode.ParentBased:
                    grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
                    break;
            }
            
            // Enable dynamic attach for more natural grabbing
            grabInteractable.useDynamicAttach = useDynamicAttach;
            
            // Allow multiple grabbers
            grabInteractable.selectMode = InteractableSelectMode.Multiple;
        }
        
        #region Event Handlers
        
        private void OnGrabStarted(SelectEnterEventArgs args)
        {
            isGrabbed = true;
            
            // Apply grab physics settings
            rb.drag = grabbedDrag;
            rb.angularDrag = grabbedAngularDrag;
            
            if (disableGravityOnGrab)
            {
                rb.useGravity = false;
            }
            
            // Trigger haptic feedback
            TriggerHapticFeedback(args.interactorObject.transform, grabHapticIntensity, grabHapticDuration);
            
            // Invoke events
            onGrab?.Invoke();
            
            Debug.Log($"[GrabbableObject] Grabbed: {gameObject.name}");
        }
        
        private void OnGrabEnded(SelectExitEventArgs args)
        {
            isGrabbed = false;
            
            // Restore original physics settings
            rb.drag = originalDrag;
            rb.angularDrag = originalAngularDrag;
            rb.useGravity = originalGravity;
            
            // Trigger haptic feedback
            TriggerHapticFeedback(args.interactorObject.transform, releaseHapticIntensity, releaseHapticDuration);
            
            // Invoke events
            onRelease?.Invoke();
            
            Debug.Log($"[GrabbableObject] Released: {gameObject.name}");
        }
        
        private void OnHoverStarted(HoverEnterEventArgs args)
        {
            isHovered = true;
            
            // Apply highlight
            if (highlightOnHover && objectRenderer != null)
            {
                objectRenderer.material = highlightMaterial;
                highlightMaterial.SetColor("_EmissionColor", highlightColor * highlightIntensity);
                highlightMaterial.EnableKeyword("_EMISSION");
            }
            
            // Light haptic feedback
            TriggerHapticFeedback(args.interactorObject.transform, 0.2f, 0.05f);
            
            // Invoke events
            onHoverEnter?.Invoke();
        }
        
        private void OnHoverEnded(HoverExitEventArgs args)
        {
            isHovered = false;
            
            // Remove highlight
            if (highlightOnHover && objectRenderer != null)
            {
                objectRenderer.material = originalMaterial;
            }
            
            // Invoke events
            onHoverExit?.Invoke();
        }
        
        #endregion
        
        private void TriggerHapticFeedback(Transform interactorTransform, float intensity, float duration)
        {
            if (!useCustomHaptics) return;
            
            var controller = interactorTransform.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRBaseController>();
            if (controller != null)
            {
                controller.SendHapticImpulse(intensity, duration);
            }
        }
        
        #region Public Methods
        
        public void ForceRelease()
        {
            if (isGrabbed && grabInteractable != null)
            {
                grabInteractable.enabled = false;
                grabInteractable.enabled = true;
            }
        }
        
        public void SetGrabEnabled(bool enabled)
        {
            if (grabInteractable != null)
            {
                grabInteractable.enabled = enabled;
            }
        }
        
        public void ChangeMaterial(Material newMaterial)
        {
            if (objectRenderer != null)
            {
                objectRenderer.material = newMaterial;
                originalMaterial = newMaterial;
                
                // Update highlight material
                if (highlightMaterial != null)
                {
                    highlightMaterial = new Material(newMaterial);
                }
            }
        }
        
        public bool IsGrabbed => isGrabbed;
        public bool IsHovered => isHovered;
        
        #endregion
    }
}
