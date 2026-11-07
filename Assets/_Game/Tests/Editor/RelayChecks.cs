using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TuanziArena.Editor
{
    // Isolated-project batch entry. Real physics ENTER exercises the production reporter once;
    // subsequent deterministic input calls exercise the same entry used by mouse buttons.
    [InitializeOnLoad]
    public static class RelayChecks
    {
        private const string Pending = "TuanziRelayChecks";
        private static PrototypeSession session;
        private static RelayController relay;
        private static UnitActor[] units;
        private static int stage, actionId;
        private static double began, stageAt, pausedAt;
        private static Vector2 leftVelocity, rightVelocity, leftPosition, rightPosition;
        private static UnitActor left, right;
        private static CanvasGroup existingGroup;
        private static Canvas disabledCanvas, lateCanvas;
        private static bool halfSeen, captured;
        static RelayChecks() { EditorApplication.playModeStateChanged += Changed; EditorApplication.update += Tick; }

        public static void RunBatch()
        {
            try
            {
                RelayAssets.CreateAssets();
                EditorSceneManager.OpenScene("Assets/Scenes/SC_Battle.unity");
                TurnAndAIChecks.Run(); CombatChecks.Run(); ScoreRulesChecks.Run();
                SessionState.SetBool(Pending, true); EditorApplication.EnterPlaymode();
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
                    session.Flow.AI.enabled = false; relay = session.Relay;
                    units = new UnitActor[6]; int p = 0, e = 3;
                    foreach (UnitActor unit in session.Flow.Turns.Units) units[unit.Team == UnitTeam.Player ? p++ : e++] = unit;
                    // Hazards have separate checks; keep relay timing/collision deterministic here.
                    foreach (HazardZone hazard in UnityEngine.Object.FindObjectsOfType<HazardZone>()) hazard.gameObject.SetActive(false);
                    existingGroup = session.Flow.Score.ScoreUI.gameObject.AddComponent<CanvasGroup>();
                    existingGroup.alpha = 0.37f; existingGroup.interactable = false;
                    disabledCanvas = new GameObject("OriginallyDisabledUI", typeof(Canvas)).GetComponent<Canvas>(); disabledCanvas.enabled = false;
                    began = stageAt = EditorApplication.timeSinceStartup; stage = 0;
                }
                catch (Exception e) { Fail(e); }
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Pending, false); Debug.Log("TUANZI_RELAY_PLAY_PASSED"); EditorApplication.Exit(0);
            }
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying || session == null) return;
            try
            {
                double now = EditorApplication.timeSinceStartup;
                Check(now - began < 100, "Relay timeout stage " + stage);
                if (stage == 0 && now - stageAt > 0.3)
                {
                    Check(relay.Settings.countdownSeconds == 2f && relay.RemainingTriggers == 3, "Default two seconds / three per round.");
                    Check(!relay.TryQueueCollision(units[0], units[1], 9f), "Cannot trigger while selecting.");
                    Arrange();
                    Check(session.TryLaunch(units[0], Vector2.right * 8f), "Launch physical shot.");
                    actionId = session.ActionId;
                    Check(!relay.TryQueueCollision(units[0], units[1], relay.Settings.minimumImpactSpeed), "Threshold is exclusive.");
                    Check(!relay.TryQueueCollision(units[0], units[3], 9f) && !relay.TryQueueCollision(units[3], units[4], 9f) &&
                        !relay.TryQueueCollision(units[0], null, 9f), "Enemy/mixed/wall contacts cannot trigger.");
                    Next(1);
                }
                else if (stage == 1)
                {
                    if (!relay.IsActive) return;
                    Check(relay.UsedThisRound == 1 && Time.timeScale == 0f && relay.UI.IsVisible, "Real physics Enter opens relay exactly once.");
                    left = relay.LeftUnit; right = relay.RightUnit;
                    Check(left == units[0] && right == units[1], "Left/right captured from actual collision X.");
                    Check(relay.UI.LeftPortrait == Portrait(left) && relay.UI.RightPortrait == Portrait(right), "Portraits read UnitActor.HeadSprite.");
                    leftVelocity = left.Body.velocity; rightVelocity = right.Body.velocity;
                    leftPosition = left.Body.position; rightPosition = right.Body.position;
                    Check((relay.Midpoint - (leftPosition + rightPosition) * 0.5f).magnitude < 0.1f, "Aim origin is collision midpoint.");
                    pausedAt = now; Next(2);
                }
                else if (stage == 2)
                {
                    if (relay.IsActive)
                    {
                        Check(left.Body.position == leftPosition && right.Body.position == rightPosition, "Physics positions freeze.");
                        Check(left.Body.velocity == leftVelocity && right.Body.velocity == rightVelocity, "Pause preserves momentum.");
                        Check(!session.CanSelect && !session.Flow.AI.TryActNow(), "Ordinary input and AI stay locked.");
                        Check(!relay.TryChoose(0, relay.Midpoint), "Zero direction cannot spend the choice.");
                        AssertUIHidden();
                        float ratio = relay.UI.ProgressRatio;
                        halfSeen |= ratio > 0.40f && ratio < 0.60f;
                        if (!captured && ratio < 0.85f)
                        {
                            session.GetComponent<RelayAimArrow>().Show(relay.Midpoint, relay.Midpoint + Vector2.up * 3f, relay.Settings);
                            Capture("RelayPreview.png"); captured = true;
                            lateCanvas = new GameObject("LateUI", typeof(Canvas)).GetComponent<Canvas>();
                        }
                        if (lateCanvas != null && now - stageAt > 0.7) Check(Suppressed(lateCanvas), "Newly created UI is hidden too.");
                        return;
                    }
                    Check(now - pausedAt >= 1.94 && halfSeen && captured, "Two-second timeout and decreasing bar.");
                    Check(Time.timeScale == 1f && !relay.UI.IsVisible && relay.UsedThisRound == 1, "Timeout restores play and consumes quota.");
                    Check(existingGroup.alpha == 0.37f && !existingGroup.interactable && !disabledCanvas.enabled, "Restore UI without changing prior states.");
                    // Continue the same action; actor usage is untouched by relays.
                    StopAll(); Place(units[0], new Vector2(1f, 0f)); Place(units[1], new Vector2(-1f, 0f));
                    Check(relay.TryQueueCollision(units[0], units[1], 9f), "Second relay queues."); Next(3);
                }
                else if (stage == 3)
                {
                    if (!relay.IsActive) return;
                    Check(relay.LeftUnit == units[1] && relay.RightUnit == units[0], "Actor order follows X, not reporter or shooter.");
                    Vector2 before = units[1].Body.velocity;
                    Check(relay.TryChoose(0, relay.Midpoint + Vector2.up * 3f), "Left mouse selects left ball.");
                    Check((units[1].Body.velocity - before - Vector2.up * relay.Settings.impulse / units[1].Body.mass).magnitude < 0.001f, "Configured impulse direction and magnitude.");
                    Check(units[0].SpeedSquared == 0f && session.ActionId == actionId && session.Flow.Turns.HasActed(units[0]) &&
                        !session.Flow.Turns.HasActed(units[1]), "Only selected actor receives force, no new action consumed.");
                    StopAll(); Place(units[0], new Vector2(0f, 1f)); Place(units[1], new Vector2(0f, -1f));
                    Check(relay.TryQueueCollision(units[0], units[1], 9f), "Vertical contact queues."); Next(4);
                }
                else if (stage == 4)
                {
                    if (!relay.IsActive) return;
                    Check(relay.LeftUnit != relay.RightUnit && (relay.LeftUnit == units[0] || relay.LeftUnit == units[1]), "Vertical assignment contains both actors.");
                    UnitActor chosen = relay.RightUnit, other = relay.LeftUnit;
                    Vector2 before = chosen.Body.velocity;
                    Check(relay.TryChoose(1, relay.Midpoint + Vector2.right * 2f), "Right mouse selects right ball.");
                    Check((chosen.Body.velocity - before - Vector2.right * relay.Settings.impulse / chosen.Body.mass).magnitude < 0.001f && other.SpeedSquared == 0f, "Right impulse.");
                    Check(relay.UsedThisRound == 3 && !relay.TryQueueCollision(units[0], units[1], 99f), "Fourth relay denied.");
                    Settle();
                    Check(session.Flow.State == BattleState.AIThinking && relay.UsedThisRound == 3, "Quota persists across faction turns.");
                    Check(session.TryLaunchForTeam(units[3], Vector2.up * 0.1f, UnitTeam.Enemy), "Enemy acts.");
                    Check(!relay.TryQueueCollision(units[0], units[1], 9f), "Player pair cannot trigger during enemy action."); Settle();
                    Check(session.TryLaunch(units[1], Vector2.up * 0.1f), "Second player action.");
                    Check(!relay.TryQueueCollision(units[0], units[1], 9f), "Quota does not reset for the next player action."); Settle();
                    Check(session.TryLaunchForTeam(units[4], Vector2.up * 0.1f, UnitTeam.Enemy), "Second enemy."); Settle();
                    Check(session.TryLaunch(units[2], Vector2.up * 0.1f), "Third player."); Settle();
                    Check(session.TryLaunchForTeam(units[5], Vector2.up * 0.1f, UnitTeam.Enemy), "Third enemy."); Settle();
                    Check(session.Flow.RoundNumber == 2 && relay.RemainingTriggers == 3, "Quota resets only at the next large round.");
                    Arrange(); Time.timeScale = 0.5f;
                    Check(session.TryLaunch(units[0], Vector2.up * 0.1f), "Next round begins."); StopAll();
                    session.GetComponent<ImpactFeedback>().TryPlay(9f, Vector2.right);
                    Check(relay.TryQueueCollision(units[0], units[1], 9f), "Take over short impact pause."); Next(5);
                }
                else if (stage == 5)
                {
                    if (!relay.IsActive) return;
                    Check(!session.GetComponent<ImpactFeedback>().enabled, "Impact writer suspended during relay.");
                    units[3].TakeDamage(999); Next(6);
                }
                else if (stage == 6)
                {
                    if (relay.IsBusy) return;
                    Check(!relay.UI.IsVisible && session.Respawns.IsPresenting && Time.timeScale == 0f, "Death preempts relay without unpausing the movie.");
                    session.RestartBattle();
                    Check(Time.timeScale == 0.5f && relay.RemainingTriggers == 3 && !relay.IsBusy && !relay.UI.IsVisible, "Restart restores custom time and relay budget.");
                    Time.timeScale = 1f;
                    Arrange(); Check(session.TryLaunch(units[0], Vector2.up * 0.1f), "Launch before mid-relay restart."); StopAll();
                    Check(relay.TryQueueCollision(units[0], units[1], 9f), "Relay before restart."); Next(7);
                }
                else if (stage == 7)
                {
                    if (!relay.IsActive) return;
                    session.RestartBattle();
                    Check(Time.timeScale == 1f && relay.RemainingTriggers == 3 && !relay.IsBusy, "Restart cancels active relay.");
                    Next(8);
                }
                else if (stage == 8 && now - stageAt > 2.2)
                {
                    Check(Time.timeScale == 1f && session.Flow.State == BattleState.PlayerSelect && !relay.UI.IsVisible, "No stale timer resumes or hides later UI.");
                    Check(existingGroup.alpha == 0.37f && !existingGroup.interactable, "Original UI group survives all pauses.");
                    Debug.Log("TUANZI_RELAY_CHECKS_PASSED: actual physics Enter; threshold/team gates; real-time 2s timeout; left/right snapshots and portraits; midpoint direction; both button impulses; UI suppression/restoration; three-per-large-round budget; action preservation; impact/death pause handoff; restart.");
                    session = null; EditorApplication.ExitPlaymode();
                }
            }
            catch (Exception e) { Fail(e); }
        }

        private static Sprite Portrait(UnitActor unit) => unit.HeadSprite != null ? unit.HeadSprite : unit.BodyVisual.sprite;
        private static bool Suppressed(Canvas canvas)
        {
            foreach (CanvasGroup group in canvas.GetComponents<CanvasGroup>()) if (group.alpha == 0f && !group.blocksRaycasts) return true;
            return false;
        }
        private static void AssertUIHidden()
        {
            foreach (Canvas canvas in UnityEngine.Object.FindObjectsOfType<Canvas>(true))
                if (!canvas.transform.IsChildOf(relay.UI.transform) && canvas != lateCanvas) Check(Suppressed(canvas), "Other UI hidden: " + canvas.name);
        }
        private static void Arrange()
        {
            StopAll();
            Place(units[0], new Vector2(-1.8f, 0f)); Place(units[1], new Vector2(0f, 0f)); Place(units[2], new Vector2(-3f, 2f));
            for (int i = 3; i < 6; i++) Place(units[i], new Vector2(3f, (i - 4) * 2f));
        }
        private static void Place(UnitActor unit, Vector2 position)
        {
            unit.transform.position = new Vector3(position.x, position.y, unit.transform.position.z);
            unit.Body.position = position; unit.StopMotion(); Physics2D.SyncTransforms();
        }
        private static void StopAll() { foreach (UnitActor unit in units) unit.StopMotion(); }
        private static void Settle()
        {
            StopAll();
            MethodInfo tick = typeof(PrototypeSession).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int i = 0; i < 40 && session.IsResolving; i++) tick.Invoke(session, null);
            Check(!session.IsResolving, "Action settles.");
        }
        private static void Capture(string name)
        {
            Camera camera = Camera.main;
            var target = new RenderTexture(1280, 720, 24);
            RenderTexture oldTarget = camera.targetTexture, oldActive = RenderTexture.active; float aspect = camera.aspect;
            camera.targetTexture = target; camera.aspect = 1280f / 720f;
            Canvas canvas = relay.UI.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1f;
            typeof(CanvasScaler).GetMethod("Handle", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(canvas.GetComponent<CanvasScaler>(), null);
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
            File.WriteAllBytes(Path.Combine(Directory.GetParent(Application.dataPath).FullName, name), texture.EncodeToPNG());
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null;
            camera.targetTexture = oldTarget; camera.aspect = aspect; RenderTexture.active = oldActive;
            UnityEngine.Object.Destroy(texture); UnityEngine.Object.Destroy(target);
        }
        private static void Next(int value) { stage = value; stageAt = EditorApplication.timeSinceStartup; }
        private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        private static void Fail(Exception e)
        {
            SessionState.SetBool(Pending, false); Time.timeScale = 1f; Debug.LogException(e); EditorApplication.Exit(1);
        }
    }
}
