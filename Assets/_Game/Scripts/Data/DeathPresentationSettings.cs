using UnityEngine;

namespace TuanziArena
{
    [CreateAssetMenu(menuName = "Tuanzi/Death Presentation Settings")]
    public sealed class DeathPresentationSettings : ScriptableObject
    {
        [Header("死亡特写")]
        public bool enableCloseUp = true;
        [Range(0.25f, 1f)] public float zoomRatio = 0.55f;
        [Min(0.01f)] public float zoomInSeconds = 0.18f;
        [Min(0.01f)] public float cameraReturnSeconds = 0.65f;
        [Header("保持大小，旋转飞出屏幕")]
        [Min(0.05f)] public float flySeconds = 0.42f;
        [Tooltip("最短飞行距离；运行时自动延长到屏幕外。")]
        [Min(0f)] public float flyDistance = 4.5f;
        public float spinDegrees = 900f;
        [Header("全场停止行动后等待，再安全复活")]
        [Min(0f)] public float respawnDelay = 0.8f;
        [Tooltip("复活时与角色、墙壁之间预留的世界距离。")]
        [Min(0.02f)] public float respawnClearance = 0.08f;
    }
}
