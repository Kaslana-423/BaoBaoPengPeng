using System.Collections.Generic;

namespace TuanziArena
{
    // Owns round eligibility only; GameFlowController decides which faction plays next.
    public sealed class TurnManager
    {
        public event System.Action EligibilityChanged;
        private readonly HashSet<UnitActor> acted = new HashSet<UnitActor>();
        private UnitActor[] units = new UnitActor[0];
        public IReadOnlyList<UnitActor> Units => units;
        public int RoundNumber { get; private set; } = 1;

        public void Initialize(UnitActor[] actors)
        {
            List<UnitActor> unique = new List<UnitActor>();
            if (actors != null)
                foreach (UnitActor unit in actors)
                    if (unit != null && !unique.Contains(unit)) unique.Add(unit);
            units = unique.ToArray();
            acted.Clear();
            RoundNumber = 1;
            EligibilityChanged?.Invoke();
        }

        public bool HasActed(UnitActor unit) => unit != null && acted.Contains(unit);
        public bool CanAct(UnitActor unit) => unit != null && unit.CanLaunch &&
            System.Array.IndexOf(units, unit) >= 0 && !acted.Contains(unit);

        public bool HasAvailable(UnitTeam team)
        {
            foreach (UnitActor unit in units) if (unit != null && unit.Team == team && CanAct(unit)) return true;
            return false;
        }

        public bool HasUnused(UnitTeam team)
        {
            foreach (UnitActor unit in units)
                if (unit != null && unit.Team == team && !acted.Contains(unit)) return true;
            return false;
        }

        public bool AllHaveActed
        {
            get
            {
                if (units.Length == 0) return false;
                foreach (UnitActor unit in units) if (!acted.Contains(unit)) return false;
                return true;
            }
        }

        public int CountAlive(UnitTeam team)
        {
            int count = 0;
            foreach (UnitActor unit in units)
                if (unit != null && unit.Team == team && unit.CanLaunch) count++;
            return count;
        }

        public void MarkActed(UnitActor unit) { if (acted.Add(unit)) EligibilityChanged?.Invoke(); }
        public void NextRound() { acted.Clear(); RoundNumber++; EligibilityChanged?.Invoke(); }
    }
}
