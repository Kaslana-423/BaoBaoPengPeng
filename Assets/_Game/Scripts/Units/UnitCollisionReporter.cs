using UnityEngine;

namespace TuanziArena
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnitActor))]
    public sealed class UnitCollisionReporter : MonoBehaviour
    {
        private UnitActor actor;
        private CollisionDamageSystem damageSystem;

        public void Bind(CollisionDamageSystem system)
        {
            actor = GetComponent<UnitActor>();
            damageSystem = system;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (damageSystem == null || actor == null) return;
            UnitActor other = collision.collider.GetComponentInParent<UnitActor>();
            if (other == null || actor.GetInstanceID() < other.GetInstanceID())
            {
                Vector2 direction = collision.contactCount > 0 ? -collision.GetContact(0).normal
                    : collision.relativeVelocity.normalized;
                Vector2 point = collision.contactCount > 0 ? collision.GetContact(0).point : actor.Body.position;
                damageSystem.ReportImpact(actor, other, collision.relativeVelocity.magnitude, direction, point);
            }
            ReportContact(collision);
        }

        // A new action can begin while two units are already touching. The damage system
        // handles that first contact once, then ignores further Stay callbacks until separation.
        private void OnCollisionStay2D(Collision2D collision) => ReportContact(collision);

        private void ReportContact(Collision2D collision)
        {
            if (damageSystem == null || actor == null) return;
            UnitActor other = collision.collider.GetComponentInParent<UnitActor>();
            if (other != null && actor.GetInstanceID() < other.GetInstanceID())
            {
                Vector2 direction = collision.contactCount > 0 ? -collision.GetContact(0).normal
                    : other.Body.position - actor.Body.position;
                damageSystem.TryResolve(actor, other, collision.relativeVelocity.magnitude, direction);
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (damageSystem == null || actor == null) return;
            UnitActor other = collision.collider.GetComponentInParent<UnitActor>();
            // Only one reporter owns each pair's Enter/Stay/Exit stream.
            if (other != null && actor.GetInstanceID() < other.GetInstanceID())
                damageSystem.EndContact(actor, other);
        }
    }
}
