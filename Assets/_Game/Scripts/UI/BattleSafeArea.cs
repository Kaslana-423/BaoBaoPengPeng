using UnityEngine;

namespace TuanziArena
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class BattleSafeArea : MonoBehaviour
    {
        private Rect lastArea;
        private Vector2Int lastScreen;
        private void OnEnable() => Apply();
        private void Update()
        {
            if (lastArea != Screen.safeArea || lastScreen.x != Screen.width || lastScreen.y != Screen.height) Apply();
        }
        private void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            lastArea = Screen.safeArea;
            lastScreen = new Vector2Int(Screen.width, Screen.height);
            RectTransform rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(lastArea.xMin / Screen.width, lastArea.yMin / Screen.height);
            rect.anchorMax = new Vector2(lastArea.xMax / Screen.width, lastArea.yMax / Screen.height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
