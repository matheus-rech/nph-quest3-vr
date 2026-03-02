using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Quest3VR.Prototype;

namespace Quest3VR.NPH
{
    [RequireComponent(typeof(MeshRenderer))]
    public class CTSliceViewer : MonoBehaviour
    {
        [Header("Slice Data")]
        [SerializeField] private List<Texture2D> sliceTextures = new();
        [SerializeField] private int currentSliceIndex = 0;

        [Header("Display")]
        [SerializeField] private float sliceWidth = 0.5f;
        [SerializeField] private float sliceHeight = 0.5f;

        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock propBlock;

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

        public void LoadAllSlices(List<Texture2D> textures)
        {
            sliceTextures = textures;
            if (sliceTextures.Count > 0)
                ShowSlice(sliceTextures.Count / 2);
        }

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

        public void SetAnalysisResult(AnalyzeResponse response)
        {
            CurrentAnalysis = response;
        }

        public byte[] GetCurrentSliceBytes()
        {
            if (currentSliceIndex < 0 || currentSliceIndex >= sliceTextures.Count)
                return null;
            return sliceTextures[currentSliceIndex].EncodeToPNG();
        }
    }
}
