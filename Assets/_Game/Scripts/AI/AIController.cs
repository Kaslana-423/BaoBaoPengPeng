using UnityEngine;

namespace TuanziArena
{
    [DisallowMultipleComponent]
    public sealed class AIController : MonoBehaviour
    {
        private GameFlowController flow;
        private PrototypeSession session;
        private readonly System.Random random = new System.Random();
        private bool thinking;
        private float remaining;
        public AIShotPlan LastPlan { get; private set; }

        public void Configure(GameFlowController controller, PrototypeSession battle)
        {
            flow = controller;
            session = battle;
            thinking = false;
        }

        private void Update()
        {
            if (flow == null || !flow.isActiveAndEnabled || session == null || !session.isActiveAndEnabled ||
                flow.State != BattleState.AIThinking)
            {
                thinking = false;
                return;
            }
            if (Time.timeScale <= 0f) return;
            if (!thinking) { remaining = flow.ThinkDelay; thinking = true; }
            remaining -= Time.deltaTime;
            if (remaining <= 0f) { thinking = false; TryActNow(); }
        }

        public bool TryActNow()
        {
            if (flow == null || session == null || flow.State != BattleState.AIThinking || Time.timeScale <= 0f) return false;
            if (!AIShotPlanner.TryBuildPlan(flow.Turns, flow.Difficulty, session.MaxBaseImpulse,
                flow.EasyAimErrorDegrees, random, out AIShotPlan plan))
            {
                flow.ReevaluateTurn();
                return false;
            }
            if (!session.TryLaunchForTeam(plan.Shooter, plan.Impulse, UnitTeam.Enemy)) return false;
            LastPlan = plan;
            return true;
        }
    }
}
