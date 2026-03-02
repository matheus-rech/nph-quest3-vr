using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Quest3VR.Prototype
{
    /// <summary>
    /// VR Dashboard Controller for Quest 3
    /// Manages a floating UI dashboard with interactive panels
    /// </summary>
    public class VRDashboardController : MonoBehaviour
    {
        [Header("Dashboard Transform")]
        [SerializeField] private Transform dashboardPanel;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float dashboardDistance = 1.5f;
        [SerializeField] private float dashboardHeight = 0.2f;
        [SerializeField] private float followSpeed = 5f;
        
        [Header("Dashboard Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject infoPanel;
        [SerializeField] private GameObject controlsPanel;
        
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI fpsText;
        
        [Header("Interactive Buttons")]
        [SerializeField] private List<VRDashboardButton> dashboardButtons = new List<VRDashboardButton>();
        
        [Header("Animation")]
        [SerializeField] private bool animateOpenClose = true;
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Input")]
        [SerializeField] private bool toggleWithMenuButton = true;
        [SerializeField] private KeyCode keyboardToggleKey = KeyCode.M;
        
        private bool isDashboardVisible = false;
        private bool isAnimating = false;
        private Vector3 targetScale;
        private GameObject currentPanel;
        private float fpsUpdateInterval = 0.5f;
        private float fpsAccumulator = 0f;
        private int fpsFrameCount = 0;
        
        public static VRDashboardController Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            // Initialize dashboard state
            targetScale = Vector3.zero;
            if (dashboardPanel != null)
            {
                dashboardPanel.localScale = targetScale;
            }
            
            // Setup default panel
            currentPanel = mainMenuPanel;
        }
        
        private void Start()
        {
            // Auto-find camera if not assigned
            if (cameraTransform == null)
            {
                cameraTransform = Camera.main?.transform;
            }
            
            // Initialize buttons
            InitializeButtons();
            
            // Show main menu by default when opened
            ShowPanel(mainMenuPanel);
        }
        
        private void Update()
        {
            HandleInput();
            UpdateDashboardPosition();
            UpdateFPSDisplay();
        }
        
        private void HandleInput()
        {
            // Keyboard toggle (for testing in editor)
            if (Input.GetKeyDown(keyboardToggleKey))
            {
                ToggleDashboard();
            }
            
            // VR controller menu button
            if (toggleWithMenuButton)
            {
                // Check for menu button press on either controller
                if (IsMenuButtonPressed())
                {
                    ToggleDashboard();
                }
            }
        }
        
        private bool IsMenuButtonPressed()
        {
            // Check Unity Input System for menu button
            // This is a simplified check - in production, use proper input actions
            return Input.GetButtonDown("MenuButton");
        }
        
        private void UpdateDashboardPosition()
        {
            if (!isDashboardVisible || cameraTransform == null || dashboardPanel == null) return;
            
            // Calculate target position in front of camera
            Vector3 targetPosition = cameraTransform.position + 
                cameraTransform.forward * dashboardDistance;
            targetPosition.y = cameraTransform.position.y + dashboardHeight;
            
            // Smoothly follow camera
            dashboardPanel.position = Vector3.Lerp(dashboardPanel.position, targetPosition, 
                Time.deltaTime * followSpeed);
            
            // Face the camera
            Vector3 lookDirection = dashboardPanel.position - cameraTransform.position;
            lookDirection.y = 0; // Keep dashboard level
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                dashboardPanel.rotation = Quaternion.Slerp(dashboardPanel.rotation, targetRotation, 
                    Time.deltaTime * followSpeed);
            }
        }
        
        private void UpdateFPSDisplay()
        {
            if (fpsText == null) return;
            
            fpsAccumulator += Time.timeScale / Time.deltaTime;
            fpsFrameCount++;
            
            if (Time.realtimeSinceStartup > fpsUpdateInterval)
            {
                float fps = fpsAccumulator / fpsFrameCount;
                fpsText.text = $"FPS: {fps:F0}";
                
                // Color code FPS
                if (fps >= 60)
                    fpsText.color = Color.green;
                else if (fps >= 30)
                    fpsText.color = Color.yellow;
                else
                    fpsText.color = Color.red;
                
                fpsAccumulator = 0f;
                fpsFrameCount = 0;
            }
        }
        
        private void InitializeButtons()
        {
            foreach (var button in dashboardButtons)
            {
                if (button.button != null)
                {
                    button.button.onClick.AddListener(() => OnButtonClicked(button));
                }
            }
        }
        
        private void OnButtonClicked(VRDashboardButton button)
        {
            Debug.Log($"[VRDashboardController] Button clicked: {button.buttonName}");
            
            // Trigger haptic feedback
            VRHapticsManager.Instance?.Tap(VRHapticsManager.ControllerType.Both);
            
            // Execute button action
            switch (button.actionType)
            {
                case ButtonActionType.ShowPanel:
                    if (button.targetPanel != null)
                    {
                        ShowPanel(button.targetPanel);
                    }
                    break;
                    
                case ButtonActionType.RecenterVR:
                    VRRigSetup.Instance?.Recenter();
                    UpdateStatus("VR Recentered");
                    break;
                    
                case ButtonActionType.ToggleSetting:
                    ToggleSetting(button.settingName);
                    break;
                    
                case ButtonActionType.CustomAction:
                    button.customAction?.Invoke();
                    break;
                    
                case ButtonActionType.CloseDashboard:
                    HideDashboard();
                    break;
            }
        }
        
        #region Public Methods
        
        public void ShowDashboard()
        {
            if (isDashboardVisible) return;
            
            isDashboardVisible = true;
            
            if (animateOpenClose)
            {
                StartCoroutine(AnimateDashboard(true));
            }
            else
            {
                dashboardPanel.localScale = Vector3.one;
            }
            
            // Trigger haptic feedback
            VRHapticsManager.Instance?.Success(VRHapticsManager.ControllerType.Both);
            
            Debug.Log("[VRDashboardController] Dashboard shown");
        }
        
        public void HideDashboard()
        {
            if (!isDashboardVisible) return;
            
            isDashboardVisible = false;
            
            if (animateOpenClose)
            {
                StartCoroutine(AnimateDashboard(false));
            }
            else
            {
                dashboardPanel.localScale = Vector3.zero;
            }
            
            Debug.Log("[VRDashboardController] Dashboard hidden");
        }
        
        public void ToggleDashboard()
        {
            if (isDashboardVisible)
            {
                HideDashboard();
            }
            else
            {
                ShowDashboard();
            }
        }
        
        public void ShowPanel(GameObject panel)
        {
            // Hide all panels
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (infoPanel != null) infoPanel.SetActive(false);
            if (controlsPanel != null) controlsPanel.SetActive(false);
            
            // Show target panel
            if (panel != null)
            {
                panel.SetActive(true);
                currentPanel = panel;
                
                // Update title
                if (titleText != null)
                {
                    titleText.text = panel.name.Replace("Panel", "").Replace("(Clone)", "");
                }
            }
        }
        
        public void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }
        
        public void SetTitle(string title)
        {
            if (titleText != null)
            {
                titleText.text = title;
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private System.Collections.IEnumerator AnimateDashboard(bool opening)
        {
            isAnimating = true;
            float elapsedTime = 0f;
            
            Vector3 startScale = opening ? Vector3.zero : Vector3.one;
            Vector3 endScale = opening ? Vector3.one : Vector3.zero;
            
            while (elapsedTime < animationDuration)
            {
                float t = animationCurve.Evaluate(elapsedTime / animationDuration);
                dashboardPanel.localScale = Vector3.Lerp(startScale, endScale, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            dashboardPanel.localScale = endScale;
            isAnimating = false;
        }
        
        private void ToggleSetting(string settingName)
        {
            // Example setting toggles
            switch (settingName.ToLower())
            {
                case "haptics":
                    bool hapticsEnabled = PlayerPrefs.GetInt("HapticsEnabled", 1) == 1;
                    PlayerPrefs.SetInt("HapticsEnabled", hapticsEnabled ? 0 : 1);
                    UpdateStatus($"Haptics: {(hapticsEnabled ? "OFF" : "ON")}");
                    break;
                    
                case "teleport":
                    var teleport = FindObjectOfType<VRTeleportation>();
                    if (teleport != null)
                    {
                        teleport.SetTeleportEnabled(!teleport.enabled);
                        UpdateStatus($"Teleport: {(teleport.enabled ? "ON" : "OFF")}");
                    }
                    break;
                    
                case "passthrough":
                    bool passthrough = PlayerPrefs.GetInt("PassthroughEnabled", 0) == 1;
                    PlayerPrefs.SetInt("PassthroughEnabled", passthrough ? 0 : 1);
                    UpdateStatus($"Passthrough: {(passthrough ? "OFF" : "ON")}");
                    break;
                    
                default:
                    UpdateStatus($"Unknown setting: {settingName}");
                    break;
            }
        }
        
        #endregion
    }
    
    #region Supporting Classes
    
    [System.Serializable]
    public class VRDashboardButton
    {
        public string buttonName;
        public Button button;
        public ButtonActionType actionType;
        public GameObject targetPanel;
        public string settingName;
        public UnityEngine.Events.UnityEvent customAction;
    }
    
    public enum ButtonActionType
    {
        ShowPanel,
        RecenterVR,
        ToggleSetting,
        CustomAction,
        CloseDashboard
    }
    
    #endregion
}
