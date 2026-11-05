using UnityEngine;

namespace TuanziArena
{
    // Physics resolution and shared launch entry. GameFlowController owns turn changes.
    public sealed class PrototypeSession : MonoBehaviour
    {
        [SerializeField] private UnitActor[] units;
        [SerializeField, Min(0.01f)] private float settledSpeed = 0.2f;
        [SerializeField, Min(0.01f)] private float settledDuration = 0.5f;
        [SerializeField, Min(1f)] private float maximumResolveTime = 8f;
        [SerializeField] private CollisionDamageSystem damageSystem;
        [SerializeField] private GameFlowController gameFlow;
        [SerializeField] private PlayerInputController launchSettingsSource;
        [SerializeField] private RespawnController respawnController;

        private float quietTime;
        private float elapsed;
        public bool IsResolving { get; private set; }
        public bool CanSelect => !IsResolving && Time.timeScale > 0f && isActiveAndEnabled &&
            gameFlow != null && gameFlow.State == BattleState.PlayerSelect;
        public GameFlowController Flow => gameFlow;
        public RespawnController Respawns => respawnController;
        public float MaxBaseImpulse => launchSettingsSource != null ? launchSettingsSource.MaxBaseImpulse : 10f;

        public UnitActor ActiveShooter { get; private set; }
        public UnitTeam? ActiveTeam { get; private set; }
        public int ActionId { get; private set; }
        public event System.Action<UnitActor, int> ActionStarted;
        public event System.Action ActionEnded;

        private void Awake() { BindCombat(); BindRespawns(); BindFlow(); }

        public void Configure(UnitActor[] sceneUnits)
        {
            units = sceneUnits;
            if (Application.isPlaying) BindCombat();
            BindRespawns();
            BindFlow();
        }

        public void RegisterLaunchSettings(PlayerInputController source) => launchSettingsSource = source;

        // Restart in place: works with the current scene even when it is not in Build Settings.
        public void RestartBattle()
        {
            respawnController?.CancelPending();
            GetComponent<ImpactFeedback>()?.CancelFeedback();
            IsResolving = false;
            ActiveShooter = null;
            ActiveTeam = null;
            ActionId = 0;
            quietTime = elapsed = 0f;
            if (units != null)
                foreach (UnitActor unit in units)
                    if (unit != null) unit.RespawnAtSpawn();
            BindCombat();
            BindRespawns();
            BindFlow();
        }
        public void SetAIDifficulty(int value)
        {
            if (gameFlow == null) BindFlow();
            gameFlow.SetDifficulty(value);
        }

        private void BindFlow()
        {
            if (gameFlow == null) gameFlow = GetComponent<GameFlowController>();
            if (gameFlow == null) gameFlow = gameObject.AddComponent<GameFlowController>();
            gameFlow.Configure(this, units);
        }

        private void BindRespawns()
        {
            if (respawnController == null) respawnController = GetComponent<RespawnController>();
            if (respawnController == null) respawnController = gameObject.AddComponent<RespawnController>();
            respawnController.Configure(units, damageSystem);
        }

        private void BindCombat()
        {
            if (damageSystem == null) damageSystem = GetComponent<CollisionDamageSystem>();
            if (damageSystem == null) damageSystem = gameObject.AddComponent<CollisionDamageSystem>();
            damageSystem.Configure(this, units);
        }

        public bool CanSelectUnit(UnitActor unit)
        {
            return CanSelect && unit != null && unit.Team == UnitTeam.Player &&
                gameFlow.CanLaunchAction(unit, UnitTeam.Player);
        }

        public bool TryLaunch(UnitActor unit, Vector2 impulse)
        {
            return TryLaunchForTeam(unit, impulse, UnitTeam.Player);
        }

        // Shared action entry for either faction. Mouse selection remains restricted to Player.
        public bool TryLaunchForTeam(UnitActor unit, Vector2 impulse, UnitTeam actingTeam)
        {
            if (IsResolving || !isActiveAndEnabled || Time.timeScale <= 0f || gameFlow == null ||
                !gameFlow.CanLaunchAction(unit, actingTeam)) return false;
            float cap = MaxBaseImpulse * (actingTeam == UnitTeam.Enemy ? AIShotPlanner.PowerCap(gameFlow.Difficulty) : 1f);
            if (!unit.TryLaunch(Vector2.ClampMagnitude(impulse, cap))) return false;
            quietTime = 0f;
            elapsed = 0f;
            ActiveShooter = unit;
            ActiveTeam = actingTeam;
            ActionId++;
            IsResolving = true;
            gameFlow.CommitAction(unit);
            ActionStarted?.Invoke(unit, ActionId);
            return true;
        }

        private void FixedUpdate()
        {
            if (!IsResolving) return;
            elapsed += Time.fixedDeltaTime;
            // Dead units stay absent until this action ends. Only the movie blocks settling.
            if (respawnController != null && respawnController.IsPresenting) { quietTime = 0f; return; }
            float highestSpeedSquared = 0f;
            foreach (UnitActor unit in units)
                if (unit != null && unit.isActiveAndEnabled && unit.Body.simulated)
                    highestSpeedSquared = Mathf.Max(highestSpeedSquared, unit.SpeedSquared);

            quietTime = highestSpeedSquared <= settledSpeed * settledSpeed
                ? quietTime + Time.fixedDeltaTime : 0f;
            // The timeout may only discard small residual motion, never a fast shot.
            if (quietTime < settledDuration &&
                !(elapsed >= maximumResolveTime && highestSpeedSquared <= 0.16f)) return;

            foreach (UnitActor unit in units)
                if (unit != null && unit.isActiveAndEnabled) unit.StopMotion();
            IsResolving = false;
            UnitTeam completedTeam = ActiveTeam.Value;
            ActiveShooter = null;
            ActiveTeam = null;
            ActionEnded?.Invoke();
            if (respawnController != null && respawnController.PendingCount > 0)
            {
                gameFlow.BeginRespawnWait();
                respawnController.RespawnAfterAction(() => gameFlow.OnPhysicsSettled(completedTeam));
            }
            else gameFlow.OnPhysicsSettled(completedTeam);
        }
    }
}
