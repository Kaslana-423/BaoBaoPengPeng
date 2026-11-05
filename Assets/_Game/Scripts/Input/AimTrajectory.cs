using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TuanziArena
{
    public struct AimPath
    {
        public Vector2 Start;
        public Vector2 Bounce;
        public Vector2 End;
        public bool HasBounce;
        public bool HitSecondObstacle;
        // Set when the guide was cut short by a scene hazard instead of solid geometry.
        public bool StoppedByHazard;
        public Vector2 HazardDirection;
        public float Length => HasBounce
            ? Vector2.Distance(Start, Bounce) + Vector2.Distance(Bounce, End)
            : Vector2.Distance(Start, End);
    }

    // Frictionless two-body impulse model. The outgoing leg uses the mass ratio and the bounciness of
    // the first obstruction, so a ball deflects like a rigid body instead of like an immovable wall.
    // Static geometry (r = 0) keeps the classic -e reflection without a separate code path.
    // Target motion and chain reactions after the first impact remain unpredictable by design.
    public sealed class AimTrajectory
    {
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(16);
        private const float ContactTolerance = 0.001f;
        private const float StoppedSpeedRatio = 0.02f;

        public AimPath Trace(CircleCollider2D source, Vector2 direction, float length, int obstacleLayers = ~0)
        {
            Vector2 origin = source.transform.TransformPoint(source.offset);
            AimPath path = new AimPath { Start = origin, Bounce = origin, End = origin };
            if (length <= 0f || direction.sqrMagnitude < 0.0001f) return path;
            direction.Normalize();
            Vector3 scale = source.transform.lossyScale;
            float radius = source.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            PhysicsScene2D scene = source.gameObject.scene.GetPhysicsScene2D();
            int layerMask = obstacleLayers & Physics2D.GetLayerCollisionMask(source.gameObject.layer);
            ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(layerMask);
            // Hazards are triggers, so they need a second pass with triggers enabled.
            ContactFilter2D triggers = new ContactFilter2D { useTriggers = true };
            triggers.SetLayerMask(layerMask);

            if (!FindHit(scene, source, origin, radius, direction, length, filter, out RaycastHit2D first, out Vector2 normal))
            {
                path.End = origin + direction * length;
                if (FindHazard(scene, source, origin, radius, direction, length, triggers,
                    out Vector2 hazardPoint, out Vector2 hazardArrow))
                {
                    path.End = hazardPoint;
                    path.StoppedByHazard = true;
                    path.HazardDirection = hazardArrow;
                }
                return path;
            }

            float firstDistance = Mathf.Clamp(first.distance, 0f, length);
            path.Bounce = origin + direction * firstDistance;
            path.End = path.Bounce;
            // A hazard before the first solid hit cuts the guide short; the preview draws an arrow there.
            if (FindHazard(scene, source, origin, radius, direction, firstDistance, triggers,
                out Vector2 shortPoint, out Vector2 shortArrow))
            {
                path.End = shortPoint;
                path.StoppedByHazard = true;
                path.HazardDirection = shortArrow;
                return path;
            }
            float remaining = length - firstDistance;
            if (remaining <= ContactTolerance) return path;

            path.HasBounce = true;
            // The magnitude of the impulse response is the shooter's speed ratio after the impact.
            Vector2 outgoing = OutgoingVelocity(source, first.collider, direction, normal);
            float speedRatio = outgoing.magnitude;
            // Equal mass with full restitution stops the shooter dead: keep the first leg only.
            if (speedRatio <= StoppedSpeedRatio) return path;
            outgoing /= speedRatio;
            // A slower shooter cannot cover the whole budget, so the preview shortens with the speed.
            float travel = remaining * Mathf.Clamp01(speedRatio);
            if (travel <= ContactTolerance) return path;
            if (FindHazard(scene, source, path.Bounce, radius, outgoing, travel, triggers,
                out Vector2 bouncePoint, out Vector2 bounceArrow))
            {
                path.End = bouncePoint;
                path.StoppedByHazard = true;
                path.HazardDirection = bounceArrow;
                return path;
            }
            // The struck body is skipped: it leaves faster than the shooter, so it cannot be hit twice.
            // Second cast only clips the outgoing leg. There is no second reflection.
            if (FindHit(scene, source, path.Bounce, radius, outgoing, travel, filter,
                out RaycastHit2D second, out _, first.collider))
            {
                path.End = path.Bounce + outgoing * Mathf.Clamp(second.distance, 0f, travel);
                path.HitSecondObstacle = true;
            }
            else path.End = path.Bounce + outgoing * travel;
            return path;
        }

        // Impulse response of one frictionless circle impact, projected onto the incoming unit speed.
        // k = 1 - (1 + e) / (1 + r): r = 0 reproduces a wall (-e), r = 1 with e = 1 removes all normal motion.
        private static Vector2 OutgoingVelocity(CircleCollider2D source, Collider2D target,
            Vector2 direction, Vector2 normal)
        {
            float approach = Vector2.Dot(direction, normal);  // Negative while the pair is closing in.
            Vector2 tangent = direction - normal * approach;  // Friction is zero, so this survives intact.
            float massRatio = MassRatio(source, target);
            float bounce = CombinedBounciness(source.sharedMaterial, target.sharedMaterial);
            float normalScale = 1f - (1f + bounce) / (1f + massRatio);
            return tangent + normal * (approach * normalScale);
        }

        private static float MassRatio(CircleCollider2D source, Collider2D target)
        {
            Rigidbody2D targetBody = target.attachedRigidbody;
            // Static and kinematic bodies behave as infinite mass, which is the r = 0 wall case.
            if (targetBody == null || targetBody.bodyType != RigidbodyType2D.Dynamic) return 0f;
            Rigidbody2D ownBody = source.attachedRigidbody;
            float ownMass = ownBody != null ? Mathf.Max(0.0001f, ownBody.mass) : 1f;
            return ownMass / Mathf.Max(0.0001f, targetBody.mass);
        }

        // Unity 2D combines restitution by taking the larger of the two materials.
        // A collider without an assigned material uses the default one, whose bounciness is zero.
        private static float CombinedBounciness(PhysicsMaterial2D first, PhysicsMaterial2D second)
        {
            float a = first != null ? first.bounciness : 0f;
            float b = second != null ? second.bounciness : 0f;
            return Mathf.Clamp01(Mathf.Max(a, b));
        }

        private bool FindHit(PhysicsScene2D scene, CircleCollider2D source, Vector2 origin,
            float radius, Vector2 direction, float length, ContactFilter2D filter,
            out RaycastHit2D nearest, out Vector2 nearestNormal, Collider2D ignored = null)
        {
            hits.Clear();
            scene.CircleCast(origin, radius, direction, length, filter, hits);
            nearest = default;
            nearestNormal = default;
            float nearestDistance = float.PositiveInfinity;
            foreach (RaycastHit2D hit in hits)
            {
                Collider2D obstacle = hit.collider;
                if (obstacle == null || obstacle == source || obstacle == ignored ||
                    obstacle.transform.IsChildOf(source.transform) ||
                    (source.attachedRigidbody != null && obstacle.attachedRigidbody == source.attachedRigidbody) ||
                    Physics2D.GetIgnoreCollision(source, obstacle)) continue;

                Vector2 normal = hit.normal;
                if (hit.distance <= ContactTolerance)
                {
                    // Recover the real normal for a cast starting in contact, allowing it to leave the surface.
                    Vector2 outward = origin - obstacle.ClosestPoint(origin);
                    if (outward.sqrMagnitude > 0.000001f) normal = outward.normalized;
                }
                if (Vector2.Dot(direction, normal) >= -0.00001f || hit.distance >= nearestDistance) continue;
                nearest = hit;
                nearestNormal = normal;
                nearestDistance = hit.distance;
            }
            return nearest.collider != null;
        }

        // The solid cast never reports triggers, so hazards get their own pass along the same leg.
        // Returns the first hazard the guide would enter and the direction it pushes toward.
        private bool FindHazard(PhysicsScene2D scene, CircleCollider2D source, Vector2 origin, float radius,
            Vector2 direction, float length, ContactFilter2D filter, out Vector2 point, out Vector2 indicator)
        {
            point = default;
            indicator = default;
            if (length <= ContactTolerance) return false;
            hits.Clear();
            scene.CircleCast(origin, radius, direction, length, filter, hits);
            HazardZone nearest = null;
            float nearestDistance = float.PositiveInfinity;
            float nearestTravel = 0f;
            foreach (RaycastHit2D hit in hits)
            {
                Collider2D obstacle = hit.collider;
                // A cast that starts inside the zone means the unit is already in it; there is no entry.
                if (obstacle == null || hit.distance <= ContactTolerance ||
                    hit.distance >= nearestDistance || obstacle.transform.IsChildOf(source.transform)) continue;
                HazardZone hazard = obstacle.GetComponentInParent<HazardZone>();
                if (hazard == null) continue;
                nearest = hazard;
                nearestDistance = hit.distance;
                nearestTravel = Mathf.Clamp(hit.distance, 0f, length);
            }
            if (nearest == null) return false;
            point = origin + direction * nearestTravel;
            indicator = nearest.SteerDirection(direction);
            return true;
        }
    }
}
