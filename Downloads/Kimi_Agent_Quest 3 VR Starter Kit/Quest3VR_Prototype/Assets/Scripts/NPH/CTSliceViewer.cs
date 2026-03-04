using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Quest3VR.Prototype;

namespace Quest3VR.NPH
{
    /// <summary>
    /// Displays CT scan slices as textures on a quad.
    /// Supports MaterialPropertyBlock for efficient texture swapping.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class CTSliceViewer : MonoBehaviour
    {
        [Header("Slice Data")]
        [SerializeField] private List<Texture2D> sliceTextures = new();
        [SerializeField] private int currentSliceIndex = 0;

        [Header("Display")]
        [SerializeField] private float sliceWidth = 0.5f;
        [SerializeField] private float sliceHeight = 0.5f;
        [SerializeField] private Material displayMaterial;

        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock propBlock;
        private Texture2D currentReadableTexture;

        public AnalyzeResponse CurrentAnalysis { get; private set; }
        public int CurrentSliceIndex => currentSliceIndex;
        public int SliceCount => sliceTextures.Count;

        public event System.Action<int> OnSliceChanged;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            propBlock = new MaterialPropertyBlock();
            
            // Set initial scale
            transform.localScale = new Vector3(sliceWidth, sliceHeight, 1f);

            // Ensure we have a material
            if (displayMaterial != null)
            {
                meshRenderer.material = displayMaterial;
            }
        }

        private void OnDestroy()
        {
            // Clean up any created readable texture
            if (currentReadableTexture != null)
            {
                Destroy(currentReadableTexture);
            }
        }

        public void LoadSlice(Texture2D texture, int index = -1)
        {
            if (texture == null)
            {
                Debug.LogWarning("[CTSliceViewer] Cannot load null texture.");
                return;
            }

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
            if (textures == null || textures.Count == 0)
            {
                Debug.LogWarning("[CTSliceViewer] No textures to load.");
                return;
            }

            sliceTextures = new List<Texture2D>(textures);
            // Start at middle slice
            ShowSlice(sliceTextures.Count / 2);
        }

        public void ShowSlice(int index)
        {
            if (sliceTextures.Count == 0) return;
            
            int previousIndex = currentSliceIndex;
            currentSliceIndex = Mathf.Clamp(index, 0, sliceTextures.Count - 1);

            // Skip if same slice
            if (previousIndex == currentSliceIndex && sliceTextures[currentSliceIndex] == GetCurrentSliceTexture())
                return;

            var texture = sliceTextures[currentSliceIndex];
            
            // Update material property block
            meshRenderer.GetPropertyBlock(propBlock);
            propBlock.SetTexture("_MainTex", texture);
            meshRenderer.SetPropertyBlock(propBlock);

            // Create a readable copy for analysis if needed
            EnsureReadableTexture(texture);

            // Trigger haptic feedback
            VRHapticsManager.Instance?.Tap(VRHapticsManager.ControllerType.Right);

            OnSliceChanged?.Invoke(currentSliceIndex);
        }

        public void NextSlice() => ShowSlice(currentSliceIndex + 1);
        public void PreviousSlice() => ShowSlice(currentSliceIndex - 1);

        public void SetAnalysisResult(AnalyzeResponse response)
        {
            CurrentAnalysis = response;
        }

        /// <summary>
        /// Get the currently displayed texture.
        /// </summary>
        public Texture2D GetCurrentSliceTexture()
        {
            if (currentSliceIndex < 0 || currentSliceIndex >= sliceTextures.Count)
                return null;
            return sliceTextures[currentSliceIndex];
        }

        /// <summary>
        /// Get the current slice as PNG bytes for analysis.
        /// Creates a readable copy if the original texture is not readable.
        /// </summary>
        public byte[] GetCurrentSliceBytes()
        {
            if (currentSliceIndex < 0 || currentSliceIndex >= sliceTextures.Count)
                return null;

            var texture = sliceTextures[currentSliceIndex];
            if (texture == null) return null;

            // Try to encode directly first
            try
            {
                return texture.EncodeToPNG();
            }
            catch (UnityException ex)
            {
                Debug.LogWarning($"[CTSliceViewer] Texture not readable, creating readable copy: {ex.Message}");
                
                // Create a readable copy
                var readableCopy = CreateReadableTexture(texture);
                if (readableCopy != null)
                {
                    byte[] data = readableCopy.EncodeToPNG();
                    Destroy(readableCopy);
                    return data;
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a readable texture copy that can be encoded to bytes.
        /// </summary>
        private Texture2D CreateReadableTexture(Texture2D source)
        {
            if (source == null) return null;

            try
            {
                // Create a temporary RenderTexture
                RenderTexture rt = RenderTexture.GetTemporary(
                    source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

                // Copy source texture to RenderTexture
                Graphics.Blit(source, rt);

                // Create readable texture
                Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
                
                // Read pixels from RenderTexture
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = rt;
                readable.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                readable.Apply();
                RenderTexture.active = previous;

                RenderTexture.ReleaseTemporary(rt);

                return readable;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CTSliceViewer] Failed to create readable texture: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Ensures a readable copy is available for the current texture.
        /// </summary>
        private void EnsureReadableTexture(Texture2D source)
        {
            if (source == null) return;

            // Clean up previous
            if (currentReadableTexture != null && currentReadableTexture != source)
            {
                Destroy(currentReadableTexture);
                currentReadableTexture = null;
            }

            // Try to check if texture is readable
            try
            {
                var _ = source.GetPixel(0, 0);
                // If we get here, texture is readable
                currentReadableTexture = source;
            }
            catch
            {
                // Create readable copy
                currentReadableTexture = CreateReadableTexture(source);
            }
        }
    }
}
