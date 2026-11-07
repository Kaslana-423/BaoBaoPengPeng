using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TuanziArena.Editor
{
    public static class RelayAssets
    {
        private const string SettingsPath = "Assets/_Game/Resources/RelaySettings.asset";
        public const string PrefabPath = "Assets/_Game/Resources/UI/PF_RelayUI.prefab";
        private static readonly Color Orange = new Color(1f, 0.53f, 0.10f);
        private static readonly Color Gold = new Color(1f, 0.80f, 0.32f);
        private static readonly Color Ink = new Color(0.06f, 0.10f, 0.13f, 0.97f);
        private static Font Font => AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Art/Fonts/ZCOOLKuaiLe-Regular.ttf");

        [MenuItem("Tools/Tuanzi/Create Relay Assets")]
        public static void CreateAssets()
        {
            if (!File.Exists(SettingsPath)) AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<RelaySettings>(), SettingsPath);
            if (!File.Exists(PrefabPath))
            {
                GameObject root = Build();
                try { PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); }
                finally { Object.DestroyImmediate(root); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("TUANZI_RELAY_ASSETS_READY");
        }

        private static GameObject Build()
        {
            var root = new GameObject("PF_RelayUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1000;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            RectTransform safe = Rect("SafeArea", root.transform, Vector2.zero, Vector2.zero);
            Stretch(safe); safe.gameObject.AddComponent<BattleSafeArea>();
            RectTransform panel = Rect("RelayPanel", safe, new Vector2(0f, 116f), new Vector2(1060f, 196f));
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0f);
            Panel("Backdrop", panel, new Vector2(0f, 6f), new Vector2(1050f, 170f), new Color(0.02f, 0.06f, 0.09f, 0.38f), 64f);
            Label("Title", panel, new Vector2(0f, 88f), new Vector2(640f, 30f), "接 力 时 刻  ·  C H A I N  A T T A C K", 21, Gold);
            Panel("TrackShadow", panel, new Vector2(0f, 51f), new Vector2(772f, 42f), new Color(0f, 0f, 0f, 0.45f), 21f);
            Panel("ProgressTrack", panel, new Vector2(0f, 54f), new Vector2(768f, 36f), Ink, 18f);
            RectTransform track = Rect("ProgressBounds", panel, new Vector2(0f, 54f), new Vector2(756f, 24f));
            RectTransform fill = Panel("OrangeProgress", track, Vector2.zero, Vector2.zero, Orange, 12f);
            Stretch(fill);
            RectTransform gloss = Panel("Highlight", fill, Vector2.zero, Vector2.zero, new Color(1f, 0.87f, 0.45f, 0.75f), 4f);
            gloss.anchorMin = new Vector2(0.02f, 0.65f); gloss.anchorMax = new Vector2(0.98f, 0.87f); gloss.offsetMin = gloss.offsetMax = Vector2.zero;
            Text timer = Label("Countdown", panel, new Vector2(0f, 5f), new Vector2(360f, 66f), "00:02:00", 52, Color.white);
            timer.fontStyle = FontStyle.BoldAndItalic;
            Text quota = Label("RoundQuota", panel, new Vector2(0f, -37f), new Vector2(500f, 32f), "本大轮剩余 2 次接力", 20, new Color(0.84f, 0.89f, 0.92f));
            Image left = Portrait(panel, "Left", -456f);
            Image right = Portrait(panel, "Right", 456f);
            RelayUI view = root.AddComponent<RelayUI>(); view.Configure(canvas, panel, fill, left, right, timer, quota);
            return root;
        }

        private static Image Portrait(Transform parent, string side, float x)
        {
            RectTransform root = Rect(side + "Portrait", parent, new Vector2(x, 52f), new Vector2(132f, 132f));
            Panel("OuterGlow", root, Vector2.zero, new Vector2(148f, 148f), new Color(1f, 0.53f, 0.1f, 0.13f), 74f);
            Panel("GoldRing", root, Vector2.zero, new Vector2(134f, 134f), Orange, 67f);
            Panel("InnerRing", root, Vector2.zero, new Vector2(125f, 125f), Gold, 63f);
            Panel("PortraitBackground", root, Vector2.zero, new Vector2(116f, 116f), Ink, 58f);
            RectTransform mask = Panel("CircleMask", root, Vector2.zero, new Vector2(110f, 110f), Color.white, 55f);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            RectTransform portrait = Rect("HeadSprite", mask, Vector2.zero, new Vector2(110f, 110f));
            Image image = portrait.gameObject.AddComponent<Image>(); image.preserveAspect = true; image.raycastTarget = false;
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Resources/UI/Head.png");
            RectTransform mouse = Panel("Mouse", root, new Vector2(-44f, -92f), new Vector2(23f, 34f), Ink, 11f);
            Panel("PressedButton", mouse, new Vector2(side == "Left" ? -5f : 5f, 6f), new Vector2(7f, 12f), Gold, 3f);
            Label("InputHint", root, new Vector2(14f, -93f), new Vector2(100f, 30f), side == "Left" ? "左键接力" : "右键接力", 20, Color.white);
            return image;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        private static RectTransform Panel(string name, Transform parent, Vector2 position, Vector2 size, Color color, float radius)
        {
            RectTransform rect = Rect(name, parent, position, size);
            rect.gameObject.AddComponent<BattleUIGraphic>().Configure(BattleUIGraphic.Shape.Panel, color, radius);
            return rect;
        }
        private static Text Label(string name, Transform parent, Vector2 position, Vector2 size, string text, int fontSize, Color color)
        {
            RectTransform rect = Rect(name, parent, position, size);
            Text label = rect.gameObject.AddComponent<Text>(); label.font = Font; label.text = text; label.fontSize = fontSize;
            label.color = color; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow; label.verticalOverflow = VerticalWrapMode.Overflow;
            Outline outline = rect.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(0f, 0f, 0f, 0.6f); outline.effectDistance = new Vector2(1.5f, -1.5f);
            return label;
        }
    }
}
