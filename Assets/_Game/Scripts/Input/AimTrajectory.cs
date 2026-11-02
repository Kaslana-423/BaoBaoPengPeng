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
        public float Length => HasBounce
            ? Vector2.Distance(Start, Bounce) + Vector2.Distance(Bounce, End)
            : Vector2.Distance(Start, End);
    }

    // Geometry guide only. Target movement and collision energy are resolved by the actual shot.
    public sealed class AimTrajectory
    {
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(16);
        private const float ContactTolerance = 0.001f;

        public AimPath Trace(CircleCollider2D source, Vector2 direction, float length, int obstacleLayers = ~0)
        {
            Vector2 origin = source.transform.TransformPoint(source.offset);
            AimPath path = new AimPath { Start = origin, Bounce = origin, End = origin };
            if (length <= 0f || direction.sqrMagnitude < 0.0001f) return path;
            direction.Normalize();
            Vector3 scale = source.transform.lossyScale;
            float radius = source.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            PhysicsScene2D scene = source.gameObject.scene.GetPhysicsScene2D();
            ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(obstacleLayers & Physics2D.GetLayerCollisionMask(source.gameObject.layer));

            if (!FindHit(scene, source, origin, radius, direction, length, filter, out RaycastHit2D first, out Vector2 normal))
            {
                path.End = origin + direction * length;
                return path;
            }

            float firstDistance = Mathf.Clamp(first.distance, 0f, length);
            path.Bounce = origin + direction * firstDistance;
            path.End = path.Bounce;
            float remaining = length - firstDistance;
            if (remaining <= ContactTolerance) return path;

            path.HasBounce = true;
            Vector2 reflected = Vector2.Reflect(direction, normal).normalized;
            // Second cast only clips the outgoing leg. There is no second reflection.
            if (FindHit(scene, source, path.Bounce, radius, reflected, remaining, filter,
                out RaycastHit2D second, out _))
            {
                path.End = path.Bounce + reflected * Mathf.Clamp(second.distance, 0f, remaining);
                path.HitSecondObstacle = true;
            }
            else path.End = path.Bounce + reflected * remaining;
            return path;
        }

        private bool FindHit(PhysicsScene2D scene, CircleCollider2D source, Vector2 origin,
            float radius, Vector2 direction, float length, ContactFilter2D filter,
            out RaycastHit2D nearest, out Vector2 nearestNormal)
        {
            hits.Clear();
            scene.CircleCast(origin, radius, direction, length, filter, hits);
            nearest = default;
            nearestNormal = default;
            float nearestDistance = float.PositiveInfinity;
            foreach (RaycastHit2D hit in hits)
            {
                Collider2D obstacle = hit.collider;
                if (obstacle == null || obstacle == source || obstacle.transform.IsChildOf(source.transform) ||
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
    }
}
