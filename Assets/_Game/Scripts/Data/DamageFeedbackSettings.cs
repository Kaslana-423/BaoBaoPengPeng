using UnityEngine;

namespace TuanziArena
{
    [CreateAssetMenu(menuName = "Tuanzi/Damage Feedback Settings")]
    public sealed class DamageFeedbackSettings : ScriptableObject
    {
        [Header("伤害数字 · 字体与描边")]
        public bool showFloatingNumbers = true;
        public Font numberFont;
        [Range(16, 120)] public int numberFontSize = 58;
        public Color playerDamageColor = new Color(1f, 0.85f, 0.22f);
        public Color enemyDamageColor = new Color(1f, 0.40f, 0.51f);
        [Tooltip("自伤（例如自己撞上尖刺）。既不属于我方战果，也不属于敌方，用独立颜色区分。")]
        public Color hazardDamageColor = new Color(1f, 0.62f, 0.18f);
        [Range(0f, 12f)] public float outlineWidth = 3.5f;
        public Color outlineColor = new Color(0.22f, 0.10f, 0.28f);

        [Header("伤害数字 · 弹跳与上浮（不受时停影响）")]
        [Min(0.1f)] public float numberLifetime = 0.95f;
        [Min(0f)] public float riseDistance = 55f;
        [Min(0f)] public float horizontalDrift = 28f;
        public Vector3 worldOffset = new Vector3(0f, 0.15f, 0f);
        [Range(1f, 2f)] public float popScale = 1.35f;
        [Range(0.1f, 0.9f)] public float fadeStart = 0.55f;
        [Min(0f)] public float numberSeparation = 26f;
        [Range(8, 64)] public int maximumNumbers = 40;

        [Header("本次行动总伤害 · 比分下方")]
        public bool showTotalDamage = true;
        public Font labelFont;
        public Vector2 totalPosition = new Vector2(0f, -199f);
        [Range(0.5f, 2f)] public float totalScale = 1f;
        [Range(16, 80)] public int totalFontSize = 42;
        public Color totalNumberColor = new Color(1f, 0.85f, 0.22f);
        [Range(0f, 8f)] public float totalOutlineWidth = 2f;
        public Color totalOutlineColor = new Color(0.20f, 0.10f, 0.25f);
        public Color panelColor = new Color(0.08f, 0.15f, 0.26f, 0.96f);

        [Header("总伤害 · 闪光背景")]
        public bool enableFlashes = true;
        public Color flashColor = new Color(1f, 0.84f, 0.35f);
        [Range(0.1f, 1.5f)] public float flashDuration = 0.55f;
        [Range(0f, 1f)] public float flashIntensity = 0.7f;
        [Range(0f, 0.4f)] public float totalPunchScale = 0.12f;
        public int Revision { get; private set; }
        private void OnValidate() { Revision++; }
    }
}
