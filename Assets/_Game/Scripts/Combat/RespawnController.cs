using System.Collections.Generic;
using UnityEngine;

namespace TuanziArena
{
    [DisallowMultipleComponent]
    public sealed class RespawnController : MonoBehaviour
    {
        private struct PendingRespawn { public UnitActor Unit; public int WaitSteps; }
        private readonly Queue<PendingRespawn> pending = new Queue<PendingRespawn>();
        private readonly HashSet<UnitActor> queued = new HashSet<UnitActor>();
        private UnitActor[] units = new UnitActor[0];
        private CollisionDamageSystem damageSystem;
        public int PendingCount => pending.Count;

        public void Configure(UnitActor[] actors, CollisionDamageSystem damage)
        {
            Unsubscribe();
            units = actors ?? new UnitActor[0];
            damageSystem = damage;
            pending.Clear();
            queued.Clear();
            foreach (UnitActor unit in units)
            {
                if (unit == null) continue;
                unit.Defeated += QueueRespawn;
                if (!unit.IsAlive) QueueRespawn(unit);
            }
        }

        private void QueueRespawn(UnitActor unit)
        {
            if (unit == null || !queued.Add(unit)) return;
            if (damageSystem != null) damageSystem.ForgetContacts(unit);
            // Leave one physics step disabled so the previous life's contact exits can finish.
            pending.Enqueue(new PendingRespawn { Unit = unit, WaitSteps = 1 });
        }

        private void FixedUpdate() => ProcessPendingRespawns();

        public void ProcessPendingRespawns()
        {
            int count = pending.Count;
            for (int i = 0; i < count; i++)
            {
                PendingRespawn item = pending.Dequeue();
                if (item.WaitSteps > 0)
                {
                    item.WaitSteps--;
                    pending.Enqueue(item);
                    continue;
                }
                queued.Remove(item.Unit);
                if (item.Unit == null || item.Unit.IsAlive) continue;
                if (damageSystem != null) damageSystem.ForgetContacts(item.Unit);
                item.Unit.RespawnAtSpawn();
            }
        }

        private void Unsubscribe()
        {
            foreach (UnitActor unit in units) if (unit != null) unit.Defeated -= QueueRespawn;
        }

        private void OnDestroy() => Unsubscribe();
    }
}
