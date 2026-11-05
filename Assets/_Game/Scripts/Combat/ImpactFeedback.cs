using UnityEngine;

namespace TuanziArena
{
    [DisallowMultipleComponent]
    public sealed class ImpactFeedback : MonoBehaviour
    {
        [SerializeField] private ImpactFeedbackSettings settings;
        [SerializeField] private Camera targetCamera;
        private ImpactFeedbackSettings fallbackSettings;
        private bool ownsHitStop;
        private float previousTimeScale;
        private double stopUntil;
        private double nextShakeTime;
        private double nextHitStopTime;
        private ImpactVisuals visuals;
        private float shakeRemaining;
        private float shakeDuration;
        private float shakeDistance;
        private Vector2 impactDirection;
        private Vector3 cameraOffset;

        public bool IsHitStopped => ownsHitStop;
        public bool IsShaking => shakeRemaining > 0f;
        public int TriggerCount { get; private set; }

        private void Awake() => LoadSettings();

        public void PlayCollision(UnitActor first, UnitActor second, float relativeSpeed, Vector2 direction, Vector2 point)
        {
            if (!Application.isPlaying || !isActiveAndEnabled || (Time.timeScale <= 0f && !ownsHitStop)) return;
            LoadSettings();
            if (visuals == null)
            {
                visuals = GetComponent<ImpactVisuals>();
                if (visuals == null) visuals = gameObject.AddComponent<ImpactVisuals>();
            }
            visuals.Play(first, second, point, direction, relativeSpeed, settings);
            TryPlay(relativeSpeed, direction);
        }

        private void LoadSettings()
        {
            if (settings != null) return;
            settings = Resources.Load<ImpactFeedbackSettings>("ImpactFeedbackSettings");
            if (settings != null) return;
            fallbackSettings = ScriptableObject.CreateInstance<ImpactFeedbackSettings>();
            fallbackSettings.hideFlags = HideFlags.HideAndDontSave;
            settings = fallbackSettings;
        }

        public bool TryPlay(float relativeSpeed, Vector2 direction)
        {
            if (!Application.isPlaying || !isActiveAndEnabled || Time.timeScale <= 0f) return false;
            LoadSettings();
            double now = Time.realtimeSinceStartupAsDouble;
            bool shake = settings.enableShake && settings.shakeSeconds > 0f &&
                relativeSpeed >= settings.minimumImpactSpeed && now >= nextShakeTime;
            bool stop = settings.enableHitStop && settings.hitStopSeconds > 0f &&
                relativeSpeed >= settings.minimumHitStopSpeed && now >= nextHitStopTime;
            if (!shake && !stop) return false;
            float strength = Mathf.InverseLerp(settings.minimumImpactSpeed,
                Mathf.Max(settings.minimumImpactSpeed + 0.01f, settings.fullImpactSpeed), relativeSpeed);
            TriggerCount++;

            if (shake)
            {
                nextShakeTime = now + Mathf.Max(0f, settings.feedbackCooldown);
                if (targetCamera == null) targetCamera = Camera.main;
                if (targetCamera != null)
                {
                    shakeDuration = Mathf.Max(0.01f, settings.shakeSeconds);
                    shakeRemaining = shakeDuration;
                    shakeDistance = Mathf.Lerp(settings.minimumShakeDistance, settings.maximumShakeDistance, strength);
                    impactDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
                }
            }
            if (stop)
            {
                nextHitStopTime = now + Mathf.Max(0f, settings.feedbackCooldown);
                previousTimeScale = Time.timeScale;
                float hitStrength = Mathf.InverseLerp(settings.minimumHitStopSpeed,
                    Mathf.Max(settings.minimumHitStopSpeed + 0.01f, settings.fullImpactSpeed), relativeSpeed);
                stopUntil = now + Mathf.Clamp(Mathf.Lerp(settings.hitStopSeconds,
                    Mathf.Max(settings.hitStopSeconds, settings.maximumHitStopSeconds), hitStrength), 0f, 0.12f);
                ownsHitStop = true;
                Time.timeScale = 0f;
            }
            return true;
        }

        private void Update()
        {
            if (ownsHitStop && Time.realtimeSinceStartupAsDouble >= stopUntil) RestoreTime();
        }

        private void LateUpdate()
        {
            RemoveCameraOffset();
            if (shakeRemaining <= 0f || targetCamera == null) return;
            shakeRemaining = Mathf.Max(0f, shakeRemaining - Time.unscaledDeltaTime);
            float envelope = Mathf.Pow(shakeRemaining / shakeDuration, 2f);
            float phase = Time.unscaledTime * settings.shakeFrequency;
            Vector2 noise = new Vector2(Mathf.PerlinNoise(phase, 0.31f) * 2f - 1f,
                Mathf.PerlinNoise(0.73f, phase) * 2f - 1f);
            Vector2 offset = (impactDirection * Mathf.Cos(phase * 2f) * 0.6f + noise * 0.4f) *
                shakeDistance * envelope;
            cameraOffset = new Vector3(offset.x, offset.y, 0f);
            targetCamera.transform.localPosition += cameraOffset;
        }

        private void RestoreTime()
        {
            if (!ownsHitStop) return;
            // Preserve a nonzero time scale assigned by another system while the stop was active.
            if (Time.timeScale == 0f) Time.timeScale = previousTimeScale;
            ownsHitStop = false;
        }

        private void RemoveCameraOffset()
        {
            if (targetCamera != null) targetCamera.transform.localPosition -= cameraOffset;
            cameraOffset = Vector3.zero;
        }

        public void CancelFeedback()
        {
            RestoreTime();
            RemoveCameraOffset();
            shakeRemaining = 0f;
            visuals?.Clear();
        }

        private void OnDisable() => CancelFeedback();
        private void OnApplicationFocus(bool focused) { if (!focused) CancelFeedback(); }
        private void OnApplicationPause(bool paused) { if (paused) CancelFeedback(); }
        private void OnDestroy()
        {
            CancelFeedback();
            if (fallbackSettings == null) return;
            if (Application.isPlaying) Destroy(fallbackSettings); else DestroyImmediate(fallbackSettings);
        }
    }
}
