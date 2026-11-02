using System;
using System.Collections.Generic;
using UnityEngine;

namespace TuanziArena
{
    public struct AIShotPlan
    {
        public UnitActor Shooter;
        public UnitActor Target;
        public Vector2 Impulse;
        public float PowerRatio;
        public AIDifficulty Difficulty;
    }

    public static class AIShotPlanner
    {
        public static float PowerCap(AIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy: return 0.5f;
                case AIDifficulty.Hard: return 1f;
                default: return 0.6f;
            }
        }

        public static bool TryBuildPlan(TurnManager turns, AIDifficulty difficulty, float fullImpulse,
            float easyErrorDegrees, System.Random random, out AIShotPlan plan)
        {
            plan = default;
            if (turns == null || fullImpulse <= 0f) return false;
            List<UnitActor> shooters = new List<UnitActor>();
            List<UnitActor> targets = new List<UnitActor>();
            foreach (UnitActor unit in turns.Units)
            {
                if (unit == null || !unit.CanLaunch) continue;
                if (unit.Team == UnitTeam.Enemy && turns.CanAct(unit)) shooters.Add(unit);
                if (unit.Team == UnitTeam.Player) targets.Add(unit);
            }
            if (shooters.Count == 0 || targets.Count == 0) return false;
            random = random ?? new System.Random();
            UnitActor shooter = shooters[0], target = targets[0];
            if (difficulty == AIDifficulty.Easy)
            {
                shooter = shooters[random.Next(shooters.Count)];
                target = targets[random.Next(targets.Count)];
            }
            else
            {
                float nearest = float.PositiveInfinity;
                foreach (UnitActor candidate in shooters)
                    foreach (UnitActor victim in targets)
                    {
                        float distance = (victim.Body.position - candidate.Body.position).sqrMagnitude;
                        if (distance >= nearest) continue;
                        nearest = distance;
                        shooter = candidate;
                        target = victim;
                    }
            }
            Vector2 direction = target.Body.position - shooter.Body.position;
            if (direction.sqrMagnitude < 0.000001f) direction = Vector2.left;
            direction.Normalize();
            float power;
            if (difficulty == AIDifficulty.Easy)
            {
                float angle = ((float)random.NextDouble() * 2f - 1f) * Mathf.Clamp(easyErrorDegrees, 1f, 60f) * Mathf.Deg2Rad;
                direction = new Vector2(direction.x * Mathf.Cos(angle) - direction.y * Mathf.Sin(angle),
                    direction.x * Mathf.Sin(angle) + direction.y * Mathf.Cos(angle));
                power = Mathf.Lerp(0.3f, 0.5f, (float)random.NextDouble());
            }
            else power = difficulty == AIDifficulty.Hard ? 1f : Mathf.Lerp(0.45f, 0.6f, (float)random.NextDouble());
            plan = new AIShotPlan { Shooter = shooter, Target = target, Difficulty = difficulty,
                PowerRatio = power, Impulse = direction * (fullImpulse * power) };
            return true;
        }
    }
}
