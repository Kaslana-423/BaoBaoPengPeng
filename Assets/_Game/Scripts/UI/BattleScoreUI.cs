using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TuanziArena
{
    public sealed class BattleScoreUI : MonoBehaviour
    {
        [Header("View references")]
        [SerializeField] private ScoreRibbonView hud, popupRibbon;
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private RectTransform popupPanel;
        [SerializeField] private CanvasGroup popupFade;
        [SerializeField] private Text headline, playerGain, enemyGain;
        [Header("Unscaled animation seconds")]
        [SerializeField, Min(0.05f)] private float enterSeconds = 0.32f;
        [SerializeField, Min(0.1f)] private float holdSeconds = 1.05f;
        [SerializeField, Min(0.05f)] private float exitSeconds = 0.28f;
        public bool IsPopupVisible => popupRoot != null && popupRoot.activeSelf;

        public void Configure(ScoreRibbonView persistent, ScoreRibbonView popup, GameObject overlay,
            RectTransform panel, CanvasGroup fade, Text title, Text blueGain, Text redGain)
        {
            hud = persistent; popupRibbon = popup; popupRoot = overlay; popupPanel = panel;
            popupFade = fade; headline = title; playerGain = blueGain; enemyGain = redGain;
            ResetView();
        }

        public void SetHUD(int player, int enemy) => hud.SetScore(player, enemy);

        public void ResetView()
        {
            SetHUD(0, 0);
            popupRoot.SetActive(false);
        }

        public IEnumerator PlayScore(int previousPlayer, int previousEnemy, int player, int enemy)
        {
            int gainP = player - previousPlayer, gainE = enemy - previousEnemy;
            headline.text = gainP > 0 ? (gainP > 1 ? "连续击破！" : "我方得分！") : (gainE > 1 ? "敌方连续得分" : "敌方得分");
            playerGain.text = gainP > 0 ? "+" + gainP : "";
            enemyGain.text = gainE > 0 ? "+" + gainE : "";
            popupRibbon.SetScore(previousPlayer, previousEnemy);
            popupPanel.anchoredPosition = new Vector2(0f, -600f);
            popupPanel.localScale = Vector3.one * 0.9f;
            popupFade.alpha = 0f;
            popupRoot.SetActive(true);
            float duration = Mathf.Max(0.05f, enterSeconds);
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float p = Mathf.Clamp01(t / duration);
                float back = EaseOutBack(p);
                popupPanel.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(-600f, 0f, back));
                popupPanel.localScale = Vector3.one * Mathf.LerpUnclamped(0.9f, 1f, back);
                popupFade.alpha = Mathf.Clamp01(p * 3f);
                yield return null;
            }
            popupPanel.anchoredPosition = Vector2.zero;
            popupPanel.localScale = Vector3.one;
            popupFade.alpha = 1f;
            popupRibbon.SetScore(player, enemy);
            SetHUD(player, enemy);
            duration = Mathf.Max(0.1f, holdSeconds);
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                // A brief score punch after the panel lands.
                float punch = Mathf.Sin(Mathf.Clamp01(t / 0.26f) * Mathf.PI) * 0.065f;
                popupRibbon.transform.localScale = Vector3.one * (1f + punch);
                yield return null;
            }
            popupRibbon.transform.localScale = Vector3.one;
            duration = Mathf.Max(0.05f, exitSeconds);
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float p = Mathf.Clamp01(t / duration);
                popupPanel.anchoredPosition = new Vector2(0f, -600f * p * p);
                popupFade.alpha = 1f - p;
                yield return null;
            }
            popupRoot.SetActive(false);
        }

        public static float EaseOutBack(float p)
        {
            float q = p - 1f;
            return 1f + 2.70158f * q * q * q + 1.70158f * q * q;
        }
    }
}
