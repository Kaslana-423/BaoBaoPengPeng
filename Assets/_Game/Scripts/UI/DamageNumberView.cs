using UnityEngine;
using UnityEngine.UI;

namespace TuanziArena
{
    public sealed class DamageNumberView : MonoBehaviour
    {
        [SerializeField] private Text value;
        [SerializeField] private Outline outline;
        [SerializeField] private CanvasGroup fade;
        private DamageFeedbackSettings settings;
        private Vector3 worldPosition;
        private UnitTeam team;
        private float age;
        private int lane;
        public string DisplayedText => value.text;
        public Vector3 HitPosition => worldPosition;

        public void Configure(Text label, Outline edge, CanvasGroup group)
        {
            value = label; outline = edge; fade = group;
        }

        public void Show(DamageHit hit, DamageFeedbackSettings style, int separationLane)
        {
            settings = style;
            worldPosition = hit.Position + style.worldOffset;
            team = hit.Attacker.Team;
            lane = separationLane;
            age = 0f;
            value.text = "-" + hit.Amount;
            ApplyStyle(style);
            fade.alpha = 1f;
            transform.localScale = Vector3.one * 0.65f;
            transform.localRotation = Quaternion.Euler(0f, 0f, lane * -5f);
            gameObject.SetActive(true);
        }

        public void ApplyStyle(DamageFeedbackSettings style)
        {
            settings = style;
            Style(value, outline, style.numberFont, style.numberFontSize,
                team == UnitTeam.Player ? style.playerDamageColor : style.enemyDamageColor,
                style.outlineWidth, style.outlineColor);
        }

        // The number keeps its impact-world position; it never follows a respawning target.
        public bool Tick(float delta, Camera worldCamera, RectTransform layer, Camera uiCamera)
        {
            age += delta;
            float p = Mathf.Clamp01(age / Mathf.Max(0.1f, settings.numberLifetime));
            if (p >= 1f) return false;
            if (worldCamera == null) { fade.alpha = 0f; return true; }
            Vector3 screen = worldCamera.WorldToScreenPoint(worldPosition);
            if (screen.z <= 0f) { fade.alpha = 0f; return true; }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, uiCamera, out Vector2 anchor);
            ((RectTransform)transform).anchoredPosition = anchor + new Vector2(
                lane * (settings.numberSeparation + p * settings.horizontalDrift),
                (1f - Mathf.Pow(1f - p, 2f)) * settings.riseDistance + Mathf.Abs(lane) * 10f);
            float scale = age < 0.10f
                ? Mathf.Lerp(0.65f, settings.popScale, age / 0.10f)
                : Mathf.Lerp(settings.popScale, 1f, Mathf.Clamp01((age - 0.10f) / 0.16f));
            transform.localScale = Vector3.one * scale;
            fade.alpha = 1f - Mathf.InverseLerp(settings.fadeStart, 1f, p);
            return true;
        }

        public static void Style(Text label, Outline edge, Font font, int size, Color color, float width, Color edgeColor)
        {
            label.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = Mathf.Max(1, size);
            label.color = color;
            edge.enabled = width > 0f;
            edge.effectDistance = Vector2.one * Mathf.Max(0f, width);
            edge.effectColor = edgeColor;
            edge.useGraphicAlpha = true;
        }
    }
}
