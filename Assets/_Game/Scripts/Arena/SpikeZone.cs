using UnityEngine;

namespace TuanziArena
{
    // Fixed damage on entry. The trigger sits on the wall surface, so the wall still bounces the ball
    // and the spike only adds the wound. Damage is credited to the acting side, which is what makes a
    // ball that is knocked into a spike count as a point for the current shooter.
    public sealed class SpikeZone : HazardZone
    {
        private bool warned;

        protected override void OnUnitEnter(UnitActor unit)
        {
            SpikeSettings spike = Settings as SpikeSettings;
            if (spike == null)
            {
                if (!warned && Settings != null)
                {
                    warned = true;
                    Debug.LogWarning("SpikeZone needs SpikeSettings, but got another hazard asset.", this);
                }
                return;
            }
            if (unit == null || spike.damage <= 0) return;
            Vector2 push = unit.Body != null ? unit.Body.position - (Vector2)transform.position : Vector2.up;
            if (push.sqrMagnitude < 0.0001f) push = Vector2.up;
            push.Normalize();
            if (Damage != null) Damage.ApplyHazardDamage(unit, spike.damage, push);
            else unit.TakeDamage(spike.damage, push);
        }
    }
}
