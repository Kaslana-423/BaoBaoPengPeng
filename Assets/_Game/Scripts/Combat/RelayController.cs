using System.Collections.Generic;
using UnityEngine;

namespace TuanziArena
{
    [DisallowMultipleComponent, DefaultExecutionOrder(500)]
    public sealed class RelayController : MonoBehaviour
    {
        [SerializeField] private RelaySettings settings;
        [SerializeField] private RelayUI uiPrefab;
        [SerializeField] private Camera worldCamera;
        private PrototypeSession session;
        private GameFlowController flow;
        private RelaySettings fallback;
        private ImpactFeedback impact;
        private bool impactWasEnabled, queued, ownsPause;
        private float previousTimeScale;
        private double deadline;
        private int startedFrame, roundNumber;
        private sealed class HiddenCanvas
        {
            public CanvasGroup Group;
            public float Alpha;
            public bool Interactable, BlocksRaycasts;
        }
        private readonly Dictionary<Canvas, HiddenCanvas> hiddenUI = new Dictionary<Canvas, HiddenCanvas>();
        private RelayAimArrow arrow;
        public RelaySettings Settings => settings;
        public RelayUI UI { get; private set; }
        public bool IsActive => ownsPause;
        public bool IsBusy => queued || ownsPause;
        public int UsedThisRound { get; private set; }
        public int RemainingTriggers => Mathf.Max(0, settings.maxTriggersPerRound - UsedThisRound);
        public float RemainingSeconds => ownsPause ? Mathf.Max(0f, (float)(deadline - Time.realtimeSinceStartupAsDouble)) : 0f;
        public UnitActor LeftUnit { get; private set; }
        public UnitActor RightUnit { get; private set; }
        public Vector2 Midpoint { get; private set; }

        public void Configure(PrototypeSession battle)
        {
            Cancel();
            if (flow != null) flow.RoundChanged -= OnRoundChanged;
            session = battle; flow = session.Flow;
            if (settings == null) settings = Resources.Load<RelaySettings>("RelaySettings");
            if (settings == null)
            {
                fallback = ScriptableObject.CreateInstance<RelaySettings>();
                fallback.hideFlags = HideFlags.HideAndDontSave; settings = fallback;
            }
            if (uiPrefab == null) uiPrefab = Resources.Load<RelayUI>("UI/PF_RelayUI");
            if (UI == null && uiPrefab != null) UI = Instantiate(uiPrefab, transform);
            if (UI == null) Debug.LogError("Missing PF_RelayUI. Run Tools > Tuanzi > Create Relay Assets.", this);
            UI?.Hide();
            if (arrow == null) arrow = gameObject.AddComponent<RelayAimArrow>();
            if (worldCamera == null) worldCamera = Camera.main;
            impact = GetComponent<ImpactFeedback>();
            flow.RoundChanged += OnRoundChanged;
            OnRoundChanged(flow.RoundNumber);
        }

        private void OnRoundChanged(int number)
        {
            Cancel(); roundNumber = number; UsedThisRound = 0;
        }

        private bool CanRelay(UnitActor first, UnitActor second)
        {
            return session != null && session.isActiveAndEnabled && session.IsResolving &&
                session.ActiveTeam == UnitTeam.Player && flow.State == BattleState.PhysicsResolving &&
                !flow.Score.Winner.HasValue && (session.Respawns == null || !session.Respawns.IsPresenting) &&
                first != null && second != null && first != second && first.CanLaunch && second.CanLaunch &&
                first.Team == UnitTeam.Player && second.Team == UnitTeam.Player;
        }

        // Called once on collision ENTER. Position/left-right assignment are captured here,
        // not when the player finally clicks. Same-frame death callbacks get priority in LateUpdate.
        public bool TryQueueCollision(UnitActor first, UnitActor second, float relativeSpeed)
        {
            if (!Application.isPlaying || !isActiveAndEnabled || settings == null || !settings.enableRelay ||
                IsBusy || UI == null || relativeSpeed <= settings.minimumImpactSpeed || !CanRelay(first, second)) return false;
            if (roundNumber != flow.RoundNumber) OnRoundChanged(flow.RoundNumber);
            if (RemainingTriggers <= 0 || (Time.timeScale <= 0f && (impact == null || !impact.IsHitStopped))) return false;
            float dx = first.Body.position.x - second.Body.position.x;
            bool firstIsLeft = dx == 0f ? Random.value < 0.5f : dx < 0f;
            LeftUnit = firstIsLeft ? first : second;
            RightUnit = firstIsLeft ? second : first;
            Midpoint = (first.Body.position + second.Body.position) * 0.5f;
            queued = true;
            return true;
        }

        private void Begin()
        {
            queued = false;
            impactWasEnabled = impact != null && impact.enabled;
            impact?.CancelFeedback();
            if (impact != null) impact.enabled = false;
            previousTimeScale = Time.timeScale;
            ownsPause = true; Time.timeScale = 0f;
            UsedThisRound++;
            startedFrame = Time.frameCount;
            deadline = Time.realtimeSinceStartupAsDouble + Mathf.Max(0.1f, settings.countdownSeconds);
            UI.Show(LeftUnit, RightUnit, settings.countdownSeconds, RemainingTriggers);
            HideOtherUI();
        }

        private void LateUpdate()
        {
            if (queued)
            {
                if (!CanRelay(LeftUnit, RightUnit)) { Cancel(); return; }
                Begin();
            }
            if (!IsActive) return;
            if (!CanRelay(LeftUnit, RightUnit)) { Cancel(); return; }
            float remaining = RemainingSeconds;
            UI.SetCountdown(remaining, Mathf.Max(0.1f, settings.countdownSeconds));
            if (remaining <= 0f) { Cancel(); return; }
            // Capture newly created canvases too, keeping Canvas.enabled independent from this
            // visibility mask (death visuals can still disable their own unit canvases).
            HideOtherUI();
            if (TryMousePoint(out Vector2 mouse))
            {
                arrow.Show(Midpoint, mouse, settings);
                // A launch/release click from the triggering frame must never auto-confirm relay.
                if (Time.frameCount > startedFrame && Application.isFocused)
                {
                    if (Input.GetMouseButtonDown(0)) TryChoose(0, mouse);
                    else if (Input.GetMouseButtonDown(1)) TryChoose(1, mouse);
                }
            }
            else arrow.Hide();
        }

        // UI/input integration: 0 = captured left actor, 1 = captured right actor.
        public bool TryChoose(int mouseButton, Vector2 mouseWorld)
        {
            if (!IsActive || RemainingSeconds <= 0f || (mouseButton != 0 && mouseButton != 1) || !CanRelay(LeftUnit, RightUnit)) return false;
            Vector2 delta = mouseWorld - Midpoint;
            if (delta.sqrMagnitude < settings.minimumAimDistance * settings.minimumAimDistance) return false;
            UnitActor chosen = mouseButton == 0 ? LeftUnit : RightUnit;
            if (!settings.preserveVelocity) chosen.StopMotion();
            chosen.Body.AddForce(delta.normalized * Mathf.Max(0f, settings.impulse), ForceMode2D.Impulse);
            // Preserve ActiveShooter, ActiveTeam, ActionId and the six actors' action eligibility.
            Cancel();
            return true;
        }

        private bool TryMousePoint(out Vector2 point)
        {
            point = default;
            if (worldCamera == null) worldCamera = Camera.main;
            if (worldCamera == null || !worldCamera.pixelRect.Contains(Input.mousePosition)) return false;
            Ray ray = worldCamera.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, LeftUnit.transform.position.z));
            if (!plane.Raycast(ray, out float distance)) return false;
            point = ray.GetPoint(distance); return true;
        }

        private void HideOtherUI()
        {
            foreach (Canvas canvas in FindObjectsOfType<Canvas>(true))
            {
                if (canvas.gameObject.scene != gameObject.scene || canvas.transform.IsChildOf(UI.transform)) continue;
                if (!hiddenUI.TryGetValue(canvas, out HiddenCanvas hidden))
                {
                    CanvasGroup cover = canvas.GetComponent<CanvasGroup>();
                    if (cover == null) cover = canvas.gameObject.AddComponent<CanvasGroup>();
                    hidden = new HiddenCanvas { Group = cover, Alpha = cover.alpha,
                        Interactable = cover.interactable, BlocksRaycasts = cover.blocksRaycasts };
                    hiddenUI.Add(canvas, hidden);
                }
                if (hidden.Group == null) continue;
                hidden.Group.alpha = 0f; hidden.Group.blocksRaycasts = false; hidden.Group.interactable = false;
            }
        }

        public void Cancel()
        {
            queued = false;
            UI?.Hide(); arrow?.Hide();
            foreach (HiddenCanvas hidden in hiddenUI.Values)
                if (hidden.Group != null)
                {
                    hidden.Group.alpha = hidden.Alpha;
                    hidden.Group.interactable = hidden.Interactable;
                    hidden.Group.blocksRaycasts = hidden.BlocksRaycasts;
                }
            hiddenUI.Clear();
            LeftUnit = RightUnit = null;
            if (!ownsPause) return;
            ownsPause = false;
            if (Time.timeScale == 0f) Time.timeScale = previousTimeScale;
            if (impact != null) impact.enabled = impactWasEnabled;
        }

        private void OnDisable() => Cancel();
        private void OnDestroy()
        {
            Cancel();
            if (flow != null) flow.RoundChanged -= OnRoundChanged;
            if (fallback != null) Destroy(fallback);
        }
    }
}
