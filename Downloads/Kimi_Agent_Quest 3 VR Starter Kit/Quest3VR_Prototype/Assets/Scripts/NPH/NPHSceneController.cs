using System;
using System.IO;
using UnityEngine;
using Quest3VR.Prototype;

namespace Quest3VR.NPH
{
    /// <summary>
    /// Main orchestrator for the NPH VR visualization scene.
    /// Coordinates CT viewer, YOLO overlay, 3D mesh, and score panel.
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

        public enum AnalysisState { Idle, Uploading, Processing, Complete, Error }
        public AnalysisState State { get; private set; } = AnalysisState.Idle;

        private void Start()
        {
            CheckBackendConnection();

            if (sampleCTSlices != null && sampleCTSlices.Length > 0)
                LoadSampleData();
        }

        /// <summary>Check if the NPH backend is reachable.</summary>
        public async void CheckBackendConnection()
        {
            scorePanel?.SetAnalysisStatus("Checking backend...");
            var health = await apiClient.CheckHealth();

            if (health != null)
            {
                scorePanel?.SetConnectionStatus(true, health.model_loaded);
                scorePanel?.SetAnalysisStatus("Ready");
                VRHapticsManager.Instance?.Success(VRHapticsManager.ControllerType.Both);
            }
            else
            {
                scorePanel?.SetConnectionStatus(false, false);
                scorePanel?.SetAnalysisStatus("Backend offline — using demo mode");
                VRHapticsManager.Instance?.Error(VRHapticsManager.ControllerType.Both);
            }
        }

        /// <summary>Load sample CT slices for demonstration.</summary>
        public void LoadSampleData()
        {
            if (sampleCTSlices == null || sampleCTSlices.Length == 0) return;

            var textures = new System.Collections.Generic.List<Texture2D>(sampleCTSlices);
            sliceViewer.LoadAllSlices(textures);
            scorePanel?.SetAnalysisStatus($"Loaded {textures.Count} sample slices");

            if (autoAnalyzeOnLoad)
                AnalyzeCurrentSlice();
        }

        /// <summary>Analyze the currently displayed CT slice via 2D YOLO.</summary>
        public async void AnalyzeCurrentSlice()
        {
            byte[] imageData = sliceViewer.GetCurrentSliceBytes();
            if (imageData == null)
            {
                scorePanel?.SetAnalysisStatus("No slice loaded");
                return;
            }

            State = AnalysisState.Uploading;
            scorePanel?.SetAnalysisStatus("Uploading slice for analysis...");

            try
            {
                var response = await apiClient.Analyze(imageData, "slice.png");

                State = AnalysisState.Complete;
                sliceViewer.SetAnalysisResult(response);

                // Render YOLO boxes
                overlayRenderer.RenderBoxes(response);

                // Display metrics
                scorePanel?.DisplayMetrics(response.metrics);

                // Compute and display score
                var scoreReq = new ScoreRequest
                {
                    evansIndex = response.metrics.evans_index,
                    deshScore = response.metrics.desh_score ?? 0,
                    sylvianDilation = response.metrics.sylvian_dilation ?? false,
                    corticalAtrophy = response.metrics.cortical_atrophy ?? "unknown",
                    triad = new System.Collections.Generic.List<bool> { false, false, false },
                };
                if (response.metrics.vsr.HasValue)
                    scoreReq.vsr = response.metrics.vsr.Value;
                if (response.metrics.callosal_angle.HasValue)
                    scoreReq.callosalAngle = response.metrics.callosal_angle.Value;

                var scoreResult = await apiClient.Score(scoreReq);
                scorePanel?.DisplayScore(scoreResult);

                // Generate 3D ventricle mesh from metrics
                ventricleMesh?.GenerateFromMetrics(response.metrics);

                string mode = response.demo_mode ? " (demo)" : "";
                scorePanel?.SetAnalysisStatus($"Analysis complete{mode} — {response.boxes.Count} detections");
            }
            catch (Exception ex)
            {
                State = AnalysisState.Error;
                scorePanel?.SetAnalysisStatus($"Error: {ex.Message}");
                VRHapticsManager.Instance?.Error(VRHapticsManager.ControllerType.Both);
                Debug.LogError($"[NPHSceneController] Analysis failed: {ex}");
            }
        }

        /// <summary>Analyze a 3D volume (NIfTI/DICOM) via TotalSegmentator.</summary>
        public async void Analyze3DVolume(byte[] fileData, string filename)
        {
            State = AnalysisState.Uploading;
            scorePanel?.SetAnalysisStatus("Uploading 3D volume...");

            try
            {
                State = AnalysisState.Processing;
                scorePanel?.SetAnalysisStatus("Running TotalSegmentator (this may take minutes)...");

                var response = await apiClient.AnalyzeCT3D(fileData, filename);

                State = AnalysisState.Complete;
                scorePanel?.DisplayMetrics(response.metrics);

                // Compute score from 3D metrics
                var scoreReq = new ScoreRequest
                {
                    evansIndex = response.metrics.evans_index,
                    corticalAtrophy = response.metrics.cortical_atrophy ?? "unknown",
                    triad = new System.Collections.Generic.List<bool> { false, false, false },
                };
                if (response.metrics.vsr.HasValue)
                    scoreReq.vsr = response.metrics.vsr.Value;

                var scoreResult = await apiClient.Score(scoreReq);
                scorePanel?.DisplayScore(scoreResult);

                // Generate 3D mesh from volumetric data
                ventricleMesh?.GenerateFromMetrics(response.metrics);

                scorePanel?.SetAnalysisStatus("3D analysis complete — real volumetric measurements");
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
            if (!File.Exists(path))
            {
                scorePanel?.SetAnalysisStatus($"File not found: {path}");
                return;
            }

            byte[] data = File.ReadAllBytes(path);
            string ext = Path.GetExtension(path).ToLower();

            if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
            {
                var tex = new Texture2D(2, 2);
                tex.LoadImage(data);
                sliceViewer.LoadSlice(tex);
                if (autoAnalyzeOnLoad) AnalyzeCurrentSlice();
            }
            else if (ext == ".nii" || path.EndsWith(".nii.gz") || ext == ".dcm")
            {
                Analyze3DVolume(data, Path.GetFileName(path));
            }
        }
    }
}
