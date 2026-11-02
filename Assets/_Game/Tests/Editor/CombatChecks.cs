using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TuanziArena.Editor
{
    public static class CombatChecks
    {
        [MenuItem("Tools/Tuanzi/Validate Unit Combat Stats")]
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
                UnitActor attacker = Unit(scene, data, UnitTeam.Player, -2f);
                UnitActor target = Unit(scene, data, UnitTeam.Enemy, 2f);
                UnitActor friend = Unit(scene, data, UnitTeam.Player, 0f);
                UnitActor secondEnemy = Unit(scene, data, UnitTeam.Enemy, 5f);
                UnitStatsUI ui = target.gameObject.AddComponent<UnitStatsUI>();
                typeof(UnitStatsUI).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
                Check(ui.AttackText.text == "10" && ui.HealthText.text == "100", "Initial badge numbers.");
                foreach (StatIconGraphic icon in ui.GetComponentsInChildren<StatIconGraphic>(true))
                    Check(icon.GetComponent<CanvasRenderer>() != null, "Every icon and badge needs a CanvasRenderer.");

                GameObject systems = new GameObject("CombatValidation");
                SceneManager.MoveGameObjectToScene(systems, scene);
                PrototypeSession session = systems.AddComponent<PrototypeSession>();
                UnitActor[] units = { attacker, target, friend, secondEnemy };
                session.Configure(units);
                CollisionDamageSystem damage = systems.AddComponent<CollisionDamageSystem>();
                damage.Configure(session, units);
                Check(!damage.TryResolve(attacker, target, 4f), "No damage while idle.");
                Check(session.TryLaunch(attacker, Vector2.right), "The test shot must start.");
                Check(session.ActiveTeam == UnitTeam.Player, "Snapshot the acting faction at launch.");
                Check(!damage.TryResolve(attacker, friend, 4f) && friend.CurrentHealth == 100,
                    "No friendly fire.");
                Check(damage.TryResolve(attacker, target, 4f) && target.CurrentHealth == 90,
                    "Attack 10 at reference speed must deal 10 damage.");
                Check(ui.HealthText.text == "90", "Health badge must update from the actor event.");
                Check(!damage.TryResolve(target, attacker, 4f) && target.CurrentHealth == 90,
                    "Reversed collision callbacks must not deal duplicate damage.");
                Check(!damage.TryResolve(attacker, target, 100f), "A persistent contact must not hurt every frame.");
                damage.EndContact(attacker, target);
                Check(damage.TryResolve(target, attacker, 4f) && target.CurrentHealth == 80,
                    "Separation and re-entry must hurt again immediately, without a time cooldown.");
                damage.EndContact(attacker, target);
                Check(damage.TryResolve(attacker, target, 0f) && target.CurrentHealth == 75,
                    "Low-speed contacts must still hurt during an action.");
                Check(damage.TryResolve(target, friend, 100f) && target.CurrentHealth == 55 && friend.CurrentHealth == 100,
                    "A non-shooting ally damages the enemy even if the enemy is moving faster.");
                Check(!damage.TryResolve(target, secondEnemy, 4f) && secondEnemy.CurrentHealth == 100,
                    "Defending-team internal collisions must not hurt either ball.");
                Check(damage.CalculateDamage(attacker, 100f) == 20, "Damage upper limit.");
                Check(data.maxHealth == 100 && attacker.CurrentHealth == 100,
                    "Shared data and the other instance health must not be modified.");

                FinishAction(session, units);
                Check(!session.ActiveTeam.HasValue, "The acting faction must clear after settling.");
                Check(!session.TryLaunch(target, Vector2.left), "Mouse action entry must still reject enemy units.");
                Check(session.TryLaunchForTeam(target, Vector2.left, UnitTeam.Enemy), "Enemy faction action entry.");
                Check(damage.TryResolve(friend, target, 4f) && friend.CurrentHealth == 90 && target.CurrentHealth == 55,
                    "Enemy actions hurt only players, including a contact retained from the previous action.");
                Check(!damage.TryResolve(target, friend, 4f), "Enemy action callback de-duplication.");
                damage.EndContact(target, friend);
                Check(damage.TryResolve(target, friend, 0f) && friend.CurrentHealth == 85,
                    "Enemy re-entry can hurt immediately at low speed too.");
                int deaths = 0;
                target.Defeated += _ => deaths++;
                target.TakeDamage(int.MaxValue);
                target.TakeDamage(10);
                Check(target.CurrentHealth == 0 && deaths == 1 && !target.CanLaunch &&
                    !target.Body.simulated && !target.GetComponent<CircleCollider2D>().enabled,
                    "Defeat must happen once and disable physics and launch.");
                Check(ui.HealthText.text == "0" && ui.AttackText.text == "10", "Defeated badge values.");
                Check(!target.TryLaunch(Vector2.right), "A defeated unit must not launch.");
                Check(damage.TryResolve(secondEnemy, friend, 4f) && friend.CurrentHealth == 75,
                    "Other acting-team balls can hurt enemies after the original shooter is defeated.");
                Debug.Log("TUANZI_COMBAT_CHECKS_PASSED: acting-team-only damage in both directions, non-shooting allies, immediate re-entry, persistent-contact de-duplication, low-speed contact, action reset, UI, defeat.");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                UnityEngine.Object.DestroyImmediate(data);
            }
        }

        private static UnitActor Unit(Scene scene, UnitData data, UnitTeam team, float x)
        {
            GameObject obj = new GameObject(team.ToString(), typeof(Rigidbody2D), typeof(CircleCollider2D));
            SceneManager.MoveGameObjectToScene(obj, scene);
            obj.transform.position = new Vector3(x, 0f, 0f);
            UnitActor unit = obj.AddComponent<UnitActor>();
            unit.Configure(data, team, null, null);
            return unit;
        }

        private static void FinishAction(PrototypeSession session, UnitActor[] units)
        {
            foreach (UnitActor unit in units) unit.StopMotion();
            Action tick = (Action)Delegate.CreateDelegate(typeof(Action), session,
                typeof(PrototypeSession).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic));
            for (int i = 0; i < 40; i++) tick();
            Check(!session.IsResolving, "The previous test action must settle.");
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Combat validation: " + message);
        }
    }
}
