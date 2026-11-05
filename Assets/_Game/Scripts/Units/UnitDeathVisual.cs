using UnityEngine;

namespace TuanziArena
{
    // Animate only the body visual (including its eyes), never the physics root or spawn point.
    [DisallowMultipleComponent, RequireComponent(typeof(UnitActor))]
    public sealed class UnitDeathVisual : MonoBehaviour
    {
        private Transform body;
        private Vector3 localPosition, localScale, startPosition;
        private Quaternion localRotation, startRotation;
        private bool wasActive, prepared;
        private Canvas[] canvases;
        private bool[] canvasStates;
        public bool IsPrepared => prepared;
        public bool HasDisappeared { get; private set; }

        public void Prepare()
        {
            if (prepared) return;
            UnitActor owner = GetComponent<UnitActor>();
            body = owner.BodyVisual != null ? owner.BodyVisual.transform : null;
            if (body != null)
            {
                localPosition = body.localPosition; localScale = body.localScale; localRotation = body.localRotation;
                startPosition = body.position; startRotation = body.rotation;
                wasActive = body.gameObject.activeSelf;
            }
            canvases = GetComponentsInChildren<Canvas>(true);
            canvasStates = new bool[canvases.Length];
            for (int i = 0; i < canvases.Length; i++) { canvasStates[i] = canvases[i].enabled; canvases[i].enabled = false; }
            HasDisappeared = false;
            prepared = true;
        }

        public float OffscreenDistance(Camera camera, Vector2 direction, float minimum)
        {
            if (body == null || camera == null) return minimum;
            // Project the ray onto the viewport. Padding covers every rotation of the whole body.
            float radius = 0f;
            foreach (SpriteRenderer sprite in body.GetComponentsInChildren<SpriteRenderer>())
                radius = Mathf.Max(radius, Vector3.Distance(startPosition, sprite.bounds.center) + sprite.bounds.extents.magnitude);
            Vector3 origin = camera.WorldToViewportPoint(startPosition);
            Vector3 step = camera.WorldToViewportPoint(startPosition + (Vector3)direction) - origin;
            Vector3 padX = camera.WorldToViewportPoint(startPosition + camera.transform.right * (radius + 0.3f)) - origin;
            Vector3 padY = camera.WorldToViewportPoint(startPosition + camera.transform.up * (radius + 0.3f)) - origin;
            float x = Mathf.Abs(step.x) > 0.00001f ? ((step.x > 0f ? 1f + Mathf.Abs(padX.x) : -Mathf.Abs(padX.x)) - origin.x) / step.x : float.PositiveInfinity;
            float y = Mathf.Abs(step.y) > 0.00001f ? ((step.y > 0f ? 1f + Mathf.Abs(padY.y) : -Mathf.Abs(padY.y)) - origin.y) / step.y : float.PositiveInfinity;
            float distance = Mathf.Min(x, y);
            return float.IsInfinity(distance) ? minimum : Mathf.Max(minimum, distance);
        }

        public void Animate(float progress, Vector2 direction, DeathPresentationSettings settings, float distance = -1f)
        {
            if (!prepared || body == null) return;
            float p = Mathf.Clamp01(progress);
            float travel = 1f - (1f - p) * (1f - p);
            body.position = startPosition + (Vector3)direction * ((distance < 0f ? settings.flyDistance : distance) * travel);
            float spinSign = direction.x < 0f ? 1f : -1f;
            body.rotation = startRotation * Quaternion.Euler(0f, 0f, settings.spinDegrees * spinSign * p);
            body.localScale = localScale;
        }

        public void Disappear()
        {
            HasDisappeared = true;
            if (body != null) body.gameObject.SetActive(false);
        }

        public void Restore()
        {
            if (!prepared) return;
            if (body != null)
            {
                body.localPosition = localPosition; body.localScale = localScale; body.localRotation = localRotation;
                body.gameObject.SetActive(wasActive);
            }
            for (int i = 0; i < canvases.Length; i++) if (canvases[i] != null) canvases[i].enabled = canvasStates[i];
            HasDisappeared = false;
            prepared = false;
        }

        private void OnDestroy() => Restore();
    }
}
