using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TuanziArena
{
    // Editable vector-style UI: no texture stretching, suitable for either landscape or portrait.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BattleUIGraphic : MaskableGraphic
    {
        public enum Shape { Panel, LeftRibbon, RightRibbon, Dango, Star }
        [SerializeField] private Shape shape;
        [SerializeField] private float cornerRadius = 20f;
        [SerializeField] private float borderWidth;
        [SerializeField] private Color borderColor = Color.white;

        public void Configure(Shape value, Color tint, float radius = 20f, float border = 0f)
        {
            shape = value;
            color = tint;
            cornerRadius = radius;
            borderWidth = border;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (borderWidth > 0f)
            {
                Fill(mesh, Points(rect), borderColor, rect.center);
                rect = new Rect(rect.x + borderWidth, rect.y + borderWidth,
                    Mathf.Max(0f, rect.width - borderWidth * 2f), Mathf.Max(0f, rect.height - borderWidth * 2f));
            }
            Fill(mesh, Points(rect), color, rect.center);
            if (shape != Shape.Dango) return;
            // Tiny white eyes and a smile identify each filled score marker as a little dango.
            Ellipse(mesh, rect.center + new Vector2(-rect.width * 0.16f, 0f), rect.size * 0.045f, Color.white);
            Ellipse(mesh, rect.center + new Vector2(rect.width * 0.16f, 0f), rect.size * 0.045f, Color.white);
            Ellipse(mesh, rect.center + new Vector2(0f, -rect.height * 0.16f), rect.size * new Vector2(0.10f, 0.04f), Color.white);
        }

        private List<Vector2> Points(Rect r)
        {
            var points = new List<Vector2>(40);
            if (shape == Shape.Star)
            {
                for (int i = 0; i < 10; i++)
                {
                    float angle = (90f + i * 36f) * Mathf.Deg2Rad;
                    float scale = i % 2 == 0 ? 0.5f : 0.23f;
                    points.Add(r.center + Vector2.Scale(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * scale, r.size));
                }
            }
            else if (shape == Shape.LeftRibbon || shape == Shape.RightRibbon)
            {
                float cut = r.height * 0.42f;
                if (shape == Shape.LeftRibbon)
                    points.AddRange(new[] { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax - cut, r.yMin),
                        new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax) });
                else
                    points.AddRange(new[] { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin),
                        new Vector2(r.xMax, r.yMax), new Vector2(r.xMin + cut, r.yMax) });
            }
            else if (shape == Shape.Dango)
            {
                for (int i = 0; i < 40; i++)
                {
                    float angle = i * 2f * Mathf.PI / 40f;
                    Vector2 p = new Vector2(Mathf.Cos(angle) * 0.48f, Mathf.Sin(angle) * 0.37f - 0.07f);
                    if (i == 7 || i == 13) p.y += 0.18f;
                    points.Add(r.center + Vector2.Scale(p, r.size));
                }
            }
            else
            {
                float radius = Mathf.Min(cornerRadius, r.width * 0.5f, r.height * 0.5f);
                Vector2[] corners = { new Vector2(r.xMax - radius, r.yMax - radius), new Vector2(r.xMin + radius, r.yMax - radius),
                    new Vector2(r.xMin + radius, r.yMin + radius), new Vector2(r.xMax - radius, r.yMin + radius) };
                for (int i = 0; i < 4; i++)
                    for (int j = 0; j <= 8; j++)
                    {
                        float a = (i * 90f + j * 90f / 8f) * Mathf.Deg2Rad;
                        points.Add(corners[i] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
                    }
            }
            return points;
        }

        private static void Ellipse(VertexHelper mesh, Vector2 centre, Vector2 radius, Color tint)
        {
            var points = new List<Vector2>(12);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f;
                points.Add(centre + Vector2.Scale(new Vector2(Mathf.Cos(a), Mathf.Sin(a)), radius));
            }
            Fill(mesh, points, tint, centre);
        }

        private static void Fill(VertexHelper mesh, List<Vector2> points, Color tint, Vector2 centre)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(centre, tint, Vector2.zero);
            foreach (Vector2 p in points) mesh.AddVert(p, tint, Vector2.zero);
            for (int i = 0; i < points.Count; i++) mesh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % points.Count);
        }
    }
}
