using System.Collections;
using UnityEngine;

namespace TuanziArena
{
    [DisallowMultipleComponent]
    public sealed class DeathPresentationController : MonoBehaviour
    {
        [SerializeField] private DeathPresentationSettings settings;
        [SerializeField] private Camera targetCamera;
        private DeathPresentationSettings fallback;
        private ImpactFeedback impact;
        private bool impactWasEnabled, ownsPause, cameraCaptured;
        private float previousTimeScale, normalSize, normalFov;
        private Vector3 normalPosition;
        private UnitDeathVisual currentVisual;
        public bool IsPresenting => ownsPause;
        public UnitActor CurrentUnit { get; private set; }
        public float RespawnDelay => settings != null ? Mathf.Max(0f, settings.respawnDelay) : 0.8f;
        public float RespawnClearance => settings != null ? Mathf.Max(0.02f, settings.respawnClearance) : 0.08f;

        public void BeginBatch()
        {
            if (ownsPause) return;
            if (settings == null) settings = Resources.Load<DeathPresentationSettings>("DeathPresentationSettings");
            if (settings == null)
            {
                fallback = ScriptableObject.CreateInstance<DeathPresentationSettings>();
                fallback.hideFlags = HideFlags.HideAndDontSave; settings = fallback;
            }
            impact = GetComponent<ImpactFeedback>();
            impactWasEnabled = impact != null && impact.enabled;
            // Finish impact shake/short hit stop before recording the normal camera/time state.
            impact?.CancelFeedback();
            if (impact != null) impact.enabled = false;
            previousTimeScale = Time.timeScale;
            if (targetCamera == null) targetCamera = Camera.main;
            cameraCaptured = targetCamera != null;
            if (cameraCaptured)
            {
                normalPosition = targetCamera.transform.position;
                normalSize = targetCamera.orthographicSize; normalFov = targetCamera.fieldOfView;
            }
            ownsPause = true;
            Time.timeScale = 0f;
        }

        public IEnumerator Play(UnitActor unit)
        {
            if (unit == null || unit.IsAlive) yield break;
            CurrentUnit = unit;
            currentVisual = unit.GetComponent<UnitDeathVisual>();
            if (currentVisual == null) currentVisual = unit.gameObject.AddComponent<UnitDeathVisual>();
            currentVisual.Prepare();
            Vector2 direction = unit.LastHitDirection;
            Vector3 focus = new Vector3(unit.Body.position.x, unit.Body.position.y, normalPosition.z);
            bool zoom = settings.enableCloseUp && cameraCaptured && targetCamera != null;
            float zoomSize = normalSize * Mathf.Clamp(settings.zoomRatio, 0.25f, 1f);
            float zoomFov = normalFov * Mathf.Clamp(settings.zoomRatio, 0.25f, 1f);
            if (zoom)
            {
                float duration = Mathf.Max(0.01f, settings.zoomInSeconds);
                for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
                {
                    SetCamera(Vector3.Lerp(normalPosition, focus, Ease(t / duration)),
                        Mathf.Lerp(normalSize, zoomSize, Ease(t / duration)), Mathf.Lerp(normalFov, zoomFov, Ease(t / duration)));
                    yield return null;
                }
                SetCamera(focus, zoomSize, zoomFov);
            }

            float flight = Mathf.Max(0.05f, settings.flySeconds);
            float distance = currentVisual.OffscreenDistance(targetCamera, direction, settings.flyDistance);
            for (float t = 0f; t < flight && currentVisual != null; t += Time.unscaledDeltaTime)
            {
                currentVisual.Animate(t / flight, direction, settings, distance);
                yield return null;
            }
            currentVisual?.Animate(1f, direction, settings, distance);
            currentVisual?.Disappear();

            // Return the camera now; respawn belongs to the end of the whole action.
            float recovery = zoom ? Mathf.Max(0.01f, settings.cameraReturnSeconds) : 0f;
            for (float t = 0f; t < recovery; t += Time.unscaledDeltaTime)
            {
                if (zoom) SetCamera(Vector3.Lerp(focus, normalPosition, Ease(t / recovery)),
                    Mathf.Lerp(zoomSize, normalSize, Ease(t / recovery)), Mathf.Lerp(zoomFov, normalFov, Ease(t / recovery)));
                yield return null;
            }
            RestoreCamera();
            // Keep the body hidden until the respawn owner restores and teleports it together.
            CurrentUnit = null;
            currentVisual = null;
        }

        private static float Ease(float value) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(value));
        private void SetCamera(Vector3 position, float size, float fov)
        {
            if (targetCamera == null) return;
            targetCamera.transform.position = position;
            targetCamera.orthographicSize = size; targetCamera.fieldOfView = fov;
        }
        private void RestoreCamera()
        {
            if (cameraCaptured) SetCamera(normalPosition, normalSize, normalFov);
        }

        public void CancelAndRestore()
        {
            currentVisual?.Restore(); currentVisual = null; CurrentUnit = null;
            FinishBatch();
        }

        public void FinishBatch()
        {
            RestoreCamera(); cameraCaptured = false;
            if (!ownsPause) return;
            if (Time.timeScale == 0f) Time.timeScale = previousTimeScale;
            ownsPause = false;
            if (impact != null) impact.enabled = impactWasEnabled;
        }

        private void OnDisable() => CancelAndRestore();
        private void OnDestroy()
        {
            CancelAndRestore();
            if (fallback != null) Destroy(fallback);
        }
    }
}
