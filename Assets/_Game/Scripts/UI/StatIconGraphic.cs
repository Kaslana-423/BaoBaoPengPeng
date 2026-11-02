using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TuanziArena
{
    // Code-native UI shapes: no image files or imported fonts are required.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StatIconGraphic : MaskableGraphic
    {
        public enum Shape { Attack, Heart, Badge }
        [SerializeField] private Shape shape;

        public void Configure(Shape value, Color tint)
        {
            shape = value;
            color = tint;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (shape == Shape.Badge)
            {
                Polygon(mesh, RoundedRect(rect, 6f), new Color(0.63f, 0.82f, 0.94f), rect.center);
                Rect inner = new Rect(rect.x + 1.5f, rect.y + 1.5f, rect.width - 3f, rect.height - 3f);
                Polygon(mesh, RoundedRect(inner, 5f), color, inner.center);
                return;
            }
            Vector2[] outline = IconPoints(rect, 1f);
            Polygon(mesh, outline, Color.white, rect.center + Vector2.down * rect.height * 0.1f);
            Polygon(mesh, IconPoints(rect, 0.77f), color, rect.center + Vector2.down * rect.height * 0.1f);
        }

        private Vector2[] IconPoints(Rect rect, float scale)
        {
            if (shape == Shape.Attack)
            {
                Vector2[] bolt = { new Vector2(0.1f, 0.5f), new Vector2(-0.42f, -0.08f),
                    new Vector2(-0.08f, -0.08f), new Vector2(-0.18f, -0.5f),
                    new Vector2(0.44f, 0.12f), new Vector2(0.08f, 0.12f) };
                for (int i = 0; i < bolt.Length; i++)
                    bolt[i] = rect.center + Vector2.Scale(bolt[i] * scale, rect.size);
                return bolt;
            }
            const int count = 48;
            Vector2[] heart = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                float t = i * Mathf.PI * 2f / count;
                float x = 16f * Mathf.Pow(Mathf.Sin(t), 3f);
                float y = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) -
                    2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t);
                heart[i] = rect.center + new Vector2(x / 32f * rect.width, (y + 2.5f) / 29f * rect.height) * scale;
            }
            return heart;
        }

        private static List<Vector2> RoundedRect(Rect rect, float radius)
        {
            List<Vector2> points = new List<Vector2>(28);
            Vector2[] centres = { new Vector2(rect.xMax - radius, rect.yMax - radius),
                new Vector2(rect.xMin + radius, rect.yMax - radius),
                new Vector2(rect.xMin + radius, rect.yMin + radius),
                new Vector2(rect.xMax - radius, rect.yMin + radius) };
            for (int corner = 0; corner < 4; corner++)
                for (int step = 0; step <= 6; step++)
                {
                    float angle = (corner * 90f + step * 15f) * Mathf.Deg2Rad;
                    points.Add(centres[corner] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
                }
            return points;
        }

        private static void Polygon(VertexHelper mesh, IList<Vector2> points, Color32 tint, Vector2 centre)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(centre, tint, Vector2.zero);
            for (int i = 0; i < points.Count; i++) mesh.AddVert(points[i], tint, Vector2.zero);
            for (int i = 0; i < points.Count; i++)
                mesh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % points.Count);
        }
    }
}
