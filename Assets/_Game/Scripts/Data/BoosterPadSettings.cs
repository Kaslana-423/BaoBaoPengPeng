using UnityEngine;

namespace TuanziArena
{
    [CreateAssetMenu(menuName = "Tuanzi/Booster Pad Settings", fileName = "BP_BoosterPad")]
    public sealed class BoosterPadSettings : HazardSettings
    {
        [Header("保底出射速度")]
        [Tooltip("无论入射多慢（哪怕几乎静止），出射速度至少这么快。0 = 不保底。\n" +
                 "参考：满力射出的球速大约 6~11（取决于质量），所以 7 约等于“至少给一次正常射门的力度”。")]
        [Min(0f)] public float guaranteedSpeed = 7f;

        [Header("出射速度 = 入射速度 × 倍率 + 附加值，再夹到上下限之间")]
        [Tooltip("1 = 不放大入射速度，只靠附加值和保底速度给推力。")]
        [Min(0f)] public float speedMultiplier = 1.2f;
        [Min(0f)] public float addedSpeed = 2f;
        [Tooltip("0 = 不限速。防止叠加后把球甩出场地。")]
        [Min(0f)] public float maximumSpeed = 22f;

        [Header("方向对齐")]
        [Tooltip("0 = 完全保留入射方向（只加速，不转向）；1 = 强制沿地块朝向飞出。" +
                 "低于 0.5 时反向入射仍会朝反方向飞出（只是变快），一般建议 0.6~1。")]
        [Range(0f, 1f)] public float steer = 0.75f;
    }
}
