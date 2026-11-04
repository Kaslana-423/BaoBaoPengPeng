using System.Collections.Generic;
using UnityEngine;

namespace TuanziArena
{
    public sealed class DamageFeedbackUI : MonoBehaviour
    {
        [SerializeField] private RectTransform floatingLayer;
        [SerializeField] private DamageNumberView numberPrefab;
        [SerializeField] private TotalDamageView total;
        [SerializeField] private Camera worldCamera;
        private readonly List<DamageNumberView> active = new List<DamageNumberView>();
        private readonly Stack<DamageNumberView> pool = new Stack<DamageNumberView>();
        private DamageFeedbackSettings settings;
        private Canvas canvas;
        private int styleRevision = -1;
        public int ActiveNumberCount => active.Count;
        public TotalDamageView Total => total;
        public IReadOnlyList<DamageNumberView> ActiveNumbers => active;

        public void Configure(RectTransform layer, DamageNumberView template, TotalDamageView counter)
        {
            floatingLayer = layer; numberPrefab = template; total = counter;
        }

        public void Initialize(DamageFeedbackSettings style)
        {
            settings = style;
            canvas = GetComponent<Canvas>();
            styleRevision = -1;
            ApplyStyle();
            while (active.Count + pool.Count < 8)
            {
                DamageNumberView view = Instantiate(numberPrefab, floatingLayer);
                view.gameObject.SetActive(false);
                pool.Push(view);
            }
        }

        public void ResetView() => BeginAction(UnitTeam.Player);

        public void EndAction() => total.EndAction();

        public void BeginAction(UnitTeam team)
        {
            while (active.Count > 0) Recycle(active.Count - 1);
            total.BeginAction(team);
        }

        public void ShowHit(DamageHit hit, int totalDamage, int hitCount)
        {
            if (settings == null) return;
            ApplyStyle();
            total.SetTotal(totalDamage);
            if (!settings.showFloatingNumbers) return;
            while (active.Count >= Mathf.Max(1, settings.maximumNumbers)) Recycle(0);
            DamageNumberView view = pool.Count > 0 ? pool.Pop() : Instantiate(numberPrefab, floatingLayer);
            // Alternate three lanes to keep rapid wall-combo damage readable without global Random.
            int lane = (hitCount - 1) % 3 - 1;
            view.Show(hit, settings, lane);
            view.transform.SetAsLastSibling();
            active.Add(view);
            RefreshNumber(view, 0f);
        }

        private void ApplyStyle()
        {
            if (settings == null || styleRevision == settings.Revision) return;
            styleRevision = settings.Revision;
            total.ApplyStyle(settings);
            foreach (DamageNumberView view in active) view.ApplyStyle(settings);
        }

        private void LateUpdate()
        {
            if (settings == null) return;
            ApplyStyle();
            total.Tick(Time.unscaledDeltaTime);
            for (int i = active.Count - 1; i >= 0; i--)
                if (!settings.showFloatingNumbers || !RefreshNumber(active[i], Time.unscaledDeltaTime)) Recycle(i);
        }

        private bool RefreshNumber(DamageNumberView view, float delta)
        {
            if (worldCamera == null) worldCamera = Camera.main;
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            return view.Tick(delta, worldCamera, floatingLayer, uiCamera);
        }

        private void Recycle(int index)
        {
            DamageNumberView view = active[index];
            active.RemoveAt(index);
            view.gameObject.SetActive(false);
            pool.Push(view);
        }
    }
}
