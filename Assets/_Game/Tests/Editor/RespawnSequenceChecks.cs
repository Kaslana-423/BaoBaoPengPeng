using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TuanziArena.Editor
{
    // Run in an isolated project via -executeMethod TuanziArena.Editor.RespawnSequenceChecks.RunBatch.
    // Real Play-mode frames verify the timing that synchronous editor rule checks cannot cover.
    [InitializeOnLoad]
    public static class RespawnSequenceChecks
    {
        private const string Pending = "TuanziRespawnSequenceChecks";
        private static PrototypeSession session;
        private static CollisionDamageSystem damage;
        private static UnitActor[] units;
        private static Vector3[] scales;
        private static Vector2[] spawns;
        private static bool[] disappeared;
        private static Camera camera;
        private static Vector3 normalPosition;
        private static float normalSize;
        private static double began, stageAt, moviesDoneAt, waitingAt;
        private static int stage, respawnEvents;
        private static bool flightSeen, zoomSeen;

        static RespawnSequenceChecks()
        {
            EditorApplication.playModeStateChanged += Changed;
            EditorApplication.update += Tick;
        }

        public static void RunBatch()
        {
            try
            {
                EditorSceneManager.OpenScene("Assets/Scenes/SC_Battle.unity");
                TurnAndAIChecks.Run(); CombatChecks.Run(); DamageFeedbackChecks.Run(); ScoreRulesChecks.Run();
                SessionState.SetBool(Pending, true);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception e) { Fail(e); }
        }

        private static void Changed(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Pending, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                try
                {
                    session = UnityEngine.Object.FindObjectOfType<PrototypeSession>();
                    session.Flow.AI.enabled = false;
                    damage = session.GetComponent<CollisionDamageSystem>();
                    units = new UnitActor[6]; int p = 0, e = 3;
                    foreach (UnitActor unit in session.Flow.Turns.Units) units[unit.Team == UnitTeam.Player ? p++ : e++] = unit;
                    scales = new Vector3[6]; spawns = new Vector2[6]; disappeared = new bool[6];
                    for (int i = 0; i < 6; i++)
                    {
                        scales[i] = units[i].BodyVisual.transform.localScale;
                        spawns[i] = units[i].SpawnPosition;
                        units[i].Respawned += OnRespawn;
                    }
                    session.Flow.StateChanged += value => { if (value == BattleState.RespawnWaiting) waitingAt = EditorApplication.timeSinceStartup; };
                    camera = Camera.main; normalPosition = camera.transform.position; normalSize = camera.orthographicSize;
                    began = stageAt = EditorApplication.timeSinceStartup; stage = 0;
                }
                catch (Exception e) { Fail(e); }
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Pending, false);
                Debug.Log("TUANZI_RESPAWN_SEQUENCE_PASSED");
                EditorApplication.Exit(0);
            }
        }

        private static void OnRespawn(UnitActor unit)
        {
            if (stage != 2) return;
            try
            {
                Check(waitingAt > 0 && EditorApplication.timeSinceStartup - waitingAt >= 0.78, "Wait 0.8 real seconds AFTER all motion settles.");
                Check(!session.IsResolving && !session.ActiveTeam.HasValue, "Old action damage must close BEFORE any revival.");
                Check(!session.Flow.Score.ScoreUI.IsPopupVisible, "Respawn precedes score presentation.");
                Check(unit.CurrentHealth == unit.MaxHealth, "Full HP on revival.");
                respawnEvents++;
            }
            catch (Exception e) { Fail(e); }
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying || session == null) return;
            try
            {
                double now = EditorApplication.timeSinceStartup;
                Check(now - began < 100, "Sequence timed out at stage " + stage);
                if (stage == 0 && now - stageAt > 0.3)
                {
                    Check(session.TryLaunch(units[0], Vector2.right * 0.1f), "Initial player action.");
                    StopAll();
                    Kill(units[0], units[3], Vector2.right);
                    Kill(units[0], units[4], Vector2.up);
                    session.Respawns.ProcessPendingRespawns();
                    Next(1);
                }
                else if (stage == 1)
                {
                    // Keep a surviving ball in motion after the movie, for longer than the delay.
                    units[0].Body.velocity = Vector2.right;
                    Check(!units[3].IsAlive && !units[4].IsAlive && session.IsResolving, "No early mid-action respawn.");
                    Check(!session.Flow.Score.ScoreUI.IsPopupVisible, "No early score popup.");
                    for (int i = 3; i <= 4; i++)
                    {
                        Transform body = units[i].BodyVisual.transform;
                        Check(Vector3.Distance(body.localScale, scales[i]) < 0.001f, "Death must NEVER shrink.");
                        flightSeen |= Vector3.Distance(body.localPosition, Vector3.zero) > 0.5f;
                        zoomSeen |= camera.orthographicSize < normalSize * 0.8f;
                        UnitDeathVisual visual = units[i].GetComponent<UnitDeathVisual>();
                        if (visual.HasDisappeared && !disappeared[i])
                        {
                            Vector3 viewport = camera.WorldToViewportPoint(body.position);
                            Check(viewport.x < 0 || viewport.x > 1 || viewport.y < 0 || viewport.y > 1, "Fly past viewport edge before hiding.");
                            disappeared[i] = true;
                        }
                    }
                    if (session.Respawns.IsPresenting) return;
                    NormalCamera();
                    if (moviesDoneAt == 0) moviesDoneAt = now;
                    if (now - moviesDoneAt < 1.0) return;
                    Check(flightSeen && zoomSeen && disappeared[3] && disappeared[4], "Both movies ran, with zoom and offscreen exit.");
                    StopAll();
                    Place(units[0], spawns[3]); // Opposing ball occupies the first victim's spawn.
                    Place(units[5], spawns[4]); // Same-team ball occupies the second victim's spawn.
                    Next(2);
                }
                else if (stage == 2)
                {
                    StopAll();
                    if (session.Respawns.IsWaitingForRespawn)
                    {
                        Check(!session.IsResolving && !session.ActiveTeam.HasValue && !session.CanSelect, "Respawn phase locks input and old damage.");
                        Check(!session.Flow.AI.TryActNow(), "AI cannot act during respawn wait.");
                        session.Flow.ReevaluateTurn();
                        Check(session.Flow.State == BattleState.RespawnWaiting, "Reevaluation cannot bypass the wait.");
                    }
                    if (session.Respawns.PendingCount > 0) return;
                    Check(respawnEvents == 2, "Exactly one revival per dead unit.");
                    NormalCamera();
                    for (int i = 3; i <= 4; i++)
                    {
                        Check(units[i].SpawnPosition == spawns[i], "Safe offset must not rewrite initial spawn.");
                        Check(Vector2.Distance(units[i].Body.position, spawns[i]) > 0.1f, "Occupied spawn requires a safe offset.");
                        AssertClear(units[i]);
                        Check(units[i].BodyVisual.gameObject.activeSelf && units[i].BodyVisual.transform.localScale == scales[i], "Restore original visual.");
                        Check(!session.Flow.Turns.HasActed(units[i]), "Unused action remains unused.");
                    }
                    Check(session.Flow.Turns.HasActed(units[0]), "Used player remains used.");
                    Check(!damage.TryResolve(units[0], units[3], 8f), "Revival cannot receive damage from the finished action.");
                    Check(session.Flow.Score.PlayerScore == 2 && session.Flow.State == BattleState.ScorePresentation, "One cumulative popup after both safe revivals.");
                    Next(3);
                }
                else if (stage == 3)
                {
                    if (session.Flow.State != BattleState.AIThinking) return;
                    Check(session.TryLaunchForTeam(units[3], Vector2.left * 0.1f, UnitTeam.Enemy), "Revived enemy can act on its next turn.");
                    StopAll();
                    int hp = units[0].CurrentHealth;
                    Check(damage.TryResolve(units[3], units[0], 1f) && units[0].CurrentHealth < hp, "Normal damage resumes next action.");
                    // Also test cancelling a death movie and the post-motion respawn delay.
                    session.RestartBattle(); NormalCamera();
                    Check(session.TryLaunch(units[0], Vector2.right * 0.1f), "Launch before movie cancellation.");
                    StopAll(); Kill(units[0], units[3], Vector2.left);
                    Next(4);
                }
                else if (stage == 4 && now - stageAt > 0.3)
                {
                    Check(session.Respawns.IsPresenting, "Movie running before restart.");
                    session.RestartBattle(); AssertRestart();
                    Check(session.TryLaunch(units[0], Vector2.right * 0.1f), "Launch before wait cancellation.");
                    StopAll(); Kill(units[0], units[3], Vector2.down);
                    Next(5);
                }
                else if (stage == 5)
                {
                    StopAll();
                    if (!session.Respawns.IsWaitingForRespawn) return;
                    session.RestartBattle(); AssertRestart(); Next(6);
                }
                else if (stage == 6 && now - stageAt > 1.1)
                {
                    AssertRestart();
                    Debug.Log("TUANZI_RESPAWN_RUNTIME_PASSED: no shrink; offscreen flights; dead units absent while survivors move; all-stop + 0.8s; occupied enemy/friendly spawns; simultaneous safe revivals; old-action damage blocked; next-action damage works; score ordering; action eligibility; restart during movie/wait.");
                    session = null; EditorApplication.ExitPlaymode();
                }
            }
            catch (Exception e) { Fail(e); }
        }

        private static void AssertClear(UnitActor unit)
        {
            Physics2D.SyncTransforms();
            CircleCollider2D shape = unit.GetComponent<CircleCollider2D>();
            foreach (Collider2D other in UnityEngine.Object.FindObjectsOfType<Collider2D>())
            {
                if (other == shape || !other.enabled || other.isTrigger || other.GetComponentInParent<UnitActor>() == unit) continue;
                ColliderDistance2D gap = shape.Distance(other);
                Check(!gap.isValid || (!gap.isOverlapped && gap.distance > 0.02f), "Safe revival must clear every unit and wall: " + other.name);
            }
        }
        private static void AssertRestart()
        {
            NormalCamera();
            Check(Time.timeScale == 1f && session.Respawns.PendingCount == 0 && !session.Respawns.IsWaitingForRespawn &&
                !session.IsResolving && session.Flow.State == BattleState.PlayerSelect && session.Flow.Score.PlayerScore == 0, "Restart clears old coroutines/pauses/scores.");
            foreach (UnitActor unit in units) Check(unit.IsAlive && unit.CurrentHealth == unit.MaxHealth &&
                unit.BodyVisual.gameObject.activeSelf && unit.Body.position == unit.SpawnPosition, "Restart restores all actors.");
        }
        private static void Kill(UnitActor attacker, UnitActor victim, Vector2 direction)
        {
            victim.TakeDamage(victim.CurrentHealth - 1);
            damage.EndContact(attacker, victim);
            Check(damage.TryResolve(attacker, victim, 8f, direction) && !victim.IsAlive, "Lethal collision.");
        }
        private static void Place(UnitActor unit, Vector2 position)
        {
            unit.transform.position = new Vector3(position.x, position.y, unit.transform.position.z);
            unit.Body.position = position; unit.StopMotion(); Physics2D.SyncTransforms();
        }
        private static void StopAll() { foreach (UnitActor unit in units) unit.StopMotion(); }
        private static void Next(int value) { stage = value; stageAt = EditorApplication.timeSinceStartup; }
        private static void NormalCamera() => Check(Vector3.Distance(camera.transform.position, normalPosition) < 0.001f &&
            Mathf.Abs(camera.orthographicSize - normalSize) < 0.001f, "Camera returns without drift.");
        private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        private static void Fail(Exception e)
        {
            SessionState.SetBool(Pending, false); Time.timeScale = 1f; Debug.LogException(e); EditorApplication.Exit(1);
        }
    }
}
