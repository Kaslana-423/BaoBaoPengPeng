using UnityEngine;
using UnityEngine.EventSystems;

namespace TuanziArena
{
    public sealed class PlayerInputController : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private PrototypeSession session;
        [SerializeField] private AimPreview aimPreview;
        [SerializeField] private LayerMask selectableLayers = ~0;
        [SerializeField, Min(0.01f)] private float maxDragDistance = 2f;
        [SerializeField, Min(0.01f)] private float impulsePerUnit = 5f;
        [SerializeField, Min(0.001f)] private float minDragDistance = 0.15f;

        private UnitActor selected;
        private CircleCollider2D selectedCollider;
        private Vector2 pressPosition;

        public void Configure(Camera camera, PrototypeSession prototypeSession, AimPreview preview)
        {
            worldCamera = camera;
            session = prototypeSession;
            aimPreview = preview;
        }

        private void Awake()
        {
            if (worldCamera == null) worldCamera = Camera.main;
            if (worldCamera != null && session != null && aimPreview != null) return;
            Debug.LogError("PlayerInputController needs World Camera, Session and Aim Preview.", this);
            enabled = false;
        }

        private void Update()
        {
            if (!session.CanSelect || !Application.isFocused)
            {
                CancelAim();
                return;
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) || UnityEngine.Input.GetMouseButtonDown(1))
            {
                CancelAim();
                return;
            }
            if (!TryGetMouseWorld(out Vector2 mouseWorld)) { CancelAim(); return; }

            if (UnityEngine.Input.GetMouseButtonDown(0) && !PointerOverUI())
                BeginAim(mouseWorld);

            if (selected == null) return;
            if (!session.CanSelectUnit(selected)) { CancelAim(); return; }
            Vector2 launchVector = Vector2.ClampMagnitude(selected.Body.position - mouseWorld, maxDragDistance);
            bool dragged = (mouseWorld - pressPosition).sqrMagnitude >= minDragDistance * minDragDistance;
            if (dragged) aimPreview.Show(selectedCollider, launchVector, maxDragDistance);
            else aimPreview.Hide();

            if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                if (dragged && launchVector.magnitude >= minDragDistance &&
                    worldCamera.pixelRect.Contains(UnityEngine.Input.mousePosition) && !PointerOverUI())
                    session.TryLaunch(selected, launchVector * impulsePerUnit);
                CancelAim();
            }
            else if (!UnityEngine.Input.GetMouseButton(0)) CancelAim();
        }

        private void BeginAim(Vector2 mouseWorld)
        {
            CancelAim();
            if (!worldCamera.pixelRect.Contains(UnityEngine.Input.mousePosition)) return;
            Collider2D[] hits = Physics2D.OverlapPointAll(mouseWorld, selectableLayers);
            float bestDistance = float.PositiveInfinity;
            foreach (Collider2D hit in hits)
            {
                UnitActor unit = hit.GetComponentInParent<UnitActor>();
                if (!session.CanSelectUnit(unit)) continue;
                float distance = (unit.Body.position - mouseWorld).sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                selected = unit;
            }
            if (selected == null) return;
            selectedCollider = selected.GetComponent<CircleCollider2D>();
            pressPosition = mouseWorld;
            selected.SetSelected(true);
        }

        private bool TryGetMouseWorld(out Vector2 point)
        {
            Ray ray = worldCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            Plane gamePlane = new Plane(Vector3.forward, Vector3.zero);
            if (gamePlane.Raycast(ray, out float distance))
            {
                point = ray.GetPoint(distance);
                return true;
            }
            point = default;
            return false;
        }

        private static bool PointerOverUI() => EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject();

        private void CancelAim()
        {
            if (selected != null) selected.SetSelected(false);
            selected = null;
            selectedCollider = null;
            if (aimPreview != null) aimPreview.Hide();
        }

        private void OnApplicationFocus(bool focused) { if (!focused) CancelAim(); }
        private void OnDisable() => CancelAim();

        private void OnValidate()
        {
            maxDragDistance = Mathf.Max(0.01f, maxDragDistance);
            minDragDistance = Mathf.Clamp(minDragDistance, 0.001f, maxDragDistance);
            impulsePerUnit = Mathf.Max(0.01f, impulsePerUnit);
        }
    }
}
