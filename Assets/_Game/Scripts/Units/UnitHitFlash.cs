using UnityEngine;

namespace TuanziArena
{
    [DisallowMultipleComponent, RequireComponent(typeof(UnitActor))]
    public sealed class UnitHitFlash : MonoBehaviour
    {
        private UnitActor owner;
        private SpriteRenderer source, overlay;
        private float age, duration, intensity;
        public bool IsFlashing => overlay != null && overlay.enabled;

        private void Awake()
        {
            owner = GetComponent<UnitActor>();
            owner.Respawned += OnRespawned;
        }

        public void Play(Material material, float seconds, float strength)
        {
            source = owner.BodyVisual;
            if (!isActiveAndEnabled || !owner.IsAlive || source == null || material == null) return;
            if (overlay == null)
            {
                GameObject child = new GameObject("HitWhiteFlash");
                child.transform.SetParent(source.transform, false);
                overlay = child.AddComponent<SpriteRenderer>();
            }
            overlay.sharedMaterial = material;
            age = 0f;
            duration = Mathf.Max(0.02f, seconds);
            intensity = Mathf.Clamp01(strength);
            Refresh();
        }

        private void LateUpdate()
        {
            if (!IsFlashing) return;
            age += Time.unscaledDeltaTime;
            if (age >= duration || source == null) { Clear(); return; }
            Refresh();
        }

        private void Refresh()
        {
            overlay.sprite = source.sprite;
            overlay.flipX = source.flipX; overlay.flipY = source.flipY;
            overlay.drawMode = source.drawMode; overlay.size = source.size;
            overlay.maskInteraction = source.maskInteraction;
            overlay.sortingLayerID = source.sortingLayerID;
            overlay.sortingOrder = source.sortingOrder + 2;
            float fade = 1f - Mathf.InverseLerp(0.35f, 1f, age / duration);
            overlay.color = new Color(1f, 1f, 1f, source.color.a * intensity * fade);
            overlay.enabled = source.enabled;
        }

        public void Clear() { if (overlay != null) overlay.enabled = false; }
        private void OnRespawned(UnitActor unit) => Clear();
        private void OnDisable() => Clear();
        private void OnDestroy()
        {
            if (owner != null) owner.Respawned -= OnRespawned;
            if (overlay != null) Destroy(overlay.gameObject);
        }
    }
}
