using UnityEngine;

namespace TuanziArena
{
    [CreateAssetMenu(menuName = "Tuanzi/Spike Settings", fileName = "SP_SpikeWall")]
    public sealed class SpikeSettings : HazardSettings
    {
        [Header("每次进入扣一次血量（贴着墙磨不会重复扣）")]
        [Min(0)] public int damage = 8;
    }
}
