using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace TuanziArena.Editor
{
    public static class PrototypeSceneBuilder
    {
        private const string Root = "Assets/_Game";
        public static string LastScenePath { get; private set; }

        [MenuItem("Tools/Tuanzi/Build Prototype Scene")]
        public static void BuildPrototypeScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play mode before building a prototype scene.");
                return;
            }
            CreateDirectories();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                    throw new System.InvalidOperationException(
                        "Save untitled scenes before building a prototype. Existing scenes have been left unchanged.");
            Scene previousScene = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                Sprite circle = CreateSprite("Circle", false);
                Sprite ring = CreateSprite("SelectionRing", true);
                Material lineMaterial = new Material(Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(lineMaterial, Unique("Materials/M_Aim.mat"));
                UnitData[] configs =
                {
                    CreateUnitData("Heavy", 1.6f, 0.5f, circle),
                    CreateUnitData("Elastic", 0.9f, 0.9f, circle),
                    CreateUnitData("Support", 1.1f, 0.7f, circle)
                };

                GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0f, 0f, -10f);
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 4.6f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.055f, 0.08f, 0.14f);

                CreateArena();
                GameObject prefab = CreateUnitPrefab(configs[0], circle, ring);
                Transform unitsRoot = new GameObject("Units").transform;
                UnitActor[] units = new UnitActor[6];
                for (int team = 0; team < 2; team++)
                {
                    for (int index = 0; index < 3; index++)
                    {
                        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                        instance.transform.SetParent(unitsRoot, false);
                        instance.name = (team == 0 ? "Player_" : "Enemy_") + configs[index].displayName;
                        instance.transform.position = new Vector3(team == 0 ? -1.8f : 1.8f, 2f - index * 2f, 0f);
                        UnitActor actor = instance.GetComponent<UnitActor>();
                        actor.Configure(configs[index], (UnitTeam)team,
                            instance.transform.Find("Visual").GetComponent<SpriteRenderer>(),
                            instance.transform.Find("SelectionIndicator").GetComponent<SpriteRenderer>());
                        PrefabUtility.RecordPrefabInstancePropertyModifications(actor);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(actor.Body);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.GetComponent<CircleCollider2D>());
                        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform.Find("Visual").GetComponent<SpriteRenderer>());
                        units[team * 3 + index] = actor;
                    }
                }

                GameObject aimObject = new GameObject("AimPreview", typeof(LineRenderer), typeof(AimPreview));
                LineRenderer line = aimObject.GetComponent<LineRenderer>();
                line.sharedMaterial = lineMaterial;
                line.startWidth = 0.065f;
                line.endWidth = 0.065f;
                line.numCapVertices = 4;
                line.numCornerVertices = 4;
                line.sortingOrder = 20;
                line.enabled = false;

                Transform systems = new GameObject("GameSystems").transform;
                PrototypeSession session = Child("PrototypeSession", systems).AddComponent<PrototypeSession>();
                session.Configure(units);
                PlayerInputController input = Child("PlayerInputController", systems).AddComponent<PlayerInputController>();
                input.Configure(camera, session, aimObject.GetComponent<AimPreview>());
                // Only an input event system; phase one deliberately has no battle UI.
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

                LastScenePath = Unique("Scenes/SC_Battle.unity");
                EditorSceneManager.SaveScene(scene, LastScenePath);
                AssetDatabase.SaveAssets();
                EditorSceneManager.CloseScene(scene, true);
                Debug.Log("Tuanzi prototype created: " + LastScenePath +
                    ". Open this scene alone, then Play. Drag a teal unit backwards with the left mouse button.");
            }
            catch
            {
                EditorSceneManager.CloseScene(scene, true);
                throw;
            }
            finally
            {
                if (previousScene.IsValid() && previousScene.isLoaded)
                    SceneManager.SetActiveScene(previousScene);
            }
            // Keep all existing scenes and unsaved changes intact. Open the generated asset explicitly.
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(LastScenePath);
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        public static void BuildForBatch()
        {
            // A fresh batch project starts with an untitled scene. Save only that disposable scene.
            if (Application.isBatchMode && string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),
                    AssetDatabase.GenerateUniqueAssetPath("Assets/__TuanziBatchBootstrap.unity"));
            BuildPrototypeScene();
            PrototypeSmokeChecks.Run();
        }

        private static UnitData CreateUnitData(string label, float mass, float bounce, Sprite circle)
        {
            PhysicsMaterial2D material = new PhysicsMaterial2D("PM_" + label)
                { friction = 0f, bounciness = bounce };
            AssetDatabase.CreateAsset(material, Unique("Materials/PM_" + label + ".physicsMaterial2D"));
            UnitData data = ScriptableObject.CreateInstance<UnitData>();
            data.displayName = label;
            data.maxHealth = label == "Heavy" ? 65 : label == "Elastic" ? 45 : 50;
            data.attackPower = label == "Heavy" ? 10 : label == "Elastic" ? 8 : 6;
            data.mass = mass;
            data.linearDamping = 1.5f;
            data.physicsMaterial = material;
            data.sprite = circle;
            AssetDatabase.CreateAsset(data, Unique("ScriptableObjects/Units/UD_" + label + ".asset"));
            return data;
        }

        private static GameObject CreateUnitPrefab(UnitData data, Sprite circle, Sprite ring)
        {
            GameObject root = new GameObject("PF_DangoUnit", typeof(Rigidbody2D), typeof(CircleCollider2D));
            root.GetComponent<CircleCollider2D>().radius = 0.44f;
            GameObject visual = Child("Visual", root.transform);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = circle;
            renderer.sortingOrder = 5;
            visual.transform.localScale = Vector3.one * 0.88f;
            SpriteRenderer indicator = Child("SelectionIndicator", root.transform).AddComponent<SpriteRenderer>();
            indicator.sprite = ring;
            indicator.color = new Color(1f, 0.88f, 0.45f);
            indicator.sortingOrder = 4;
            indicator.transform.localScale = Vector3.one * 1.12f;
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject eye = Child(side < 0 ? "Eye_Left" : "Eye_Right", visual.transform);
                eye.transform.localPosition = new Vector3(side * 0.17f, 0.07f, 0f);
                eye.transform.localScale = new Vector3(0.075f, 0.12f, 1f);
                SpriteRenderer eyeRenderer = eye.AddComponent<SpriteRenderer>();
                eyeRenderer.sprite = circle;
                eyeRenderer.color = new Color(0.07f, 0.12f, 0.19f);
                eyeRenderer.sortingOrder = 6;
            }
            UnitActor actor = root.AddComponent<UnitActor>();
            actor.Configure(data, UnitTeam.Player, renderer, indicator);
            root.AddComponent<UnitCollisionReporter>();
            root.AddComponent<UnitStatsUI>().BuildView();
            string path = Unique("Prefabs/Units/PF_DangoUnit.prefab");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateArena()
        {
            Transform arena = new GameObject("Arena").transform;
            string path = Unique("Art/Arenas/PrototypeSquare.png");
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            Sprite square = ImportSprite(path, 2f);
            Rectangle("Background", arena, square, Vector2.zero, new Vector2(11f, 7f),
                new Color(0.13f, 0.21f, 0.28f), -10, false);
            Rectangle("CenterLine", arena, square, Vector2.zero, new Vector2(0.035f, 6.7f),
                new Color(0.25f, 0.37f, 0.44f), -9, false);
            // Temporary guard walls for phase one. Replace with cliff knockouts in the arena phase.
            Color wallColor = new Color(0.3f, 0.43f, 0.5f);
            Rectangle("TemporaryWall_Left", arena, square, new Vector2(-5.75f, 0f), new Vector2(0.5f, 8f), wallColor, -5, true);
            Rectangle("TemporaryWall_Right", arena, square, new Vector2(5.75f, 0f), new Vector2(0.5f, 8f), wallColor, -5, true);
            Rectangle("TemporaryWall_Top", arena, square, new Vector2(0f, 3.75f), new Vector2(11f, 0.5f), wallColor, -5, true);
            Rectangle("TemporaryWall_Bottom", arena, square, new Vector2(0f, -3.75f), new Vector2(11f, 0.5f), wallColor, -5, true);
        }

        private static void Rectangle(string label, Transform parent, Sprite sprite,
            Vector2 position, Vector2 size, Color color, int order, bool collider)
        {
            GameObject rectangle = Child(label, parent);
            rectangle.transform.localPosition = position;
            rectangle.transform.localScale = new Vector3(size.x, size.y, 1f);
            SpriteRenderer renderer = rectangle.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            if (collider) rectangle.AddComponent<BoxCollider2D>();
        }

        private static Sprite CreateSprite(string label, bool ring)
        {
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float radius = new Vector2(x + 0.5f - size / 2f, y + 0.5f - size / 2f).magnitude;
                    float alpha = Mathf.Clamp01(size / 2f - 1f - radius);
                    if (ring) alpha *= Mathf.Clamp01(radius - (size / 2f - 7f));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            texture.SetPixels(pixels);
            texture.Apply();
            string path = Unique("Art/Characters/Prototype" + label + ".png");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            return ImportSprite(path, size);
        }

        private static Sprite ImportSprite(string path, float pixelsPerUnit)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static GameObject Child(string label, Transform parent)
        {
            GameObject child = new GameObject(label);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static string Unique(string relativePath) => AssetDatabase.GenerateUniqueAssetPath(Root + "/" + relativePath);

        private static void CreateDirectories()
        {
            string[] folders = { "Art/Characters", "Art/Arenas", "Art/UI", "Audio/BGM", "Audio/SFX",
                "Materials", "Prefabs/Units", "Prefabs/Arena", "Prefabs/UI", "Prefabs/Effects", "Scenes",
                "Scripts/Core", "Scripts/Units", "Scripts/Input", "Scripts/Physics", "Scripts/Combat",
                "Scripts/AI", "Scripts/Arena", "Scripts/UI", "Scripts/Data", "ScriptableObjects/Units",
                "ScriptableObjects/Arena", "Editor", "Tests/Editor" };
            foreach (string folder in folders) Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
        }
    }
}
