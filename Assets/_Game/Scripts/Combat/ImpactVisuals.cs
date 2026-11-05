using System.Collections.Generic;
using UnityEngine;

namespace TuanziArena
{
    public sealed class ImpactVisuals : MonoBehaviour
    {
        private readonly List<CollisionSparkView> active = new List<CollisionSparkView>();
        private readonly Stack<CollisionSparkView> pool = new Stack<CollisionSparkView>();
        private readonly HashSet<UnitHitFlash> flashes = new HashSet<UnitHitFlash>();
        private CollisionSparkView prefab;
        private Material flashMaterial;
        private int sequence;
        public int ActiveSparkCount => active.Count;
        public int SpawnCount { get; private set; }

        public void Play(UnitActor first, UnitActor second, Vector2 point, Vector2 direction, float speed, ImpactFeedbackSettings settings)
        {
            // Sparks and white flashes are reserved for opposing-unit collisions.
            if (first == null || second == null || first.Team == second.Team) return;
            if (!isActiveAndEnabled || speed < settings.minimumVisualSpeed) return;
            if (settings.enableSpark)
            {
                if (prefab == null) prefab = Resources.Load<CollisionSparkView>("FX/PF_CollisionSpark");
                if (prefab != null)
                {
                    if (active.Count >= 24) Recycle(0);
                    CollisionSparkView view = pool.Count > 0 ? pool.Pop() : Instantiate(prefab, transform);
                    float strength = Mathf.InverseLerp(settings.minimumVisualSpeed,
                        Mathf.Max(settings.minimumVisualSpeed + 0.01f, settings.fullImpactSpeed), speed);
                    view.Play(point, direction, Mathf.Lerp(settings.minimumSparkSize, settings.maximumSparkSize, strength),
                        settings.sparkLifetime, settings.sparkColor, sequence++);
                    active.Add(view); SpawnCount++;
                }
            }
            if (settings.enableWhiteFlash)
            {
                if (flashMaterial == null) flashMaterial = Resources.Load<Material>("FX/M_HitWhiteFlash");
                Flash(first, settings); Flash(second, settings);
            }
        }

        private void Flash(UnitActor actor, ImpactFeedbackSettings settings)
        {
            if (actor == null || !actor.IsAlive || flashMaterial == null) return;
            UnitHitFlash flash = actor.GetComponent<UnitHitFlash>();
            if (flash == null) flash = actor.gameObject.AddComponent<UnitHitFlash>();
            flashes.Add(flash);
            flash.Play(flashMaterial, settings.whiteFlashSeconds, settings.whiteFlashIntensity);
        }

        private void Update()
        {
            for (int i = active.Count - 1; i >= 0; i--)
                if (!active[i].Tick(Time.unscaledDeltaTime)) Recycle(i);
        }

        private void Recycle(int index)
        {
            CollisionSparkView view = active[index]; active.RemoveAt(index);
            view.gameObject.SetActive(false); pool.Push(view);
        }

        public void Clear()
        {
            while (active.Count > 0) Recycle(active.Count - 1);
            foreach (UnitHitFlash flash in flashes) if (flash != null) flash.Clear();
        }
        private void OnDisable() => Clear();
    }
}
