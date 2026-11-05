using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TuanziArena.Editor
{
    public static class ImpactVisualAssets
    {
        [MenuItem("Tools/Tuanzi/Create Impact Visual Assets")]
        public static void CreateAssets()
        {
            const string folder = "Assets/_Game/Resources/FX";
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            MaterialAt(folder + "/M_HitWhiteFlash.mat", "Tuanzi/Hit White Flash");
            Material spark = MaterialAt(folder + "/M_CollisionSpark.mat", "Sprites/Default");
            string path = folder + "/PF_CollisionSpark.prefab";
            if (!File.Exists(path))
            {
                GameObject root = new GameObject("PF_CollisionSpark", typeof(MeshFilter), typeof(MeshRenderer), typeof(CollisionSparkView));
                try
                {
                    MeshRenderer renderer = root.GetComponent<MeshRenderer>();
                    renderer.sharedMaterial = spark;
                    renderer.sortingOrder = 30;
                    renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { Object.DestroyImmediate(root); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("TUANZI_IMPACT_VISUAL_ASSETS_READY");
        }

        private static Material MaterialAt(string path, string shaderName)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = Shader.Find(shaderName);
            if (shader == null) throw new System.InvalidOperationException("Missing shader: " + shaderName);
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
