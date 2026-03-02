using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Quest3VR.Prototype;

namespace Quest3VR.NPH
{
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
        [SerializeField] private float baseScale = 0.15f;

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
                ventricleMaterial.SetFloat("_Mode", 3);
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

        public void GenerateFromMetrics(NPHMetrics metrics)
        {
            currentEvansIndex = metrics.evans_index;
            currentVSR = metrics.vsr;

            float lateralScale = Mathf.Lerp(0.5f, 1.5f, Mathf.InverseLerp(0.2f, 0.5f, metrics.evans_index));

            float volumeScale = 1.0f;
            if (metrics.vsr.HasValue)
                volumeScale = Mathf.Lerp(0.7f, 2.0f, Mathf.InverseLerp(0.5f, 3.0f, metrics.vsr.Value));

            Mesh mesh = GenerateLateralVentricleMesh(lateralScale, volumeScale);
            meshFilter.mesh = mesh;

            float severity = metrics.evans_index > 0.3f ? Mathf.InverseLerp(0.3f, 0.5f, metrics.evans_index) : 0f;
            Color color = Color.Lerp(normalColor, enlargedColor, severity);
            ventricleMaterial.color = color;

            VRHapticsManager.Instance?.Success(VRHapticsManager.ControllerType.Both);

            Debug.Log($"[VentricleMesh] Generated: EI={metrics.evans_index:F3}, VSR={metrics.vsr}, scale={volumeScale:F2}");
        }

        private Mesh GenerateLateralVentricleMesh(float lateralScale, float volumeScale)
        {
            var mesh = new Mesh();
            mesh.name = "LateralVentricles";

            int segments = resolution;
            int rings = resolution / 2;
            int vertCount = (segments + 1) * (rings + 1) * 2;
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

                        float x = xSign * (0.3f * lateralScale + 0.15f * Mathf.Sin(phi)) * Mathf.Sin(theta) * scale;
                        float y = 0.4f * Mathf.Cos(phi) * scale;
                        float z = 0.6f * Mathf.Sin(phi) * Mathf.Cos(theta) * scale;

                        x += xSign * 0.1f * lateralScale * scale;

                        vertices[idx] = new Vector3(x, y, z);
                        normals[idx] = new Vector3(x, y, z).normalized;
                    }
                }

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
