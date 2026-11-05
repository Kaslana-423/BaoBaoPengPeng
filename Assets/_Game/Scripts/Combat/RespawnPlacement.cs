using UnityEngine;
using UnityEngine.SceneManagement;

namespace TuanziArena
{
    // Search outward from the original spawn, keeping the entire circle clear of solid objects.
    public static class RespawnPlacement
    {
        private static Collider2D[] overlaps = new Collider2D[32];
        private static RaycastHit2D[] crossings = new RaycastHit2D[32];

        public static bool TryFind(UnitActor unit, float clearance, out Vector2 position)
        {
            Physics2D.SyncTransforms();
            CircleCollider2D shape = unit.GetComponent<CircleCollider2D>();
            Vector3 scale = shape.transform.lossyScale;
            float radius = shape.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            // RespawnAt resets world rotation. Keep the collider's local offset at that pose.
            Vector2 offset = new Vector2(shape.offset.x * scale.x, shape.offset.y * scale.y);
            Vector2 spawn = unit.SpawnPosition;
            PhysicsScene2D scene = unit.gameObject.scene.GetPhysicsScene2D();
            ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(~0);
            float paddedRadius = radius + Mathf.Max(0.02f, clearance);
            position = spawn;
            if (Clear(scene, filter, unit, spawn + offset, paddedRadius)) return true;

            float step = Mathf.Max(0.1f, radius * 0.3f);
            for (int ring = 1; ring <= 128; ring++)
            {
                float distance = ring * step;
                int samples = Mathf.Clamp(Mathf.CeilToInt(2f * Mathf.PI * distance / step), 12, 128);
                for (int sample = 0; sample < samples; sample++)
                {
                    float angle = sample * Mathf.PI * 2f / samples;
                    Vector2 delta = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                    Vector2 candidate = spawn + delta;
                    if (!Clear(scene, filter, unit, candidate + offset, paddedRadius)) continue;
                    // A free point beyond an arena wall is not a valid spawn. Ignore units along
                    // this segment, but reject crossing walls/obstacles to leave the spawn region.
                    int count;
                    while ((count = scene.Raycast(spawn + offset, delta.normalized, distance, filter, crossings)) == crossings.Length)
                        System.Array.Resize(ref crossings, crossings.Length * 2);
                    bool blocked = false;
                    for (int i = 0; i < count; i++)
                    {
                        Collider2D hit = crossings[i].collider;
                        if (hit != null && hit.GetComponentInParent<UnitActor>() == null) { blocked = true; break; }
                    }
                    if (blocked) continue;
                    position = candidate;
                    return true;
                }
            }
            return false; // Never silently revive overlapping an enemy or outside the walls.
        }

        private static bool Clear(PhysicsScene2D scene, ContactFilter2D filter, UnitActor unit, Vector2 center, float radius)
        {
            int count;
            while ((count = scene.OverlapCircle(center, radius, filter, overlaps)) == overlaps.Length)
                System.Array.Resize(ref overlaps, overlaps.Length * 2);
            for (int i = 0; i < count; i++)
                if (overlaps[i] != null && overlaps[i].GetComponentInParent<UnitActor>() != unit) return false;
            return true;
        }
    }
}
