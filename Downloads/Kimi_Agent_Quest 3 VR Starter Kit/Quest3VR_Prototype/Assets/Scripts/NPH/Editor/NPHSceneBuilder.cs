using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Quest3VR.NPH.Editor
{
    /// <summary>
    /// Editor script that programmatically builds the NPH VR scene hierarchy.
    /// Run via menu: Tools > NPH > Build NPH Scene
    /// Or batch mode: Unity -executeMethod Quest3VR.NPH.Editor.NPHSceneBuilder.BuildScene
    /// </summary>
    public static class NPHSceneBuilder
    {
        [MenuItem("Tools/NPH/Build NPH Scene")]
        public static void BuildScene()
        {
            // Open the starter scene
            string scenePath = "Assets/Scenes/VRStarterScene.unity";
            if (!System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, "../", scenePath)))
            {
                Debug.LogWarning($"[NPHSceneBuilder] Scene not found at {scenePath}, building in current scene.");
            }
            else
            {
                EditorSceneManager.OpenScene(scenePath);
            }

            // Create root NPH_System object
            var nphSystem = new GameObject("NPH_System");
            Undo.RegisterCreatedObjectUndo(nphSystem, "Create NPH System");

            // --- NPHApiClient ---
            var apiClientObj = new GameObject("NPHApiClient");
            apiClientObj.transform.SetParent(nphSystem.transform);
            var apiClient = apiClientObj.AddComponent<NPHApiClient>();

            // --- CT Viewer (Quad) ---
            var ctViewerObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ctViewerObj.name = "CTViewer";
            ctViewerObj.transform.SetParent(nphSystem.transform);
            ctViewerObj.transform.localPosition = new Vector3(0f, 1.2f, 1.5f);
            ctViewerObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            ctViewerObj.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

            // Remove default collider (not needed for display quad)
            var quadCollider = ctViewerObj.GetComponent<Collider>();
            if (quadCollider != null) Object.DestroyImmediate(quadCollider);

            var sliceViewer = ctViewerObj.AddComponent<CTSliceViewer>();
            var overlayRenderer = ctViewerObj.AddComponent<YOLOOverlayRenderer>();

            // --- Ventricle Mesh ---
            var ventricleObj = new GameObject("VentricleMesh");
            ventricleObj.transform.SetParent(nphSystem.transform);
            ventricleObj.transform.localPosition = new Vector3(0.5f, 1.0f, 1.5f);
            ventricleObj.AddComponent<MeshFilter>();
            ventricleObj.AddComponent<MeshRenderer>();
            var ventricleMesh = ventricleObj.AddComponent<VentricleMeshGenerator>();

            // --- Slice Scroll Controller (on NPH_System root) ---
            var scrollController = nphSystem.AddComponent<SliceScrollController>();
            // Wire slice viewer reference via SerializedObject
            SetSerializedField(scrollController, "sliceViewer", sliceViewer);

            // --- NPH Score Panel (under UI/VR_Dashboard) ---
            GameObject nphPanelObj = BuildNPHPanel(nphSystem.transform);
            var scorePanel = nphPanelObj.GetComponent<NPHScorePanel>();

            // --- Scene Controller (orchestrator) ---
            var controllerObj = new GameObject("NPHSceneController");
            controllerObj.transform.SetParent(nphSystem.transform);
            var sceneController = controllerObj.AddComponent<NPHSceneController>();

            // Wire all references on the scene controller
            SetSerializedField(sceneController, "apiClient", apiClient);
            SetSerializedField(sceneController, "sliceViewer", sliceViewer);
            SetSerializedField(sceneController, "overlayRenderer", overlayRenderer);
            SetSerializedField(sceneController, "ventricleMesh", ventricleMesh);
            SetSerializedField(sceneController, "scorePanel", scorePanel);

            // Mark scene dirty so it can be saved
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("[NPHSceneBuilder] NPH scene hierarchy built successfully!");
            Debug.Log("[NPHSceneBuilder] Hierarchy:");
            Debug.Log("  NPH_System");
            Debug.Log("    ├── NPHApiClient");
            Debug.Log("    ├── CTViewer (Quad + CTSliceViewer + YOLOOverlayRenderer)");
            Debug.Log("    ├── VentricleMesh (MeshFilter + MeshRenderer + VentricleMeshGenerator)");
            Debug.Log("    ├── NPHSceneController");
            Debug.Log("    └── NPH_Panel (NPHScorePanel + all UI elements)");
            Debug.Log("");
            Debug.Log("[NPHSceneBuilder] Next steps:");
            Debug.Log("  1. Set NPHApiClient > Server Url to your dev machine IP");
            Debug.Log("  2. Import sample CT PNGs to Assets/Textures/SampleCT/");
            Debug.Log("  3. Assign them to NPHSceneController > Sample CT Slices");
            Debug.Log("  4. Save the scene (Ctrl+S)");
        }

        /// <summary>Build the NPH score panel UI hierarchy.</summary>
        private static GameObject BuildNPHPanel(Transform parent)
        {
            // Create panel root with Canvas for world-space UI
            var panelObj = new GameObject("NPH_Panel");
            panelObj.transform.SetParent(parent);
            panelObj.transform.localPosition = new Vector3(-0.5f, 1.2f, 1.5f);
            panelObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var canvas = panelObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rectTransform = panelObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(400f, 600f);
            rectTransform.localScale = Vector3.one * 0.001f; // Scale to meters

            panelObj.AddComponent<CanvasScaler>();
            panelObj.AddComponent<GraphicRaycaster>();

            // Background
            var bg = panelObj.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.1f, 0.85f);

            // Add NPHScorePanel component
            var scorePanel = panelObj.AddComponent<NPHScorePanel>();

            // Score Text
            var scoreTextObj = CreateTMPText(panelObj.transform, "ScoreText",
                new Vector2(0f, 250f), new Vector2(200f, 80f), "—", 48f, TextAlignmentOptions.Center);
            SetSerializedField(scorePanel, "scoreText", scoreTextObj.GetComponent<TextMeshProUGUI>());

            // Label Text
            var labelTextObj = CreateTMPText(panelObj.transform, "LabelText",
                new Vector2(0f, 200f), new Vector2(350f, 40f), "Awaiting Analysis", 24f, TextAlignmentOptions.Center);
            SetSerializedField(scorePanel, "labelText", labelTextObj.GetComponent<TextMeshProUGUI>());

            // Score Bar
            var scoreBarObj = new GameObject("ScoreBar");
            scoreBarObj.transform.SetParent(panelObj.transform, false);
            var scoreBarRect = scoreBarObj.AddComponent<RectTransform>();
            scoreBarRect.anchoredPosition = new Vector2(0f, 165f);
            scoreBarRect.sizeDelta = new Vector2(350f, 15f);
            var scoreBarImage = scoreBarObj.AddComponent<Image>();
            scoreBarImage.color = Color.gray;
            scoreBarImage.type = Image.Type.Filled;
            scoreBarImage.fillMethod = Image.FillMethod.Horizontal;
            scoreBarImage.fillAmount = 0f;
            SetSerializedField(scorePanel, "scoreBar", scoreBarImage);

            // Metrics group
            float yPos = 120f;
            float yStep = -35f;

            var evansObj = CreateTMPText(panelObj.transform, "EvansIndexText",
                new Vector2(0f, yPos), new Vector2(350f, 30f), "Evans Index: —", 18f);
            SetSerializedField(scorePanel, "evansIndexText", evansObj.GetComponent<TextMeshProUGUI>());

            var vsrObj = CreateTMPText(panelObj.transform, "VSRText",
                new Vector2(0f, yPos + yStep), new Vector2(350f, 30f), "VSR: —", 18f);
            SetSerializedField(scorePanel, "vsrText", vsrObj.GetComponent<TextMeshProUGUI>());

            var callosalObj = CreateTMPText(panelObj.transform, "CallosalAngleText",
                new Vector2(0f, yPos + yStep * 2), new Vector2(350f, 30f), "Callosal Angle: —", 18f);
            SetSerializedField(scorePanel, "callosalAngleText", callosalObj.GetComponent<TextMeshProUGUI>());

            var deshObj = CreateTMPText(panelObj.transform, "DESHScoreText",
                new Vector2(0f, yPos + yStep * 3), new Vector2(350f, 30f), "DESH Score: —", 18f);
            SetSerializedField(scorePanel, "deshScoreText", deshObj.GetComponent<TextMeshProUGUI>());

            var sylvianObj = CreateTMPText(panelObj.transform, "SylvianText",
                new Vector2(0f, yPos + yStep * 4), new Vector2(350f, 30f), "Sylvian Dilation: —", 18f);
            SetSerializedField(scorePanel, "sylvianText", sylvianObj.GetComponent<TextMeshProUGUI>());

            // Recommendation Text
            var recObj = CreateTMPText(panelObj.transform, "RecommendationText",
                new Vector2(0f, -80f), new Vector2(350f, 60f), "", 16f, TextAlignmentOptions.TopLeft);
            recObj.GetComponent<TextMeshProUGUI>().enableWordWrapping = true;
            SetSerializedField(scorePanel, "recommendationText", recObj.GetComponent<TextMeshProUGUI>());

            // Connection Status
            var connObj = CreateTMPText(panelObj.transform, "ConnectionStatusText",
                new Vector2(0f, -200f), new Vector2(350f, 25f), "Backend: Checking...", 14f);
            connObj.GetComponent<TextMeshProUGUI>().color = Color.yellow;
            SetSerializedField(scorePanel, "connectionStatusText", connObj.GetComponent<TextMeshProUGUI>());

            // Analysis Status
            var statusObj = CreateTMPText(panelObj.transform, "AnalysisStatusText",
                new Vector2(0f, -230f), new Vector2(350f, 25f), "Idle", 14f);
            SetSerializedField(scorePanel, "analysisStatusText", statusObj.GetComponent<TextMeshProUGUI>());

            return panelObj;
        }

        /// <summary>Create a TextMeshPro UI element.</summary>
        private static GameObject CreateTMPText(Transform parent, string name,
            Vector2 anchoredPos, Vector2 sizeDelta, string text,
            float fontSize, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;

            return obj;
        }

        /// <summary>Wire a serialized field reference via SerializedObject.</summary>
        private static void SetSerializedField(Component target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning($"[NPHSceneBuilder] Field '{fieldName}' not found on {target.GetType().Name}");
            }
        }
    }
}
