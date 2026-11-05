using UnityEngine;

namespace TuanziArena
{
    // Directional launcher. Instead of adding an impulse (which a ball entering from the opposite side
    // simply cancels out), the pad snaps the exit direction toward its own facing and sets a fresh
    // speed. Entry from any angle therefore produces a visible launch along the pad.
    public sealed class BoosterPad : HazardZone
    {
        private bool warned;

        // The aim preview asks the same formula at the cut-off point, so the drawn arrow can never
        // disagree with what the pad will actually do.
        public override Vector2 SteerDirection(Vector2 incoming) =>
            Resolve(incoming, transform.right, SteerAmount);

        private float SteerAmount
        {
            get
            {
                BoosterPadSettings pad = Settings as BoosterPadSettings;
                return pad != null ? pad.steer : 0.75f;
            }
        }

        // Blends the incoming travel direction toward the pad facing. steer = 0 keeps the shot as it is,
        // steer = 1 always exits along the pad. A blend that collapses to zero (exactly opposite entry at
        // steer = 0.5) falls back to the pad facing instead of producing a zero vector. A crawl has no
        // meaningful heading either, so it is treated as "no incoming direction" as well.
        public static Vector2 Resolve(Vector2 incoming, Vector2 facing, float steer)
        {
            Vector2 padDirection = facing.sqrMagnitude > 0.0001f ? facing.normalized : Vector2.right;
            Vector2 inDirection = incoming.sqrMagnitude > 0.25f ? incoming.normalized : padDirection;
            Vector2 blend = Vector2.Lerp(inDirection, padDirection, Mathf.Clamp01(steer));
            return blend.sqrMagnitude > 0.0001f ? blend.normalized : padDirection;
        }

        protected override void OnUnitEnter(UnitActor unit)
        {
            BoosterPadSettings pad = Settings as BoosterPadSettings;
            if (pad == null)
            {
                if (!warned && Settings != null)
                {
                    warned = true;
                    Debug.LogWarning("BoosterPad needs BoosterPadSettings, but got another hazard asset.", this);
                }
                return;
            }
            if (unit == null) return;

            Vector2 velocity = unit.Velocity;
            float speed = velocity.magnitude;
            Vector2 direction = Resolve(velocity, transform.right, pad.steer);
            float baseSpeed = speed * Mathf.Max(0f, pad.speedMultiplier) + Mathf.Max(0f, pad.addedSpeed);
            // The guaranteed speed is applied last, so a slow entry still leaves the pad with a real kick.
            float guaranteed = Mathf.Max(0f, pad.guaranteedSpeed);
            float maximum = pad.maximumSpeed > 0f
                ? Mathf.Max(guaranteed, pad.maximumSpeed)
                : float.PositiveInfinity;
            unit.SetVelocity(direction * Mathf.Clamp(baseSpeed, guaranteed, maximum));
        }
    }
}
