using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

namespace Quest3VR.Prototype
{
    /// <summary>
    /// Teleportation system for Quest 3 VR
    /// Supports arc teleportation with preview and fade transitions
    /// </summary>
    public class VRTeleportation : MonoBehaviour
    {
        [Header("Teleportation Controller")]
        [SerializeField] private XRRayInteractor teleportRayInteractor;
        [SerializeField] private Transform xrRig;
        [SerializeField] private Transform cameraOffset;
        
        [Header("Arc Settings")]
        [SerializeField] private float arcVelocity = 10f;
        [SerializeField] private float arcGravity = 9.81f;
        [SerializeField] private int arcResolution = 30;
        [SerializeField] private float maxTeleportDistance = 15f;
        
        [Header("Visual Feedback")]
        [SerializeField] private LineRenderer arcLineRenderer;
        [SerializeField] private GameObject teleportReticlePrefab;
        [SerializeField] private Gradient validTeleportColor;
        [SerializeField] private Gradient invalidTeleportColor;
        
        [Header("Teleportation Layers")]
        [SerializeField] private LayerMask teleportableLayers;
        [SerializeField] private LayerMask blockingLayers;
        
        [Header("Transition Effects")]
        [SerializeField] private bool useFadeTransition = true;
        [SerializeField] private float fadeDuration = 0.2f;
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Haptics")]
        [SerializeField] private bool enableHaptics = true;
        [SerializeField] private float teleportHapticIntensity = 0.7f;
        [SerializeField] private float teleportHapticDuration = 0.15f;
        
        private GameObject teleportReticle;
        private bool isAiming = false;
        private bool validTeleportTarget = false;
        private Vector3 targetPosition;
        private Vector3 targetNormal;
        
        private UnityEngine.XR.Interaction.Toolkit.XRBaseController controller;
        private Camera fadeCamera;
        private Texture2D fadeTexture;
        private bool isFading = false;
        private float fadeAlpha = 0f;
        
        private void Awake()
        {
            // Setup arc line renderer
            if (arcLineRenderer == null)
            {
                arcLineRenderer = gameObject.AddComponent<LineRenderer>();
                arcLineRenderer.startWidth = 0.02f;
                arcLineRenderer.endWidth = 0.02f;
                arcLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                arcLineRenderer.positionCount = arcResolution;
                arcLineRenderer.enabled = false;
            }
            
            // Create or get teleport reticle
            if (teleportReticlePrefab != null)
            {
                teleportReticle = Instantiate(teleportReticlePrefab);
                teleportReticle.SetActive(false);
            }
            else
            {
                // Create default reticle
                teleportReticle = CreateDefaultReticle();
            }
            
            // Get controller reference
            controller = GetComponent<UnityEngine.XR.Interaction.Toolkit.XRBaseController>();
            
            // Setup fade texture
            if (useFadeTransition)
            {
                SetupFadeEffect();
            }
        }
        
        private void Start()
        {
            // Configure teleport ray interactor
            if (teleportRayInteractor != null)
            {
                teleportRayInteractor.lineType = XRRayInteractor.LineType.ProjectileCurve;
                teleportRayInteractor.velocity = arcVelocity;
                teleportRayInteractor.acceleration = Vector3.down * arcGravity;
                teleportRayInteractor.additionalGroundHeight = 0.1f;
            }
        }
        
        private void Update()
        {
            HandleTeleportInput();
            UpdateArcVisualization();
        }
        
        private void HandleTeleportInput()
        {
            // Check for teleport button (typically thumbstick press or grip)
            bool teleportButtonPressed = IsTeleportButtonPressed();
            
            if (teleportButtonPressed && !isAiming)
            {
                StartAiming();
            }
            else if (!teleportButtonPressed && isAiming)
            {
                if (validTeleportTarget)
                {
                    ExecuteTeleport();
                }
                StopAiming();
            }
            else if (teleportButtonPressed && isAiming)
            {
                UpdateAim();
            }
        }
        
        private bool IsTeleportButtonPressed()
        {
            // Check thumbstick press for teleport activation
            // This can be customized based on input mapping
            var inputData = GetComponent<UnityEngine.XR.Interaction.Toolkit.ActionBasedController>();
            if (inputData != null)
            {
                // Check if thumbstick is pressed
                return inputData.selectAction.action.ReadValue<float>() > 0.5f;
            }
            return false;
        }
        
        private void StartAiming()
        {
            isAiming = true;
            arcLineRenderer.enabled = true;
            
            if (teleportRayInteractor != null)
            {
                teleportRayInteractor.enabled = true;
            }
            
            Debug.Log("[VRTeleportation] Teleport aiming started");
        }
        
        private void StopAiming()
        {
            isAiming = false;
            arcLineRenderer.enabled = false;
            
            if (teleportReticle != null)
            {
                teleportReticle.SetActive(false);
            }
            
            if (teleportRayInteractor != null)
            {
                teleportRayInteractor.enabled = false;
            }
            
            Debug.Log("[VRTeleportation] Teleport aiming stopped");
        }
        
        private void UpdateAim()
        {
            if (teleportRayInteractor == null) return;
            
            // Raycast to find teleport target
            RaycastHit hit;
            bool hitValid = teleportRayInteractor.TryGetCurrent3DRaycastHit(out hit);
            
            if (hitValid)
            {
                // Check if hit point is on valid teleport layer
                validTeleportTarget = ((1 << hit.collider.gameObject.layer) & teleportableLayers) != 0;
                
                // Check for blocking objects
                if (validTeleportTarget && blockingLayers != 0)
                {
                    float distanceToHit = Vector3.Distance(transform.position, hit.point);
                    if (Physics.Raycast(transform.position, (hit.point - transform.position).normalized, 
                        distanceToHit, blockingLayers))
                    {
                        validTeleportTarget = false;
                    }
                }
                
                if (validTeleportTarget)
                {
                    targetPosition = hit.point;
                    targetNormal = hit.normal;
                    
                    // Update reticle
                    if (teleportReticle != null)
                    {
                        teleportReticle.SetActive(true);
                        teleportReticle.transform.position = targetPosition;
                        teleportReticle.transform.rotation = Quaternion.FromToRotation(Vector3.up, targetNormal);
                        
                        // Update reticle color
                        var reticleRenderer = teleportReticle.GetComponentInChildren<Renderer>();
                        if (reticleRenderer != null)
                        {
                            reticleRenderer.material.color = Color.green;
                        }
                    }
                }
                else
                {
                    if (teleportReticle != null)
                    {
                        var reticleRenderer = teleportReticle.GetComponentInChildren<Renderer>();
                        if (reticleRenderer != null)
                        {
                            reticleRenderer.material.color = Color.red;
                        }
                    }
                }
            }
            else
            {
                validTeleportTarget = false;
                if (teleportReticle != null)
                {
                    teleportReticle.SetActive(false);
                }
            }
        }
        
        private void UpdateArcVisualization()
        {
            if (!isAiming || arcLineRenderer == null) return;
            
            // Update arc color based on validity
            Gradient currentGradient = validTeleportTarget ? validTeleportColor : invalidTeleportColor;
            arcLineRenderer.colorGradient = currentGradient;
        }
        
        private void ExecuteTeleport()
        {
            if (xrRig == null) return;
            
            Debug.Log($"[VRTeleportation] Teleporting to: {targetPosition}");
            
            // Trigger haptic feedback
            if (enableHaptics && controller != null)
            {
                controller.SendHapticImpulse(teleportHapticIntensity, teleportHapticDuration);
            }
            
            // Calculate teleport position (offset by camera height)
            Vector3 cameraPosition = Camera.main.transform.position;
            Vector3 rigPosition = xrRig.position;
            float heightOffset = cameraPosition.y - rigPosition.y;
            
            Vector3 finalPosition = targetPosition;
            finalPosition.y += heightOffset;
            
            // Execute teleport with or without fade
            if (useFadeTransition)
            {
                StartCoroutine(TeleportWithFade(finalPosition));
            }
            else
            {
                xrRig.position = finalPosition;
            }
        }
        
        private IEnumerator TeleportWithFade(Vector3 destination)
        {
            isFading = true;
            
            // Fade to black
            float elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                fadeAlpha = fadeCurve.Evaluate(elapsedTime / fadeDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            fadeAlpha = 1f;
            
            // Teleport
            xrRig.position = destination;
            
            // Fade from black
            elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                fadeAlpha = 1f - fadeCurve.Evaluate(elapsedTime / fadeDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            fadeAlpha = 0f;
            isFading = false;
        }
        
        private void OnGUI()
        {
            if (isFading && fadeTexture != null)
            {
                GUI.color = new Color(0, 0, 0, fadeAlpha);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), fadeTexture);
            }
        }
        
        private void SetupFadeEffect()
        {
            fadeTexture = new Texture2D(1, 1);
            fadeTexture.SetPixel(0, 0, Color.black);
            fadeTexture.Apply();
        }
        
        private GameObject CreateDefaultReticle()
        {
            GameObject reticle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(reticle.GetComponent<Collider>());
            
            reticle.transform.localScale = new Vector3(0.3f, 0.01f, 0.3f);
            
            var renderer = reticle.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.material.color = Color.green;
            
            reticle.SetActive(false);
            return reticle;
        }
        
        // Public methods
        public void SetTeleportEnabled(bool enabled)
        {
            this.enabled = enabled;
        }
        
        public bool IsAiming => isAiming;
        public bool HasValidTarget => validTeleportTarget;
    }
}
