using System.Collections.Generic;
using UnityEngine;

namespace TuanziArena
{
    // Shared trigger plumbing for every scene hazard. The first contact of each entry fires once and
    // later Stay callbacks are ignored until the unit leaves, so a resting ball can never farm the
    // effect. A missed Enter (a very fast crossing) still resolves on its first Stay callback.
    [DisallowMultipleComponent]
    public abstract class HazardZone : MonoBehaviour
    {
        [SerializeField] private HazardSettings settings;
        [SerializeField] private SpriteRenderer bodyRenderer;
        private readonly HashSet<UnitActor> inside = new HashSet<UnitActor>();
        private PrototypeSession session;

        public HazardSettings Settings => settings;
        public PrototypeSession Session => session;
        protected CollisionDamageSystem Damage { get; private set; }

        // Where the hazard would send a unit that arrives travelling along the given direction.
        // Zero means "no direction to show" and the aim preview draws no arrow.
        public virtual Vector2 SteerDirection(Vector2 incoming) => Vector2.zero;

        public void Configure(HazardSettings value, SpriteRenderer body)
        {
            settings = value;
            bodyRenderer = body;
            ApplyTint();
        }

        public void Bind(PrototypeSession battle)
        {
            session = battle;
            Damage = battle != null ? battle.GetComponent<CollisionDamageSystem>() : null;
        }

        private void Awake()
        {
            Collider2D shape = GetComponent<Collider2D>();
            if (shape == null) Debug.LogWarning("HazardZone needs a Collider2D on the same object.", this);
            else shape.isTrigger = true;
            ApplyTint();
        }

        private void OnTriggerEnter2D(Collider2D other) => Touch(other);

        // Fallback for an Enter that was missed while crossing at a very high speed.
        private void OnTriggerStay2D(Collider2D other) => Touch(other);

        private void OnTriggerExit2D(Collider2D other)
        {
            UnitActor unit = other != null ? other.GetComponentInParent<UnitActor>() : null;
            if (unit != null) inside.Remove(unit);
        }

        private void Touch(Collider2D other)
        {
            UnitActor unit = other != null ? other.GetComponentInParent<UnitActor>() : null;
            if (unit == null) return;
            // Presence is tracked even while hazards are dormant, so a ball parked inside when a shot
            // begins does not receive a free effect; only a real crossing counts as an entry.
            if (!inside.Add(unit)) return;
            if (!CanAffect(unit)) return;
            OnUnitEnter(unit);
        }

        protected bool CanAffect(UnitActor unit)
        {
            if (unit == null || settings == null || !unit.CanLaunch) return false;
            // Hazards act only while a shot is being resolved, never while aiming or during a pause.
            if (session != null && !session.IsResolving) return false;
            return unit.Team == UnitTeam.Player ? settings.affectPlayer : settings.affectEnemy;
        }

        protected abstract void OnUnitEnter(UnitActor unit);

        private void ApplyTint()
        {
            if (bodyRenderer != null && settings != null) bodyRenderer.color = settings.tint;
        }
    }
}
