using UnityEngine;

namespace TuanziArena
{
    // Shared configuration only. Current health belongs to each UnitActor instance.
    [CreateAssetMenu(menuName = "Tuanzi/Unit Data", fileName = "UD_Dango")]
    public sealed class UnitData : ScriptableObject
    {
        public string displayName = "Dango";
        [Min(1)] public int maxHealth = 50;
        [Min(0)] public int attackPower = 10;
        [Min(0.01f)] public float mass = 1f;
        [Min(0.01f)] public float linearDamping = 1.5f;
        [Min(0.01f)] public float launchMultiplier = 1f;
        public PhysicsMaterial2D physicsMaterial;
        public Sprite sprite;
        public Color playerColor = new Color(0.29f, 0.83f, 0.76f);
        public Color enemyColor = new Color(1f, 0.48f, 0.57f);
    }
}
