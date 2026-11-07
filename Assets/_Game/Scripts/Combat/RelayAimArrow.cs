using UnityEngine;

namespace TuanziArena
{
    public sealed class RelayAimArrow : MonoBehaviour
    {
        private LineRenderer shaft, head;
        private Material material;
        public bool IsVisible => shaft != null && shaft.enabled;

        public void Show(Vector2 origin, Vector2 target, RelaySettings settings)
        {
            Vector2 delta = target - origin;
            if (delta.magnitude < settings.minimumAimDistance) { Hide(); return; }
            if (shaft == null)
            {
                material = new Material(Shader.Find("Sprites/Default"));
                shaft = CreateLine("RelayDirection", 2); head = CreateLine("RelayArrowHead", 3);
            }
            Vector2 direction = delta.normalized;
            float length = Mathf.Min(delta.magnitude, Mathf.Max(0.1f, settings.arrowMaxLength));
            float tipLength = Mathf.Min(settings.arrowHeadLength, length * 0.45f);
            Vector2 tip = origin + direction * length;
            Vector2 side = new Vector2(-direction.y, direction.x) * tipLength * 0.6f;
            shaft.enabled = head.enabled = true;
            shaft.startWidth = shaft.endWidth = head.startWidth = head.endWidth = settings.arrowWidth;
            shaft.startColor = shaft.endColor = head.startColor = head.endColor = settings.arrowColor;
            shaft.SetPosition(0, origin); shaft.SetPosition(1, tip);
            head.SetPosition(0, tip - direction * tipLength + side); head.SetPosition(1, tip);
            head.SetPosition(2, tip - direction * tipLength - side);
        }

        private LineRenderer CreateLine(string name, int count)
        {
            var child = new GameObject(name); child.transform.SetParent(transform, false);
            LineRenderer line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = true; line.positionCount = count; line.sharedMaterial = material;
            line.sortingOrder = 150; line.numCapVertices = 4; line.numCornerVertices = 4;
            return line;
        }
        public void Hide() { if (shaft != null) shaft.enabled = false; if (head != null) head.enabled = false; }
        private void OnDestroy()
        {
            if (shaft != null) Destroy(shaft.gameObject);
            if (head != null) Destroy(head.gameObject);
            if (material != null) Destroy(material);
        }
    }
}
