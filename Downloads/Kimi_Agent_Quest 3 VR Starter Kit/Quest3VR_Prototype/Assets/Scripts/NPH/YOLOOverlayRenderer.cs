using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Quest3VR.NPH
{
    [RequireComponent(typeof(CTSliceViewer))]
    public class YOLOOverlayRenderer : MonoBehaviour
    {
        [Header("Overlay Settings")]
        [SerializeField] private float lineWidth = 0.003f;
        [SerializeField] private float labelFontSize = 0.08f;
        [SerializeField] private float zOffset = -0.001f;

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

        public void RenderBoxes(AnalyzeResponse response)
        {
            ClearBoxes();
            if (response?.boxes == null) return;

            float imgW = response.image_width;
            float imgH = response.image_height;

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
            lr.positionCount = 5;

            lr.SetPosition(0, new Vector3(x1, y1, zOffset));
            lr.SetPosition(1, new Vector3(x2, y1, zOffset));
            lr.SetPosition(2, new Vector3(x2, y2, zOffset));
            lr.SetPosition(3, new Vector3(x1, y2, zOffset));
            lr.SetPosition(4, new Vector3(x1, y1, zOffset));

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
