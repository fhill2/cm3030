#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Game.Core
{
    public static class EquipmentOrientationFixer
    {
        private const string WeaponsFolder = "Assets/Resources/Equipment/Weapons";
        private const string ShieldsFolder = "Assets/Resources/Equipment/Shields";

        [MenuItem("Tools/Equipment/Report Orientations")]
        public static void ReportAll()
        {
            var sb = new StringBuilder("[Orientation Report]\n");
            foreach (string path in AllPrefabs())
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    Bounds b = RendererBounds(root);
                    Vector3 thin = AxisOf(b.extents, true);
                    Vector3 lon = AxisOf(b.extents, false);
                    sb.AppendLine(string.Format(
                        "{0,-16} ext=({1:0.00},{2:0.00},{3:0.00}) thin={4} long={5}",
                        Path.GetFileNameWithoutExtension(path),
                        b.extents.x, b.extents.y, b.extents.z,
                        thin, lon));
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            Debug.Log(sb.ToString());
        }

        [MenuItem("Tools/Equipment/Fix Orientations")]
        public static void FixAll()
        {
            int count = 0;
            foreach (string path in AllPrefabs())
            {
                FixOne(path);
                count++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Orientation] Fixed {count} prefabs. Only files under Resources/Equipment were modified.");
        }

        private static void FixOne(string path)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Bounds before = RendererBounds(root);
                if (before.extents.sqrMagnitude < 1e-8f)
                {
                    Debug.LogWarning($"[Orientation] {Path.GetFileName(path)} has no renderers, skipped.");
                    return;
                }

                Quaternion corrective = CorrectiveRotation(root);

                var children = new List<Transform>();
                foreach (Transform child in root.transform) children.Add(child);

                foreach (Transform child in children)
                {
                    child.localRotation = corrective * child.localRotation;
                    child.localPosition = corrective * child.localPosition - corrective * before.center;
                }

                Bounds after = RendererBounds(root);
                var collider = root.GetComponent<BoxCollider>();
                if (collider != null)
                {
                    collider.center = after.center;
                    collider.size = after.size;
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [MenuItem("Tools/Equipment/Flip Vertical (Selection)")]
        public static void FlipVerticalSelection()
        {
            var paths = new List<string>();
            foreach (Object o in Selection.objects)
            {
                string p = AssetDatabase.GetAssetPath(o);
                if (!string.IsNullOrEmpty(p) && p.EndsWith(".prefab") &&
                    (p.StartsWith(WeaponsFolder) || p.StartsWith(ShieldsFolder)))
                    paths.Add(p);
            }
            FlipPrefabs(paths);
        }

        public static void FlipPrefabs(IEnumerable<string> paths)
        {
            int count = 0;
            foreach (string path in paths)
            {
                FlipOne(path);
                count++;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Orientation] Flipped {count} prefabs vertically.");
        }

        private static void FlipOne(string path)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Quaternion flip = Quaternion.AngleAxis(180f, Vector3.forward);

                var children = new List<Transform>();
                foreach (Transform child in root.transform) children.Add(child);

                foreach (Transform child in children)
                {
                    child.localRotation = flip * child.localRotation;
                    child.localPosition = flip * child.localPosition;
                }

                Bounds after = RendererBounds(root);
                foreach (Transform child in children)
                    child.localPosition -= after.center;

                after = RendererBounds(root);
                var collider = root.GetComponent<BoxCollider>();
                if (collider != null)
                {
                    collider.center = after.center;
                    collider.size = after.size;
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Quaternion CorrectiveRotation(GameObject root)
        {
            Bounds b = RendererBounds(root);
            Vector3 thin = AxisOf(b.extents, true);
            Vector3 lon = AxisOf(b.extents, false);
            return Quaternion.Inverse(Quaternion.LookRotation(thin, lon));
        }

        private static Vector3 AxisOf(Vector3 extents, bool smallest)
        {
            float x = Mathf.Abs(extents.x);
            float y = Mathf.Abs(extents.y);
            float z = Mathf.Abs(extents.z);

            if (smallest)
            {
                if (x <= y && x <= z) return Vector3.right;
                if (y <= z) return Vector3.up;
                return Vector3.forward;
            }

            if (x >= y && x >= z) return Vector3.right;
            if (y >= z) return Vector3.up;
            return Vector3.forward;
        }

        private static Bounds RendererBounds(GameObject root)
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool any = false;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
            {
                if (!any)
                {
                    bounds = r.bounds;
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }
            return bounds;
        }




        private static IEnumerable<string> AllPrefabs()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { WeaponsFolder, ShieldsFolder }))
                yield return AssetDatabase.GUIDToAssetPath(guid);
        }
    }
}
#endif
