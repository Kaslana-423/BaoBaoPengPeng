using System.Collections;
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
        private readonly List<UnitActor> waiting = new List<UnitActor>();
        private UnitActor[] units = new UnitActor[0];
        private CollisionDamageSystem damageSystem;
        private DeathPresentationController presentation;
        private Coroutine routine;
        private UnitActor activeUnit;
        public int PendingCount => queued.Count;
        public bool IsPresenting => pending.Count > 0 || routine != null;
        public bool IsWaitingForRespawn { get; private set; }
        public DeathPresentationController Presentation => presentation;

        public void Configure(UnitActor[] actors, CollisionDamageSystem damage)
        {
            CancelPending();
            Unsubscribe();
            units = actors ?? new UnitActor[0];
            damageSystem = damage;
            pending.Clear();
            queued.Clear();
            if (Application.isPlaying)
            {
                presentation = GetComponent<DeathPresentationController>();
                if (presentation == null) presentation = gameObject.AddComponent<DeathPresentationController>();
            }
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
            waiting.Add(unit);
            if (damageSystem != null) damageSystem.ForgetContacts(unit);
            if (Application.isPlaying)
            {
                UnitDeathVisual deathVisual = unit.GetComponent<UnitDeathVisual>();
                if (deathVisual == null) deathVisual = unit.gameObject.AddComponent<UnitDeathVisual>();
                deathVisual.Prepare();
            }
            // Runtime leaves physics disabled throughout the movie; edit-mode checks use two steps.
            pending.Enqueue(new PendingRespawn { Unit = unit, WaitSteps = 1 });
        }

        private void Update()
        {
            if (Application.isPlaying) ProcessPendingRespawns();
        }

        public void ProcessPendingRespawns()
        {
            if (Application.isPlaying)
            {
                if (isActiveAndEnabled && pending.Count > 0 && routine == null)
                    routine = StartCoroutine(PresentDeaths());
                return;
            }
            // Edit-mode rules checks have no rendered frames; keep their deterministic step API.
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
                waiting.Remove(item.Unit);
                if (item.Unit == null || item.Unit.IsAlive) continue;
                if (damageSystem != null) damageSystem.ForgetContacts(item.Unit);
                if (RespawnPlacement.TryFind(item.Unit, 0.08f, out Vector2 position)) item.Unit.RespawnAt(position);
            }
        }

        private IEnumerator PresentDeaths()
        {
            presentation.BeginBatch();
            // Yield once so the coroutine handle exists even when all queued actors were removed.
            yield return null;
            while (pending.Count > 0)
            {
                PendingRespawn item = pending.Dequeue();
                activeUnit = item.Unit;
                if (activeUnit != null && !activeUnit.IsAlive)
                {
                    yield return presentation.Play(activeUnit);
                }
                activeUnit = null;
            }
            // Leave every defeated body hidden and non-simulated while survivors finish moving.
            presentation.FinishBatch();
            routine = null;
        }

        public void RespawnAfterAction(System.Action completed)
        {
            if (IsWaitingForRespawn) return;
            IsWaitingForRespawn = true;
            StartCoroutine(CompleteRespawns(completed));
        }

        private IEnumerator CompleteRespawns(System.Action completed)
        {
            // PrototypeSession has already cleared ActiveTeam/IsResolving and stopped all bodies.
            // No old-action damage or new input is allowed during this delay or the respawn batch.
            while (IsPresenting) yield return null;
            presentation.BeginBatch();
            yield return new WaitForSecondsRealtime(presentation.RespawnDelay);
            bool reportedBlocked = false;
            while (waiting.Count > 0)
            {
                for (int i = 0; i < waiting.Count;)
                {
                    UnitActor unit = waiting[i];
                    if (unit != null && !unit.IsAlive)
                    {
                        if (!RespawnPlacement.TryFind(unit, presentation.RespawnClearance, out Vector2 position))
                        {
                            if (!reportedBlocked) Debug.LogWarning("No clear respawn position. Waiting for free arena space.", unit);
                            reportedBlocked = true;
                            i++;
                            continue;
                        }
                        damageSystem?.ForgetContacts(unit);
                        unit.GetComponent<UnitDeathVisual>()?.Restore();
                        unit.RespawnAt(position);
                        // Reserve this body immediately for subsequent respawns, without a physics step.
                        Physics2D.SyncTransforms();
                    }
                    queued.Remove(unit);
                    waiting.RemoveAt(i);
                }
                if (waiting.Count > 0) yield return new WaitForSecondsRealtime(0.2f);
            }
            presentation.FinishBatch();
            IsWaitingForRespawn = false;
            completed?.Invoke();
        }

        public void CancelPending()
        {
            StopAllCoroutines(); routine = null;
            presentation?.CancelAndRestore();
            foreach (UnitActor unit in queued)
                if (unit != null) unit.GetComponent<UnitDeathVisual>()?.Restore();
            pending.Clear(); queued.Clear(); waiting.Clear(); activeUnit = null;
            IsWaitingForRespawn = false;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            foreach (UnitActor unit in units) if (unit != null && !unit.IsAlive) QueueRespawn(unit);
        }

        private void OnDisable() => CancelPending();

        private void Unsubscribe()
        {
            foreach (UnitActor unit in units) if (unit != null) unit.Defeated -= QueueRespawn;
        }

        private void OnDestroy() { CancelPending(); Unsubscribe(); }
    }
}
