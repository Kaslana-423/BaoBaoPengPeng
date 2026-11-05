using System;
using UnityEngine;

namespace TuanziArena
{
    [DisallowMultipleComponent]
    public sealed class DamageFeedbackController : MonoBehaviour
    {
        [SerializeField] private DamageFeedbackSettings settings;
        [SerializeField] private DamageFeedbackUI uiPrefab;
        private DamageFeedbackSettings fallback;
        private PrototypeSession session;
        private CollisionDamageSystem damage;
        private int actionId = -1;
        public int TotalDamage { get; private set; }
        public int HitCount { get; private set; }
        public UnitTeam ActingTeam { get; private set; }
        public DamageFeedbackUI UI { get; private set; }
        public event Action<int> TotalDamageChanged;

        public void Configure(PrototypeSession battle, CollisionDamageSystem source)
        {
            Unbind();
            session = battle; damage = source;
            if (session != null)
            {
                session.ActionStarted += OnActionStarted;
                session.ActionEnded += OnActionEnded;
            }
            if (damage != null) damage.DamageDealt += OnDamage;
            if (Application.isPlaying)
            {
                if (settings == null) settings = Resources.Load<DamageFeedbackSettings>("DamageFeedbackSettings");
                if (settings == null)
                {
                    fallback = ScriptableObject.CreateInstance<DamageFeedbackSettings>();
                    fallback.hideFlags = HideFlags.HideAndDontSave;
                    settings = fallback;
                }
                if (uiPrefab == null) uiPrefab = Resources.Load<DamageFeedbackUI>("UI/PF_DamageFeedbackUI");
                if (UI == null && uiPrefab != null) UI = Instantiate(uiPrefab, transform);
                if (UI != null) UI.Initialize(settings);
                else Debug.LogError("Missing PF_DamageFeedbackUI. Run Tools > Tuanzi > Create Damage UI Assets.", this);
            }
            actionId = -1;
            TotalDamage = HitCount = 0;
            ActingTeam = UnitTeam.Player;
            UI?.ResetView();
            TotalDamageChanged?.Invoke(0);
        }

        private void OnActionStarted(UnitActor actor, int id)
        {
            actionId = id;
            TotalDamage = HitCount = 0;
            // A hazard can be the first thing to hurt anyone in an action, and it carries no attacker.
            if (actor != null) ActingTeam = actor.Team;
            UI?.BeginAction(ActingTeam);
            TotalDamageChanged?.Invoke(0);
        }

        private void OnDamage(DamageHit hit)
        {
            if (hit.Amount <= 0) return;
            if (hit.ActionId != actionId) OnActionStarted(hit.Attacker, hit.ActionId);
            // A hazard hit with no attacker is self-inflicted: it is shown in its own colour, but it
            // belongs to nobody and must not inflate this action's damage total.
            if (hit.Attacker == null)
            {
                UI?.ShowHazardHit(hit);
                return;
            }
            TotalDamage = (int)Math.Min(int.MaxValue, (long)TotalDamage + hit.Amount);
            HitCount++;
            UI?.ShowHit(hit, TotalDamage, HitCount);
            TotalDamageChanged?.Invoke(TotalDamage);
        }

        private void OnActionEnded() => UI?.EndAction();

        private void Unbind()
        {
            if (session != null)
            {
                session.ActionStarted -= OnActionStarted;
                session.ActionEnded -= OnActionEnded;
            }
            if (damage != null) damage.DamageDealt -= OnDamage;
        }
        private void OnDestroy()
        {
            Unbind();
            if (fallback != null)
            {
                if (Application.isPlaying) Destroy(fallback); else DestroyImmediate(fallback);
            }
        }
    }
}
