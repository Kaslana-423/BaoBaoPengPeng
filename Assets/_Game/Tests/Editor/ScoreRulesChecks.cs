using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TuanziArena.Editor
{
    public static class ScoreRulesChecks
    {
        [MenuItem("Tools/Tuanzi/Validate Score Rules")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
            Scene scene = EditorSceneManager.NewPreviewScene();
            UnitData data = ScriptableObject.CreateInstance<UnitData>();
            data.maxHealth = 10; data.attackPower = 20;
            try
            {
                UnitActor[] units = new UnitActor[6];
                for (int i = 0; i < units.Length; i++)
                {
                    var go = new GameObject("ScoreUnit_" + i);
                    SceneManager.MoveGameObjectToScene(go, scene);
                    go.transform.position = new Vector3(i * 2, 0f, 0f);
                    units[i] = go.AddComponent<UnitActor>();
                    units[i].Configure(data, i < 3 ? UnitTeam.Player : UnitTeam.Enemy, null, null);
                }
                var root = new GameObject("ScoreTest");
                SceneManager.MoveGameObjectToScene(root, scene);
                PrototypeSession session = root.AddComponent<PrototypeSession>();
                session.Configure(units);
                BattleScoreController score = session.Flow.Score;
                units[3].TakeDamage(100);
                Check(score.PlayerScore == 0 && score.EnemyScore == 0, "Deaths outside actions do not score.");
                Respawn(session);
                Check(session.TryLaunch(units[0], Vector2.right), "Player starts.");
                units[1].TakeDamage(100); Respawn(session);
                Check(score.PlayerScore == 0 && score.EnemyScore == 0, "A friendly/debug death cannot award the opposing team.");
                for (int i = 1; i <= 6; i++)
                {
                    units[3].TakeDamage(100); units[3].TakeDamage(100);
                    Check(score.PlayerScore == i && score.EnemyScore == 0, "One point per life; repeated callbacks never double-count.");
                    Respawn(session);
                    Check(!session.Flow.Turns.HasActed(units[3]), "Scoring and respawning cannot consume an action.");
                }
                Check(score.Winner == UnitTeam.Player, "The first side to six wins.");
                units[4].TakeDamage(100); Respawn(session);
                Check(score.PlayerScore == 6, "Score is capped at the winning target.");
                Settle(session, units);
                Check(session.Flow.State == BattleState.ScorePresentation && !session.CanSelect && !session.Flow.AI.TryActNow(),
                    "Scored actions must pass through the presentation gate, without advancing the AI.");
                session.Flow.ReevaluateTurn();
                Check(session.Flow.State == BattleState.ScorePresentation, "Reevaluation must not bypass the presentation.");
                session.RestartBattle();
                Check(score.PlayerScore == 0 && score.EnemyScore == 0 && !score.Winner.HasValue &&
                    session.Flow.State == BattleState.PlayerSelect && session.Flow.RoundNumber == 1, "Restart resets scores and turns.");
                foreach (UnitActor unit in units)
                    Check(unit.IsAlive && unit.CurrentHealth == unit.MaxHealth && !session.Flow.Turns.HasActed(unit), "Restart resets all six lives/actions.");
                Check(session.TryLaunch(units[0], Vector2.right), "Restarted player action.");
                Settle(session, units);
                Check(session.Flow.State == BattleState.AIThinking, "A zero-kill action skips the score popup.");
                Check(session.TryLaunchForTeam(units[3], Vector2.left, UnitTeam.Enemy), "Enemy action.");
                for (int i = 0; i < 6; i++) { units[0].TakeDamage(100); Respawn(session); }
                Check(score.EnemyScore == 6 && score.PlayerScore == 0 && score.Winner == UnitTeam.Enemy, "Enemy kills correctly yield defeat.");
                Debug.Log("TUANZI_SCORE_RULES_PASSED: both factions, one point per life, repeated kills, six-point latch, presentation gate, zero-kill skip, restart.");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); UnityEngine.Object.DestroyImmediate(data); }
        }

        private static void Respawn(PrototypeSession session)
        {
            session.Respawns.ProcessPendingRespawns(); session.Respawns.ProcessPendingRespawns();
        }
        private static void Settle(PrototypeSession session, UnitActor[] units)
        {
            foreach (UnitActor unit in units) unit.StopMotion();
            MethodInfo tick = typeof(PrototypeSession).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int i = 0; i < 40 && session.IsResolving; i++) tick.Invoke(session, null);
            Check(!session.IsResolving, "Physics settles.");
        }
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Score rules: " + message);
        }
    }
}
