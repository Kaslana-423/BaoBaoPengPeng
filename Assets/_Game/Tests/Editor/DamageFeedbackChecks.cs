using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TuanziArena.Editor
{
    public static class DamageFeedbackChecks
    {
        [MenuItem("Tools/Tuanzi/Validate Damage Feedback")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
            Scene scene = EditorSceneManager.NewPreviewScene();
            UnitData data = ScriptableObject.CreateInstance<UnitData>();
            data.maxHealth = 25; data.attackPower = 10;
            try
            {
                UnitActor[] units = new UnitActor[4];
                for (int i = 0; i < units.Length; i++)
                {
                    var go = new GameObject("DamageUnit_" + i);
                    SceneManager.MoveGameObjectToScene(go, scene);
                    go.transform.position = new Vector3(i * 2f, 0f, 0f);
                    units[i] = go.AddComponent<UnitActor>();
                    units[i].Configure(data, i < 2 ? UnitTeam.Player : UnitTeam.Enemy, null, null);
                }
                var root = new GameObject("DamageTest"); SceneManager.MoveGameObjectToScene(root, scene);
                PrototypeSession session = root.AddComponent<PrototypeSession>(); session.Configure(units);
                CollisionDamageSystem damage = root.AddComponent<CollisionDamageSystem>(); damage.Configure(session, units);
                DamageFeedbackController feedback = root.AddComponent<DamageFeedbackController>(); feedback.Configure(session, damage);
                session.Respawns.Configure(units, damage);
                int events = 0, sum = 0; DamageHit last = default;
                damage.DamageDealt += hit => { events++; sum += hit.Amount; last = hit; };
                Check(!damage.TryResolve(units[0], units[2], 4f) && events == 0, "Idle collisions never emit damage.");
                Check(session.TryLaunch(units[0], Vector2.right), "Player launch.");
                Check(!damage.TryResolve(units[0], units[1], 4f) && events == 0, "No friendly damage UI.");
                Check(damage.TryResolve(units[0], units[2], 4f), "First hit.");
                Check(feedback.TotalDamage == 10 && last.Amount == 10 && last.ActionId == session.ActionId, "Actual damage is published.");
                Check(!damage.TryResolve(units[2], units[0], 4f) && events == 1, "Reverse callback is not a second number.");
                damage.EndContact(units[0], units[2]); damage.TryResolve(units[0], units[2], 4f);
                damage.EndContact(units[0], units[2]); damage.TryResolve(units[0], units[2], 4f);
                Check(last.Amount == 5 && sum == 25 && feedback.TotalDamage == 25 && feedback.HitCount == 3,
                    "Overkill reports only remaining HP; total is 10+10+5, not 30.");
                Vector3 impact = last.Position;
                session.Respawns.ProcessPendingRespawns(); session.Respawns.ProcessPendingRespawns();
                units[2].Body.position = new Vector2(20f, 20f);
                Check(last.Position == impact, "Impact location is a value, not a moving/respawning transform.");
                Check(damage.TryResolve(units[1], units[2], 4f) && feedback.TotalDamage == 35,
                    "A passive friendly actor also contributes to the same action total.");
                session.RestartBattle();
                // In edit mode, the automatic presentation component is deliberately not configured.
                feedback.Configure(session, damage);
                Check(feedback.TotalDamage == 0 && feedback.HitCount == 0, "Battle restart resets totals.");
                Check(session.TryLaunch(units[0], Vector2.right), "Fresh player shot.");
                foreach (UnitActor unit in units) unit.StopMotion();
                MethodInfo tick = typeof(PrototypeSession).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
                for (int i = 0; i < 40 && session.IsResolving; i++) tick.Invoke(session, null);
                Check(session.TryLaunchForTeam(units[2], Vector2.left, UnitTeam.Enemy), "Enemy launch.");
                damage.TryResolve(units[2], units[0], 4f);
                Check(feedback.TotalDamage == 10 && feedback.HitCount == 1 && feedback.ActingTeam == UnitTeam.Enemy,
                    "New action clears the old total and switches the attacking team.");
                Check(!session.TryLaunch(units[1], Vector2.zero) && feedback.TotalDamage == 10, "Rejected/cancelled input never resets total.");
                Debug.Log("TUANZI_DAMAGE_CHECKS_PASSED: actual HP loss, overkill clamp, contact dedup, passive teammate, impact snapshot, action reset, restart, both factions.");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); UnityEngine.Object.DestroyImmediate(data); }
        }
        private static void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException("Damage feedback: " + message);
        }
    }
}
