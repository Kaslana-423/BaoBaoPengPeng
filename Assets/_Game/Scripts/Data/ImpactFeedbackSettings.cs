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
        [Range(0f, 0.12f)] public float hitStopSeconds = 0.045f;
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
