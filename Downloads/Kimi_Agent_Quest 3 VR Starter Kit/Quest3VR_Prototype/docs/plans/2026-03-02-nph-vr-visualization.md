# NPH VR Visualization — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a Unity-native Quest 3 VR app that visualizes NPH (Normal Pressure Hydrocephalus) detection results — CT slices with YOLO bounding box overlays, 3D ventricle meshes from TotalSegmentator, and an interactive NPH score dashboard — all powered by the existing FastAPI backend.

**Architecture:** The Quest 3 VR app (Unity 2022.3 LTS) communicates over HTTP with the NPH FastAPI backend (`api.py` on port 8000). CT images are uploaded from the headset or preloaded. The backend returns YOLO detection boxes (2D) and volumetric metrics (3D). Unity renders CT slices as world-space textured quads, overlays bounding boxes via LineRenderer, generates 3D ventricle meshes from marching cubes, and displays the NPH risk score on the existing VR Dashboard.

**Tech Stack:** Unity 2022.3 LTS, XR Interaction Toolkit 2.5.2, Oculus XR Plugin 4.1.2, C# UnityWebRequest, FastAPI (Python), YOLOv8, TotalSegmentator

---

## Existing Codebase Reference

### Unity Scripts (namespace: `Quest3VR.Prototype`)
| File | Key APIs to integrate with |
|------|---------------------------|
| `Assets/Scripts/VRRigSetup.cs` | Singleton `Instance`, `Recenter()`, camera refs |
| `Assets/Scripts/VRDashboardController.cs` | `UpdateStatus(string)`, `SetTitle(string)`, `ShowPanel(GameObject)`, panel system |
| `Assets/Scripts/VRGrabInteraction.cs` | `XRDirectInteractor` grab events, velocity-based throw |
| `Assets/Scripts/GrabbableObject.cs` | `XRGrabInteractable`, hover highlight, `onGrab`/`onRelease` events |
| `Assets/Scripts/VRHapticsManager.cs` | Singleton `Instance`, `Tap()`, `Success()`, `Warning()`, `Error()` |
| `Assets/Scripts/VRTeleportation.cs` | Arc teleport, fade transition |

### Assembly Definition: `Assets/Scripts/Quest3VR.Prototype.asmdef`
References: `Unity.XR.Interaction.Toolkit`, `Unity.XR.Oculus`, `Unity.XR.Management`, `Unity.InputSystem`

**Must add to asmdef:** `Unity.Networking` (for UnityWebRequest in assembly)

### NPH Backend Endpoints (FastAPI, port 8000)
| Endpoint | Method | Input | Output |
|----------|--------|-------|--------|
| `/analyze` | POST | PNG/JPEG file upload | `{boxes: [{class, x1, y1, x2, y2, confidence}], metrics: {evans_index, vsr, ...}, image_width, image_height, demo_mode}` |
| `/score` | POST | JSON `{evansIndex, callosalAngle, deshScore, sylvianDilation, vsr, triad, corticalAtrophy}` | `{score, label, color, recommendation}` |
| `/analyze-ct3d` | POST | NIfTI/DICOM file upload | `{metrics: {evans_index, evans_slice, vsr, nph_probability, ...}, source, demo_mode}` |
| `/health` | GET | — | `{status, yolo_model, model_loaded}` |

### YOLO Classes
`0=ventricle, 1=sylvian_fissure, 2=tight_convexity, 3=pvh, 4=skull_inner`

---

## Task 1: NPH API Client Service

**Files:**
- Create: `Assets/Scripts/NPH/NPHApiClient.cs`
- Create: `Assets/Scripts/NPH/NPHDataModels.cs`
- Create: `Assets/Scripts/NPH/Quest3VR.NPH.asmdef`

**Step 1: Create the NPH assembly definition**

Create `Assets/Scripts/NPH/Quest3VR.NPH.asmdef`:

```json
{
    "name": "Quest3VR.NPH",
    "rootNamespace": "Quest3VR.NPH",
    "references": [
        "Quest3VR.Prototype"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

**Step 2: Create data models**

Create `Assets/Scripts/NPH/NPHDataModels.cs`:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Quest3VR.NPH
{
    [Serializable]
    public class DetectionBox
    {
        public string @class;
        public int x1, y1, x2, y2;
        public float confidence;
    }

    [Serializable]
    public class NPHMetrics
    {
        public float evans_index;
        public float? callosal_angle;
        public int? desh_score;
        public bool? sylvian_dilation;
        public float? vsr;
        public bool? periventricular_changes;
        public string cortical_atrophy;
        public float nph_probability;
        public int? evans_slice;
    }

    [Serializable]
    public class AnalyzeResponse
    {
        public List<DetectionBox> boxes;
        public NPHMetrics metrics;
        public int image_width;
        public int image_height;
        public bool demo_mode;
    }

    [Serializable]
    public class AnalyzeCT3DResponse
    {
        public NPHMetrics metrics;
        public string source;
        public bool demo_mode;
    }

    [Serializable]
    public class ScoreRequest
    {
        public float evansIndex;
        public float? callosalAngle;
        public int deshScore;
        public bool sylvianDilation;
        public float? vsr;
        public List<bool> triad;
        public string corticalAtrophy;
    }

    [Serializable]
    public class ScoreResponse
    {
        public int score;
        public string label;
        public string color;
        public string recommendation;
    }

    [Serializable]
    public class HealthResponse
    {
        public string status;
        public string yolo_model;
        public bool model_loaded;
    }

    // Color map for YOLO classes
    public static class NPHClassColors
    {
        public static readonly Dictionary<string, Color> Map = new()
        {
            { "ventricle",        new Color(0.2f, 0.6f, 1.0f, 0.8f) },  // blue
            { "sylvian_fissure",  new Color(1.0f, 0.8f, 0.2f, 0.8f) },  // yellow
            { "tight_convexity",  new Color(0.2f, 1.0f, 0.4f, 0.8f) },  // green
            { "pvh",              new Color(1.0f, 0.3f, 0.3f, 0.8f) },  // red
            { "skull_inner",      new Color(0.7f, 0.7f, 0.7f, 0.5f) },  // gray
        };

        public static Color Get(string className)
        {
            return Map.TryGetValue(className, out var c) ? c : Color.white;
        }
    }
}
```

**Step 3: Create the API client**

Create `Assets/Scripts/NPH/NPHApiClient.cs`:

```csharp
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

        /// <summary>Check backend health.</summary>
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

        /// <summary>Upload a PNG/JPEG image for 2D YOLO analysis.</summary>
        public async Task<AnalyzeResponse> Analyze(byte[] imageData, string filename)
        {
            string json = await PostFileRequest($"{serverUrl}/analyze", imageData, filename);
            return JsonUtility.FromJson<AnalyzeResponse>(json);
        }

        /// <summary>Compute NPH score from structured metrics.</summary>
        public async Task<ScoreResponse> Score(ScoreRequest request)
        {
            string body = JsonUtility.ToJson(request);
            string json = await PostJsonRequest($"{serverUrl}/score", body);
            return JsonUtility.FromJson<ScoreResponse>(json);
        }

        /// <summary>Upload NIfTI/DICOM for 3D volumetric analysis.</summary>
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
            req.timeout = 300; // 3D analysis can take minutes
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception($"POST {url} failed: {req.error}");

            return req.downloadHandler.text;
        }
    }
}
```

**Step 4: Commit**

```bash
git add Assets/Scripts/NPH/
git commit -m "feat(nph): add API client and data models for NPH backend"
```

---

## Task 2: CT Slice Viewer with YOLO Overlay

**Files:**
- Create: `Assets/Scripts/NPH/CTSliceViewer.cs`
- Create: `Assets/Scripts/NPH/YOLOOverlayRenderer.cs`

**Step 1: Create the CT slice viewer**

Create `Assets/Scripts/NPH/CTSliceViewer.cs` — a world-space quad that displays CT slice textures and supports controller-based scrolling:

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Quest3VR.Prototype;

namespace Quest3VR.NPH
{
    /// <summary>
    /// Displays CT slice images on a world-space quad.
    /// Attach to a Quad with a MeshRenderer.
    /// Supports thumbstick scrolling through slices.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class CTSliceViewer : MonoBehaviour
    {
        [Header("Slice Data")]
        [SerializeField] private List<Texture2D> sliceTextures = new();
        [SerializeField] private int currentSliceIndex = 0;

        [Header("Display")]
        [SerializeField] private float sliceWidth = 0.5f;  // meters
        [SerializeField] private float sliceHeight = 0.5f;

        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock propBlock;

        // Current analysis result for overlay
        public AnalyzeResponse CurrentAnalysis { get; private set; }
        public int CurrentSliceIndex => currentSliceIndex;
        public int SliceCount => sliceTextures.Count;

        public event System.Action<int> OnSliceChanged;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            propBlock = new MaterialPropertyBlock();
            transform.localScale = new Vector3(sliceWidth, sliceHeight, 1f);
        }

        /// <summary>Load a single CT image (from file picker or network).</summary>
        public void LoadSlice(Texture2D texture, int index = -1)
        {
            if (index < 0 || index >= sliceTextures.Count)
            {
                sliceTextures.Add(texture);
                index = sliceTextures.Count - 1;
            }
            else
            {
                sliceTextures[index] = texture;
            }
            ShowSlice(index);
        }

        /// <summary>Replace all slices at once (e.g. from a NIfTI volume).</summary>
        public void LoadAllSlices(List<Texture2D> textures)
        {
            sliceTextures = textures;
            if (sliceTextures.Count > 0)
                ShowSlice(sliceTextures.Count / 2); // start at middle slice
        }

        /// <summary>Display slice at given index.</summary>
        public void ShowSlice(int index)
        {
            if (sliceTextures.Count == 0) return;
            currentSliceIndex = Mathf.Clamp(index, 0, sliceTextures.Count - 1);

            meshRenderer.GetPropertyBlock(propBlock);
            propBlock.SetTexture("_MainTex", sliceTextures[currentSliceIndex]);
            meshRenderer.SetPropertyBlock(propBlock);

            OnSliceChanged?.Invoke(currentSliceIndex);
        }

        public void NextSlice() => ShowSlice(currentSliceIndex + 1);
        public void PreviousSlice() => ShowSlice(currentSliceIndex - 1);

        /// <summary>Store analysis result for overlay rendering.</summary>
        public void SetAnalysisResult(AnalyzeResponse response)
        {
            CurrentAnalysis = response;
        }

        /// <summary>Get the byte data of the current slice for API upload.</summary>
        public byte[] GetCurrentSliceBytes()
        {
            if (currentSliceIndex < 0 || currentSliceIndex >= sliceTextures.Count)
                return null;
            return sliceTextures[currentSliceIndex].EncodeToPNG();
        }
    }
}
```

**Step 2: Create the YOLO bounding box overlay**

Create `Assets/Scripts/NPH/YOLOOverlayRenderer.cs` — draws bounding boxes and labels over the CT slice quad using LineRenderers:

```csharp
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Quest3VR.NPH
{
    /// <summary>
    /// Renders YOLO detection boxes over a CTSliceViewer quad.
    /// Attach to the same GameObject as CTSliceViewer.
    /// </summary>
    [RequireComponent(typeof(CTSliceViewer))]
    public class YOLOOverlayRenderer : MonoBehaviour
    {
        [Header("Overlay Settings")]
        [SerializeField] private float lineWidth = 0.003f;
        [SerializeField] private float labelFontSize = 0.08f;
        [SerializeField] private float zOffset = -0.001f; // slight offset toward camera

        private CTSliceViewer sliceViewer;
        private readonly List<GameObject> boxObjects = new();

        private void Awake()
        {
            sliceViewer = GetComponent<CTSliceViewer>();
            sliceViewer.OnSliceChanged += _ => ClearBoxes();
        }

        private void OnDestroy()
        {
            if (sliceViewer != null)
                sliceViewer.OnSliceChanged -= _ => ClearBoxes();
        }

        /// <summary>Render detection boxes from an AnalyzeResponse.</summary>
        public void RenderBoxes(AnalyzeResponse response)
        {
            ClearBoxes();
            if (response?.boxes == null) return;

            float imgW = response.image_width;
            float imgH = response.image_height;

            // The quad is centered at (0,0) with scale = (sliceWidth, sliceHeight).
            // Pixel (px, py) maps to local coords:
            //   x = (px / imgW - 0.5) * localScale.x
            //   y = (0.5 - py / imgH) * localScale.y
            Vector3 scale = transform.localScale;

            foreach (var box in response.boxes)
            {
                Color color = NPHClassColors.Get(box.@class);
                float x1 = (box.x1 / imgW - 0.5f) * scale.x;
                float y1 = (0.5f - box.y1 / imgH) * scale.y;
                float x2 = (box.x2 / imgW - 0.5f) * scale.x;
                float y2 = (0.5f - box.y2 / imgH) * scale.y;

                var boxObj = CreateBoxLineRenderer(x1, y1, x2, y2, color, box.@class, box.confidence);
                boxObjects.Add(boxObj);
            }
        }

        public void ClearBoxes()
        {
            foreach (var obj in boxObjects)
            {
                if (obj != null) Destroy(obj);
            }
            boxObjects.Clear();
        }

        private GameObject CreateBoxLineRenderer(float x1, float y1, float x2, float y2,
            Color color, string className, float confidence)
        {
            var go = new GameObject($"Box_{className}");
            go.transform.SetParent(transform, false);

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = color;
            lr.endColor = color;
            lr.positionCount = 5; // closed rectangle

            lr.SetPosition(0, new Vector3(x1, y1, zOffset));
            lr.SetPosition(1, new Vector3(x2, y1, zOffset));
            lr.SetPosition(2, new Vector3(x2, y2, zOffset));
            lr.SetPosition(3, new Vector3(x1, y2, zOffset));
            lr.SetPosition(4, new Vector3(x1, y1, zOffset)); // close loop

            // Add label
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(go.transform, false);
            labelObj.transform.localPosition = new Vector3(x1, y1 + 0.01f, zOffset - 0.001f);

            var tmp = labelObj.AddComponent<TextMeshPro>();
            tmp.text = $"{className} {confidence:P0}";
            tmp.fontSize = labelFontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.BottomLeft;
            tmp.rectTransform.sizeDelta = new Vector2(0.3f, 0.05f);

            return go;
        }
    }
}
```

**Step 3: Commit**

```bash
git add Assets/Scripts/NPH/CTSliceViewer.cs Assets/Scripts/NPH/YOLOOverlayRenderer.cs
git commit -m "feat(nph): add CT slice viewer and YOLO overlay renderer"
```

---

## Task 3: NPH Score Dashboard Panel

**Files:**
- Create: `Assets/Scripts/NPH/NPHScorePanel.cs`

**Step 1: Create the NPH score panel**

This integrates with the existing `VRDashboardController` panel system. It creates a new dashboard panel that shows the NPH risk score, metrics breakdown, and clinical recommendation.

Create `Assets/Scripts/NPH/NPHScorePanel.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Quest3VR.Prototype;

namespace Quest3VR.NPH
{
    /// <summary>
    /// NPH Score display panel for the VR Dashboard.
    /// Shows score gauge, risk label, metrics, and recommendation.
    /// Attach to a UI panel GameObject under the VR_Dashboard prefab.
    /// </summary>
    public class NPHScorePanel : MonoBehaviour
    {
        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private TextMeshProUGUI recommendationText;
        [SerializeField] private Image scoreBar;

        [Header("Metrics Breakdown")]
        [SerializeField] private TextMeshProUGUI evansIndexText;
        [SerializeField] private TextMeshProUGUI vsrText;
        [SerializeField] private TextMeshProUGUI callosalAngleText;
        [SerializeField] private TextMeshProUGUI deshScoreText;
        [SerializeField] private TextMeshProUGUI sylvianText;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI connectionStatusText;
        [SerializeField] private TextMeshProUGUI analysisStatusText;

        public static NPHScorePanel Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        /// <summary>Update the score display from a ScoreResponse.</summary>
        public void DisplayScore(ScoreResponse score)
        {
            if (scoreText != null)
                scoreText.text = $"{score.score}";

            if (labelText != null)
            {
                labelText.text = score.label;
                if (ColorUtility.TryParseHtmlString(score.color, out Color c))
                    labelText.color = c;
            }

            if (recommendationText != null)
                recommendationText.text = score.recommendation;

            if (scoreBar != null)
            {
                scoreBar.fillAmount = score.score / 100f;
                if (ColorUtility.TryParseHtmlString(score.color, out Color barColor))
                    scoreBar.color = barColor;
            }

            // Haptic feedback based on risk level
            if (score.score >= 75)
                VRHapticsManager.Instance?.Warning(VRHapticsManager.ControllerType.Both);
            else if (score.score >= 50)
                VRHapticsManager.Instance?.DoublePulse(VRHapticsManager.ControllerType.Both);
            else
                VRHapticsManager.Instance?.Tap(VRHapticsManager.ControllerType.Both);

            // Update dashboard status
            VRDashboardController.Instance?.UpdateStatus($"NPH Score: {score.score} — {score.label}");
        }

        /// <summary>Update metrics breakdown from AnalyzeResponse or CT3D metrics.</summary>
        public void DisplayMetrics(NPHMetrics metrics)
        {
            if (evansIndexText != null)
                evansIndexText.text = $"Evans Index: {metrics.evans_index:F3}";

            if (vsrText != null)
                vsrText.text = metrics.vsr.HasValue
                    ? $"VSR: {metrics.vsr.Value:F3}"
                    : "VSR: N/A (2D only)";

            if (callosalAngleText != null)
                callosalAngleText.text = metrics.callosal_angle.HasValue
                    ? $"Callosal Angle: {metrics.callosal_angle.Value:F1}°"
                    : "Callosal Angle: N/A";

            if (deshScoreText != null)
                deshScoreText.text = metrics.desh_score.HasValue
                    ? $"DESH Score: {metrics.desh_score.Value}/3"
                    : "DESH Score: N/A";

            if (sylvianText != null)
                sylvianText.text = metrics.sylvian_dilation.HasValue
                    ? $"Sylvian Dilation: {(metrics.sylvian_dilation.Value ? "Yes" : "No")}"
                    : "Sylvian Dilation: N/A";
        }

        public void SetConnectionStatus(bool connected, bool modelLoaded)
        {
            if (connectionStatusText != null)
            {
                connectionStatusText.text = connected
                    ? (modelLoaded ? "Backend: Connected (YOLO loaded)" : "Backend: Connected (demo mode)")
                    : "Backend: Disconnected";
                connectionStatusText.color = connected ? Color.green : Color.red;
            }
        }

        public void SetAnalysisStatus(string status)
        {
            if (analysisStatusText != null)
                analysisStatusText.text = status;
        }
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/NPH/NPHScorePanel.cs
git commit -m "feat(nph): add NPH score dashboard panel"
```

---

## Task 4: 3D Ventricle Mesh Renderer

**Files:**
- Create: `Assets/Scripts/NPH/VentricleMeshGenerator.cs`

**Step 1: Create the mesh generator**

This generates a 3D mesh from ventricle segmentation data. For the initial version, it creates a procedural mesh from the Evans Index / VSR values (approximate ellipsoid). A future version can load actual NIfTI segmentation masks via marching cubes.

Create `Assets/Scripts/NPH/VentricleMeshGenerator.cs`:

```csharp
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Quest3VR.Prototype;

namespace Quest3VR.NPH
{
    /// <summary>
    /// Generates and displays a 3D ventricle representation.
    /// Creates an approximate ellipsoid mesh based on Evans Index / VSR.
    /// Attach to an empty GameObject. The mesh is grabbable.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class VentricleMeshGenerator : MonoBehaviour
    {
        [Header("Mesh Settings")]
        [SerializeField] private int resolution = 24;
        [SerializeField] private Material ventricleMaterial;
        [SerializeField] private Color normalColor = new(0.2f, 0.5f, 1.0f, 0.6f);
        [SerializeField] private Color enlargedColor = new(1.0f, 0.3f, 0.2f, 0.6f);

        [Header("Scale")]
        [SerializeField] private float baseScale = 0.15f; // meters — about palm-sized

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private float currentEvansIndex;
        private float? currentVSR;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            if (ventricleMaterial == null)
            {
                ventricleMaterial = new Material(Shader.Find("Standard"));
                ventricleMaterial.SetFloat("_Mode", 3); // Transparent
                ventricleMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                ventricleMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                ventricleMaterial.SetInt("_ZWrite", 0);
                ventricleMaterial.DisableKeyword("_ALPHATEST_ON");
                ventricleMaterial.EnableKeyword("_ALPHABLEND_ON");
                ventricleMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                ventricleMaterial.renderQueue = 3000;
            }
            meshRenderer.material = ventricleMaterial;
        }

        /// <summary>
        /// Generate ventricle mesh from NPH metrics.
        /// Evans Index affects lateral width; VSR affects overall volume.
        /// </summary>
        public void GenerateFromMetrics(NPHMetrics metrics)
        {
            currentEvansIndex = metrics.evans_index;
            currentVSR = metrics.vsr;

            // Map Evans Index to lateral ventricle width ratio
            // Normal: EI < 0.3, dilated: EI > 0.3
            float lateralScale = Mathf.Lerp(0.5f, 1.5f, Mathf.InverseLerp(0.2f, 0.5f, metrics.evans_index));

            // Map VSR to overall volume scale
            float volumeScale = 1.0f;
            if (metrics.vsr.HasValue)
                volumeScale = Mathf.Lerp(0.7f, 2.0f, Mathf.InverseLerp(0.5f, 3.0f, metrics.vsr.Value));

            // Generate butterfly-shaped lateral ventricle approximation
            Mesh mesh = GenerateLateralVentricleMesh(lateralScale, volumeScale);
            meshFilter.mesh = mesh;

            // Color based on severity
            float severity = metrics.evans_index > 0.3f ? Mathf.InverseLerp(0.3f, 0.5f, metrics.evans_index) : 0f;
            Color color = Color.Lerp(normalColor, enlargedColor, severity);
            ventricleMaterial.color = color;

            // Haptic feedback on generation
            VRHapticsManager.Instance?.Success(VRHapticsManager.ControllerType.Both);

            Debug.Log($"[VentricleMesh] Generated: EI={metrics.evans_index:F3}, VSR={metrics.vsr}, scale={volumeScale:F2}");
        }

        private Mesh GenerateLateralVentricleMesh(float lateralScale, float volumeScale)
        {
            // Create a double-horn ellipsoid approximating lateral ventricles
            // Two elongated ellipsoids joined at the midline
            var mesh = new Mesh();
            mesh.name = "LateralVentricles";

            int segments = resolution;
            int rings = resolution / 2;
            int vertCount = (segments + 1) * (rings + 1) * 2; // left + right
            var vertices = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var triangles = new System.Collections.Generic.List<int>();

            float scale = baseScale * volumeScale;

            for (int side = 0; side < 2; side++)
            {
                float xSign = side == 0 ? -1f : 1f;
                int offset = side * (segments + 1) * (rings + 1);

                for (int r = 0; r <= rings; r++)
                {
                    float phi = Mathf.PI * r / rings;
                    for (int s = 0; s <= segments; s++)
                    {
                        float theta = 2f * Mathf.PI * s / segments;
                        int idx = offset + r * (segments + 1) + s;

                        // Lateral ventricle shape: elongated A-P, narrow M-L, medium S-I
                        float x = xSign * (0.3f * lateralScale + 0.15f * Mathf.Sin(phi)) * Mathf.Sin(theta) * scale;
                        float y = 0.4f * Mathf.Cos(phi) * scale;  // superior-inferior
                        float z = 0.6f * Mathf.Sin(phi) * Mathf.Cos(theta) * scale;  // anterior-posterior

                        // Offset each horn laterally
                        x += xSign * 0.1f * lateralScale * scale;

                        vertices[idx] = new Vector3(x, y, z);
                        normals[idx] = new Vector3(x, y, z).normalized;
                    }
                }

                // Triangles
                for (int r = 0; r < rings; r++)
                {
                    for (int s = 0; s < segments; s++)
                    {
                        int a = offset + r * (segments + 1) + s;
                        int b = a + segments + 1;

                        triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                        triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
                    }
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>Make the mesh grabbable by adding required XR components.</summary>
        public void MakeGrabbable()
        {
            if (GetComponent<Rigidbody>() == null)
            {
                var rb = gameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = true;
            }

            if (GetComponent<MeshCollider>() == null)
            {
                var col = gameObject.AddComponent<MeshCollider>();
                col.convex = true;
            }

            if (GetComponent<XRGrabInteractable>() == null)
            {
                var grab = gameObject.AddComponent<XRGrabInteractable>();
                grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
                grab.useDynamicAttach = true;
            }

            if (GetComponent<GrabbableObject>() == null)
            {
                gameObject.AddComponent<GrabbableObject>();
            }
        }
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/NPH/VentricleMeshGenerator.cs
git commit -m "feat(nph): add 3D ventricle mesh generator from NPH metrics"
```

---

## Task 5: NPH Scene Controller (Orchestrator)

**Files:**
- Create: `Assets/Scripts/NPH/NPHSceneController.cs`

**Step 1: Create the scene controller**

This is the main orchestrator that ties everything together — manages the analysis workflow, coordinates between CT viewer, YOLO overlay, 3D mesh, and score panel.

Create `Assets/Scripts/NPH/NPHSceneController.cs`:

```csharp
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
```

**Step 2: Commit**

```bash
git add Assets/Scripts/NPH/NPHSceneController.cs
git commit -m "feat(nph): add scene controller orchestrating analysis workflow"
```

---

## Task 6: Backend CORS + Mesh Export Endpoint

**Files:**
- Modify: `~/Pictures/Interactive Appraisal_ MCS for Central Neuropathic Pain_files/NPHProject/api.py`

The backend already has CORS configured with `allow_origins=["*"]`, which is sufficient. But we need a new endpoint to export ventricle mesh data as OBJ format for future use.

**Step 1: Add mesh export endpoint to api.py**

Add after the `/analyze-ct3d` endpoint (after line 475 in `api.py`):

```python
@app.post("/mesh-ventricle")
async def mesh_ventricle(file: UploadFile = File(...)):
    """
    Accept NIfTI, run TotalSegmentator, return ventricle mesh as OBJ vertices + faces.
    Lighter response than full 3D analysis — just the mesh data.
    """
    filename = file.filename or ""
    if not (filename.endswith(".nii.gz") or filename.endswith(".nii")):
        raise HTTPException(status_code=400, detail="Only NIfTI files accepted for mesh export.")

    tmp = tempfile.NamedTemporaryFile(delete=False, suffix=".nii.gz")
    workdir = tempfile.mkdtemp()
    try:
        tmp.write(await file.read())
        tmp.flush()
        tmp.close()

        import asyncio
        loop = asyncio.get_event_loop()

        def _generate_mesh():
            from totalsegmentator.python_api import totalsegmentator
            from skimage.measure import marching_cubes

            seg_dir = os.path.join(workdir, "seg")
            os.makedirs(seg_dir, exist_ok=True)
            totalsegmentator(input=tmp.name, output=seg_dir, task="brain_structures", fast=True, quiet=True)

            # Combine left + right lateral ventricles
            left_path = os.path.join(seg_dir, "lateralventricle_left.nii.gz")
            right_path = os.path.join(seg_dir, "lateralventricle_right.nii.gz")

            if not (os.path.exists(left_path) and os.path.exists(right_path)):
                raise HTTPException(status_code=422, detail="Ventricle masks not found")

            left_data = nib.load(left_path).get_fdata()
            right_data = nib.load(right_path).get_fdata()
            combined = (left_data + right_data) > 0

            # Marching cubes to extract mesh
            verts, faces, normals, _ = marching_cubes(combined.astype(float), level=0.5)

            # Apply voxel spacing from the NIfTI affine
            ct_img = nib.load(tmp.name)
            spacing = ct_img.header.get_zooms()[:3]
            verts *= np.array(spacing)

            return {
                "vertices": verts.tolist(),
                "faces": faces.tolist(),
                "normals": normals.tolist(),
                "vertex_count": len(verts),
                "face_count": len(faces),
            }

        mesh_data = await loop.run_in_executor(_executor, _generate_mesh)
        return JSONResponse(mesh_data)
    finally:
        if os.path.exists(tmp.name):
            os.unlink(tmp.name)
        shutil.rmtree(workdir, ignore_errors=True)
```

**Step 2: Commit in the NPH project directory**

```bash
cd ~/Pictures/Interactive\ Appraisal_\ MCS\ for\ Central\ Neuropathic\ Pain_files/NPHProject
git add api.py
git commit -m "feat(api): add /mesh-ventricle endpoint for OBJ mesh export"
```

---

## Task 7: Unity Scene Setup (Manual Instructions)

This task requires Unity Editor — it cannot be automated via script. These are step-by-step manual instructions.

**Step 1: Open the project in Unity 2022.3 LTS**

Open `Quest3VR_Prototype/` as a Unity project.

**Step 2: Create NPH objects in the scene hierarchy**

In `VRStarterScene.unity`, add:

```
VRStarterScene
├── (existing objects...)
├── NPH_System                          [Empty GameObject]
│   ├── NPHApiClient                    [Add NPHApiClient.cs]
│   ├── CTViewer                        [Quad mesh, add CTSliceViewer.cs + YOLOOverlayRenderer.cs]
│   │   Position: (0, 1.2, 1.5)        [In front of player at eye level]
│   │   Rotation: (0, 180, 0)          [Face the player]
│   │   Scale: (0.5, 0.5, 1)           [50cm × 50cm]
│   ├── VentricleMesh                   [Empty, add VentricleMeshGenerator.cs]
│   │   Position: (0.5, 1.0, 1.5)      [Right of CT viewer]
│   └── NPHSceneController             [Add NPHSceneController.cs, wire all refs]
└── UI
    └── VR_Dashboard
        └── NPH_Panel                  [New panel under dashboard]
            ├── ScoreText               [TextMeshPro]
            ├── LabelText               [TextMeshPro]
            ├── RecommendationText      [TextMeshPro]
            ├── ScoreBar                [Image, filled horizontal]
            ├── MetricsGroup            [Vertical layout]
            │   ├── EvansIndexText      [TextMeshPro]
            │   ├── VSRText             [TextMeshPro]
            │   ├── CallosalAngleText   [TextMeshPro]
            │   ├── DESHScoreText       [TextMeshPro]
            │   └── SylvianText         [TextMeshPro]
            ├── ConnectionStatusText    [TextMeshPro]
            └── AnalysisStatusText      [TextMeshPro]
```

**Step 3: Configure NPHApiClient**

- Set `Server Url` to your dev machine's local IP (e.g., `http://192.168.1.100:8000`)
- Ensure the NPH backend is running: `cd NPHProject && source .venv/bin/activate && uvicorn api:app --host 0.0.0.0`

**Step 4: Wire NPHSceneController references**

In the Inspector for `NPHSceneController`:
- `Api Client` → drag `NPHApiClient` GameObject
- `Slice Viewer` → drag `CTViewer` GameObject
- `Overlay Renderer` → drag `CTViewer` GameObject
- `Ventricle Mesh` → drag `VentricleMesh` GameObject
- `Score Panel` → drag `NPH_Panel` GameObject

**Step 5: Add sample CT slice textures**

Import 2-3 sample CT PNG images into `Assets/Textures/SampleCT/` and assign them to `NPHSceneController.sampleCTSlices` array.

**Step 6: Build and test**

```
File → Build Settings → Android → Build and Run
```

Ensure Quest 3 is connected via USB or on the same WiFi network as the dev machine running the FastAPI backend.

---

## Task 8: Add VR Slice Scrolling Input

**Files:**
- Create: `Assets/Scripts/NPH/SliceScrollController.cs`

**Step 1: Create thumbstick-based slice scrolling**

Create `Assets/Scripts/NPH/SliceScrollController.cs`:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace Quest3VR.NPH
{
    /// <summary>
    /// Allows scrolling through CT slices using the right thumbstick.
    /// Attach to the right controller or XR Rig.
    /// </summary>
    public class SliceScrollController : MonoBehaviour
    {
        [SerializeField] private CTSliceViewer sliceViewer;
        [SerializeField] private InputActionProperty thumbstickAction;
        [SerializeField] private float scrollThreshold = 0.5f;
        [SerializeField] private float scrollCooldown = 0.2f;

        private float lastScrollTime;

        private void Update()
        {
            if (sliceViewer == null || thumbstickAction.action == null) return;

            Vector2 thumbstick = thumbstickAction.action.ReadValue<Vector2>();

            if (Time.time - lastScrollTime < scrollCooldown) return;

            if (thumbstick.y > scrollThreshold)
            {
                sliceViewer.NextSlice();
                lastScrollTime = Time.time;
            }
            else if (thumbstick.y < -scrollThreshold)
            {
                sliceViewer.PreviousSlice();
                lastScrollTime = Time.time;
            }
        }
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/NPH/SliceScrollController.cs
git commit -m "feat(nph): add thumbstick-based CT slice scrolling"
```

---

## Summary

| Task | Component | Files Created | Dependencies |
|------|-----------|--------------|-------------|
| 1 | API Client + Models | `NPHApiClient.cs`, `NPHDataModels.cs`, `Quest3VR.NPH.asmdef` | None |
| 2 | CT Slice Viewer + YOLO Overlay | `CTSliceViewer.cs`, `YOLOOverlayRenderer.cs` | Task 1 |
| 3 | NPH Score Dashboard | `NPHScorePanel.cs` | Task 1 |
| 4 | 3D Ventricle Mesh | `VentricleMeshGenerator.cs` | Task 1 |
| 5 | Scene Controller | `NPHSceneController.cs` | Tasks 1-4 |
| 6 | Backend Mesh Export | `api.py` modification | None (NPH project) |
| 7 | Unity Scene Setup | Manual Unity Editor | Tasks 1-5 |
| 8 | Slice Scrolling Input | `SliceScrollController.cs` | Task 2 |

**Parallel-safe tasks:** Tasks 1-4 and 6 are independent of each other. Task 5 depends on 1-4. Tasks 7-8 depend on 5.
