using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TuanziArena.Editor
{
    public static class AimPreviewChecks
    {
        [MenuItem("Tools/Tuanzi/Validate Aim Preview")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before validating aim preview.");
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                CircleCollider2D source = Circle(scene, "Source", Vector2.zero);
                AimTrajectory trajectory = new AimTrajectory();
                Physics2D.SyncTransforms();
                AimPath clear = trajectory.Trace(source, Vector2.right, 6f);
                Check(!clear.HasBounce && Near(clear.End.x, 6f) && Near(clear.Length, 6f),
                    "An empty path must ignore the source collider and use the full length budget.");

                GameObject rightWall = Box(scene, "RightWall", new Vector2(3f, 0f), new Vector2(0.5f, 10f));
                Physics2D.SyncTransforms();
                AimPath limited = trajectory.Trace(source, Vector2.right, 1f);
                Check(!limited.HasBounce && Near(limited.End.x, 1f), "Obstacles beyond the limit must not reflect.");
                AimPath bounce = trajectory.Trace(source, Vector2.right, 6f);
                Check(bounce.HasBounce && !bounce.HitSecondObstacle && Near(bounce.Bounce.x, 2.25f) &&
                    Near(bounce.End.x, -1.5f) && Near(bounce.Length, 6f),
                    "Wall reflection must include character radius and share the total length budget.");

                GameObject leftWall = Box(scene, "LeftWall", new Vector2(-2f, 0f), new Vector2(0.5f, 10f));
                Physics2D.SyncTransforms();
                AimPath twice = trajectory.Trace(source, Vector2.right, 10f);
                Check(twice.HasBounce && twice.HitSecondObstacle && Near(twice.End.x, -1.25f) &&
                    twice.Length <= 10.001f, "The outgoing leg must stop at the second obstacle without another bounce.");
                UnityEngine.Object.DestroyImmediate(leftWall);
                UnityEngine.Object.DestroyImmediate(rightWall);

                CircleCollider2D target = Circle(scene, "UnitTarget", new Vector2(3f, 0.8f));
                Physics2D.SyncTransforms();
                AimPath curved = trajectory.Trace(source, Vector2.right, 6f);
                Check(curved.HasBounce && Near(curved.Bounce.x, 2.4f) && curved.End.y < -0.5f &&
                    curved.Length <= 6.001f, "A glancing circular hit must reflect even when the centre ray would miss.");
                target.isTrigger = true;
                Physics2D.SyncTransforms();
                Check(!trajectory.Trace(source, Vector2.right, 6f).HasBounce, "Triggers must not block the guide.");
                target.isTrigger = false;
                Check(!trajectory.Trace(source, Vector2.right, 6f, 0).HasBounce, "Obstacle layer filters must be respected.");
                Physics2D.IgnoreCollision(source, target, true);
                Check(!trajectory.Trace(source, Vector2.right, 6f).HasBounce, "Ignored collider pairs must not block the guide.");
                UnityEngine.Object.DestroyImmediate(target.gameObject);

                Box(scene, "TouchingWall", new Vector2(1.25f, 0f), new Vector2(1.5f, 10f));
                Physics2D.SyncTransforms();
                AimPath leaving = trajectory.Trace(source, Vector2.left, 3f);
                Check(!leaving.HasBounce && Near(leaving.End.x, -3f), "A guide moving away from a touching wall must stay clear.");
                AimPath entering = trajectory.Trace(source, Vector2.right, 3f);
                Check(entering.HasBounce && entering.End.x < -2.9f, "A touching wall must produce one outgoing reflection.");
                Debug.Log("TUANZI_AIM_CHECKS_PASSED: length cap, self exclusion, walls, one bounce, second-obstacle stop, circular glancing hits, triggers, masks, ignored pairs, touching surfaces.");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        private static CircleCollider2D Circle(Scene scene, string name, Vector2 position)
        {
            GameObject obj = new GameObject(name);
            SceneManager.MoveGameObjectToScene(obj, scene);
            obj.transform.position = position;
            CircleCollider2D collider = obj.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
            return collider;
        }

        private static GameObject Box(Scene scene, string name, Vector2 position, Vector2 size)
        {
            GameObject obj = new GameObject(name);
            SceneManager.MoveGameObjectToScene(obj, scene);
            obj.transform.position = position;
            obj.AddComponent<BoxCollider2D>().size = size;
            return obj;
        }

        private static bool Near(float value, float expected) => Mathf.Abs(value - expected) < 0.03f;
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Aim preview validation: " + message);
        }
    }
}
