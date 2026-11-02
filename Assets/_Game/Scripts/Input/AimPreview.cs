using UnityEngine;

namespace TuanziArena
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class AimPreview : MonoBehaviour
    {
        [SerializeField] private LineRenderer aimLine;
        [Tooltip("Total length of the incoming and reflected legs at full charge, in world units.")]
        [SerializeField, Min(0.1f)] private float maxPreviewLength = 6f;
        [SerializeField] private LayerMask obstacleLayers = ~0;
        [SerializeField] private Color pathColor = new Color(0.95f, 1f, 1f, 0.95f);
        [SerializeField, Min(0.01f)] private float lineWidth = 0.09f;
        [SerializeField, Min(0.05f)] private float dashSpacing = 0.3f;

        private readonly AimTrajectory trajectory = new AimTrajectory();
        private Texture2D dashTexture;
        private Material runtimeMaterial;
        private Material originalMaterial;

        private void Awake() => Hide();

        public void Show(CircleCollider2D source, Vector2 launchVector, float maxDragDistance)
        {
            if (source == null || launchVector.sqrMagnitude < 0.0001f) { Hide(); return; }
            EnsureRenderer();
            float charge = Mathf.Clamp01(launchVector.magnitude / Mathf.Max(0.01f, maxDragDistance));
            AimPath path = trajectory.Trace(source, launchVector, maxPreviewLength * charge, obstacleLayers);
            Vector2 direction = launchVector.normalized;
            float firstLength = Vector2.Distance(path.Start, path.HasBounce ? path.Bounce : path.End);
            Vector3 scale = source.transform.lossyScale;
            float radius = source.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            // Draw outside the character; collision calculations still start at its centre.
            Vector2 visibleStart = path.Start + direction * Mathf.Min(radius + lineWidth, firstLength);
            aimLine.positionCount = path.HasBounce ? 3 : 2;
            SetPoint(0, visibleStart);
            if (path.HasBounce) SetPoint(1, path.Bounce);
            SetPoint(aimLine.positionCount - 1, path.End);
            Color color = pathColor;
            color.a *= Mathf.Lerp(0.6f, 1f, charge);
            aimLine.startColor = color;
            aimLine.endColor = color;
            aimLine.widthMultiplier = lineWidth;
            aimLine.textureScale = new Vector2(1f / Mathf.Max(0.05f, dashSpacing), 1f);
            aimLine.enabled = path.Length > radius + lineWidth;
        }

        private void SetPoint(int index, Vector2 point) =>
            aimLine.SetPosition(index, new Vector3(point.x, point.y, -0.2f));

        private void EnsureRenderer()
        {
            if (aimLine == null) aimLine = GetComponent<LineRenderer>();
            if (runtimeMaterial != null) return;
            originalMaterial = aimLine.sharedMaterial;
            runtimeMaterial = originalMaterial != null ? new Material(originalMaterial)
                : new Material(Shader.Find("Sprites/Default"));
            runtimeMaterial.hideFlags = HideFlags.HideAndDontSave;
            dashTexture = CreateDashTexture();
            runtimeMaterial.mainTexture = dashTexture;
            aimLine.sharedMaterial = runtimeMaterial;
            aimLine.useWorldSpace = true;
            aimLine.loop = false;
            aimLine.textureMode = LineTextureMode.Tile;
            aimLine.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
            aimLine.numCapVertices = 0;
            aimLine.numCornerVertices = 4;
            aimLine.sortingOrder = 20;
        }

        private static Texture2D CreateDashTexture()
        {
            const int width = 64, height = 16;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "AimPreview_Dashes",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x + 0.5f - width / 2f) - 8f, 0f);
                    float dy = y + 0.5f - height / 2f;
                    float alpha = Mathf.Clamp01(7.5f - Mathf.Sqrt(dx * dx + dy * dy));
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        public void Hide()
        {
            if (aimLine == null) aimLine = GetComponent<LineRenderer>();
            aimLine.enabled = false;
        }

        private void OnDisable() => Hide();

        private void OnDestroy()
        {
            if (aimLine != null && aimLine.sharedMaterial == runtimeMaterial)
                aimLine.sharedMaterial = originalMaterial;
            Release(runtimeMaterial);
            Release(dashTexture);
        }

        private static void Release(Object resource)
        {
            if (resource == null) return;
            if (Application.isPlaying) Destroy(resource); else DestroyImmediate(resource);
        }
    }
}
