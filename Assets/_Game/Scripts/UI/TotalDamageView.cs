using UnityEngine;
using UnityEngine.UI;

namespace TuanziArena
{
    public sealed class TotalDamageView : MonoBehaviour
    {
        [SerializeField] private RectTransform punchRoot;
        [SerializeField] private BattleUIGraphic panel;
        [SerializeField] private Text title, number;
        [SerializeField] private Outline numberOutline;
        [SerializeField] private DamageBurstGraphic burst;
        [SerializeField] private Image sweep;
        [SerializeField] private BattleUIGraphic[] stars;
        private DamageFeedbackSettings settings;
        private UnitTeam team;
        private int target;
        private bool hasVisibleDamage;
        private float displayed, sinceHit = 10f, clock;
        public int TargetDamage => target;
        public string DisplayedText => number.text;

        public void Configure(RectTransform animated, BattleUIGraphic background, Text heading, Text value,
            Outline edge, DamageBurstGraphic rays, Image shine, BattleUIGraphic[] sparkles)
        {
            punchRoot = animated; panel = background; title = heading; number = value;
            numberOutline = edge; burst = rays; sweep = shine; stars = sparkles;
        }

        public void ApplyStyle(DamageFeedbackSettings style)
        {
            settings = style;
            ((RectTransform)transform).anchoredPosition = style.totalPosition;
            transform.localScale = Vector3.one * style.totalScale;
            panel.color = style.panelColor;
            if (style.labelFont != null) title.font = style.labelFont;
            DamageNumberView.Style(number, numberOutline, style.numberFont, style.totalFontSize,
                style.totalNumberColor, style.totalOutlineWidth, style.totalOutlineColor);
            RefreshTitle();
            RefreshVisibility();
        }

        public void BeginAction(UnitTeam actingTeam)
        {
            team = actingTeam;
            target = 0; displayed = 0f; sinceHit = 10f;
            hasVisibleDamage = false;
            number.text = "0";
            RefreshTitle();
            if (settings != null) Tick(0f);
            RefreshVisibility();
        }

        public void EndAction()
        {
            hasVisibleDamage = false;
            RefreshVisibility();
        }

        private void RefreshVisibility() => gameObject.SetActive(
            settings != null && settings.showTotalDamage && hasVisibleDamage);

        private void RefreshTitle()
        {
            title.text = team == UnitTeam.Player ? "我方 · 总伤害" : "敌方 · 总伤害";
            title.color = team == UnitTeam.Player ? new Color(0.57f, 0.88f, 1f) : new Color(1f, 0.62f, 0.71f);
        }

        public void SetTotal(int value)
        {
            target = value;
            sinceHit = 0f;
            hasVisibleDamage = value > 0;
            RefreshVisibility();
        }

        public void Tick(float delta)
        {
            if (settings == null) return;
            sinceHit += delta;
            clock += delta;
            displayed = Mathf.MoveTowards(displayed, target, Mathf.Max(80f, Mathf.Abs(target - displayed) * 15f) * delta);
            number.text = Mathf.RoundToInt(displayed).ToString();
            float progress = Mathf.Clamp01(sinceHit / Mathf.Max(0.1f, settings.flashDuration));
            float flash = target > 0 && settings.enableFlashes ? 1f - progress : 0f;
            float punch = target > 0 ? Mathf.Sin(Mathf.Clamp01(sinceHit / 0.25f) * Mathf.PI) * settings.totalPunchScale : 0f;
            punchRoot.localScale = Vector3.one * (1f + punch);
            Color tint = settings.flashColor;
            tint.a *= flash * settings.flashIntensity;
            burst.color = tint;
            burst.SetPhase(progress);
            sweep.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-145f, 145f, progress), 0f);
            tint.a *= 0.65f;
            sweep.color = tint;
            for (int i = 0; i < stars.Length; i++)
            {
                float twinkle = (Mathf.Sin(clock * 2.8f + i * 2.2f) + 1f) * 0.5f;
                float intensity = target > 0 && settings.enableFlashes ? (0.15f * twinkle + flash * 0.85f) : 0f;
                tint = settings.flashColor; tint.a *= intensity * settings.flashIntensity;
                stars[i].color = tint;
                stars[i].transform.localScale = Vector3.one * (0.55f + 0.5f * twinkle + 0.3f * flash);
                stars[i].transform.localRotation = Quaternion.Euler(0f, 0f, clock * (i % 2 == 0 ? 15f : -15f));
            }
        }
    }
}
