using UnityEngine;

namespace TuanziArena
{
    [CreateAssetMenu(menuName = "Tuanzi/Impact Feedback Settings", fileName = "ImpactFeedbackSettings")]
    public sealed class ImpactFeedbackSettings : ScriptableObject
    {
        [Header("Impact threshold (relative speed, world units / second)")]
        [Min(0f)] public float minimumImpactSpeed = 2f;
        [Min(0.1f)] public float fullImpactSpeed = 8f;
        [Header("Hit stop")]
        public bool enableHitStop = true;
        [Min(0f)] public float minimumHitStopSpeed = 4f;
        [Range(0f, 0.12f)] public float hitStopSeconds = 0.045f;
        [Range(0f, 0.12f)] public float maximumHitStopSeconds = 0.075f;
        [Header("十字闪光与角色闪白")]
        public bool enableSpark = true;
        public bool enableWhiteFlash = true;
        [Min(0f)] public float minimumVisualSpeed = 0.35f;
        [Range(0.05f, 0.5f)] public float sparkLifetime = 0.18f;
        [Min(0.1f)] public float minimumSparkSize = 0.8f;
        [Min(0.1f)] public float maximumSparkSize = 1.55f;
        public Color sparkColor = new Color(1f, 0.85f, 0.35f);
        [Range(0.02f, 0.3f)] public float whiteFlashSeconds = 0.09f;
        [Range(0f, 1f)] public float whiteFlashIntensity = 1f;
        [Header("Camera shake")]
        public bool enableShake = true;
        [Min(0f)] public float shakeSeconds = 0.14f;
        [Min(0f)] public float minimumShakeDistance = 0.03f;
        [Min(0f)] public float maximumShakeDistance = 0.12f;
        [Min(1f)] public float shakeFrequency = 35f;
        [Header("Feedback only; never throttles collision damage")]
        [Min(0f)] public float feedbackCooldown = 0.12f;
    }
}
