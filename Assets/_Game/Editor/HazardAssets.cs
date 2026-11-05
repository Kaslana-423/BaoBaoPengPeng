using System.IO;
using UnityEditor;
using UnityEngine;

namespace TuanziArena.Editor
{
    // Generates the placeholder art, the default settings assets and the two hazard prefabs.
    // Nothing is written into a scene: drop the prefabs wherever you want them.
    public static class HazardAssets
    {
        private const string PixelPath = "Assets/_Game/Art/Hazards/HazardPixel.png";
        private const string SettingsFolder = "Assets/_Game/ScriptableObjects/Hazards";
        private const string PrefabFolder = "Assets/_Game/Prefabs/Arena";

        [MenuItem("Tools/Tuanzi/Create Hazard Assets")]
        public static void CreateAssets()
        {
            Directory.CreateDirectory("Assets/_Game/Art/Hazards");
            Directory.CreateDirectory(SettingsFolder);
            Directory.CreateDirectory(PrefabFolder);
            AssetDatabase.Refresh();

            Sprite pixel = LoadPixel();
            BoosterPadSettings pad = LoadSettings<BoosterPadSettings>(
                SettingsFolder + "/BP_BoosterPad.asset", new Color(0.22f, 0.82f, 1f, 0.32f));
            SpikeSettings spike = LoadSettings<SpikeSettings>(
                SettingsFolder + "/SP_SpikeWall.asset", new Color(0.52f, 0.12f, 0.16f, 0.42f));

            // Never overwrite prefabs that were already adjusted by hand.
            string padPath = PrefabFolder + "/PF_BoosterPad.prefab";
            if (!File.Exists(padPath)) Save(BuildBooster(pixel, pad), padPath);
            // Existing prefabs keep their GUID and their scene references; only the arrow art is rebuilt.
            else RepairBoosterArrow(padPath, pixel, pad);
            string spikePath = PrefabFolder + "/PF_SpikeWall.prefab";
            if (!File.Exists(spikePath)) Save(BuildSpike(pixel, spike), spikePath);

            AssetDatabase.SaveAssets();
            Debug.Log("TUANZI_HAZARD_ASSETS_READY: " + SettingsFolder + " + " + PrefabFolder);
        }

        private static GameObject BuildBooster(Sprite pixel, BoosterPadSettings settings)
        {
            GameObject root = new GameObject("PF_BoosterPad", typeof(BoxCollider2D));
            BoxCollider2D shape = root.GetComponent<BoxCollider2D>();
            shape.isTrigger = true;
            // Kept thick on purpose: trigger overlap is discrete, so a thin pad can be tunnelled by a
            // fast ball between two physics steps.
            shape.size = new Vector2(1.8f, 0.6f);

            GameObject body = Quad("Body", root.transform, pixel, new Vector2(1.8f, 0.6f),
                Vector2.zero, settings.tint, 1);
            BuildArrow(root.transform, pixel);

            BoosterPad booster = root.AddComponent<BoosterPad>();
            booster.Configure(settings, body.GetComponent<SpriteRenderer>());
            return root;
        }

        // A true chevron: both arms meet on the +X tip and sweep back, so the head points the way the
        // pad pushes. Defined by endpoints instead of hand-tuned Euler angles.
        private static void BuildArrow(Transform parent, Sprite pixel)
        {
            Color arrow = new Color(0.72f, 0.98f, 1f, 0.95f);
            Bar("ArrowShaft", parent, pixel, new Vector2(-0.55f, 0f), new Vector2(0.32f, 0f), 0.09f, arrow, 2);
            Bar("ArrowHeadTop", parent, pixel, new Vector2(0.36f, 0.22f), new Vector2(0.72f, 0f), 0.13f, arrow, 2);
            Bar("ArrowHeadBottom", parent, pixel, new Vector2(0.36f, -0.22f), new Vector2(0.72f, 0f), 0.13f, arrow, 2);
        }

        private static GameObject Bar(string name, Transform parent, Sprite pixel, Vector2 from, Vector2 to,
            float width, Color color, int sortingOrder)
        {
            Vector2 delta = to - from;
            GameObject bar = Quad(name, parent, pixel,
                new Vector2(Mathf.Max(0.01f, delta.magnitude), width), (from + to) * 0.5f, color, sortingOrder);
            bar.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            return bar;
        }

        // Rebuilds the arrow inside an existing prefab so its GUID, and therefore every scene instance,
        // survives the fix.
        private static void RepairBoosterArrow(string path, Sprite pixel, BoosterPadSettings settings)
        {
            if (!File.Exists(path)) return;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = root.transform.GetChild(i);
                    if (!child.name.StartsWith("Arrow")) continue;
                    Object.DestroyImmediate(child.gameObject);
                }
                BuildArrow(root.transform, pixel);
                Transform bodyTransform = root.transform.Find("Body");
                BoosterPad booster = root.GetComponent<BoosterPad>();
                if (booster != null)
                    booster.Configure(settings, bodyTransform != null ? bodyTransform.GetComponent<SpriteRenderer>() : null);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static GameObject BuildSpike(Sprite pixel, SpikeSettings settings)
        {
            GameObject root = new GameObject("PF_SpikeWall", typeof(BoxCollider2D));
            BoxCollider2D shape = root.GetComponent<BoxCollider2D>();
            shape.isTrigger = true;
            shape.size = new Vector2(1.8f, 0.5f);
            // The offset pushes the trigger onto the wall face, so the wall collision is untouched.
            shape.offset = new Vector2(0f, 0f);

            GameObject basePlate = Quad("Base", root.transform, pixel, new Vector2(1.8f, 0.5f),
                Vector2.zero, settings.tint, 1);
            Color tooth = new Color(1f, 0.5f, 0.36f, 0.95f);
            for (int i = 0; i < 9; i++)
            {
                float x = -0.72f + i * 0.18f;
                GameObject spike = Quad("Tooth_" + i, root.transform, pixel, new Vector2(0.2f, 0.2f),
                    new Vector2(x, 0.04f), tooth, 2);
                spike.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }

            SpikeZone zone = root.AddComponent<SpikeZone>();
            zone.Configure(settings, basePlate.GetComponent<SpriteRenderer>());
            return root;
        }

        private static GameObject Quad(string name, Transform parent, Sprite pixel, Vector2 size,
            Vector2 position, Color color, int sortingOrder)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = position;
            child.transform.localScale = new Vector3(size.x, size.y, 1f);
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = pixel;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return child;
        }

        private static void Save(GameObject root, string path)
        {
            try { PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { Object.DestroyImmediate(root); }
        }

        private static T LoadSettings<T>(string path, Color tint) where T : HazardSettings
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            asset.tint = tint;
            asset.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static Sprite LoadPixel()
        {
            if (!File.Exists(PixelPath))
            {
                const int size = 4;
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[size * size];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(PixelPath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(PixelPath, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(PixelPath);
            importer.textureType = TextureImporterType.Sprite;
            // 4 pixels at 4 PPU gives exactly one world unit, so a Quad scale is a world size.
            importer.spritePixelsPerUnit = 4f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(PixelPath);
        }
    }
}
