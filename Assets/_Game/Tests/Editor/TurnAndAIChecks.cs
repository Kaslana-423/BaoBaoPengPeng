using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TuanziArena.Editor
{
    public static class TurnAndAIChecks
    {
        [MenuItem("Tools/Tuanzi/Validate Turns And AI")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before validation.");
            Scene scene = EditorSceneManager.NewPreviewScene();
            UnitData data = ScriptableObject.CreateInstance<UnitData>();
            data.maxHealth = 100;
            data.attackPower = 10;
            try
            {
                UnitActor[] units = new UnitActor[6];
                for (int i = 0; i < 6; i++)
                {
                    GameObject obj = new GameObject("TurnUnit_" + i, typeof(Rigidbody2D), typeof(CircleCollider2D));
                    SceneManager.MoveGameObjectToScene(obj, scene);
                    obj.transform.position = new Vector3(i < 3 ? -2f : 2f, (i % 3) * 2f - 2f, 0f);
                    units[i] = obj.AddComponent<UnitActor>();
                    units[i].Configure(data, i < 3 ? UnitTeam.Player : UnitTeam.Enemy, null, null);
                    UnitStatsUI ui = obj.AddComponent<UnitStatsUI>();
                    typeof(UnitStatsUI).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
                }
                GameObject root = new GameObject("TurnSystems");
                SceneManager.MoveGameObjectToScene(root, scene);
                PrototypeSession session = root.AddComponent<PrototypeSession>();
                session.Configure(units);
                GameFlowController flow = session.Flow;
                Check(flow.State == BattleState.PlayerSelect && flow.RoundNumber == 1, "Player must start round one.");
                for (int i = 0; i < 6; i++)
                    Check(units[i].GetComponent<UnitStatsUI>().IsActionIndicatorVisible == (i < 3),
                        "Only unused player units have action rings.");
                Check(!session.TryLaunchForTeam(units[3], Vector2.left, UnitTeam.Enemy), "Enemy cannot launch during player selection.");
                Check(!session.TryLaunch(units[0], Vector2.zero) && !flow.Turns.HasActed(units[0]), "Cancelled shots do not consume a turn.");
                Check(session.TryLaunch(units[0], Vector2.right), "Start player shot.");
                Check(flow.State == BattleState.PhysicsResolving && !session.CanSelect, "Physics must lock all selections.");
                Check(!units[0].GetComponent<UnitStatsUI>().IsActionIndicatorVisible, "Using a player unit hides its ring immediately.");
                for (int i = 0; i < 3; i++)
                    Check(!units[i].GetComponent<UnitStatsUI>().IsActionIndicatorVisible, "Launching hides every player action ring immediately.");
                units[0].Body.position = new Vector2(8f, 8f);
                units[0].TakeDamage(int.MaxValue);
                Respawn(session, units[0]);
                Check(flow.Turns.HasActed(units[0]) && !units[0].GetComponent<UnitStatsUI>().IsActionIndicatorVisible,
                    "A used unit's death and respawn must not restore its action or ring.");
                Check(!flow.Turns.HasActed(units[1]), "Passive movement must not consume another character's action.");
                Settle(session, units);
                Check(flow.State == BattleState.AIThinking && !session.CanSelect, "Enemy turn after player settling.");
                units[1].Body.position = new Vector2(7f, -7f);
                units[1].TakeDamage(int.MaxValue);
                Check(!units[1].GetComponent<UnitStatsUI>().IsActionIndicatorVisible, "Enemy thinking keeps unused player rings hidden even on defeat.");
                Respawn(session, units[1]);
                Check(!flow.Turns.HasActed(units[1]) && !units[1].GetComponent<UnitStatsUI>().IsActionIndicatorVisible,
                    "Respawn preserves unused status without showing rings during enemy turns.");

                CheckPlans(flow.Turns);
                flow.SetDifficulty((int)AIDifficulty.Hard);
                int difficultyEvents = 0;
                flow.DifficultyChanged += _ => difficultyEvents++;
                session.SetAIDifficulty(0);
                Check(flow.Difficulty == AIDifficulty.Easy && difficultyEvents == 1, "Dropdown adapter and difficulty event.");
                Check(flow.AI.TryActNow() && flow.AI.LastPlan.PowerRatio <= 0.5f,
                    "Changing difficulty while thinking must apply to the upcoming shot.");
                Check(!flow.AI.TryActNow(), "AI must not fire twice during physics.");
                Settle(session, units);
                Check(flow.State == BattleState.PlayerSelect && !session.CanSelectUnit(units[0]) && session.CanSelectUnit(units[1]),
                    "Used player units cannot fire again in this round.");
                Check(!units[0].GetComponent<UnitStatsUI>().IsActionIndicatorVisible &&
                    units[1].GetComponent<UnitStatsUI>().IsActionIndicatorVisible && units[2].GetComponent<UnitStatsUI>().IsActionIndicatorVisible,
                    "Returning to player selection restores only the unused player rings.");

                HashSet<UnitActor> enemyActors = new HashSet<UnitActor>();
                enemyActors.Add(flow.AI.LastPlan.Shooter);
                for (int player = 1; player <= 2; player++)
                {
                    Check(session.TryLaunch(units[player], Vector2.right), "Remaining player action.");
                    Settle(session, units);
                    Check(flow.AI.TryActNow(), "Remaining enemy action.");
                    Check(enemyActors.Add(flow.AI.LastPlan.Shooter), "Each enemy acts at most once per round.");
                    Settle(session, units);
                }
                Check(flow.RoundNumber == 2 && session.CanSelectUnit(units[0]), "All six actions must reset eligibility for round two.");
                for (int i = 0; i < 3; i++)
                    Check(units[i].GetComponent<UnitStatsUI>().IsActionIndicatorVisible, "New rounds restore all player rings.");

                session.Configure(units);
                for (int i = 3; i < 6; i++) units[i].TakeDamage(int.MaxValue);
                Check(flow.State == BattleState.PlayerSelect && flow.RoundNumber == 1 && flow.Turns.HasUnused(UnitTeam.Enemy),
                    "A whole defeated team must not end the battle or remove its unused actions.");
                session.Respawns.ProcessPendingRespawns();
                session.Respawns.ProcessPendingRespawns();
                for (int i = 3; i < 6; i++) Check(units[i].IsAlive && !flow.Turns.HasActed(units[i]), "All enemies respawn with their original eligibility.");
                for (int player = 0; player < 3; player++)
                {
                    Check(session.TryLaunch(units[player], Vector2.right), "Player action in respawn round.");
                    Settle(session, units);
                    if (player == 2)
                    {
                        UnitActor lastEnemy = null;
                        foreach (UnitActor unit in units)
                            if (unit.Team == UnitTeam.Enemy && flow.Turns.CanAct(unit)) lastEnemy = unit;
                        lastEnemy.TakeDamage(int.MaxValue);
                        Check(!flow.Turns.AllHaveActed && flow.RoundNumber == 1 && !flow.AI.TryActNow() &&
                            flow.State == BattleState.AIThinking, "The sixth unused actor's death must not advance the round.");
                        Respawn(session, lastEnemy);
                    }
                    Check(flow.AI.TryActNow(), "Every enemy must still take its own action after respawn.");
                    if (player == 0)
                    {
                        UnitActor usedEnemy = flow.AI.LastPlan.Shooter;
                        usedEnemy.TakeDamage(int.MaxValue);
                        Respawn(session, usedEnemy);
                        Check(flow.Turns.HasActed(usedEnemy) && !flow.Turns.CanAct(usedEnemy), "Used enemy eligibility survives respawn too.");
                    }
                    Settle(session, units);
                    Check(flow.RoundNumber == (player == 2 ? 2 : 1), "Only the sixth completed action ends the large round.");
                }
                Check(flow.State == BattleState.PlayerSelect, "Play continues into the next large round without scoring or a game-over state.");
                Debug.Log("TUANZI_TURN_AI_CHECKS_PASSED: six-action large rounds, spawn/full-health respawn, used/unused preservation, player rings, no elimination ending, AI difficulty caps.");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                UnityEngine.Object.DestroyImmediate(data);
            }
        }

        private static void CheckPlans(TurnManager turns)
        {
            System.Random random = new System.Random(8516);
            bool observedEasyError = false;
            foreach (AIDifficulty difficulty in new[] { AIDifficulty.Easy, AIDifficulty.Medium, AIDifficulty.Hard })
                for (int i = 0; i < 250; i++)
                {
                    Check(AIShotPlanner.TryBuildPlan(turns, difficulty, 16f, 22f, random, out AIShotPlan plan), "AI must produce a valid plan.");
                    Check(plan.Shooter.Team == UnitTeam.Enemy && turns.CanAct(plan.Shooter) && plan.Target.Team == UnitTeam.Player,
                        "AI chooses only eligible enemies and living player targets.");
                    Check(plan.Impulse.magnitude <= 16f * AIShotPlanner.PowerCap(difficulty) + 0.0001f, "Power cap exceeded.");
                    Vector2 exact = (plan.Target.Body.position - plan.Shooter.Body.position).normalized;
                    float angle = Vector2.Angle(exact, plan.Impulse);
                    if (difficulty == AIDifficulty.Easy)
                    {
                        Check(angle <= 22.01f, "Easy aiming error bound.");
                        observedEasyError |= angle > 1f;
                    }
                    else Check(angle < 0.05f, "Medium and hard must aim at the current target centre.");
                    if (difficulty == AIDifficulty.Hard)
                        Check(Mathf.Abs(plan.Impulse.magnitude - 16f) < 0.0001f && plan.PowerRatio == 1f, "Hard must use exactly full power.");
                }
            Check(observedEasyError, "Easy must exhibit approximate aiming.");
        }

        private static void Settle(PrototypeSession session, UnitActor[] units)
        {
            foreach (UnitActor unit in units) unit.StopMotion();
            Action tick = (Action)Delegate.CreateDelegate(typeof(Action), session,
                typeof(PrototypeSession).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic));
            for (int i = 0; i < 40 && session.IsResolving; i++) { session.Respawns.ProcessPendingRespawns(); tick(); }
            Check(!session.IsResolving, "Test physics must settle.");
        }

        private static void Respawn(PrototypeSession session, UnitActor unit)
        {
            Check(!unit.IsAlive && !unit.Body.simulated, "Defeated unit temporarily leaves physics.");
            session.Respawns.ProcessPendingRespawns();
            session.Respawns.ProcessPendingRespawns();
            Check(unit.IsAlive && unit.CurrentHealth == unit.MaxHealth && unit.Body.simulated &&
                unit.GetComponent<CircleCollider2D>().enabled && unit.SpeedSquared == 0f &&
                Vector2.Distance(unit.Body.position, unit.SpawnPosition) < 0.001f,
                "Respawn must restore full HP and physics at the initial spawn with no residual velocity.");
            Check(unit.GetComponent<UnitStatsUI>().HealthText.text == unit.MaxHealth.ToString(), "Respawn updates the HP UI.");
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Turn/AI validation: " + message);
        }
    }
}
