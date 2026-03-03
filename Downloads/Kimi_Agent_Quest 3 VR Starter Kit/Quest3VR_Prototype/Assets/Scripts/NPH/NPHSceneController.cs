using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Quest3VR.Prototype;

namespace Quest3VR.NPH
{
    /// <summary>
    /// Main orchestrator for the NPH VR visualization scene.
    /// Coordinates CT viewer, YOLO overlay, 3D mesh, and score panel.
    /// Supports both backend-connected and demo (offline) modes.
    /// </summary>
    public class NPHSceneController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NPHApiClient apiClient;
        [SerializeField] private CTSliceViewer sliceViewer;
        [SerializeField] private YOLOOverlayRenderer overlayRenderer;
        [SerializeField] private VentricleMeshGenerator ventricleMesh;
        [SerializeField] private NPHScorePanel scorePanel;

        [Header("Sample Data")]
        [SerializeField] private Texture2D[] sampleCTSlices;

        [Header("Settings")]
        [SerializeField] private bool autoAnalyzeOnLoad = true;
        [SerializeField] private bool useDemoModeFallback = true;
        [SerializeField, Range(0.2f, 0.5f)] private float demoEvansIndex = 0.35f;

        public enum AnalysisState { Idle, Uploading, Processing, Complete, Error }
        public AnalysisState State { get; private set; } = AnalysisState.Idle;
        public bool IsDemoMode { get; private set; } = false;

        private void Start()
        {
            InitializeSystem();
        }

        /// <summary>
        /// Initialize the NPH system by checking backend and loading sample data.
        /// </summary>
        private async void InitializeSystem()
        {
            await CheckBackendConnectionAsync();

            if (sampleCTSlices != null && sampleCTSlices.Length > 0)
                LoadSampleData();
        }

        /// <summary>
        /// Check if the NPH backend is reachable asynchronously.
        /// </summary>
        public async Task CheckBackendConnectionAsync()
        {
            scorePanel?.SetAnalysisStatus("Checking backend...");
            
            try
            {
                var health = await apiClient.CheckHealth();

                if (health != null)
                {
                    IsDemoMode = false;
                    scorePanel?.SetConnectionStatus(true, health.model_loaded);
                    scorePanel?.SetAnalysisStatus("Ready");
                    VRHapticsManager.Instance?.Success(VRHapticsManager.ControllerType.Both);
                    Debug.Log("[NPHSceneController] Backend connected.");
                }
                else
                {
                    EnterDemoMode("Backend unreachable");
                }
            }
            catch (Exception ex)
            {
                EnterDemoMode($"Connection error: {ex.Message}");
            }
        }

        /// <summary>
        /// Switch to demo mode when backend is unavailable.
        /// </summary>
        private void EnterDemoMode(string reason)
        {
            IsDemoMode = true;
            scorePanel?.SetConnectionStatus(false, false);
            scorePanel?.SetAnalysisStatus($"Demo mode — {reason}");
            VRHapticsManager.Instance?.Warning(VRHapticsManager.ControllerType.Both);
            Debug.Log($"[NPHSceneController] Demo mode activated: {reason}");
        }

        /// <summary>
        /// Manually trigger demo mode with specific Evans Index for testing.
        /// </summary>
        [ContextMenu("Force Demo Mode")]
        public void ForceDemoMode()
        {
            EnterDemoMode("Manually triggered");
        }

        /// <summary>Load sample CT slices for demonstration.</summary>
        public void LoadSampleData()
        {
            if (sampleCTSlices == null || sampleCTSlices.Length == 0) 
            {
                Debug.LogWarning("[NPHSceneController] No sample CT slices assigned.");
                return;
            }

            var textures = new System.Collections.Generic.List<Texture2D>(sampleCTSlices);
            sliceViewer.LoadAllSlices(textures);
            scorePanel?.SetAnalysisStatus($"Loaded {textures.Count} sample slices");

            if (autoAnalyzeOnLoad)
                AnalyzeCurrentSlice();
        }

        /// <summary>Analyze the currently displayed CT slice via 2D YOLO.</summary>
        public async void AnalyzeCurrentSlice()
        {
            if (sliceViewer.SliceCount == 0)
            {
                scorePanel?.SetAnalysisStatus("No slices loaded");
                return;
            }

            State = AnalysisState.Uploading;
            scorePanel?.SetAnalysisStatus("Analyzing...");

            try
            {
                AnalyzeResponse response;
                ScoreResponse scoreResult;

                if (IsDemoMode || !useDemoModeFallback)
                {
                    // Use synthetic data in demo mode
                    response = GenerateDemoAnalysis();
                    scoreResult = DemoModeData.CalculateDemoScore(response.metrics);
                }
                else
                {
                    // Try to analyze with backend
                    response = await AnalyzeWithBackend();
                    if (response == null) return; // Error already handled

                    scoreResult = await GetScoreFromBackend(response.metrics);
                }

                // Display results
                DisplayAnalysisResults(response, scoreResult);
                State = AnalysisState.Complete;
            }
            catch (Exception ex)
            {
                HandleAnalysisError(ex);
            }
        }

        /// <summary>
        /// Analyze with backend API.
        /// </summary>
        private async Task<AnalyzeResponse> AnalyzeWithBackend()
        {
            byte[] imageData = await GetSliceImageDataAsync();
            if (imageData == null)
            {
                scorePanel?.SetAnalysisStatus("Failed to read slice data");
                State = AnalysisState.Error;
                return null;
            }

            try
            {
                return await apiClient.Analyze(imageData, "slice.png");
            }
            catch (Exception ex)
            {
                // If backend fails and fallback is enabled, switch to demo mode
                if (useDemoModeFallback)
                {
                    Debug.LogWarning($"[NPHSceneController] Backend analysis failed, switching to demo mode: {ex.Message}");
                    EnterDemoMode("Analysis failed");
                    return GenerateDemoAnalysis();
                }
                throw;
            }
        }

        /// <summary>
        /// Get score from backend API.
        /// </summary>
        private async Task<ScoreResponse> GetScoreFromBackend(NPHMetrics metrics)
        {
            var scoreReq = new ScoreRequest
            {
                evansIndex = metrics.evans_index,
                deshScore = metrics.desh_score ?? 0,
                sylvianDilation = metrics.sylvian_dilation ?? false,
                corticalAtrophy = metrics.cortical_atrophy ?? "unknown",
                triad = new System.Collections.Generic.List<bool> { false, false, false },
            };
            
            if (metrics.vsr.HasValue)
                scoreReq.vsr = metrics.vsr.Value;
            if (metrics.callosal_angle.HasValue)
                scoreReq.callosalAngle = metrics.callosal_angle.Value;

            return await apiClient.Score(scoreReq);
        }

        /// <summary>
        /// Generate demo analysis when backend is unavailable.
        /// </summary>
        private AnalyzeResponse GenerateDemoAnalysis()
        {
            return DemoModeData.GenerateSyntheticAnalyzeResponse(512, 512);
        }

        /// <summary>
        /// Get image data from current slice with validation.
        /// </summary>
        private async Task<byte[]> GetSliceImageDataAsync()
        {
            // Run on main thread since texture access must be on main thread
            byte[] imageData = null;
            bool isReadable = false;
            
            await Task.Run(() =>
            {
                // Check if we can read the texture
                var currentTexture = sliceViewer.GetCurrentSliceTexture();
                if (currentTexture != null)
                {
                    try
                    {
                        // Try to access a pixel to check if readable
                        var testPixel = currentTexture.GetPixel(0, 0);
                        isReadable = true;
                    }
                    catch (UnityException)
                    {
                        isReadable = false;
                    }
                }
            });

            // Use the viewer's method which handles encoding
            imageData = sliceViewer.GetCurrentSliceBytes();
            
            // If texture is not readable, we may need to handle this differently
            if (imageData == null || imageData.Length == 0)
            {
                Debug.LogWarning("[NPHSceneController] Could not get slice image data. Texture may not be readable.");
            }
            
            return imageData;
        }

        /// <summary>
        /// Display analysis results on UI and 3D visualizations.
        /// </summary>
        private void DisplayAnalysisResults(AnalyzeResponse response, ScoreResponse scoreResult)
        {
            sliceViewer.SetAnalysisResult(response);

            // Render YOLO boxes
            overlayRenderer?.RenderBoxes(response);

            // Display metrics
            scorePanel?.DisplayMetrics(response.metrics);
            scorePanel?.DisplayScore(scoreResult);

            // Generate 3D ventricle mesh from metrics
            ventricleMesh?.GenerateFromMetrics(response.metrics);

            string mode = response.demo_mode ? " (demo mode)" : "";
            int boxCount = response.boxes?.Count ?? 0;
            scorePanel?.SetAnalysisStatus($"Analysis complete{mode} — {boxCount} detections");

            VRHapticsManager.Instance?.Success(VRHapticsManager.ControllerType.Both);
        }

        /// <summary>
        /// Handle analysis errors with appropriate user feedback.
        /// </summary>
        private void HandleAnalysisError(Exception ex)
        {
            State = AnalysisState.Error;
            scorePanel?.SetAnalysisStatus($"Error: {ex.Message}");
            VRHapticsManager.Instance?.Error(VRHapticsManager.ControllerType.Both);
            Debug.LogError($"[NPHSceneController] Analysis failed: {ex}");
        }

        /// <summary>Analyze a 3D volume (NIfTI/DICOM) via TotalSegmentator.</summary>
        public async void Analyze3DVolume(byte[] fileData, string filename)
        {
            State = AnalysisState.Uploading;
            scorePanel?.SetAnalysisStatus("Uploading 3D volume...");

            try
            {
                AnalyzeCT3DResponse response;
                ScoreResponse scoreResult;

                if (IsDemoMode)
                {
                    // Simulate 3D processing delay in demo mode
                    await Task.Delay(2000);
                    var metrics = DemoModeData.GenerateSyntheticMetrics(demoEvansIndex);
                    response = new AnalyzeCT3DResponse
                    {
                        metrics = metrics,
                        source = "demo",
                        demo_mode = true
                    };
                    scoreResult = DemoModeData.CalculateDemoScore(metrics);
                }
                else
                {
                    State = AnalysisState.Processing;
                    scorePanel?.SetAnalysisStatus("Running TotalSegmentator (this may take minutes)...");

                    response = await apiClient.AnalyzeCT3D(fileData, filename);

                    // Compute score from 3D metrics
                    var scoreReq = new ScoreRequest
                    {
                        evansIndex = response.metrics.evans_index,
                        corticalAtrophy = response.metrics.cortical_atrophy ?? "unknown",
                        triad = new System.Collections.Generic.List<bool> { false, false, false },
                    };
                    if (response.metrics.vsr.HasValue)
                        scoreReq.vsr = response.metrics.vsr.Value;

                    scoreResult = await apiClient.Score(scoreReq);
                }

                State = AnalysisState.Complete;
                scorePanel?.DisplayMetrics(response.metrics);
                scorePanel?.DisplayScore(scoreResult);

                // Generate 3D mesh from volumetric data
                ventricleMesh?.GenerateFromMetrics(response.metrics);

                string mode = response.demo_mode ? " (demo)" : "";
                scorePanel?.SetAnalysisStatus($"3D analysis complete{mode}");
                VRHapticsManager.Instance?.Success(VRHapticsManager.ControllerType.Both);
            }
            catch (Exception ex)
            {
                State = AnalysisState.Error;
                scorePanel?.SetAnalysisStatus($"3D Error: {ex.Message}");
                VRHapticsManager.Instance?.Error(VRHapticsManager.ControllerType.Both);
                Debug.LogError($"[NPHSceneController] 3D analysis failed: {ex}");
            }
        }

        /// <summary>Load a file from the device's storage.</summary>
        public void LoadFileFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                scorePanel?.SetAnalysisStatus("Invalid file path");
                return;
            }

            if (!File.Exists(path))
            {
                scorePanel?.SetAnalysisStatus($"File not found: {path}");
                return;
            }

            try
            {
                byte[] data = File.ReadAllBytes(path);
                string ext = Path.GetExtension(path).ToLower();

                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                {
                    var tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                    if (tex.LoadImage(data))
                    {
                        tex.name = Path.GetFileNameWithoutExtension(path);
                        sliceViewer.LoadSlice(tex);
                        scorePanel?.SetAnalysisStatus($"Loaded: {tex.name}");
                        if (autoAnalyzeOnLoad) AnalyzeCurrentSlice();
                    }
                    else
                    {
                        scorePanel?.SetAnalysisStatus("Failed to load image");
                        Destroy(tex);
                    }
                }
                else if (ext == ".nii" || path.EndsWith(".nii.gz", StringComparison.OrdinalIgnoreCase) || ext == ".dcm")
                {
                    Analyze3DVolume(data, Path.GetFileName(path));
                }
                else
                {
                    scorePanel?.SetAnalysisStatus($"Unsupported file type: {ext}");
                }
            }
            catch (Exception ex)
            {
                scorePanel?.SetAnalysisStatus($"File error: {ex.Message}");
                Debug.LogError($"[NPHSceneController] Failed to load file {path}: {ex}");
            }
        }

        /// <summary>
        /// Re-analyze the current slice (useful for retry or after backend reconnection).
        /// </summary>
        [ContextMenu("Re-analyze Current Slice")]
        public void ReanalyzeCurrentSlice()
        {
            if (State == AnalysisState.Uploading || State == AnalysisState.Processing)
            {
                Debug.LogWarning("[NPHSceneController] Analysis already in progress.");
                return;
            }
            AnalyzeCurrentSlice();
        }

        /// <summary>
        /// Switch API URL at runtime and re-check connection.
        /// </summary>
        public async void SetApiUrl(string newUrl)
        {
            if (apiClient != null)
            {
                apiClient.ServerUrl = newUrl;
                await CheckBackendConnectionAsync();
            }
        }
    }
}
