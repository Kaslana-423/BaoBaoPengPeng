using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TuanziArena
{
    // Standalone prefab. All visual children and references are editable in Prefab Mode.
    public sealed class BattleResultUI : MonoBehaviour
    {
        [SerializeField] private GameObject overlay;
        [SerializeField] private RectTransform card;
        [SerializeField] private CanvasGroup fade;
        [SerializeField] private Text title, subtitle, playerValue, enemyValue;
        [SerializeField] private BattleUIGraphic accent, emblem;
        [SerializeField] private Button restartButton;
        [SerializeField, Min(0.05f)] private float enterSeconds = 0.42f;
        private Action restart;
        public bool IsVisible => overlay != null && overlay.activeSelf;
        public bool PlayerWon { get; private set; }
        public Button RestartButton => restartButton;

        public void Configure(GameObject root, RectTransform panel, CanvasGroup group, Text heading, Text description,
            Text blueScore, Text redScore, BattleUIGraphic stripe, BattleUIGraphic icon, Button button)
        {
            overlay = root; card = panel; fade = group; title = heading; subtitle = description;
            playerValue = blueScore; enemyValue = redScore; accent = stripe; emblem = icon; restartButton = button;
        }

        public void BindRestart(Action callback)
        {
            restart = callback;
            restartButton.onClick.RemoveListener(Restart);
            restartButton.onClick.AddListener(Restart);
        }

        private void Restart() => restart?.Invoke();

        public void Hide()
        {
            StopAllCoroutines();
            overlay.SetActive(false);
        }

        public void Show(bool playerWon, int player, int enemy)
        {
            StopAllCoroutines();
            PlayerWon = playerWon;
            title.text = playerWon ? "胜利！" : "失败";
            subtitle.text = playerWon ? "漂亮的碰撞！我方率先拿下六分" : "敌方率先拿下六分，下局再战！";
            title.color = playerWon ? new Color(0.10f, 0.48f, 0.76f) : new Color(0.77f, 0.19f, 0.32f);
            accent.color = playerWon ? new Color(0.18f, 0.66f, 0.94f) : new Color(0.95f, 0.33f, 0.45f);
            emblem.color = playerWon ? new Color(1f, 0.76f, 0.23f) : new Color(0.67f, 0.74f, 0.84f);
            playerValue.text = player.ToString(); enemyValue.text = enemy.ToString();
            card.anchoredPosition = new Vector2(0f, -100f);
            card.localScale = Vector3.one * 0.7f;
            fade.alpha = 0f;
            restartButton.interactable = false;
            overlay.SetActive(true);
            StartCoroutine(AnimateIn());
        }

        private IEnumerator AnimateIn()
        {
            float duration = Mathf.Max(0.05f, enterSeconds);
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float p = Mathf.Clamp01(t / duration), back = BattleScoreUI.EaseOutBack(p);
                card.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(-100f, 0f, back));
                card.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, back);
                fade.alpha = Mathf.Clamp01(p * 2f);
                yield return null;
            }
            card.anchoredPosition = Vector2.zero;
            card.localScale = Vector3.one;
            fade.alpha = 1f;
            restartButton.interactable = true;
        }
    }
}
