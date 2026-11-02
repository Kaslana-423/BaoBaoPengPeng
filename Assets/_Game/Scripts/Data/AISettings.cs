using UnityEngine;

namespace TuanziArena
{
    [CreateAssetMenu(menuName = "Tuanzi/AI Settings", fileName = "AISettings")]
    public sealed class AISettings : ScriptableObject
    {
        public AIDifficulty defaultDifficulty = AIDifficulty.Medium;
        [Min(0f)] public float thinkDelay = 0.6f;
        [Range(1f, 60f)] public float easyAimErrorDegrees = 22f;
    }
}
