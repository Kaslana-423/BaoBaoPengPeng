using UnityEngine;

namespace TuanziArena
{
    // A physics-test input gate, deliberately not a turn or combat manager.
    public sealed class PrototypeSession : MonoBehaviour
    {
        [SerializeField] private UnitActor[] units;
        [SerializeField, Min(0.01f)] private float settledSpeed = 0.2f;
        [SerializeField, Min(0.01f)] private float settledDuration = 0.5f;
        [SerializeField, Min(1f)] private float maximumResolveTime = 8f;
        [SerializeField] private CollisionDamageSystem damageSystem;

        private float quietTime;
        private float elapsed;
        public bool IsResolving { get; private set; }
        public bool CanSelect => !IsResolving && Time.timeScale > 0f && isActiveAndEnabled;

        public UnitActor ActiveShooter { get; private set; }
        public UnitTeam? ActiveTeam { get; private set; }
        public int ActionId { get; private set; }

        private void Awake() => BindCombat();

        public void Configure(UnitActor[] sceneUnits)
        {
            units = sceneUnits;
            if (Application.isPlaying) BindCombat();
        }

        private void BindCombat()
        {
            if (damageSystem == null) damageSystem = GetComponent<CollisionDamageSystem>();
            if (damageSystem == null) damageSystem = gameObject.AddComponent<CollisionDamageSystem>();
            damageSystem.Configure(this, units);
        }

        public bool CanSelectUnit(UnitActor unit)
        {
            return CanSelect && unit != null && unit.Team == UnitTeam.Player &&
                unit.CanLaunch && units != null && System.Array.IndexOf(units, unit) >= 0;
        }

        public bool TryLaunch(UnitActor unit, Vector2 impulse)
        {
            return TryLaunchForTeam(unit, impulse, UnitTeam.Player);
        }

        // Shared action entry for either faction. Mouse selection remains restricted to Player.
        public bool TryLaunchForTeam(UnitActor unit, Vector2 impulse, UnitTeam actingTeam)
        {
            if (!CanSelect || unit == null || unit.Team != actingTeam || !unit.CanLaunch ||
                units == null || System.Array.IndexOf(units, unit) < 0 || !unit.TryLaunch(impulse)) return false;
            quietTime = 0f;
            elapsed = 0f;
            ActiveShooter = unit;
            ActiveTeam = actingTeam;
            ActionId++;
            IsResolving = true;
            return true;
        }

        private void FixedUpdate()
        {
            if (!IsResolving) return;
            elapsed += Time.fixedDeltaTime;
            float highestSpeedSquared = 0f;
            foreach (UnitActor unit in units)
                if (unit != null && unit.isActiveAndEnabled && unit.Body.simulated)
                    highestSpeedSquared = Mathf.Max(highestSpeedSquared, unit.SpeedSquared);

            quietTime = highestSpeedSquared <= settledSpeed * settledSpeed
                ? quietTime + Time.fixedDeltaTime : 0f;
            // The timeout may only discard small residual motion, never a fast shot.
            if (quietTime < settledDuration &&
                !(elapsed >= maximumResolveTime && highestSpeedSquared <= 0.16f)) return;

            foreach (UnitActor unit in units)
                if (unit != null && unit.isActiveAndEnabled) unit.StopMotion();
            IsResolving = false;
            ActiveShooter = null;
            ActiveTeam = null;
        }
    }
}
