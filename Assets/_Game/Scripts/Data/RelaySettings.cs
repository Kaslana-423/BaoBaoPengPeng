using UnityEngine;

namespace TuanziArena
{
    [CreateAssetMenu(menuName = "Tuanzi/Relay Settings")]
    public sealed class RelaySettings : ScriptableObject
    {
        [Header("触发规则（仅我方行动、我方互撞）")]
        public bool enableRelay = true;
        [Tooltip("碰撞相对速度必须大于此值，单位：世界单位/秒。")]
        [Min(0f)] public float minimumImpactSpeed = 3f;
        [Min(0)] public int maxTriggersPerRound = 3;
        [Min(0.1f)] public float countdownSeconds = 2f;
        [Header("追加推力")]
        [Tooltip("Impulse 冲量；相同质量下越大飞得越快。不消耗额外行动次数。")]
        [Min(0f)] public float impulse = 8f;
        public bool preserveVelocity = true;
        [Tooltip("鼠标太接近中点时忽略点击，避免没有明确方向。")]
        [Min(0.001f)] public float minimumAimDistance = 0.1f;
        [Header("方向箭头")]
        [Min(0.1f)] public float arrowMaxLength = 3f;
        [Min(0.01f)] public float arrowWidth = 0.07f;
        [Min(0.05f)] public float arrowHeadLength = 0.28f;
        public Color arrowColor = new Color(1f, 0.64f, 0.12f, 1f);
    }
}
