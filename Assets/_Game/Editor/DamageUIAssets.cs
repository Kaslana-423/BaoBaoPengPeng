using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TuanziArena.Editor
{
    public static class DamageUIAssets
    {
        private const string Folder = "Assets/_Game/Resources/UI";
        [MenuItem("Tools/Tuanzi/Create Damage UI Assets")]
        public static void CreateAssets()
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            const string settingsPath = "Assets/_Game/Resources/DamageFeedbackSettings.asset";
            DamageFeedbackSettings settings = AssetDatabase.LoadAssetAtPath<DamageFeedbackSettings>(settingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<DamageFeedbackSettings>();
                settings.numberFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Art/Fonts/LilitaOne-Regular.ttf");
                settings.labelFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Art/Fonts/ZCOOLKuaiLe-Regular.ttf");
                if (settings.numberFont == null || settings.labelFont == null) throw new System.InvalidOperationException("Missing damage UI fonts.");
                AssetDatabase.CreateAsset(settings, settingsPath);
            }
            string numberPath = Folder + "/PF_DamageNumber.prefab";
            if (!File.Exists(numberPath)) Save(BuildNumber(settings), numberPath);
            DamageNumberView template = AssetDatabase.LoadAssetAtPath<GameObject>(numberPath).GetComponent<DamageNumberView>();
            string uiPath = Folder + "/PF_DamageFeedbackUI.prefab";
            if (!File.Exists(uiPath)) Save(BuildUI(settings, template), uiPath);
            AssetDatabase.SaveAssets();
            Debug.Log("TUANZI_DAMAGE_ASSETS_READY: DamageFeedbackSettings + PF_DamageNumber + PF_DamageFeedbackUI");
        }

        private static void Save(GameObject root, string path)
        {
            try { PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { Object.DestroyImmediate(root); }
        }

        private static GameObject BuildNumber(DamageFeedbackSettings settings)
        {
            RectTransform root = Rect("PF_DamageNumber", null, Vector2.zero, new Vector2(240f, 120f));
            CanvasGroup fade = root.gameObject.AddComponent<CanvasGroup>();
            fade.blocksRaycasts = false; fade.interactable = false;
            DamageNumberView view = root.gameObject.AddComponent<DamageNumberView>();
            Text text = Label("Damage", root, Vector2.zero, new Vector2(240f, 120f), "-20", settings.numberFontSize, settings.numberFont, settings.playerDamageColor);
            Outline outline = text.gameObject.AddComponent<Outline>();
            DamageNumberView.Style(text, outline, settings.numberFont, settings.numberFontSize,
                settings.playerDamageColor, settings.outlineWidth, settings.outlineColor);
            view.Configure(text, outline, fade);
            return root.gameObject;
        }

        private static GameObject BuildUI(DamageFeedbackSettings settings, DamageNumberView template)
        {
            RectTransform root = Rect("PF_DamageFeedbackUI", null, Vector2.zero, Vector2.zero);
            Canvas canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 180;
            CanvasScaler scaler = root.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            // Intentionally no GraphicRaycaster: damage UI must never intercept aiming input.
            DamageFeedbackUI ui = root.gameObject.AddComponent<DamageFeedbackUI>();
            RectTransform layer = Rect("FloatingNumbers", root, Vector2.zero, Vector2.zero); Stretch(layer);
            RectTransform safe = Rect("SafeArea", root, Vector2.zero, Vector2.zero); Stretch(safe);
            safe.gameObject.AddComponent<BattleSafeArea>();
            RectTransform total = Rect("TotalDamage", safe, settings.totalPosition, new Vector2(184f, 70f));
            total.anchorMin = total.anchorMax = new Vector2(0.5f, 1f);
            TotalDamageView view = total.gameObject.AddComponent<TotalDamageView>();
            RectTransform animated = Rect("Punch", total, Vector2.zero, new Vector2(184f, 70f));
            RectTransform rays = Rect("Burst", animated, Vector2.zero, new Vector2(310f, 138f));
            DamageBurstGraphic burst = rays.gameObject.AddComponent<DamageBurstGraphic>();
            burst.color = Color.clear; burst.raycastTarget = false;
            Graphic("Shadow", animated, new Vector2(0f, -4f), new Vector2(188f, 72f), BattleUIGraphic.Shape.Panel, new Color(0.02f, 0.06f, 0.15f, 0.3f), 18f);
            BattleUIGraphic panel = Graphic("Panel", animated, Vector2.zero, new Vector2(184f, 70f), BattleUIGraphic.Shape.Panel, settings.panelColor, 16f, 1.5f);
            RectTransform clip = Rect("ShineClip", animated, Vector2.zero, new Vector2(172f, 62f));
            clip.gameObject.AddComponent<RectMask2D>();
            RectTransform stripe = Rect("SweepingLight", clip, new Vector2(-145f, 0f), new Vector2(28f, 135f));
            stripe.localRotation = Quaternion.Euler(0f, 0f, -25f);
            Image sweep = stripe.gameObject.AddComponent<Image>();
            sweep.color = Color.clear; sweep.raycastTarget = false;
            Text title = Label("Title", animated, new Vector2(0f, 20f), new Vector2(168f, 24f), "我方 · 总伤害", 18, settings.labelFont, Color.white);
            Text number = Label("Total", animated, new Vector2(0f, -12f), new Vector2(166f, 54f), "0", settings.totalFontSize, settings.numberFont, settings.totalNumberColor);
            Outline outline = number.gameObject.AddComponent<Outline>();
            var stars = new BattleUIGraphic[4];
            Vector2[] positions = { new Vector2(-98f, 22f), new Vector2(99f, -21f), new Vector2(64f, 39f), new Vector2(-62f, -39f) };
            for (int i = 0; i < stars.Length; i++)
                stars[i] = Graphic("Sparkle_" + i, animated, positions[i], Vector2.one * (i < 2 ? 20f : 12f), BattleUIGraphic.Shape.Star, Color.clear);
            view.Configure(animated, panel, title, number, outline, burst, sweep, stars);
            view.ApplyStyle(settings);
            view.BeginAction(UnitTeam.Player);
            ui.Configure(layer, template, view);
            return root.gameObject;
        }

        private static BattleUIGraphic Graphic(string name, Transform parent, Vector2 position, Vector2 size,
            BattleUIGraphic.Shape shape, Color color, float radius = 0f, float border = 0f)
        {
            BattleUIGraphic graphic = Rect(name, parent, position, size).gameObject.AddComponent<BattleUIGraphic>();
            graphic.Configure(shape, color, radius, border);
            return graphic;
        }
        private static Text Label(string name, Transform parent, Vector2 position, Vector2 size, string content, int fontSize, Font font, Color color)
        {
            Text text = Rect(name, parent, position, size).gameObject.AddComponent<Text>();
            text.font = font; text.fontSize = fontSize; text.text = content; text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            if (parent != null) rect.SetParent(parent, false);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
