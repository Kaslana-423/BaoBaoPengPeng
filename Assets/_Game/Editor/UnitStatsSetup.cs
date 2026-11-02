using UnityEditor;
using UnityEngine;

namespace TuanziArena.Editor
{
    public static class UnitStatsSetup
    {
        [MenuItem("Tools/Tuanzi/Upgrade Unit Stats Prefab")]
        public static void UpgradePrefab()
        {
            const string path = "Assets/_Game/Prefabs/Units/PF_DangoUnit.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.GetComponent<UnitCollisionReporter>() == null) root.AddComponent<UnitCollisionReporter>();
                UnitStatsUI ui = root.GetComponent<UnitStatsUI>();
                if (ui == null) ui = root.AddComponent<UnitStatsUI>();
                ui.BuildView();
                foreach (StatIconGraphic graphic in root.GetComponentsInChildren<StatIconGraphic>(true))
                    if (graphic.GetComponent<CanvasRenderer>() == null)
                        graphic.gameObject.AddComponent<CanvasRenderer>();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log("Unit stats prefab upgraded: attack/health badges and collision reporting.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
