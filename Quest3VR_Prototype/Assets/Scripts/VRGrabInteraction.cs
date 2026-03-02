using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

namespace Quest3VR.Prototype
{
    /// <summary>
    /// Enhanced grab interaction system for Quest 3 controllers
    /// Supports direct grab, distance grab, and throw physics
    /// </summary>
    [RequireComponent(typeof(XRDirectInteractor))]
    public class VRGrabInteraction : MonoBehaviour
    {
        [Header("Grab Settings")]
        [SerializeField] private GrabType grabType = GrabType.Direct;
        [SerializeField] private float grabRadius = 0.1f;
        [SerializeField] private LayerMask grabbableLayers = ~0;
        
        [Header("Throw Physics")]
        [SerializeField] private bool enableThrowing = true;
        [SerializeField] private float throwMultiplier = 1.5f;
        [SerializeField] private int velocityHistorySize = 5;
        
        [Header("Haptics")]
        [SerializeField] private bool enableHaptics = true;
        [SerializeField] private float grabHapticIntensity = 0.5f;
        [SerializeField] private float grabHapticDuration = 0.1f;
        
        [Header("Visual Feedback")]
        [SerializeField] private bool showGrabLine = true;
        [SerializeField] private Color grabLineColor = Color.cyan;
        [SerializeField] private float grabLineWidth = 0.01f;
        
        private XRDirectInteractor directInteractor;
        private XRRayInteractor rayInteractor;
        private LineRenderer grabLineRenderer;
        
        private Queue<Vector3> velocityHistory = new Queue<Vector3>();
        private Queue<Vector3> angularVelocityHistory = new Queue<Vector3>();
        
        private Transform currentGrabbedObject;
        private Rigidbody grabbedRigidbody;
        private bool isGrabbing = false;
        
        public enum GrabType
        {
            Direct,     // Touch-based grabbing
            Ray,        // Ray-based grabbing for distance
            Both        // Supports both methods
        }
        
        private void Awake()
        {
            directInteractor = GetComponent<XRDirectInteractor>();
            
            // Setup line renderer for visual feedback
            if (showGrabLine)
            {
                grabLineRenderer = gameObject.AddComponent<LineRenderer>();
                grabLineRenderer.startWidth = grabLineWidth;
                grabLineRenderer.endWidth = grabLineWidth;
                grabLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                grabLineRenderer.startColor = grabLineColor;
                grabLineRenderer.endColor = grabLineColor;
                grabLineRenderer.positionCount = 2;
                grabLineRenderer.enabled = false;
            }
            
            // Subscribe to interaction events
            directInteractor.selectEntered.AddListener(OnGrabStarted);
            directInteractor.selectExited.AddListener(OnGrabEnded);
            directInteractor.hoverEntered.AddListener(OnHoverEntered);
            directInteractor.hoverExited.AddListener(OnHoverExited);
        }
        
        private void OnDestroy()
        {
            if (directInteractor != null)
            {
                directInteractor.selectEntered.RemoveListener(OnGrabStarted);
                directInteractor.selectExited.RemoveListener(OnGrabEnded);
                directInteractor.hoverEntered.RemoveListener(OnHoverEntered);
                directInteractor.hoverExited.RemoveListener(OnHoverExited);
            }
        }
        
        private void Update()
        {
            if (isGrabbing && enableThrowing)
            {
                RecordVelocity();
            }
            
            UpdateGrabLine();
        }
        
        private void OnGrabStarted(SelectEnterEventArgs args)
        {
            currentGrabbedObject = args.interactableObject.transform;
            grabbedRigidbody = currentGrabbedObject.GetComponent<Rigidbody>();
            isGrabbing = true;
            
            // Clear velocity history for new grab
            velocityHistory.Clear();
            angularVelocityHistory.Clear();
            
            // Trigger haptic feedback
            if (enableHaptics)
            {
                TriggerHapticFeedback(grabHapticIntensity, grabHapticDuration);
            }
            
            // Configure rigidbody for grabbing
            if (grabbedRigidbody != null)
            {
                grabbedRigidbody.useGravity = false;
                grabbedRigidbody.drag = 10f;
                grabbedRigidbody.angularDrag = 10f;
            }
            
            Debug.Log($"[VRGrabInteraction] Grabbed: {currentGrabbedObject.name}");
        }
        
        private void OnGrabEnded(SelectExitEventArgs args)
        {
            if (currentGrabbedObject != null)
            {
                // Apply throw velocity if enabled
                if (enableThrowing && grabbedRigidbody != null)
                {
                    ApplyThrowVelocity();
                }
                
                // Restore rigidbody settings
                if (grabbedRigidbody != null)
                {
                    grabbedRigidbody.useGravity = true;
                    grabbedRigidbody.drag = 0f;
                    grabbedRigidbody.angularDrag = 0.05f;
                }
                
                Debug.Log($"[VRGrabInteraction] Released: {currentGrabbedObject.name}");
            }
            
            currentGrabbedObject = null;
            grabbedRigidbody = null;
            isGrabbing = false;
        }
        
        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            // Light haptic feedback on hover
            if (enableHaptics)
            {
                TriggerHapticFeedback(0.2f, 0.05f);
            }
        }
        
        private void OnHoverExited(HoverExitEventArgs args)
        {
            // Hide grab line when not hovering
            if (grabLineRenderer != null)
            {
                grabLineRenderer.enabled = false;
            }
        }
        
        private void RecordVelocity()
        {
            if (velocityHistory.Count >= velocityHistorySize)
            {
                velocityHistory.Dequeue();
                angularVelocityHistory.Dequeue();
            }
            
            velocityHistory.Enqueue(transform.position);
            angularVelocityHistory.Enqueue(transform.rotation.eulerAngles);
        }
        
        private void ApplyThrowVelocity()
        {
            if (velocityHistory.Count < 2) return;
            
            Vector3[] velocities = velocityHistory.ToArray();
            Vector3 averageVelocity = Vector3.zero;
            
            for (int i = 1; i < velocities.Length; i++)
            {
                averageVelocity += (velocities[i] - velocities[i - 1]) / Time.deltaTime;
            }
            
            averageVelocity /= (velocities.Length - 1);
            
            // Apply throw multiplier
            grabbedRigidbody.velocity = averageVelocity * throwMultiplier;
            
            Debug.Log($"[VRGrabInteraction] Throw velocity applied: {grabbedRigidbody.velocity}");
        }
        
        private void UpdateGrabLine()
        {
            if (grabLineRenderer == null) return;
            
            if (isGrabbing && currentGrabbedObject != null)
            {
                grabLineRenderer.enabled = true;
                grabLineRenderer.SetPosition(0, transform.position);
                grabLineRenderer.SetPosition(1, currentGrabbedObject.position);
            }
            else
            {
                grabLineRenderer.enabled = false;
            }
        }
        
        private void TriggerHapticFeedback(float intensity, float duration)
        {
            var controller = GetComponent<UnityEngine.XR.Interaction.Toolkit.XRBaseController>();
            if (controller != null)
            {
                controller.SendHapticImpulse(intensity, duration);
            }
        }
        
        // Public methods for external access
        public bool IsGrabbing => isGrabbing;
        public Transform CurrentGrabbedObject => currentGrabbedObject;
        
        public void ForceRelease()
        {
            if (directInteractor != null && isGrabbing)
            {
                directInteractor.enabled = false;
                directInteractor.enabled = true;
            }
        }
    }
}
