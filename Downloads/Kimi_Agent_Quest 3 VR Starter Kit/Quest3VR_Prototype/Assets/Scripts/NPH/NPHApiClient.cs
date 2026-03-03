using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Quest3VR.NPH
{
    /// <summary>
    /// HTTP client for NPH backend API communication.
    /// Provides methods for health checks, image analysis, scoring, and mesh generation.
    /// </summary>
    public class NPHApiClient : MonoBehaviour
    {
        [SerializeField] private string serverUrl = "http://192.168.1.100:8000";
        [SerializeField] private float healthCheckTimeout = 5f;

        public static NPHApiClient Instance { get; private set; }

        public string ServerUrl
        {
            get => serverUrl;
            set => serverUrl = value.TrimEnd('/');
        }

        public bool IsConnected { get; private set; }
        public string LastError { get; private set; }

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
            }
        }

        /// <summary>
        /// Check if the backend server is reachable and healthy.
        /// </summary>
        public async Task<HealthResponse> CheckHealth()
        {
            try
            {
                string json = await GetRequest($"{serverUrl}/health", (int)healthCheckTimeout);
                IsConnected = true;
                LastError = null;
                return JsonUtility.FromJson<HealthResponse>(json);
            }
            catch (Exception ex)
            {
                IsConnected = false;
                LastError = ex.Message;
                Debug.LogWarning($"[NPHApiClient] Health check failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Analyze a CT slice image using YOLO detection.
        /// </summary>
        public async Task<AnalyzeResponse> Analyze(byte[] imageData, string filename)
        {
            try
            {
                string json = await PostFileRequest($"{serverUrl}/analyze", imageData, filename);
                IsConnected = true;
                LastError = null;
                return JsonUtility.FromJson<AnalyzeResponse>(json);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                throw;
            }
        }

        /// <summary>
        /// Calculate NPH probability score from metrics.
        /// </summary>
        public async Task<ScoreResponse> Score(ScoreRequest request)
        {
            try
            {
                string body = JsonUtility.ToJson(request);
                string json = await PostJsonRequest($"{serverUrl}/score", body);
                IsConnected = true;
                LastError = null;
                return JsonUtility.FromJson<ScoreResponse>(json);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                throw;
            }
        }

        /// <summary>
        /// Analyze a 3D CT volume using TotalSegmentator.
        /// </summary>
        public async Task<AnalyzeCT3DResponse> AnalyzeCT3D(byte[] fileData, string filename)
        {
            try
            {
                string json = await PostFileRequest($"{serverUrl}/analyze-ct3d", fileData, filename);
                IsConnected = true;
                LastError = null;
                return JsonUtility.FromJson<AnalyzeCT3DResponse>(json);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                throw;
            }
        }

        /// <summary>
        /// Generate a ventricle mesh from volumetric data using marching cubes.
        /// Returns the mesh data as JSON that can be deserialized into a MeshVentricleResponse.
        /// </summary>
        public async Task<MeshVentricleResponse> GenerateVentricleMesh(byte[] ctData, string filename, float isoValue = 0.5f)
        {
            try
            {
                // Create form with additional parameters
                var form = new WWWForm();
                string mime = GetMimeType(filename);
                form.AddBinaryData("file", ctData, filename, mime);
                form.AddField("iso_value", isoValue.ToString());

                using var req = UnityWebRequest.Post($"{serverUrl}/mesh-ventricle", form);
                req.timeout = 300;
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                if (req.result != UnityWebRequest.Result.Success)
                    throw new Exception($"POST /mesh-ventricle failed: {req.error}");

                IsConnected = true;
                LastError = null;
                return JsonUtility.FromJson<MeshVentricleResponse>(req.downloadHandler.text);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                throw;
            }
        }

        private string GetMimeType(string filename)
        {
            if (filename.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return "image/png";
            if (filename.EndsWith(".nii.gz", StringComparison.OrdinalIgnoreCase)) return "application/gzip";
            if (filename.EndsWith(".nii", StringComparison.OrdinalIgnoreCase)) return "application/octet-stream";
            if (filename.EndsWith(".dcm", StringComparison.OrdinalIgnoreCase)) return "application/dicom";
            return "image/jpeg";
        }

        private async Task<string> GetRequest(string url, int timeoutSeconds = 10)
        {
            using var req = UnityWebRequest.Get(url);
            req.timeout = timeoutSeconds;
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result == UnityWebRequest.Result.ConnectionError)
                throw new Exception($"Connection failed. Check network and server URL.");
            if (req.result == UnityWebRequest.Result.ProtocolError)
                throw new Exception($"HTTP error {req.responseCode}: {req.error}");
            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception($"Request failed: {req.error}");

            return req.downloadHandler.text;
        }

        private async Task<string> PostJsonRequest(string url, string jsonBody, int timeoutSeconds = 30)
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = timeoutSeconds;

            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result == UnityWebRequest.Result.ConnectionError)
                throw new Exception($"Connection failed. Check network and server URL.");
            if (req.result == UnityWebRequest.Result.ProtocolError)
                throw new Exception($"HTTP error {req.responseCode}: {req.error}");
            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception($"Request failed: {req.error}");

            return req.downloadHandler.text;
        }

        private async Task<string> PostFileRequest(string url, byte[] fileData, string filename, int timeoutSeconds = 300)
        {
            var form = new WWWForm();
            string mime = GetMimeType(filename);
            form.AddBinaryData("file", fileData, filename, mime);

            using var req = UnityWebRequest.Post(url, form);
            req.timeout = timeoutSeconds;
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result == UnityWebRequest.Result.ConnectionError)
                throw new Exception($"Connection failed. Check network and server URL.");
            if (req.result == UnityWebRequest.Result.ProtocolError)
                throw new Exception($"HTTP error {req.responseCode}: {req.error}");
            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception($"Request failed: {req.error}");

            return req.downloadHandler.text;
        }
    }
}
