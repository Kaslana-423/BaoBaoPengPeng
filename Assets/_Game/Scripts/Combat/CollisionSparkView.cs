using System.Collections.Generic;
using UnityEngine;

namespace TuanziArena
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class CollisionSparkView : MonoBehaviour
    {
        private Mesh mesh;
        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock tint;
        private float age, duration, size, angle;
        private bool meshBuilt;
        private Color meshColor;

        public void Play(Vector2 point, Vector2 direction, float scale, float seconds, Color color, int sequence)
        {
            if (mesh == null)
            {
                mesh = new Mesh { name = "Collision cross spark" };
                GetComponent<MeshFilter>().sharedMesh = mesh;
                meshRenderer = GetComponent<MeshRenderer>();
                tint = new MaterialPropertyBlock();
            }
            if (!meshBuilt || meshColor != color)
            {
                BuildMesh(color); meshColor = color; meshBuilt = true;
            }
            age = 0f; duration = Mathf.Max(0.05f, seconds); size = scale;
            angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 20f + sequence % 3 * 13f;
            transform.position = new Vector3(point.x, point.y, 0f);
            gameObject.SetActive(true);
            Tick(0f);
        }

        public bool Tick(float delta)
        {
            age += delta;
            float p = Mathf.Clamp01(age / duration);
            float expand = p < 0.2f ? Mathf.Lerp(0.45f, 1.1f, p / 0.2f) : Mathf.Lerp(1.1f, 1.35f, (p - 0.2f) / 0.8f);
            transform.localScale = Vector3.one * size * expand;
            transform.rotation = Quaternion.Euler(0f, 0f, angle + p * 8f);
            float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 1f, p));
            tint.SetColor("_Color", new Color(1f, 1f, 1f, alpha));
            meshRenderer.SetPropertyBlock(tint);
            return p < 1f;
        }

        private void BuildMesh(Color gold)
        {
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            // Soft warm halo, fading to transparent at the edge.
            int centre = vertices.Count;
            vertices.Add(Vector3.zero); colors.Add(new Color(gold.r, gold.g, gold.b, 0.7f));
            for (int i = 0; i < 32; i++)
            {
                float a = i * Mathf.PI * 2f / 32f;
                vertices.Add(new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 0.43f);
                colors.Add(new Color(gold.r, gold.g, gold.b, 0f));
            }
            for (int i = 0; i < 32; i++) triangles.AddRange(new[] { centre, centre + 1 + i, centre + 1 + (i + 1) % 32 });
            // Small radial streaks around the main cross.
            for (int i = 0; i < 8; i++)
            {
                float a = (i * 45f + 17f) * Mathf.Deg2Rad;
                Vector2 outward = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Vector2 side = new Vector2(-outward.y, outward.x) * 0.018f;
                Polygon(vertices, colors, triangles, new[] { outward * 0.32f, outward * 0.48f + side,
                    outward * (i % 2 == 0 ? 0.77f : 0.61f), outward * 0.48f - side }, gold);
            }
            Cross(vertices, colors, triangles, 0.86f, 45f, new Color(gold.r, gold.g, gold.b, 0.8f));
            Cross(vertices, colors, triangles, 1.1f, 0f, gold);
            Cross(vertices, colors, triangles, 0.88f, 0f, Color.white);
            mesh.Clear(); mesh.SetVertices(vertices); mesh.SetColors(colors);
            mesh.SetUVs(0, new List<Vector2>(new Vector2[vertices.Count]));
            mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
        }

        private static void Cross(List<Vector3> v, List<Color> c, List<int> t, float scale, float angle, Color tint)
        {
            Vector2[] points = { new Vector2(0f, 0.68f), new Vector2(0.075f, 0.075f), new Vector2(0.5f, 0f),
                new Vector2(0.075f, -0.075f), new Vector2(0f, -0.68f), new Vector2(-0.075f, -0.075f),
                new Vector2(-0.5f, 0f), new Vector2(-0.075f, 0.075f) };
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
            for (int i = 0; i < points.Length; i++) points[i] = rotation * (points[i] * scale);
            Polygon(v, c, t, points, tint);
        }

        private static void Polygon(List<Vector3> v, List<Color> c, List<int> t, Vector2[] points, Color tint)
        {
            int start = v.Count;
            Vector2 centre = Vector2.zero;
            foreach (Vector2 p in points) centre += p;
            v.Add(centre / points.Length); c.Add(tint);
            foreach (Vector2 p in points) { v.Add(p); c.Add(tint); }
            for (int i = 0; i < points.Length; i++) t.AddRange(new[] { start, start + 1 + i, start + 1 + (i + 1) % points.Length });
        }

        private void OnDestroy() { if (mesh != null) Destroy(mesh); }
    }
}
