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
        [Tooltip("Colour and length of the boost hint drawn where a hazard cuts the guide short.")]
        [SerializeField] private Color hazardColor = new Color(0.4f, 0.97f, 1f, 0.95f);
        [SerializeField, Min(0.05f)] private float hazardArrowSize = 0.34f;
        [Tooltip("Fixed length of the hint's stem, so the boosted direction is readable at any charge.")]
        [SerializeField, Min(0.1f)] private float hazardArrowLength = 0.9f;

        private readonly AimTrajectory trajectory = new AimTrajectory();
        private Texture2D dashTexture;
        private Material runtimeMaterial;
        private Material originalMaterial;
        private LineRenderer hazardArrow;
        private LineRenderer hazardTail;

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
            ShowHazardArrow(path);
        }

        // Hazards cut the guide short, so the hint continues from the entry point along the exact exit
        // direction with a fixed-length stem and a chevron head.
        private void ShowHazardArrow(AimPath path)
        {
            bool show = path.StoppedByHazard && path.HazardDirection.sqrMagnitude > 0.0001f &&
                hazardArrow != null && hazardTail != null;
            if (!show)
            {
                if (hazardArrow != null) hazardArrow.enabled = false;
                if (hazardTail != null) hazardTail.enabled = false;
                return;
            }
            Vector2 direction = path.HazardDirection.normalized;
            Vector2 tip = path.End + direction * hazardArrowLength;
            Vector2 back = tip - direction * hazardArrowSize * 0.85f;
            Vector2 side = new Vector2(-direction.y, direction.x) * hazardArrowSize * 0.5f;

            hazardTail.positionCount = 2;
            hazardTail.SetPosition(0, new Vector3(path.End.x, path.End.y, -0.3f));
            hazardTail.SetPosition(1, new Vector3(tip.x, tip.y, -0.3f));
            hazardTail.textureScale = new Vector2(1f / Mathf.Max(0.05f, dashSpacing), 1f);
            hazardTail.startColor = hazardColor;
            hazardTail.endColor = hazardColor;
            hazardTail.widthMultiplier = lineWidth;
            hazardTail.enabled = true;

            hazardArrow.positionCount = 3;
            hazardArrow.SetPosition(0, new Vector3(back.x + side.x, back.y + side.y, -0.31f));
            hazardArrow.SetPosition(1, new Vector3(tip.x, tip.y, -0.31f));
            hazardArrow.SetPosition(2, new Vector3(back.x - side.x, back.y - side.y, -0.31f));
            hazardArrow.textureScale = new Vector2(4f, 1f);
            hazardArrow.startColor = hazardColor;
            hazardArrow.endColor = hazardColor;
            hazardArrow.widthMultiplier = lineWidth;
            hazardArrow.enabled = true;
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
            EnsureHazardArrow();
        }

        private void EnsureHazardArrow()
        {
            hazardArrow = EnsureHazardLine("HazardArrow", 22);
            hazardTail = EnsureHazardLine("HazardTail", 21);
        }

        private LineRenderer EnsureHazardLine(string name, int sortingOrder)
        {
            Transform existing = transform.Find(name);
            LineRenderer line = existing != null ? existing.GetComponent<LineRenderer>() : null;
            if (line == null)
            {
                GameObject host = new GameObject(name, typeof(LineRenderer));
                host.transform.SetParent(transform, false);
                line = host.GetComponent<LineRenderer>();
            }
            line.sharedMaterial = runtimeMaterial;
            line.useWorldSpace = true;
            line.loop = false;
            line.textureMode = LineTextureMode.Tile;
            line.numCapVertices = 0;
            line.numCornerVertices = 2;
            line.sortingOrder = sortingOrder;
            line.enabled = false;
            return line;
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
            if (hazardArrow != null) hazardArrow.enabled = false;
            if (hazardTail != null) hazardTail.enabled = false;
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
