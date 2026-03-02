using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Quest3VR.NPH
{
    public class NPHApiClient : MonoBehaviour
    {
        [SerializeField] private string serverUrl = "http://192.168.1.100:8000";

        public static NPHApiClient Instance { get; private set; }

        public string ServerUrl
        {
            get => serverUrl;
            set => serverUrl = value.TrimEnd('/');
        }

        public bool IsConnected { get; private set; }

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

        public async Task<HealthResponse> CheckHealth()
        {
            try
            {
                string json = await GetRequest($"{serverUrl}/health");
                IsConnected = true;
                return JsonUtility.FromJson<HealthResponse>(json);
            }
            catch
            {
                IsConnected = false;
                return null;
            }
        }

        public async Task<AnalyzeResponse> Analyze(byte[] imageData, string filename)
        {
            string json = await PostFileRequest($"{serverUrl}/analyze", imageData, filename);
            return JsonUtility.FromJson<AnalyzeResponse>(json);
        }

        public async Task<ScoreResponse> Score(ScoreRequest request)
        {
            string body = JsonUtility.ToJson(request);
            string json = await PostJsonRequest($"{serverUrl}/score", body);
            return JsonUtility.FromJson<ScoreResponse>(json);
        }

        public async Task<AnalyzeCT3DResponse> AnalyzeCT3D(byte[] fileData, string filename)
        {
            string json = await PostFileRequest($"{serverUrl}/analyze-ct3d", fileData, filename);
            return JsonUtility.FromJson<AnalyzeCT3DResponse>(json);
        }

        private async Task<string> GetRequest(string url)
        {
            using var req = UnityWebRequest.Get(url);
            req.timeout = 10;
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception($"GET {url} failed: {req.error}");

            return req.downloadHandler.text;
        }

        private async Task<string> PostJsonRequest(string url, string jsonBody)
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 30;

            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception($"POST {url} failed: {req.error}");

            return req.downloadHandler.text;
        }

        private async Task<string> PostFileRequest(string url, byte[] fileData, string filename)
        {
            var form = new WWWForm();
            string mime = filename.EndsWith(".png") ? "image/png" :
                          filename.EndsWith(".nii.gz") ? "application/gzip" :
                          filename.EndsWith(".nii") ? "application/octet-stream" :
                          filename.EndsWith(".dcm") ? "application/dicom" :
                          "image/jpeg";
            form.AddBinaryData("file", fileData, filename, mime);

            using var req = UnityWebRequest.Post(url, form);
            req.timeout = 300;
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception($"POST {url} failed: {req.error}");

            return req.downloadHandler.text;
        }
    }
}
