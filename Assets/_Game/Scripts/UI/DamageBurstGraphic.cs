using UnityEngine;
using UnityEngine.UI;

namespace TuanziArena
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DamageBurstGraphic : MaskableGraphic
    {
        private float phase;
        public void SetPhase(float value) { phase = value; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = GetPixelAdjustedRect();
            for (int i = 0; i < 12; i++)
            {
                float angle = (i * 30f + phase * 12f) * Mathf.Deg2Rad;
                float width = 3.5f * Mathf.Deg2Rad;
                int start = mesh.currentVertCount;
                mesh.AddVert(r.center, color, Vector2.zero);
                Color tip = color; tip.a = 0f;
                mesh.AddVert(r.center + new Vector2(Mathf.Cos(angle - width) * r.width * 0.5f,
                    Mathf.Sin(angle - width) * r.height * 0.5f), tip, Vector2.zero);
                mesh.AddVert(r.center + new Vector2(Mathf.Cos(angle + width) * r.width * 0.5f,
                    Mathf.Sin(angle + width) * r.height * 0.5f), tip, Vector2.zero);
                mesh.AddTriangle(start, start + 1, start + 2);
            }
        }
    }
}
