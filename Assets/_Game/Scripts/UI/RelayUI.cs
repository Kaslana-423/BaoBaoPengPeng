using UnityEngine;
using UnityEngine.UI;

namespace TuanziArena
{
    public sealed class RelayUI : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform panel, progress;
        [SerializeField] private Image leftPortrait, rightPortrait;
        [SerializeField] private Text countdown, quota;
        private float shownAt;
        public bool IsVisible => canvas != null && canvas.enabled;
        public Sprite LeftPortrait => leftPortrait.sprite;
        public Sprite RightPortrait => rightPortrait.sprite;
        public float ProgressRatio => progress.anchorMax.x - progress.anchorMin.x;

        public void Configure(Canvas rootCanvas, RectTransform content, RectTransform fill,
            Image left, Image right, Text timer, Text remaining)
        {
            canvas = rootCanvas; panel = content; progress = fill;
            leftPortrait = left; rightPortrait = right; countdown = timer; quota = remaining;
        }

        public void Show(UnitActor left, UnitActor right, float seconds, int remaining)
        {
            SetPortrait(leftPortrait, left); SetPortrait(rightPortrait, right);
            quota.text = "本大轮剩余 " + remaining + " 次接力";
            SetCountdown(seconds, seconds);
            shownAt = Time.unscaledTime;
            panel.localScale = Vector3.one * 0.94f;
            canvas.enabled = true;
        }

        private static void SetPortrait(Image image, UnitActor unit)
        {
            bool hasHead = unit.HeadSprite != null;
            image.sprite = hasHead ? unit.HeadSprite : unit.BodyVisual != null ? unit.BodyVisual.sprite : unit.Data.sprite;
            image.color = hasHead || unit.BodyVisual == null ? Color.white : unit.BodyVisual.color;
            image.enabled = image.sprite != null;
        }

        public void SetCountdown(float seconds, float duration)
        {
            float ratio = Mathf.Clamp01(seconds / Mathf.Max(0.01f, duration));
            // Both outer ends retract symmetrically toward the centre.
            progress.anchorMin = new Vector2(0.5f - ratio * 0.5f, 0f);
            progress.anchorMax = new Vector2(0.5f + ratio * 0.5f, 1f);
            progress.offsetMin = progress.offsetMax = Vector2.zero;
            progress.gameObject.SetActive(ratio > 0f);
            int hundredths = Mathf.CeilToInt(Mathf.Max(0f, seconds) * 100f);
            countdown.text = string.Format("{0:00}:{1:00}:{2:00}", hundredths / 6000, hundredths / 100 % 60, hundredths % 100);
        }

        private void Update()
        {
            if (!IsVisible) return;
            float t = Mathf.Clamp01((Time.unscaledTime - shownAt) / 0.16f);
            panel.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, Mathf.Sin(t * Mathf.PI * 0.5f));
        }

        public void Hide() { if (canvas != null) canvas.enabled = false; }
    }
}
