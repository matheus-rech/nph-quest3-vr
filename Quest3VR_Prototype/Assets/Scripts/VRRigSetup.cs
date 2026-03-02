using UnityEngine;
using Unity.XR.Oculus;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace Quest3VR.Prototype
{
    /// <summary>
    /// VR Rig Setup for Meta Quest 3
    /// Configures XR settings and initializes the VR environment
    /// </summary>
    public class VRRigSetup : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private Camera xrCamera;
        [SerializeField] private float renderScale = 1.0f;
        
        [Header("Tracking Space")]
        [SerializeField] private TrackingSpaceType trackingSpace = TrackingSpaceType.RoomScale;
        
        [Header("Hand Controllers")]
        [SerializeField] private GameObject leftController;
        [SerializeField] private GameObject rightController;
        
        [Header("Quest 3 Specific")]
        [SerializeField] private bool useHandTracking = false;
        [SerializeField] private bool usePassthrough = false;
        
        public static VRRigSetup Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            InitializeXR();
        }
        
        private void Start()
        {
            ConfigureQuest3Settings();
            SetupControllers();
        }
        
        private void InitializeXR()
        {
            // Set render scale for optimal Quest 3 performance
            if (XRSettings.enabled)
            {
                XRSettings.eyeTextureResolutionScale = renderScale;
            }
            
            // Configure tracking space
            XRDevice.SetTrackingSpaceType(trackingSpace);
            
            Debug.Log($"[VRRigSetup] XR Initialized - Tracking Space: {trackingSpace}");
        }
        
        private void ConfigureQuest3Settings()
        {
            // Quest 3 specific optimizations
            Application.targetFrameRate = 72; // Quest 3 default refresh rate
            QualitySettings.vSyncCount = 0;
            
            // Set Oculus-specific settings
            OculusSettings oculusSettings = new OculusSettings();
            oculusSettings.m_StereoRenderingModeDesktop = OculusSettings.StereoRenderingMode.MultiPass;
            oculusSettings.m_StereoRenderingModeAndroid = OculusSettings.StereoRenderingMode.Multiview;
            
            Debug.Log("[VRRigSetup] Quest 3 settings configured");
        }
        
        private void SetupControllers()
        {
            if (leftController == null || rightController == null)
            {
                Debug.LogWarning("[VRRigSetup] Controller references not set. Please assign in inspector.");
                return;
            }
            
            // Ensure controllers have required components
            EnsureControllerComponents(leftController, XRNode.LeftHand);
            EnsureControllerComponents(rightController, XRNode.RightHand);
        }
        
        private void EnsureControllerComponents(GameObject controller, XRNode node)
        {
            // Add XR Controller component if missing
            var xrController = controller.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRBaseController>();
            if (xrController == null)
            {
                controller.AddComponent<UnityEngine.XR.Interaction.Toolkit.ActionBasedController>();
            }
            
            Debug.Log($"[VRRigSetup] Configured {node} controller");
        }
        
        public void Recenter()
        {
            InputTracking.Recenter();
            Debug.Log("[VRRigSetup] VR view recentered");
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                // Re-initialize XR when app regains focus
                InitializeXR();
            }
        }
    }
}
