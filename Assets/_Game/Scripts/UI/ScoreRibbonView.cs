using UnityEngine;
using UnityEngine.UI;

namespace TuanziArena
{
    public sealed class ScoreRibbonView : MonoBehaviour
    {
        [SerializeField] private Text playerValue, enemyValue;
        [SerializeField] private BattleUIGraphic[] playerMarkers, enemyMarkers;
        [SerializeField] private Color playerColor = new Color(0.12f, 0.64f, 0.94f);
        [SerializeField] private Color enemyColor = new Color(0.96f, 0.28f, 0.43f);
        public void Configure(Text player, Text enemy, BattleUIGraphic[] blue, BattleUIGraphic[] red)
        {
            playerValue = player; enemyValue = enemy; playerMarkers = blue; enemyMarkers = red;
            SetScore(0, 0);
        }

        public void SetScore(int player, int enemy)
        {
            playerValue.text = player.ToString();
            enemyValue.text = enemy.ToString();
            for (int i = 0; i < playerMarkers.Length; i++)
                playerMarkers[i].color = i < player ? playerColor : new Color(0.78f, 0.88f, 0.93f);
            for (int i = 0; i < enemyMarkers.Length; i++)
                enemyMarkers[i].color = i < enemy ? enemyColor : new Color(0.95f, 0.82f, 0.85f);
        }
    }
}
