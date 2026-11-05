using UnityEngine;

namespace TuanziArena
{
    // Shared tuning for every scene hazard. Adding a new hazard kind means adding a subclass here
    // plus a prefab; nothing in the runtime has to change.
    public abstract class HazardSettings : ScriptableObject
    {
        public string displayName = "Hazard";
        [Tooltip("Used as the placeholder body colour and by the aim preview hint.")]
        public Color tint = new Color(0.25f, 0.85f, 1f, 0.35f);
        public bool affectPlayer = true;
        public bool affectEnemy = true;
    }
}
