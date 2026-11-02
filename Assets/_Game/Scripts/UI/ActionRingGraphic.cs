using UnityEngine;
using UnityEngine.UI;

namespace TuanziArena
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ActionRingGraphic : MaskableGraphic
    {
        [SerializeField] private float rotationSpeed = 70f;
        [SerializeField, Min(1f)] private float thickness = 3.2f;
        private float angle;

        private void Update()
        {
            if (!Application.isPlaying) return;
            angle = Mathf.Repeat(angle + rotationSpeed * Time.deltaTime, 360f);
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f - 9f;
            if (radius <= thickness) return;
            Arc(mesh, rect.center, radius, 10f, 150f);
            Arc(mesh, rect.center, radius, 190f, 330f);
        }

        private void Arc(VertexHelper mesh, Vector2 centre, float radius, float start, float end)
        {
            const int segments = 48;
            for (int i = 0; i < segments; i++)
            {
                float a = Mathf.Lerp(start, end, i / (float)segments) * Mathf.Deg2Rad;
                float b = Mathf.Lerp(start, end, (i + 1f) / segments) * Mathf.Deg2Rad;
                Vector2 da = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Vector2 db = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                int vertex = mesh.currentVertCount;
                mesh.AddVert(centre + da * (radius - thickness / 2f), color, Vector2.zero);
                mesh.AddVert(centre + da * (radius + thickness / 2f), color, Vector2.zero);
                mesh.AddVert(centre + db * (radius + thickness / 2f), color, Vector2.zero);
                mesh.AddVert(centre + db * (radius - thickness / 2f), color, Vector2.zero);
                mesh.AddTriangle(vertex, vertex + 1, vertex + 2);
                mesh.AddTriangle(vertex, vertex + 2, vertex + 3);
            }
            float radians = end * Mathf.Deg2Rad;
            Vector2 radial = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            Vector2 tangent = new Vector2(-radial.y, radial.x);
            Vector2 tip = centre + radial * radius + tangent * 10f;
            Vector2 tail = centre + radial * radius - tangent * 3f;
            int index = mesh.currentVertCount;
            mesh.AddVert(tip, color, Vector2.zero);
            mesh.AddVert(tail + radial * 7f, color, Vector2.zero);
            mesh.AddVert(tail - radial * 7f, color, Vector2.zero);
            mesh.AddTriangle(index, index + 1, index + 2);
        }
    }
}
