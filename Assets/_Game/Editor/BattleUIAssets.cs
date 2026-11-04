using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TuanziArena.Editor
{
    public static class BattleUIAssets
    {
        public const string Folder = "Assets/_Game/Resources/UI";
        private static readonly Color Blue = new Color(0.12f, 0.64f, 0.94f);
        private static readonly Color Red = new Color(0.96f, 0.28f, 0.43f);
        private static readonly Color Ink = new Color(0.12f, 0.24f, 0.36f);
        private static Font Font => AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Art/Fonts/ZCOOLKuaiLe-Regular.ttf");

        [MenuItem("Tools/Tuanzi/Create Score UI Assets")]
        public static void CreateAssets()
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            if (Font == null) throw new System.InvalidOperationException("Missing ZCOOLKuaiLe-Regular.ttf.");
            // Never overwrite an artist's edited prefab when the menu is run again.
            if (!File.Exists(Folder + "/PF_BattleScoreUI.prefab")) Save(BuildScore(), "PF_BattleScoreUI");
            if (!File.Exists(Folder + "/PF_BattleResultUI.prefab")) Save(BuildResult(), "PF_BattleResultUI");
            AssetDatabase.SaveAssets();
            Debug.Log("TUANZI_SCORE_ASSETS_READY: " + Folder);
        }

        private static void Save(GameObject root, string name)
        {
            try { PrefabUtility.SaveAsPrefabAsset(root, Folder + "/" + name + ".prefab"); }
            finally { Object.DestroyImmediate(root); }
        }

        private static GameObject CanvasRoot(string name, int order)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            return root;
        }

        private static GameObject BuildScore()
        {
            GameObject root = CanvasRoot("PF_BattleScoreUI", 200);
            BattleScoreUI ui = root.AddComponent<BattleScoreUI>();
            RectTransform safe = Rect("SafeArea", root.transform, Vector2.zero, Vector2.zero);
            Stretch(safe);
            safe.gameObject.AddComponent<BattleSafeArea>();
            RectTransform hudRect = Rect("HUD", safe, new Vector2(0f, -82f), new Vector2(1040f, 260f));
            hudRect.anchorMin = hudRect.anchorMax = new Vector2(0.5f, 1f);
            hudRect.localScale = Vector3.one * 0.48f;
            ScoreRibbonView hud = Ribbon(hudRect);
            Label("Goal", hudRect, new Vector2(0f, -95f), new Vector2(450f, 40f), "率先获得六分获胜", 24, Color.white, true);

            RectTransform overlay = Overlay("ScorePopup", root.transform, 0.52f);
            CanvasGroup fade = overlay.gameObject.AddComponent<CanvasGroup>();
            RectTransform panel = Rect("AnimatedPanel", overlay, Vector2.zero, new Vector2(1100f, 400f));
            Text title = Label("Headline", panel, new Vector2(0f, 190f), new Vector2(800f, 70f), "我方得分！", 48, Color.white, true);
            Label("Target", panel, new Vector2(0f, -158f), new Vector2(700f, 40f), "率先获得六分获胜", 26, new Color(0.85f, 0.93f, 1f));
            RectTransform ribbon = Rect("ScoreRibbon", panel, Vector2.zero, new Vector2(1040f, 260f));
            ScoreRibbonView popup = Ribbon(ribbon);
            Text gainP = Label("PlayerGain", panel, new Vector2(-340f, -100f), new Vector2(240f, 65f), "+1", 52, Blue, true);
            Text gainE = Label("EnemyGain", panel, new Vector2(340f, -100f), new Vector2(240f, 65f), "", 52, Red, true);
            Digits(gainP); Digits(gainE);
            ui.Configure(hud, popup, overlay.gameObject, panel, fade, title, gainP, gainE);
            return root;
        }

        private static ScoreRibbonView Ribbon(RectTransform parent)
        {
            Graphic("Shadow", parent, new Vector2(0f, -8f), new Vector2(1040f, 120f), BattleUIGraphic.Shape.Panel, new Color(0.04f, 0.14f, 0.25f, 0.3f));
            Graphic("BlueRibbon", parent, new Vector2(-260f, 0f), new Vector2(520f, 120f), BattleUIGraphic.Shape.LeftRibbon, new Color(0.95f, 0.99f, 1f));
            Graphic("RedRibbon", parent, new Vector2(260f, 0f), new Vector2(520f, 120f), BattleUIGraphic.Shape.RightRibbon, new Color(1f, 0.97f, 0.97f));
            Graphic("BlueEdge", parent, new Vector2(-268f, -49f), new Vector2(504f, 16f), BattleUIGraphic.Shape.LeftRibbon, new Color(0.64f, 0.86f, 0.98f));
            Graphic("RedEdge", parent, new Vector2(268f, -49f), new Vector2(504f, 16f), BattleUIGraphic.Shape.RightRibbon, new Color(1f, 0.73f, 0.79f));
            Label("PlayerLabel", parent, new Vector2(-355f, 39f), new Vector2(240f, 32f), "我方", 25, Blue);
            Label("EnemyLabel", parent, new Vector2(355f, 39f), new Vector2(240f, 32f), "敌方", 25, Red);
            var blue = new BattleUIGraphic[BattleScoreController.WinningScore];
            var red = new BattleUIGraphic[BattleScoreController.WinningScore];
            for (int i = 0; i < blue.Length; i++)
            {
                blue[i] = Graphic("PlayerPoint_" + (i + 1), parent, new Vector2(-466f + i * 44f, -8f), new Vector2(36f, 38f), BattleUIGraphic.Shape.Dango, Blue);
                red[i] = Graphic("EnemyPoint_" + (i + 1), parent, new Vector2(466f - i * 44f, -8f), new Vector2(36f, 38f), BattleUIGraphic.Shape.Dango, Red);
            }
            Text p = Label("PlayerScore", parent, new Vector2(-100f, 60f), new Vector2(190f, 216f), "0", 178, Blue, true);
            Text e = Label("EnemyScore", parent, new Vector2(100f, 60f), new Vector2(190f, 216f), "0", 178, Red, true);
            Digits(p); Digits(e);
            Digits(Label("Dash", parent, new Vector2(0f, 64f), new Vector2(90f, 100f), "-", 92, Color.white, true));
            Graphic("CentreStar", parent, new Vector2(0f, 8f), new Vector2(24f, 24f), BattleUIGraphic.Shape.Star, new Color(1f, 0.82f, 0.25f));
            ScoreRibbonView view = parent.gameObject.AddComponent<ScoreRibbonView>();
            view.Configure(p, e, blue, red);
            return view;
        }

        private static GameObject BuildResult()
        {
            GameObject root = CanvasRoot("PF_BattleResultUI", 300);
            BattleResultUI ui = root.AddComponent<BattleResultUI>();
            RectTransform overlay = Overlay("ResultOverlay", root.transform, 0.72f);
            CanvasGroup fade = overlay.gameObject.AddComponent<CanvasGroup>();
            RectTransform card = Rect("ResultCard", overlay, Vector2.zero, new Vector2(690f, 570f));
            Graphic("Shadow", card, new Vector2(0f, -12f), new Vector2(700f, 580f), BattleUIGraphic.Shape.Panel, new Color(0.02f, 0.09f, 0.19f, 0.4f), 38f);
            Graphic("Card", card, Vector2.zero, new Vector2(690f, 570f), BattleUIGraphic.Shape.Panel, new Color(0.96f, 0.98f, 1f), 32f, 5f);
            BattleUIGraphic accent = Graphic("Accent", card, new Vector2(0f, 205f), new Vector2(630f, 100f), BattleUIGraphic.Shape.Panel, Blue, 24f);
            Graphic("EmblemBack", card, new Vector2(0f, 256f), new Vector2(110f, 110f), BattleUIGraphic.Shape.Panel, new Color(1f, 0.97f, 0.85f), 55f, 5f);
            BattleUIGraphic emblem = Graphic("Star", card, new Vector2(0f, 256f), new Vector2(80f, 80f), BattleUIGraphic.Shape.Star, new Color(1f, 0.76f, 0.23f), 0f, 3f);
            Label("MatchComplete", card, new Vector2(0f, 193f), new Vector2(400f, 50f), "对局结束", 28, Color.white);
            Text title = Label("Title", card, new Vector2(0f, 105f), new Vector2(500f, 100f), "胜利！", 80, new Color(0.10f, 0.48f, 0.76f));
            Text subtitle = Label("Subtitle", card, new Vector2(0f, 36f), new Vector2(620f, 42f), "漂亮的碰撞！我方率先拿下六分", 25, Ink);
            Graphic("ScoreInset", card, new Vector2(0f, -70f), new Vector2(550f, 150f), BattleUIGraphic.Shape.Panel, new Color(0.89f, 0.94f, 0.98f), 22f);
            Label("PlayerLabel", card, new Vector2(-170f, -30f), new Vector2(160f, 35f), "我方", 25, Blue);
            Label("EnemyLabel", card, new Vector2(170f, -30f), new Vector2(160f, 35f), "敌方", 25, Red);
            Text p = Label("PlayerScore", card, new Vector2(-170f, -89f), new Vector2(170f, 110f), "6", 86, Blue);
            Text e = Label("EnemyScore", card, new Vector2(170f, -89f), new Vector2(170f, 110f), "2", 86, Red);
            Digits(p); Digits(e);
            Digits(Label("Dash", card, new Vector2(0f, -80f), new Vector2(100f, 100f), "-", 64, Ink));
            BattleUIGraphic buttonGraphic = Graphic("RestartButton", card, new Vector2(0f, -205f), new Vector2(330f, 72f), BattleUIGraphic.Shape.Panel, Blue, 36f);
            buttonGraphic.raycastTarget = true;
            Button button = buttonGraphic.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonGraphic;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.9f, 0.97f, 1f);
            colors.pressedColor = new Color(0.7f, 0.85f, 0.96f);
            button.colors = colors;
            Label("Label", button.transform, Vector2.zero, new Vector2(300f, 60f), "再来一局", 33, Color.white);
            ui.Configure(overlay.gameObject, card, fade, title, subtitle, p, e, accent, emblem, button);
            return root;
        }

        private static RectTransform Overlay(string name, Transform parent, float alpha)
        {
            RectTransform rect = Rect(name, parent, Vector2.zero, Vector2.zero);
            Stretch(rect);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.025f, 0.075f, 0.15f, alpha);
            image.raycastTarget = true;
            return rect;
        }

        private static BattleUIGraphic Graphic(string name, Transform parent, Vector2 position, Vector2 size,
            BattleUIGraphic.Shape shape, Color color, float radius = 20f, float border = 0f)
        {
            BattleUIGraphic graphic = Rect(name, parent, position, size).gameObject.AddComponent<BattleUIGraphic>();
            graphic.Configure(shape, color, radius, border);
            return graphic;
        }

        private static Text Label(string name, Transform parent, Vector2 position, Vector2 size,
            string value, int fontSize, Color color, bool outline = false)
        {
            Text text = Rect(name, parent, position, size).gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = fontSize;
            text.text = value;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            if (outline)
            {
                Outline edge = text.gameObject.AddComponent<Outline>();
                edge.effectColor = fontSize > 100 ? Color.white : new Color(0.06f, 0.18f, 0.3f, 0.5f);
                edge.effectDistance = Vector2.one * (fontSize > 100 ? 5f : 2f);
            }
            return text;
        }

        private static void Digits(Text text)
        {
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
