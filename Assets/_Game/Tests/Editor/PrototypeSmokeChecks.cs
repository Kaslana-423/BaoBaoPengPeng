using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TuanziArena.Editor
{
    // Runs real Unity 2D simulation in a separate scene, preserving the user's scenes.
    public static class PrototypeSmokeChecks
    {
        [MenuItem("Tools/Tuanzi/Validate Prototype Physics")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before validation.");
            string[] prefabGuids = AssetDatabase.FindAssets("PF_DangoUnit t:Prefab", new[] { "Assets/_Game" });
            Require(prefabGuids.Length > 0, "Build the prototype first.");
            ValidateSavedScene();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(prefabGuids[0]));
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                UnitActor player = Spawn(prefab, scene, UnitTeam.Player, new Vector2(-1f, 0f));
                UnitActor enemy = Spawn(prefab, scene, UnitTeam.Enemy, new Vector2(1f, 0f));
                GameObject gateObject = new GameObject("ValidationSession");
                SceneManager.MoveGameObjectToScene(gateObject, scene);
                PrototypeSession session = gateObject.AddComponent<PrototypeSession>();
                session.Configure(new[] { player, enemy });
                // Invoke the C# tick directly: SendMessage of a Unity lifecycle method asserts in edit mode.
                Action fixedTick = (Action)Delegate.CreateDelegate(typeof(Action), session,
                    typeof(PrototypeSession).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic));
                Require(!session.CanSelectUnit(enemy), "Enemy must not be selectable.");
                Require(session.CanSelectUnit(player), "Player must be selectable while idle.");
                PhysicsScene2D physics = scene.GetPhysicsScene2D();
                Require(physics.IsValid() && physics != Physics2D.defaultPhysicsScene,
                    "Validation requires an isolated physics scene; user scenes must not be simulated.");
                Physics2D.SyncTransforms();
                player.SetSelected(true);
                for (int i = 0; i < 10; i++) physics.Simulate(Time.fixedDeltaTime);
                Require(Vector2.Distance(player.Body.position, new Vector2(-1f, 0f)) < 0.001f,
                    "Selection must not move the rigidbody.");
                Require(!session.TryLaunch(player, Vector2.zero) && !session.IsResolving,
                    "A zero impulse must not lock input.");
                Require(session.TryLaunch(player, Vector2.right * 10f), "Player impulse was rejected.");
                Require(!session.TryLaunch(player, Vector2.right * 10f), "A resolving shot must lock input.");
                float maxEnemySpeed = 0f;
                for (int i = 0; i < 500; i++)
                {
                    physics.Simulate(Time.fixedDeltaTime);
                    maxEnemySpeed = Mathf.Max(maxEnemySpeed, enemy.SpeedSquared);
                    fixedTick();
                }
                Require(maxEnemySpeed > 0.05f && enemy.Body.position.x > 1.1f,
                    "The target did not receive physical collision momentum.");
                Require(!session.IsResolving && player.SpeedSquared < 0.001f && enemy.SpeedSquared < 0.001f,
                    "Damping and settle detection must restore selection.");
                Require(session.Flow.State == BattleState.AIThinking && !session.CanSelectUnit(player),
                    "Player shot must hand off to the enemy after settling.");
                session.Flow.SetDifficulty(AIDifficulty.Hard);
                Require(session.TryLaunchForTeam(enemy, Vector2.left * 10f, UnitTeam.Enemy), "Enemy action must start.");
                for (int i = 0; i < 500; i++) { physics.Simulate(Time.fixedDeltaTime); fixedTick(); }
                Require(session.CanSelectUnit(player) && session.Flow.RoundNumber == 2,
                    "Enemy settling must start the next player round in this one-versus-one fixture.");
                Debug.Log("TUANZI_PHYSICS_CHECKS_PASSED: selection, shot lock, collision, damping, player/enemy settle handoff.");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static UnitActor Spawn(GameObject prefab, Scene scene, UnitTeam team, Vector2 position)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.position = position;
            UnitActor actor = instance.GetComponent<UnitActor>();
            actor.Configure(actor.Data, team,
                instance.transform.Find("Visual").GetComponent<SpriteRenderer>(),
                instance.transform.Find("SelectionIndicator").GetComponent<SpriteRenderer>());
            return actor;
        }

        private static void ValidateSavedScene()
        {
            string[] sceneGuids = AssetDatabase.FindAssets("SC_Battle t:Scene", new[] { "Assets" });
            Require(sceneGuids.Length > 0, "The saved prototype scene is missing.");
            string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[0]);
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedForValidation = !scene.IsValid() || !scene.isLoaded;
            if (openedForValidation) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                int players = 0, enemies = 0;
                PrototypeSession session = null;
                PlayerInputController input = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                        Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0,
                            "Missing script on " + child.name);
                    foreach (UnitActor unit in root.GetComponentsInChildren<UnitActor>(true))
                    {
                        if (unit.Team == UnitTeam.Player) players++; else enemies++;
                        Require(unit.Data != null && unit.Data.sprite != null && unit.Data.physicsMaterial != null,
                            "Missing unit configuration or art on " + unit.name);
                        Require(unit.GetComponent<CircleCollider2D>().radius > 0f,
                            "Missing collision radius on " + unit.name);
                    }
                    session = root.GetComponentInChildren<PrototypeSession>(true) ?? session;
                    input = root.GetComponentInChildren<PlayerInputController>(true) ?? input;
                }
                Require(players == 3 && enemies == 3, "The scene must contain three units per team.");
                Require(session != null && input != null, "Scene controllers are missing.");
                SerializedObject inputFields = new SerializedObject(input);
                foreach (string field in new[] { "worldCamera", "session", "aimPreview" })
                    Require(inputFields.FindProperty(field).objectReferenceValue != null,
                        "Input reference is not bound: " + field);
                SerializedProperty units = new SerializedObject(session).FindProperty("units");
                Require(units.arraySize == 6, "Session must track all six units.");
                for (int i = 0; i < units.arraySize; i++)
                    Require(units.GetArrayElementAtIndex(i).objectReferenceValue != null, "Missing session unit reference.");
                Debug.Log("TUANZI_SCENE_CHECKS_PASSED: six units, three per team, configuration, sprites, materials, controller bindings, no missing scripts.");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (openedForValidation) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Tuanzi validation: " + message);
        }
    }
}
