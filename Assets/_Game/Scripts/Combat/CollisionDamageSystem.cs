using System.Collections.Generic;
using UnityEngine;

namespace TuanziArena
{
    [DisallowMultipleComponent]
    public sealed class CollisionDamageSystem : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float referenceImpactSpeed = 4f;
        [SerializeField, Min(0)] private int minimumDamage = 4;
        [SerializeField, Min(1)] private int maximumDamage = 20;
        private readonly HashSet<ulong> activeContacts = new HashSet<ulong>();
        private int contactActionId = -1;
        private PrototypeSession session;
        private ImpactFeedback impactFeedback;
        public event System.Action<DamageHit> DamageDealt;

        public void Configure(PrototypeSession prototypeSession, UnitActor[] units)
        {
            session = prototypeSession;
            if (Application.isPlaying)
            {
                impactFeedback = GetComponent<ImpactFeedback>();
                if (impactFeedback == null) impactFeedback = gameObject.AddComponent<ImpactFeedback>();
                DamageFeedbackController feedback = GetComponent<DamageFeedbackController>();
                if (feedback == null) feedback = gameObject.AddComponent<DamageFeedbackController>();
                feedback.Configure(session, this);
            }
            activeContacts.Clear();
            contactActionId = -1;
            if (units == null) return;
            foreach (UnitActor unit in units)
            {
                if (unit == null) continue;
                if (unit.GetComponent<UnitStatsUI>() == null) unit.gameObject.AddComponent<UnitStatsUI>();
                UnitCollisionReporter reporter = unit.GetComponent<UnitCollisionReporter>();
                if (reporter == null) reporter = unit.gameObject.AddComponent<UnitCollisionReporter>();
                reporter.Bind(this);
            }
        }

        public int CalculateDamage(UnitActor attacker, float relativeSpeed)
        {
            if (attacker == null || attacker.AttackPower <= 0) return 0;
            float strength = Mathf.Clamp(relativeSpeed / Mathf.Max(0.1f, referenceImpactSpeed), 0.5f, 2f);
            return Mathf.Clamp(Mathf.RoundToInt(attacker.AttackPower * strength),
                Mathf.Min(minimumDamage, maximumDamage), maximumDamage);
        }

        public void ReportImpact(UnitActor unit, float relativeSpeed, Vector2 direction)
        {
            if (session != null && session.IsResolving && unit != null && unit.IsAlive && impactFeedback != null)
                impactFeedback.TryPlay(relativeSpeed, direction);
        }

        public bool TryResolve(UnitActor first, UnitActor second, float relativeSpeed)
        {
            if (session == null || !session.IsResolving || !session.ActiveTeam.HasValue || first == null || second == null ||
                (session.Flow != null && session.Flow.Score != null && session.Flow.Score.Winner.HasValue) ||
                first == second || !first.IsAlive || !second.IsAlive || first.Team == second.Team ||
                !first.Body.simulated || !second.Body.simulated) return false;
            if (contactActionId != session.ActionId)
            {
                activeContacts.Clear();
                contactActionId = session.ActionId;
            }
            ulong key = ContactKey(first, second);
            if (activeContacts.Contains(key)) return false;

            // Every member of the acting faction deals damage; only the opposing faction can lose HP.
            UnitActor attacker = first.Team == session.ActiveTeam.Value ? first : second;
            int damage = CalculateDamage(attacker, relativeSpeed);
            if (damage <= 0) return false;
            activeContacts.Add(key);
            UnitActor defender = attacker == first ? second : first;
            // Capture actual HP loss and the impact location before defeat/respawn callbacks.
            DamageHit hit = new DamageHit(attacker, defender, Mathf.Min(damage, defender.CurrentHealth),
                session.ActionId, defender.Body.position);
            defender.TakeDamage(damage);
            DamageDealt?.Invoke(hit);
            return true;
        }

        public void EndContact(UnitActor first, UnitActor second)
        {
            if (first != null && second != null) activeContacts.Remove(ContactKey(first, second));
        }

        public void ForgetContacts(UnitActor unit)
        {
            if (unit == null) return;
            uint id = (uint)unit.GetInstanceID();
            activeContacts.RemoveWhere(key => (uint)(key >> 32) == id || (uint)key == id);
        }

        private static ulong ContactKey(UnitActor first, UnitActor second)
        {
            int idA = first.GetInstanceID(), idB = second.GetInstanceID();
            return idA < idB ? ((ulong)(uint)idA << 32) | (uint)idB
                : ((ulong)(uint)idB << 32) | (uint)idA;
        }
    }
}
