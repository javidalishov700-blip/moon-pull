using UnityEngine;

namespace MoonPull.Gameplay.Visuals
{
    /// <summary>
    /// Lightweight post-processing for the built-in pipeline (see MoonPull/Post): bloom so lamps, lanterns and the
    /// moon glow, plus a cinematic grade and vignette. Skips itself cleanly when the shader is unsupported.
    /// Screen-space overlay UI is not affected.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public sealed class MoonPullPost : MonoBehaviour
    {
        private const int MaxIterations = 5;

        private static readonly int ThresholdId = Shader.PropertyToID("_Threshold");
        private static readonly int BloomTexId = Shader.PropertyToID("_BloomTex");
        private static readonly int BloomIntensityId = Shader.PropertyToID("_BloomIntensity");
        private static readonly int VignetteId = Shader.PropertyToID("_Vignette");
        private static readonly int ChromaticId = Shader.PropertyToID("_Chromatic");
        private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
        private static readonly int ContrastId = Shader.PropertyToID("_Contrast");
        private static readonly int ShadowTintId = Shader.PropertyToID("_ShadowTint");
        private static readonly int HighlightTintId = Shader.PropertyToID("_HighlightTint");

        [SerializeField] private Shader shader;

        [Header("Bloom")]
        [SerializeField, Range(0f, 3f)] private float bloomIntensity = 0.9f;
        [SerializeField, Range(0f, 1.5f)] private float threshold = 0.72f;
        [SerializeField, Range(0.01f, 1f)] private float softKnee = 0.5f;
        [SerializeField, Range(2, MaxIterations)] private int iterations = 4;

        [Header("Grade")]
        [SerializeField, Range(0f, 2f)] private float saturation = 1.08f;
        [SerializeField, Range(0.5f, 1.5f)] private float contrast = 1.06f;
        [SerializeField] private Color shadowTint = new Color(0.9f, 0.95f, 1.08f);
        [SerializeField] private Color highlightTint = new Color(1.06f, 1.0f, 0.93f);
        [SerializeField, Range(0f, 1f)] private float vignette = 0.32f;
        [SerializeField, Range(0f, 0.05f)] private float chromatic = 0.012f;

        private readonly RenderTexture[] levels = new RenderTexture[MaxIterations];
        private Material material;

        private void OnDisable()
        {
            if (material != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(material);
                }
                else
                {
                    DestroyImmediate(material);
                }

                material = null;
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (material == null)
            {
                if (shader == null || !shader.isSupported)
                {
                    Graphics.Blit(source, destination);
                    return;
                }

                material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }

            float knee = threshold * softKnee + 0.00001f;
            material.SetVector(ThresholdId, new Vector4(threshold, knee, knee * 2f, 0.25f / knee));
            material.SetFloat(BloomIntensityId, bloomIntensity);
            material.SetFloat(VignetteId, vignette);
            material.SetFloat(ChromaticId, chromatic);
            material.SetFloat(SaturationId, saturation);
            material.SetFloat(ContrastId, contrast);
            material.SetColor(ShadowTintId, shadowTint);
            material.SetColor(HighlightTintId, highlightTint);

            int width = source.width / 2;
            int height = source.height / 2;
            RenderTextureFormat format = source.format;
            RenderTexture current = RenderTexture.GetTemporary(width, height, 0, format);
            current.filterMode = FilterMode.Bilinear;
            Graphics.Blit(source, current, material, 0);
            levels[0] = current;
            int count = 1;
            for (; count < iterations; count++)
            {
                width /= 2;
                height /= 2;
                if (width < 4 || height < 4)
                {
                    break;
                }

                RenderTexture next = RenderTexture.GetTemporary(width, height, 0, format);
                next.filterMode = FilterMode.Bilinear;
                Graphics.Blit(current, next, material, 1);
                levels[count] = next;
                current = next;
            }

            for (int i = count - 2; i >= 0; i--)
            {
                Graphics.Blit(current, levels[i], material, 2);
                current = levels[i];
            }

            material.SetTexture(BloomTexId, current);
            Graphics.Blit(source, destination, material, 3);

            for (int i = 0; i < count; i++)
            {
                RenderTexture.ReleaseTemporary(levels[i]);
                levels[i] = null;
            }
        }
    }
}
