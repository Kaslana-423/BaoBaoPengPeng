using System;
using UnityEngine;

namespace TuanziArena
{
    public enum BattleState { BattleSetup, PlayerSelect, PhysicsResolving, AIThinking, ScorePresentation, BattleEnded, RespawnWaiting }

    [DisallowMultipleComponent]
    public sealed class GameFlowController : MonoBehaviour
    {
        [SerializeField] private AISettings settings;
        [SerializeField] private AIDifficulty difficulty = AIDifficulty.Medium;
        [SerializeField] private BattleState state = BattleState.BattleSetup;
        private PrototypeSession session;
        private AISettings fallbackSettings;
        private bool difficultyInitialized;
        public TurnManager Turns { get; } = new TurnManager();
        public AIController AI { get; private set; }
        public BattleScoreController Score { get; private set; }
        public BattleState State => state;
        public AIDifficulty Difficulty => difficulty;
        public int RoundNumber => Turns.RoundNumber;
        public float ThinkDelay => settings != null ? Mathf.Max(0f, settings.thinkDelay) : 0.6f;
        public float EasyAimErrorDegrees => settings != null ? settings.easyAimErrorDegrees : 22f;
        public event Action<BattleState> StateChanged;
        public event Action<AIDifficulty> DifficultyChanged;
        public event Action<int> RoundChanged;

        public void Configure(PrototypeSession battle, UnitActor[] units)
        {
            session = battle;
            if (settings == null) settings = Resources.Load<AISettings>("AISettings");
            if (settings == null)
            {
                fallbackSettings = ScriptableObject.CreateInstance<AISettings>();
                fallbackSettings.hideFlags = HideFlags.HideAndDontSave;
                settings = fallbackSettings;
            }
            if (!difficultyInitialized) { difficulty = settings.defaultDifficulty; difficultyInitialized = true; }
            Turns.Initialize(units);
            foreach (UnitActor unit in Turns.Units)
                unit.GetComponent<UnitStatsUI>()?.BindFlow(this);
            AI = GetComponent<AIController>();
            if (AI == null) AI = gameObject.AddComponent<AIController>();
            AI.Configure(this, session);
            Score = GetComponent<BattleScoreController>();
            if (Score == null) Score = gameObject.AddComponent<BattleScoreController>();
            Score.Configure(session, this, Turns.Units);
            SetState(BattleState.BattleSetup);
            RoundChanged?.Invoke(RoundNumber);
            if (Turns.Units.Count > 0) EnterAvailableTurn(UnitTeam.Player);
        }

        public void SetDifficulty(AIDifficulty value)
        {
            value = (AIDifficulty)Mathf.Clamp((int)value, 0, 2);
            difficultyInitialized = true;
            if (difficulty == value) return;
            difficulty = value;
            DifficultyChanged?.Invoke(value);
        }

        // Compatible with Dropdown/TMP_Dropdown.onValueChanged(int).
        public void SetDifficulty(int value) => SetDifficulty((AIDifficulty)value);

        public bool CanLaunchAction(UnitActor unit, UnitTeam team)
        {
            return isActiveAndEnabled && Turns.CanAct(unit) && unit.Team == team &&
                ((team == UnitTeam.Player && state == BattleState.PlayerSelect) ||
                 (team == UnitTeam.Enemy && state == BattleState.AIThinking));
        }

        public void CommitAction(UnitActor unit)
        {
            Turns.MarkActed(unit);
            SetState(BattleState.PhysicsResolving);
        }

        public void BeginRespawnWait()
        {
            if (state == BattleState.PhysicsResolving) SetState(BattleState.RespawnWaiting);
        }

        public void OnPhysicsSettled(UnitTeam actingTeam)
        {
            if (state != BattleState.PhysicsResolving && state != BattleState.RespawnWaiting) return;
            if (Score.HasUnpresentedScore)
            {
                SetState(BattleState.ScorePresentation);
                Score.PresentAction(actingTeam);
                return;
            }
            EnterAvailableTurn(actingTeam == UnitTeam.Player ? UnitTeam.Enemy : UnitTeam.Player);
        }

        internal void CompleteScorePresentation(UnitTeam actingTeam)
        {
            if (state != BattleState.ScorePresentation) return;
            if (Score.Winner.HasValue) SetState(BattleState.BattleEnded);
            else EnterAvailableTurn(actingTeam == UnitTeam.Player ? UnitTeam.Enemy : UnitTeam.Player);
        }

        public void ReevaluateTurn()
        {
            if (state == BattleState.PhysicsResolving || state == BattleState.RespawnWaiting || state == BattleState.ScorePresentation ||
                state == BattleState.BattleEnded) return;
            EnterAvailableTurn(state == BattleState.AIThinking ? UnitTeam.Enemy : UnitTeam.Player);
        }

        private void EnterAvailableTurn(UnitTeam preferred)
        {
            if (Turns.AllHaveActed)
            {
                Turns.NextRound();
                RoundChanged?.Invoke(RoundNumber);
                preferred = UnitTeam.Player;
            }
            UnitTeam other = preferred == UnitTeam.Player ? UnitTeam.Enemy : UnitTeam.Player;
            // Death never removes a roster member's unused action. Wait for its respawn if necessary.
            SetTurn(Turns.HasUnused(preferred) ? preferred : other);
        }

        private void SetTurn(UnitTeam team) => SetState(team == UnitTeam.Player ? BattleState.PlayerSelect : BattleState.AIThinking);

        private void SetState(BattleState value)
        {
            if (state == value) return;
            state = value;
            StateChanged?.Invoke(value);
        }

        private void OnDestroy()
        {
            if (fallbackSettings != null)
            {
                if (Application.isPlaying) Destroy(fallbackSettings); else DestroyImmediate(fallbackSettings);
            }
        }
    }
}
