using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TuanziArena
{
    // One point per opposing defeat. All defeated units return only after the action ends;
    // later actions can score against their new lives.
    [DisallowMultipleComponent]
    public sealed class BattleScoreController : MonoBehaviour
    {
        public const int WinningScore = 6;
        [SerializeField] private BattleScoreUI scorePrefab;
        [SerializeField] private BattleResultUI resultPrefab;
        private readonly List<UnitActor> subscribedUnits = new List<UnitActor>();
        private PrototypeSession session;
        private GameFlowController flow;
        private int presentedPlayer, presentedEnemy;
        private bool ownsPause;
        private float resumeTimeScale = 1f;
        public int PlayerScore { get; private set; }
        public int EnemyScore { get; private set; }
        public UnitTeam? Winner { get; private set; }
        public bool HasUnpresentedScore => PlayerScore != presentedPlayer || EnemyScore != presentedEnemy;
        public BattleScoreUI ScoreUI { get; private set; }
        public BattleResultUI ResultUI { get; private set; }
        public event Action<int, int> ScoreChanged;
        public event Action<UnitTeam> BattleFinished;

        public void Configure(PrototypeSession battle, GameFlowController controller, IReadOnlyList<UnitActor> units)
        {
            StopAllCoroutines();
            ReleasePause();
            Unsubscribe();
            session = battle;
            flow = controller;
            PlayerScore = EnemyScore = presentedPlayer = presentedEnemy = 0;
            Winner = null;
            if (units != null)
                foreach (UnitActor unit in units)
                {
                    if (unit == null || subscribedUnits.Contains(unit)) continue;
                    subscribedUnits.Add(unit);
                    unit.Defeated += OnDefeated;
                }
            if (Application.isPlaying) EnsureViews();
            ScoreUI?.ResetView();
            ResultUI?.Hide();
            ScoreChanged?.Invoke(0, 0);
        }

        private void OnDefeated(UnitActor victim)
        {
            // Ignore setup/debug deaths and friendly deaths: only the active attacking side scores.
            if (Winner.HasValue || session == null || !session.IsResolving ||
                flow.State != BattleState.PhysicsResolving || !session.ActiveTeam.HasValue ||
                victim.Team == session.ActiveTeam.Value) return;
            UnitTeam scorer = session.ActiveTeam.Value;
            if (scorer == UnitTeam.Player) PlayerScore = Mathf.Min(WinningScore, PlayerScore + 1);
            else EnemyScore = Mathf.Min(WinningScore, EnemyScore + 1);
            if (PlayerScore >= WinningScore || EnemyScore >= WinningScore) Winner = scorer;
            ScoreUI?.SetHUD(PlayerScore, EnemyScore);
            ScoreChanged?.Invoke(PlayerScore, EnemyScore);
        }

        private void EnsureViews()
        {
            if (scorePrefab == null) scorePrefab = Resources.Load<BattleScoreUI>("UI/PF_BattleScoreUI");
            if (resultPrefab == null) resultPrefab = Resources.Load<BattleResultUI>("UI/PF_BattleResultUI");
            if (ScoreUI == null && scorePrefab != null) ScoreUI = Instantiate(scorePrefab, transform);
            if (ResultUI == null && resultPrefab != null) ResultUI = Instantiate(resultPrefab, transform);
            if (ScoreUI == null || ResultUI == null)
                Debug.LogError("Missing battle UI prefabs. Run Tools > Tuanzi > Create Score UI Assets.", this);
            if (ResultUI != null) ResultUI.BindRestart(session.RestartBattle);
        }

        internal void PresentAction(UnitTeam actingTeam)
        {
            if (!Application.isPlaying) return;
            // End the short impact pause before acquiring the longer presentation pause.
            // Its timer must never unpause our score/result screen later.
            GetComponent<ImpactFeedback>()?.CancelFeedback();
            resumeTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            ownsPause = true;
            Time.timeScale = 0f;
            StartCoroutine(Present(actingTeam));
        }

        private IEnumerator Present(UnitTeam actingTeam)
        {
            if (ScoreUI != null)
                yield return ScoreUI.PlayScore(presentedPlayer, presentedEnemy, PlayerScore, EnemyScore);
            presentedPlayer = PlayerScore;
            presentedEnemy = EnemyScore;
            if (Winner.HasValue)
            {
                flow.CompleteScorePresentation(actingTeam);
                ResultUI?.Show(Winner.Value == UnitTeam.Player, PlayerScore, EnemyScore);
                BattleFinished?.Invoke(Winner.Value);
                // Keep physics and AI paused until RestartBattle or scene destruction.
            }
            else
            {
                ReleasePause();
                flow.CompleteScorePresentation(actingTeam);
            }
        }

        private void ReleasePause()
        {
            if (!ownsPause) return;
            if (Time.timeScale == 0f) Time.timeScale = resumeTimeScale;
            ownsPause = false;
        }

        private void Unsubscribe()
        {
            foreach (UnitActor unit in subscribedUnits) if (unit != null) unit.Defeated -= OnDefeated;
            subscribedUnits.Clear();
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            Unsubscribe();
            ReleasePause();
        }
    }
}
